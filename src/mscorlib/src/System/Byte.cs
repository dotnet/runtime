// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: This class will encapsulate a byte and provide an
**          Object representation of it.
**
** 
===========================================================*/


using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace System
{
    // The Byte class extends the Value class and 
    // provides object representation of the byte primitive type. 
    // 
    [Serializable]
    [System.Runtime.InteropServices.StructLayout(LayoutKind.Sequential)]
    public struct Byte : IComparable, IFormattable, IConvertible
            , IComparable<Byte>, IEquatable<Byte>
    {
        private byte _value;

        // The maximum value that a Byte may represent: 255.
        public const byte MaxValue = (byte)0xFF;

        // The minimum value that a Byte may represent: 0.
        public const byte MinValue = 0;


        // Compares this object to another object, returning an integer that
        // indicates the relationship. 
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type byte, this method throws an ArgumentException.
        // 
        public int CompareTo(Object value)
        {
            if (value == null)
            {
                return 1;
            }
            if (!(value is Byte))
            {
                throw new ArgumentException(SR.Arg_MustBeByte);
            }

            return _value - (((Byte)value)._value);
        }

        public int CompareTo(Byte value)
        {
            return _value - value;
        }

        // Determines whether two Byte objects are equal.
        public override bool Equals(Object obj)
        {
            if (!(obj is Byte))
            {
                return false;
            }
            return _value == ((Byte)obj)._value;
        }

        [System.Runtime.Versioning.NonVersionable]
        public bool Equals(Byte obj)
        {
            return _value == obj;
        }

        // Gets a hash code for this instance.
        public override int GetHashCode()
        {
            return _value;
        }

        [Pure]
        public static byte Parse(String s)
        {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
        }

        [Pure]
        public static byte Parse(String s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.CurrentInfo);
        }

        [Pure]
        public static byte Parse(String s, IFormatProvider provider)
        {
            return Parse(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }


        // Parses an unsigned byte from a String in the given style.  If
        // a NumberFormatInfo isn't specified, the current culture's 
        // NumberFormatInfo is assumed.
        [Pure]
        public static byte Parse(String s, NumberStyles style, IFormatProvider provider)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return Parse(s, style, NumberFormatInfo.GetInstance(provider));
        }

        private static byte Parse(String s, NumberStyles style, NumberFormatInfo info)
        {
            int i = 0;
            try
            {
                i = Number.ParseInt32(s, style, info);
            }
            catch (OverflowException e)
            {
                throw new OverflowException(SR.Overflow_Byte, e);
            }

            if (i < MinValue || i > MaxValue) throw new OverflowException(SR.Overflow_Byte);
            return (byte)i;
        }

        public static bool TryParse(String s, out Byte result)
        {
            return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(String s, NumberStyles style, IFormatProvider provider, out Byte result)
        {
            NumberFormatInfo.ValidateParseStyleInteger(style);
            return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool TryParse(String s, NumberStyles style, NumberFormatInfo info, out Byte result)
        {
            result = 0;
            int i;
            if (!Number.TryParseInt32(s, style, info, out i))
            {
                return false;
            }
            if (i < MinValue || i > MaxValue)
            {
                return false;
            }
            result = (byte)i;
            return true;
        }

        [Pure]
        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatInt32(_value, null, NumberFormatInfo.CurrentInfo);
        }

        [Pure]
        public String ToString(String format)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatInt32(_value, format, NumberFormatInfo.CurrentInfo);
        }

        [Pure]
        public String ToString(IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatInt32(_value, null, NumberFormatInfo.GetInstance(provider));
        }

        [Pure]
        public String ToString(String format, IFormatProvider provider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return Number.FormatInt32(_value, format, NumberFormatInfo.GetInstance(provider));
        }

        //
        // IConvertible implementation
        // 
        [Pure]
        public TypeCode GetTypeCode()
        {
            return TypeCode.Byte;
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
            return _value;
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
            return Convert.ToUInt64(_value);
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
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Byte", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
