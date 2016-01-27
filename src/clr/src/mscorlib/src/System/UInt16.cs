// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** Purpose: This class will encapsulate a short and provide an
**          Object representation of it.
**
** 
===========================================================*/
namespace System {
    using System.Globalization;
    using System;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

    // Wrapper for unsigned 16 bit integers.
[Serializable]
[CLSCompliant(false), System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)] 
[System.Runtime.InteropServices.ComVisible(true)]
    public struct UInt16 : IComparable, IFormattable, IConvertible
        , IComparable<UInt16>, IEquatable<UInt16>
    {
        private ushort m_value;
    
        public const ushort MaxValue = (ushort)0xFFFF;
        public const ushort MinValue = 0;
            
        
        // Compares this object to another object, returning an integer that
        // indicates the relationship. 
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type UInt16, this method throws an ArgumentException.
        // 
        public int CompareTo(Object value) {
            if (value == null) {
                return 1;
            }
            if (value is UInt16) {
                return ((int)m_value - (int)(((UInt16)value).m_value));
            }
            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeUInt16"));
        }

        public int CompareTo(UInt16 value) {
            return ((int)m_value - (int)value);
        }
    
        public override bool Equals(Object obj) {
            if (!(obj is UInt16)) {
                return false;
            }
            return m_value == ((UInt16)obj).m_value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public bool Equals(UInt16 obj)
        {
            return m_value == obj;
        }

        // Returns a HashCode for the UInt16
        public override int GetHashCode() {
            return (int)m_value;
        }

        // Converts the current value to a String in base-10 with no extra padding.
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
        public static ushort Parse(String s) {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }
        
        [CLSCompliant(false)]
        public static ushort Parse(String s, NumberStyles style) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }


        [CLSCompliant(false)]
        public static ushort Parse(String s, IFormatProvider provider) {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }
        
        [CLSCompliant(false)]
        public static ushort Parse(String s, NumberStyles style, IFormatProvider provider) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }
        
        private static ushort Parse(String s, NumberStyles style, NumberFormatInfo info) {
            
            uint i = 0;
            try {
                i = Number.ParseUInt32(s, style, info);
            }
            catch(OverflowException e) {
                throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"), e);
            }

            if (i > MaxValue) throw new OverflowException(Environment.GetResourceString("Overflow_UInt16"));
            return (ushort)i;
        }

        [CLSCompliant(false)]
        public static bool TryParse(String s, out UInt16 result) {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        [CLSCompliant(false)]
        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out UInt16 result) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }
        
        private static bool TryParse(String s, NumberStyles style, NumberFormatInfo info, out UInt16 result) {

            result = 0;
            UInt32 i;
            if (!Number.TryParseUInt32(s, style, info, out i)) {
                return false;
            }
            if (i > MaxValue) {
                return false;
            }
            result = (UInt16) i;
            return true;
        }

        //
        // IConvertible implementation
        // 
        
        public TypeCode GetTypeCode() {
            return TypeCode.UInt16;
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
            return m_value;
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
            return Convert.ToDouble(m_value);
        }

        /// <internalonly/>
        Decimal IConvertible.ToDecimal(IFormatProvider provider) {
            return Convert.ToDecimal(m_value);
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "UInt16", "DateTime"));
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider) {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
