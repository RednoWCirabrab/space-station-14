using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS14.Server.Services.Atmos
{
    /// <summary>
    /// The different temperature measurements used
    /// </summary>
    public enum TempType
    {
        Kelvin,
        Celsius,
        Fahrenheit
    }

    /// <summary>
    /// Represents temperatures throughout the game and has
    /// several functions to manipulate and evaluate them.
    /// </summary>
    public class Temperature
    {
        #region ### Properties ###
        private float _value; // Stored in Kelvin by default

        public float degK { get { return _value; } }
        public float degC { get { return toC(_value); } }
        public float degF { get { return toF(_value); } }
        #endregion

        //*********************************************************

        #region ### Constructors ###
        public Temperature()
        {
            _value = 293.15f;

        }

        public Temperature(float value, TempType type = TempType.Kelvin)
        {
            switch (type)
            {
                case TempType.Kelvin:
                    _value = value;
                    break;
                case TempType.Celsius:
                    _value = toK(value);
                    break;
                case TempType.Fahrenheit:
                    _value = toK(value, TempType.Fahrenheit);
                    break;
            }

        }
        #endregion

        //*********************************************************

        #region ### Static Conversion Methods ###

        public static float toK(float value, TempType type = TempType.Celsius)
        {
            switch (type)
            {
                case TempType.Kelvin:
                    return value;
                case TempType.Celsius:
                    return value + 273.15f;
                case TempType.Fahrenheit:
                    return ((value - 32.0f) / 1.8f) + 273.15f;
                default:
                    return 0.0f;
            }

        }

        public static float toC(float value, TempType type = TempType.Kelvin)
        {
            switch (type)
            {
                case TempType.Kelvin:
                    return value - 273.15f;
                case TempType.Celsius:
                    return value;
                case TempType.Fahrenheit:
                    return (value - 32.0f) / 1.8f;
                default:
                    return 0.0f;
            }
        }

        public static float toF(float value, TempType type = TempType.Kelvin)
        {
            switch (type)
            {
                case TempType.Kelvin:
                    return ((value - 273.15f) * 1.8f) + 32.0f;
                case TempType.Celsius:
                    return (value * 1.8f) + 32.0f;
                case TempType.Fahrenheit:
                    return value;
                default:
                    return 0.0f;
            }
        }

        #endregion

        //*********************************************************

        #region ### Operator Overrides ###
        public static Temperature operator +(Temperature t1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t1) || Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            t1._value += t2._value;
            return t1;
        }

        public static Temperature operator -(Temperature t1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t1) || Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            t1._value -= t2._value;
            return t1;
        }

        public static bool operator >(Temperature t1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t1) || Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            if (t1._value > t2._value)
                return true;
            else
                return false;
        }

        public static bool operator >(float f1, Temperature t1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            if (t1._value > f1)
                return true;
            else
                return false;
        }

        public static bool operator <(int i1, Temperature t1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            float f1 = (float)i1;
            if (t1._value < f1)
                return true;
            else
                return false;
        }

        public static bool operator <(Temperature t1, int i1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            float f1 = (float)i1;
            if (t1._value < f1)
                return true;
            else
                return false;
        }

        public static bool operator <(Temperature t1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t1) || Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            if (t1._value < t2._value)
                return true;
            else
                return false;
        }

        public static bool operator <(float f1, Temperature t1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            if (t1._value < f1)
                return true;
            else
                return false;
        }

        public static bool operator >(int i1, Temperature t1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            float f1 = (float)i1;
            if (t1._value > f1)
                return true;
            else
                return false;
        }

        public static bool operator >(Temperature t1, int i1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            float f1 = (float)i1;
            if (f1 > t1._value)
                return true;
            else
                return false;
        }

        public static bool operator ==(Temperature t1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t1) || Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            if (t1._value == t2._value)
                return true;
            else
                return false;
        }

        public static bool operator ==(float f1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            if (f1 == t2._value)
                return true;
            else
                return false;
        }

        public static bool operator ==(int i1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            float f1 = (float)i1;
            if (f1 == t2._value)
                return true;
            else
                return false;
        }

        public static bool operator !=(Temperature t1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t1) || Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            if (t1._value != t2._value)
                return true;
            else
                return false;
        }

        public static bool operator !=(float f1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            if (f1 != t2._value)
                return true;
            else
                return false;
        }

        public static bool operator !=(int i1, Temperature t2)
        {
            if (Object.ReferenceEquals(null, t2))
                throw new NullReferenceException();

            float f1 = (float)i1;
            if (f1 != t2._value)
                return true;
            else
                return false;
        }

        public static float operator *(float f1, Temperature t1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            return f1 * t1._value;
        }

        public static float operator *(Temperature t1, float f1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            return f1 * t1._value;
        }

        public static float operator *(int i1, Temperature t1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            float f1 = (float)i1;
            return f1 * t1._value;
        }

        public static float operator *(Temperature t1, int i1)
        {
            if (Object.ReferenceEquals(null, t1))
                throw new NullReferenceException();

            float f1 = (float)i1;
            return f1 * t1._value;
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(null, obj) || Object.ReferenceEquals(null, this))
                throw new NullReferenceException();

            if (((Temperature)this)._value == ((Temperature)obj)._value)
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        //*********************************************************

    }
}
