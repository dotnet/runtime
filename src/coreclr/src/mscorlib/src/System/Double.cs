// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: A representation of an IEEE double precision
**          floating point number.
**
**
===========================================================*/
namespace System {

    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct Double : IComparable, IFormattable, IConvertible
        , IComparable<Double>, IEquatable<Double>
    {
        internal double m_value;

        //
        // Public Constants
        //
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;

        // Note Epsilon should be a double whose hex representation is 0x1
        // on little endian machines.
        public const double Epsilon  = 4.9406564584124654E-324;
        public const double NegativeInfinity = (double)-1.0 / (double)(0.0);
        public const double PositiveInfinity = (double)1.0 / (double)(0.0);
        public const double NaN = (double)0.0 / (double)0.0;
        
        internal static double NegativeZero = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000));

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool IsInfinity(double d) {
            return (*(long*)(&d) & 0x7FFFFFFFFFFFFFFF) == 0x7FF0000000000000;
        }

        [Pure]
        [System.Runtime.Versioning.NonVersionable]
        public static bool IsPositiveInfinity(double d) {
            //Jit will generate inlineable code with this
            if (d == double.PositiveInfinity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [Pure]
        [System.Runtime.Versioning.NonVersionable]
        public static bool IsNegativeInfinity(double d) {
            //Jit will generate inlineable code with this
            if (d == double.NegativeInfinity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static bool IsNegative(double d) {
            return (*(UInt64*)(&d) & 0x8000000000000000) == 0x8000000000000000;
        }

        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool IsNaN(double d)
        {
            return (*(UInt64*)(&d) & 0x7FFFFFFFFFFFFFFFL) > 0x7FF0000000000000L;
        }


        // Compares this object to another object, returning an instance of System.Relation.
        // Null is considered less than any instance.
        //
        // If object is not of type Double, this method throws an ArgumentException.
        //
        // Returns a value less than zero if this  object
        //
        public int CompareTo(Object value) {
            if (value == null) {
                return 1;
            }
            if (value is Double) {
                double d = (double)value;
                if (m_value < d) return -1;
                if (m_value > d) return 1;
                if (m_value == d) return 0;

                // At least one of the values is NaN.
                if (IsNaN(m_value))
                    return (IsNaN(d) ? 0 : -1);
                else
                    return 1;
            }
            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDouble"));
        }

        public int CompareTo(Double value) {
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            if (m_value == value) return 0;

            // At least one of the values is NaN.
            if (IsNaN(m_value))
                return (IsNaN(value) ? 0 : -1);
            else
                return 1;
        }

        // True if obj is another Double with the same value as the current instance.  This is
        // a method of object equality, that only returns true if obj is also a double.
        public override bool Equals(Object obj) {
            if (!(obj is Double)) {
                return false;
            }
            double temp = ((Double)obj).m_value;
            // This code below is written this way for performance reasons i.e the != and == check is intentional.
            if (temp == m_value) {
                return true;
            }
            return IsNaN(temp) && IsNaN(m_value);
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator ==(Double left, Double right) {
            return left == right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator !=(Double left, Double right) {
            return left != right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator <(Double left, Double right) {
            return left < right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator >(Double left, Double right) {
            return left > right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator <=(Double left, Double right) {
            return left <= right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator >=(Double left, Double right) {
            return left >= right;
        }

        public bool Equals(Double obj)
        {
            if (obj == m_value) {
                return true;
            }
            return IsNaN(obj) && IsNaN(m_value);
        }

        //The hashcode for a double is the absolute value of the integer representation
        //of that double.
        //
        [System.Security.SecuritySafeCritical]
        public unsafe override int GetHashCode() {
            double d = m_value;
            if (d == 0) {
                // Ensure that 0 and -0 have the same hash code
                return 0;
            }
            long value = *(long*)(&d);
            return unchecked((int)value) ^ ((int)(value >> 32));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatDouble(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(String format) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatDouble(m_value, format, NumberFormatInfo.CurrentInfo);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatDouble(m_value, null, NumberFormatInfo.GetInstance(provider));
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(String format, IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatDouble(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        public static double Parse(String s) {
            return Parse(s, NumberStyles.Float| NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(String s, NumberStyles style) {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static double Parse(String s, IFormatProvider provider) {
            return Parse(s, NumberStyles.Float| NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
        }

        public static double Parse(String s, NumberStyles style, IFormatProvider provider) {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }
        
        // Parses a double from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's
        // NumberFormatInfo is assumed.
        //
        // This method will not throw an OverflowException, but will return
        // PositiveInfinity or NegativeInfinity for a number that is too
        // large or too small.
        //
        private static double Parse(String s, NumberStyles style, NumberFormatInfo info) {
            return Number.ParseDouble(s, style, info);
        }

        public static bool TryParse(String s, out double result) {
            return TryParse(s, NumberStyles.Float| NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out double result) {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }
        
        private static bool TryParse(String s, NumberStyles style, NumberFormatInfo info, out double result) {
            if (s == null) {
                result = 0;
                return false;
            }
            bool success = Number.TryParseDouble(s, style, info, out result);
            if (!success) {
                String sTrim = s.Trim();
                if (sTrim.Equals(info.PositiveInfinitySymbol)) {
                    result = PositiveInfinity;
                } else if (sTrim.Equals(info.NegativeInfinitySymbol)) {
                    result = NegativeInfinity;
                } else if (sTrim.Equals(info.NaNSymbol)) {
                    result = NaN;
                } else
                    return false; // We really failed
            }
            return true;
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode() {
            return TypeCode.Double;
        }

        /// <internalonly/>
        bool IConvertible.ToBoolean(IFormatProvider provider) {
            return Convert.ToBoolean(m_value);
        }

        /// <internalonly/>
        char IConvertible.ToChar(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Double", "Char"));
        }

        /// <internalonly/>
        sbyte IConvertible.ToSByte(IFormatProvider provider) {
            return Convert.ToSByte(m_value);
        }

        /// <internalonly/>
        byte IConvertible.ToByte(IFormatProvider provider) {
            return Convert.ToByte(m_value);
        }

        /// <internalonly/>
        short IConvertible.ToInt16(IFormatProvider provider) {
            return Convert.ToInt16(m_value);
        }

        /// <internalonly/>
        ushort IConvertible.ToUInt16(IFormatProvider provider) {
            return Convert.ToUInt16(m_value);
        }

        /// <internalonly/>
        int IConvertible.ToInt32(IFormatProvider provider) {
            return Convert.ToInt32(m_value);
        }

        /// <internalonly/>
        uint IConvertible.ToUInt32(IFormatProvider provider) {
            return Convert.ToUInt32(m_value);
        }

        /// <internalonly/>
        long IConvertible.ToInt64(IFormatProvider provider) {
            return Convert.ToInt64(m_value);
        }

        /// <internalonly/>
        ulong IConvertible.ToUInt64(IFormatProvider provider) {
            return Convert.ToUInt64(m_value);
        }

        /// <internalonly/>
        float IConvertible.ToSingle(IFormatProvider provider) {
            return Convert.ToSingle(m_value);
        }

        /// <internalonly/>
        double IConvertible.ToDouble(IFormatProvider provider) {
            return m_value;
        }

        /// <internalonly/>
        Decimal IConvertible.ToDecimal(IFormatProvider provider) {
            return Convert.ToDecimal(m_value);
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Double", "DateTime"));
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider) {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
