﻿using System;
using SS14.Shared.Maths;

namespace SS14.Shared
{
    public class VectorEventArgs : EventArgs
    {
        public VectorEventArgs(Vector2 vectorFrom, Vector2 vectorTo)
        {
            VectorFrom = vectorFrom;
            VectorTo = vectorTo;
        }

        public Vector2 VectorFrom { get; private set; }
        public Vector2 VectorTo { get; private set; }
    }
}