// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** Purpose: This class will encapsulate an unsigned long and 
**          provide an Object representation of it.
**
** 
===========================================================*/

using System.Globalization;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace System
{
    // Wrapper for unsigned 64 bit integers.
    [Serializable]
    [CLSCompliant(false), System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    public struct UInt64 : IComparable, IFormattable, IConvertible
            , IComparable<UInt64>, IEquatable<UInt64>
    {
        private ulong _value;

        public const ulong MaxValue = (ulong)0xffffffffffffffffL;
        public const ulong MinValue = 0x0;

        // Compares this object to another object, returning an integer that
        // indicates the relationship. 
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type UInt64, this method throws an ArgumentException.
        // 
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }
            if (value is UInt64)
            {
                // Need to use compare because subtraction will wrap
                // to positive for very large neg numbers, etc.
                ulong i = (ulong)value;
                if (_value < i) return -1;
                if (_value > i) return 1;
                return 0;
            }
            throw new ArgumentException(SR.Arg_MustBeUInt64);
        }

        public int CompareTo(UInt64 value)
        {
            // Need to use compare because subtraction will wrap
            // to positive for very large neg numbers, etc.
            if (_value < value) return -1;
            if (_value > value) return 1;
            return 0;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is UInt64))
            {
                return false;
            }
            return _value == ((UInt64)obj)._value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public bool Equals(UInt64 obj)
        {
            return _value == obj;
        }

        // The value of the lower 32 bits XORed with the uppper 32 bits.
        public override int GetHashCode()
        {
            return ((int)_value) ^ (int)(_value >> 32);
        }

        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt64(_value, null, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt64(_value, null, NumberFormatInfo.GetInstance(provider));
        }

        public String ToString(String format)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt64(_value, format, NumberFormatInfo.CurrentInfo);
        }

        public String ToString(String format, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatUInt64(_value, format, NumberFormatInfo.GetInstance(provider));
        }

        [CLSCompliant(false)]
        public static ulong Parse(String s)
        {
            return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        [CLSCompliant(false)]
        public static ulong Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt64(s, style, NumberFormatInfo.CurrentInfo);
        }

        [CLSCompliant(false)]
        public static ulong Parse(string s, IFormatProvider provider)
        {
            return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        [CLSCompliant(false)]
        public static ulong Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.ParseUInt64(s, style, NumberFormatInfo.GetInstance(provider));
        }

        [CLSCompliant(false)]
        public static Boolean TryParse(String s, out UInt64 result)
        {
            return Number.TryParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        [CLSCompliant(false)]
        public static Boolean TryParse(String s, NumberStyles style, IFormatProvider provider, out UInt64 result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Number.TryParseUInt64(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        //
        // IConvertible implementation
        // 

        public TypeCode GetTypeCode()
        {
            return TypeCode.UInt64;
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return Convert.ToBoolean(_value);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return Convert.ToChar(_value);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(_value);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(_value);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(_value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(_value);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(_value);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(_value);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(_value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return _value;
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(_value);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(_value);
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt64", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
