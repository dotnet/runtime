// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: A wrapper class for the primitive type float.
**
**
===========================================================*/
namespace System {

    using System.Globalization;
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Diagnostics.Contracts;

[Serializable]
[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
[System.Runtime.InteropServices.ComVisible(true)]
    public struct Single : IComparable, IFormattable, IConvertible
        , IComparable<Single>, IEquatable<Single> 
    {
        internal float m_value;

        //
        // Public constants
        //
        public const float MinValue = (float)-3.40282346638528859e+38;
        public const float Epsilon = (float)1.4e-45;
        public const float MaxValue = (float)3.40282346638528859e+38;
        public const float PositiveInfinity = (float)1.0 / (float)0.0;
        public const float NegativeInfinity = (float)-1.0 / (float)0.0;
        public const float NaN = (float)0.0 / (float)0.0;

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool IsInfinity(float f) {
            return (*(int*)(&f) & 0x7FFFFFFF) == 0x7F800000;
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool IsPositiveInfinity(float f) {
            return *(int*)(&f) == 0x7F800000;
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool IsNegativeInfinity(float f) {
            return *(int*)(&f) == unchecked((int)0xFF800000);
        }

        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [System.Security.SecuritySafeCritical]
        [System.Runtime.Versioning.NonVersionable]
        public unsafe static bool IsNaN(float f) {
            return (*(int*)(&f) & 0x7FFFFFFF) > 0x7F800000;
        }

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Single, this method throws an ArgumentException.
        //
        public int CompareTo(Object value) {
            if (value == null) {
                return 1;
            }
            if (value is Single) {
                float f = (float)value;
                if (m_value < f) return -1;
                if (m_value > f) return 1;
                if (m_value == f) return 0;

                // At least one of the values is NaN.
                if (IsNaN(m_value))
                    return (IsNaN(f) ? 0 : -1);
                else // f is NaN.
                    return 1;
            }
            throw new ArgumentException (Environment.GetResourceString("Arg_MustBeSingle"));
        }


        public int CompareTo(Single value) {
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            if (m_value == value) return 0;

            // At least one of the values is NaN.
            if (IsNaN(m_value))
                return (IsNaN(value) ? 0 : -1);
            else // f is NaN.
                return 1;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator ==(Single left, Single right) {
            return left == right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator !=(Single left, Single right) {
            return left != right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator <(Single left, Single right) {
            return left < right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator >(Single left, Single right) {
            return left > right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator <=(Single left, Single right) {
            return left <= right;
        }

        [System.Runtime.Versioning.NonVersionable]
        public static bool operator >=(Single left, Single right) {
            return left >= right;
        }

        public override bool Equals(Object obj) {
            if (!(obj is Single)) {
                return false;
            }
            float temp = ((Single)obj).m_value;
            if (temp == m_value) {
                return true;
            }

            return IsNaN(temp) && IsNaN(m_value);
        }

        public bool Equals(Single obj)
        {
            if (obj == m_value) {
                return true;
            }

            return IsNaN(obj) && IsNaN(m_value);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe override int GetHashCode() {
            float f = m_value;
            if (f == 0) {
                // Ensure that 0 and -0 have the same hash code
                return 0;
            }
            int v = *(int*)(&f);
            return v;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatSingle(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatSingle(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(String format) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatSingle(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(String format, IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatSingle(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        // Parses a float from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's
        // NumberFormatInfo is assumed.
        //
        // This method will not throw an OverflowException, but will return
        // PositiveInfinity or NegativeInfinity for a number that is too
        // large or too small.
        //
        public static float Parse(String s) {
            return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
        }

        public static float Parse(String s, NumberStyles style) {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static float Parse(String s, IFormatProvider provider) {
            return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
        }

        public static float Parse(String s, NumberStyles style, IFormatProvider provider) {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }
        
        private static float Parse(String s, NumberStyles style, NumberFormatInfo info) {
            return Number.ParseSingle(s, style, info);
        }

        public static Boolean TryParse(String s, out Single result) {
            return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
        }

        public static Boolean TryParse(String s, NumberStyles style, IFormatProvider provider, out Single result) {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }
        
        private static Boolean TryParse(String s, NumberStyles style, NumberFormatInfo info, out Single result) {
            if (s == null) {
                result = 0;
                return false;
            }
            bool success = Number.TryParseSingle(s, style, info, out result);
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
            return TypeCode.Single;
        }


        /// <internalonly/>
        bool IConvertible.ToBoolean(IFormatProvider provider) {
            return Convert.ToBoolean(m_value);
        }

        /// <internalonly/>
        char IConvertible.ToChar(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Single", "Char"));
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
            return m_value;
        }

        /// <internalonly/>
        double IConvertible.ToDouble(IFormatProvider provider) {
            return Convert.ToDouble(m_value);
        }

        /// <internalonly/>
        Decimal IConvertible.ToDecimal(IFormatProvider provider) {
            return Convert.ToDecimal(m_value);
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Single", "DateTime"));
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider) {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
