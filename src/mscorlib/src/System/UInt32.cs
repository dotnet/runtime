// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: This class will encapsulate an uint and 
**          provide an Object representation of it.
**
** 
===========================================================*/
namespace System {
    using System.Globalization;
    using System;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

    // * Wrapper for unsigned 32 bit integers.
    [Serializable]
    [CLSCompliant(false), System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)] 
    [System.Runtime.InteropServices.ComVisible(true)]
    public struct UInt32 : IComparable, IFormattable, IConvertible
        , IComparable<UInt32>, IEquatable<UInt32>
    {
        private uint m_value;

        public const uint MaxValue = (uint)0xffffffff;
        public const uint MinValue = 0U;
    
    
        // Compares this object to another object, returning an integer that
        // indicates the relationship. 
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type UInt32, this method throws an ArgumentException.
        // 
        public int CompareTo(Object value) {
            if (value == null) {
                return 1;
            }
            if (value is UInt32) {
                // Need to use compare because subtraction will wrap
                // to positive for very large neg numbers, etc.
                uint i = (uint)value;
                if (m_value < i) return -1;
                if (m_value > i) return 1;
                return 0;
            }
            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeUInt32"));
        }

        public int CompareTo(UInt32 value) {
            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (m_value < value) return -1;
            if (m_value > value) return 1;
            return 0;
        }
    
        public override bool Equals(Object obj) {
            if (!(obj is UInt32)) {
                return false;
            }
            return m_value == ((UInt32)obj).m_value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public bool Equals(UInt32 obj)
        {
            return m_value == obj;
        }

        // The absolute value of the int contained.
        public override int GetHashCode() {
            return ((int) m_value);
        }
    
        // The base 10 representation of the number with no extra padding.
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt32(m_value, null, NumberFormatInfo.CurrentInfo);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt32(m_value, null, NumberFormatInfo.GetInstance(provider));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(String format) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt32(m_value, format, NumberFormatInfo.CurrentInfo);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(String format, IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt32(m_value, format, NumberFormatInfo.GetInstance(provider));
        }

        [CLSCompliant(false)]
        public static uint Parse(String s) {
            return Number.ParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }
        
        [CLSCompliant(false)]
        public static uint Parse(String s, NumberStyles style) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt32(s, style, NumberFormatInfo.CurrentInfo);
        }


        [CLSCompliant(false)]
        public static uint Parse(String s, IFormatProvider provider) {
            return Number.ParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        [CLSCompliant(false)]
        public static uint Parse(String s, NumberStyles style, IFormatProvider provider) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt32(s, style, NumberFormatInfo.GetInstance(provider));
        }

        [CLSCompliant(false)]
        public static bool TryParse(String s, out UInt32 result) {
            return Number.TryParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        [CLSCompliant(false)]
        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out UInt32 result) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseUInt32(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        //
        // IConvertible implementation
        // 
        
        public TypeCode GetTypeCode() {
            return TypeCode.UInt32;
        }

        /// <internalonly/>
        bool IConvertible.ToBoolean(IFormatProvider provider) {
            return Convert.ToBoolean(m_value);
        }

        /// <internalonly/>
        char IConvertible.ToChar(IFormatProvider provider) {
            return Convert.ToChar(m_value);
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
            return m_value;
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
            return Convert.ToDouble(m_value);
        }

        /// <internalonly/>
        Decimal IConvertible.ToDecimal(IFormatProvider provider) {
            return Convert.ToDecimal(m_value);
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "UInt32", "DateTime"));
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider) {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
