﻿using Lidgren.Network;
using SS14.Server.Interfaces;
using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Network;
using SS14.Server.Interfaces.Placement;
using SS14.Server.Interfaces.Player;
using SS14.Server.Services.Log;
using SS14.Server.Services.Map;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Maths;

namespace SS14.Server.Services.Placement
{
    public class PlacementManager : IPlacementManager
    {
        //TO-DO: Expand for multiple permission per mob?
        //       Add support for multi-use placeables (tiles etc.).
        public List<PlacementInformation> BuildPermissions = new List<PlacementInformation>();
        //Holds build permissions for all mobs. A list of mobs and the objects they're allowed to request and how. One permission per mob.

        private ISS14Server _server;

        #region IPlacementManager Members

        public void Initialize(ISS14Server server)
        {
            _server = server;
        }

        /// <summary>
        ///  Handles placement related client messages.
        /// </summary>
        public void HandleNetMessage(NetIncomingMessage msg)
        {
            var messageType = (PlacementManagerMessage) msg.ReadByte();

            switch (messageType)
            {
                case PlacementManagerMessage.StartPlacement:
                    break;
                case PlacementManagerMessage.CancelPlacement:
                    break;
                case PlacementManagerMessage.RequestPlacement:
                    HandlePlacementRequest(msg);
                    break;
            }
        }

        public void HandlePlacementRequest(NetIncomingMessage msg)
        {
            string alignRcv = msg.ReadString();

            Boolean isTile = msg.ReadBoolean();

            var mapMgr = (MapManager)IoCManager.Resolve<IMapManager>();

            ushort tileType = 0;

            string entityTemplateName = "";

            if (isTile) tileType = msg.ReadUInt16();
            else entityTemplateName = msg.ReadString();

            float xRcv = msg.ReadFloat();
            float yRcv = msg.ReadFloat();
            var dirRcv = (Direction) msg.ReadByte();

            IPlayerSession session = IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection);
            if (session.attachedEntity == null)
                return; //Don't accept placement requests from nobodys

            PlacementInformation permission = GetPermission(session.attachedEntity.Uid, alignRcv);
            Boolean isAdmin =
                IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).adminPermissions.
                    isAdmin;

            float a = (float)Math.Floor(xRcv);
            float b = (float)Math.Floor(yRcv);
            Vector2 tilePos = new Vector2(a, b);

            if (permission != null || true)
                //isAdmin) Temporarily disable actual permission check / admin check. REENABLE LATER
            {
                if (permission != null)
                {
                    if (permission.Uses > 0)
                    {
                        permission.Uses--;
                        if (permission.Uses <= 0)
                        {
                            BuildPermissions.Remove(permission);
                            SendPlacementCancel(session.attachedEntity);
                        }
                    }
                    else
                    {
                        BuildPermissions.Remove(permission);
                        SendPlacementCancel(session.attachedEntity);
                        return;
                    }
                }

                if (!isTile)
                {
                    Entity created = _server.EntityManager.SpawnEntityAt(entityTemplateName, new Vector2(xRcv, yRcv));
                    if (created != null)
                    {
                        created.GetComponent<ITransformComponent>(ComponentFamily.Transform).TranslateTo(
                            new Vector2(xRcv, yRcv));
                        if(created.HasComponent(ComponentFamily.Direction))
                            created.GetComponent<IDirectionComponent>(ComponentFamily.Direction).Direction = dirRcv;
                        if(created.HasComponent(ComponentFamily.WallMounted))
                            created.GetComponent<IWallMountedComponent>(ComponentFamily.WallMounted).AttachToTile(mapMgr.GetTileRef(tilePos));
                    }
                }
                else
                {
                    mapMgr.Tiles[tilePos] = new Tile(tileType);
                }
            }
            else //They are not allowed to request this. Send 'PlacementFailed'. TBA
            {
                LogManager.Log("Invalid placement request: "
                               + IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).name +
                               " - " +
                               IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).
                                   attachedEntity.Uid.ToString() +
                               " - " + alignRcv.ToString());

                SendPlacementCancel(
                    IoCManager.Resolve<IPlayerManager>().GetSessionByConnection(msg.SenderConnection).attachedEntity);
            }
        }

        /// <summary>
        ///  Places mob in entity placement mode with given settings.
        /// </summary>
        public void SendPlacementBegin(Entity mob, int range, string objectType, string alignOption)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte) NetMessage.PlacementManagerMessage);
            message.Write((byte) PlacementManagerMessage.StartPlacement);
            message.Write(range);
            message.Write(false); //Not a tile
            message.Write(objectType);
            message.Write(alignOption);

            if(mob.HasComponent(ComponentFamily.Actor))
            {
                var playerConnection = mob.GetComponent<IActorComponent>(ComponentFamily.Actor).GetPlayerSession().ConnectedClient;
                if(playerConnection != null)
                {
                    IoCManager.Resolve<ISS14NetServer>().SendMessage(message, playerConnection,
                                                                     NetDeliveryMethod.ReliableOrdered);
                }
            }
        }

        /// <summary>
        ///  Places mob in tile placement mode with given settings.
        /// </summary>
        public void SendPlacementBeginTile(Entity mob, int range, string tileType, string alignOption)
        {
            var mapMgr = (MapManager) IoCManager.Resolve<IMapManager>();
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte) NetMessage.PlacementManagerMessage);
            message.Write((byte) PlacementManagerMessage.StartPlacement);
            message.Write(range);
            message.Write(true); //Is a tile.
            message.Write(tileType);
            message.Write(alignOption);
            if (mob.HasComponent(ComponentFamily.Actor))
            {
                var playerConnection = mob.GetComponent<IActorComponent>(ComponentFamily.Actor).GetPlayerSession().ConnectedClient;
                if (playerConnection != null)
                {
                    IoCManager.Resolve<ISS14NetServer>().SendMessage(message, playerConnection,
                                                                     NetDeliveryMethod.ReliableOrdered);
                }
            }
        }

        /// <summary>
        ///  Cancels object placement mode for given mob.
        /// </summary>
        public void SendPlacementCancel(Entity mob)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte) NetMessage.PlacementManagerMessage);
            message.Write((byte) PlacementManagerMessage.CancelPlacement);

            if (mob.HasComponent(ComponentFamily.Actor))
            {
                var playerConnection = mob.GetComponent<IActorComponent>(ComponentFamily.Actor).GetPlayerSession().ConnectedClient;
                if (playerConnection != null)
                {
                    IoCManager.Resolve<ISS14NetServer>().SendMessage(message, playerConnection,
                                                                     NetDeliveryMethod.ReliableOrdered);
                }
            }
        }

        /// <summary>
        ///  Gives Mob permission to place entity and places it in object placement mode.
        /// </summary>
        public void StartBuilding(Entity mob, int range, string objectType, string alignOption)
        {
            AssignBuildPermission(mob, range, objectType, alignOption);
            SendPlacementBegin(mob, range, objectType, alignOption);
        }

        /// <summary>
        ///  Gives Mob permission to place tile and places it in object placement mode.
        /// </summary>
        public void StartBuildingTile(Entity mob, int range, string tileType, string alignOption)
        {
            AssignBuildPermission(mob, range, tileType, alignOption);
            SendPlacementBeginTile(mob, range, tileType, alignOption);
        }

        /// <summary>
        ///  Revokes open placement Permission and cancels object placement mode.
        /// </summary>
        public void CancelBuilding(Entity mob)
        {
            RevokeAllBuildPermissions(mob);
            SendPlacementCancel(mob);
        }

        /// <summary>
        ///  Gives a mob a permission to place a given Entity.
        /// </summary>
        public void AssignBuildPermission(Entity mob, int range, string objectType, string alignOption)
        {
            var newPermission = new PlacementInformation
                                    {
                                        MobUid = mob.Uid,
                                        Range = range,
                                        IsTile = false,
                                        EntityType = objectType,
                                        PlacementOption = alignOption
                                    };

            IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                               where permission.MobUid == mob.Uid
                                                               select permission;

            if (mobPermissions.Any()) //Already has one? Revoke the old one and add this one.
            {
                RevokeAllBuildPermissions(mob);
                BuildPermissions.Add(newPermission);
            }
            else
            {
                BuildPermissions.Add(newPermission);
            }
        }

        /// <summary>
        ///  Gives a mob a permission to place a given Tile.
        /// </summary>
        public void AssignBuildPermissionTile(Entity mob, int range, string tileType, string alignOption)
        {
            var newPermission = new PlacementInformation
                                    {
                                        MobUid = mob.Uid,
                                        Range = range,
                                        IsTile = true,
                                        TileType = IoCManager.Resolve<ITileDefinitionManager>()[tileType].TileId,
                                        PlacementOption = alignOption
                                    };

            IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                               where permission.MobUid == mob.Uid
                                                               select permission;

            if (mobPermissions.Any()) //Already has one? Revoke the old one and add this one.
            {
                RevokeAllBuildPermissions(mob);
                BuildPermissions.Add(newPermission);
            }
            else
            {
                BuildPermissions.Add(newPermission);
            }
        }

        /// <summary>
        ///  Removes all building Permissions for given mob.
        /// </summary>
        public void RevokeAllBuildPermissions(Entity mob)
        {
            IEnumerable<PlacementInformation> mobPermissions = from PlacementInformation permission in BuildPermissions
                                                               where permission.MobUid == mob.Uid
                                                               select permission;

            if (mobPermissions.Any())
                BuildPermissions.RemoveAll(x => mobPermissions.Contains(x));
        }

        #endregion

        private PlacementInformation GetPermission(int uid, string alignOpt)
        {
            IEnumerable<PlacementInformation> permission = from p in BuildPermissions
                                                           where p.MobUid == uid && p.PlacementOption.Equals(alignOpt)
                                                           select p;

            if (permission.Any()) return permission.First();
            else return null;
        }
    }
}