// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: This class will encapsulate a short and provide an
**          Object representation of it.
**
** 
===========================================================*/

namespace System {
    
    using System;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

[System.Runtime.InteropServices.ComVisible(true)]
[Serializable]
[System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    public struct Int16 : IComparable, IFormattable, IConvertible
        , IComparable<Int16>, IEquatable<Int16>
    {
        internal short m_value;
    
        public const short MaxValue = (short)0x7FFF;
        public const short MinValue = unchecked((short)0x8000);
        
        // Compares this object to another object, returning an integer that
        // indicates the relationship. 
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Int16, this method throws an ArgumentException.
        // 
        public int CompareTo(Object value) {
            if (value == null) {
                return 1;
            }
    
            if (value is Int16) {
                return m_value - ((Int16)value).m_value;
            }
    
            throw new ArgumentException(Environment.GetResourceString("Arg_MustBeInt16"));
        }

        public int CompareTo(Int16 value) {
            return m_value - value;
        }
    
        public override bool Equals(Object obj) {
            if (!(obj is Int16)) {
                return false;
            }
            return m_value == ((Int16)obj).m_value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public bool Equals(Int16 obj)
        {
            return m_value == obj;
        }

        // Returns a HashCode for the Int16
        public override int GetHashCode() {
            return ((int)((ushort)m_value) | (((int)m_value) << 16));
        }
    

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatInt32(m_value, null, NumberFormatInfo.CurrentInfo);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public String ToString(IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatInt32(m_value, null, NumberFormatInfo.GetInstance(provider));
        }        

        public String ToString(String format) {
            Contract.Ensures(Contract.Result<String>() != null);
            return ToString(format, NumberFormatInfo.CurrentInfo);
        }        
    
        public String ToString(String format, IFormatProvider provider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return ToString(format, NumberFormatInfo.GetInstance(provider));
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private String ToString(String format, NumberFormatInfo info) {
            Contract.Ensures(Contract.Result<String>() != null);

            if (m_value<0 && format!=null && format.Length>0 && (format[0]=='X' || format[0]=='x')) {
                uint temp = (uint)(m_value & 0x0000FFFF);
                return Number.FormatUInt32(temp,format, info);
            }
            return Number.FormatInt32(m_value, format, info);
        }
    
        public static short Parse(String s) {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }
    
        public static short Parse(String s, NumberStyles style) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }

        public static short Parse(String s, IFormatProvider provider) {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static short Parse(String s, NumberStyles style, IFormatProvider provider) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }
        
        private static short Parse(String s, NumberStyles style, NumberFormatInfo info) {        

            int i = 0;
            try {
                i = Number.ParseInt32(s, style, info);
            }
            catch(OverflowException e) {
                throw new OverflowException(Environment.GetResourceString("Overflow_Int16"), e);
            }

            // We need this check here since we don't allow signs to specified in hex numbers. So we fixup the result
            // for negative numbers
            if ((style & NumberStyles.AllowHexSpecifier) != 0) { // We are parsing a hexadecimal number
                if ((i < 0) || (i > UInt16.MaxValue)) {
                    throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
                }
                return (short)i;
            }
                
            if (i < MinValue || i > MaxValue) throw new OverflowException(Environment.GetResourceString("Overflow_Int16"));
            return (short)i;
        }

        public static bool TryParse(String s, out Int16 result) {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out Int16 result) {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }
        
        private static bool TryParse(String s, NumberStyles style, NumberFormatInfo info, out Int16 result) {

            result = 0;
            int i;
            if (!Number.TryParseInt32(s, style, info, out i)) {
                return false;
            }

            // We need this check here since we don't allow signs to specified in hex numbers. So we fixup the result
            // for negative numbers
            if ((style & NumberStyles.AllowHexSpecifier) != 0) { // We are parsing a hexadecimal number
                if ((i < 0) || i > UInt16.MaxValue) {
                    return false;
                }
                result = (Int16) i;
                return true;
            }
                
            if (i < MinValue || i > MaxValue) {
                return false;
            }
            result = (Int16) i;
            return true;
        }

        //
        // IConvertible implementation
        // 
        
        public TypeCode GetTypeCode() {
            return TypeCode.Int16;
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
            return m_value;
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
            return Convert.ToDouble(m_value);
        }

        /// <internalonly/>
        Decimal IConvertible.ToDecimal(IFormatProvider provider) {
            return Convert.ToDecimal(m_value);
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromTo", "Int16", "DateTime"));
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider) {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

    }
}
