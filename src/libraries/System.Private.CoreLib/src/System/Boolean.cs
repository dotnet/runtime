// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: The boolean class serves as a wrapper for the primitive
** type boolean.
**
**
===========================================================*/

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct Boolean : IComparable, IConvertible, IComparable<bool>, IEquatable<bool>
    {
        //
        // Member Variables
        //
        private readonly bool m_value; // Do not rename (binary serialization)

        // The true value.
        //
        internal const int True = 1;

        // The false value.
        //
        internal const int False = 0;

        //
        // Internal Constants are real consts for performance.
        //

        // The internal string representation of true.
        //
        internal const string TrueLiteral = "True";

        // The internal string representation of false.
        //
        internal const string FalseLiteral = "False";

        //
        // Public Constants
        //

        // The public string representation of true.
        //
        public static readonly string TrueString = TrueLiteral;

        // The public string representation of false.
        //
        public static readonly string FalseString = FalseLiteral;

        //
        // Overridden Instance Methods
        //
        /*=================================GetHashCode==================================
        **Args:  None
        **Returns: 1 or 0 depending on whether this instance represents true or false.
        **Exceptions: None
        **Overridden From: Value
        ==============================================================================*/
        // Provides a hash code for this instance.
        public override int GetHashCode()
        {
            return (m_value) ? True : False;
        }

        /*===================================ToString===================================
        **Args: None
        **Returns:  "True" or "False" depending on the state of the boolean.
        **Exceptions: None.
        ==============================================================================*/
        // Converts the boolean value of this instance to a String.
        public override string ToString()
        {
            if (false == m_value)
            {
                return FalseLiteral;
            }
            return TrueLiteral;
        }

        public string ToString(IFormatProvider? provider)
        {
            return ToString();
        }

        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            if (m_value)
            {
                if (destination.Length > 3)
                {
                    ulong true_val = BitConverter.IsLittleEndian ? 0x65007500720054ul : 0x54007200750065ul; // "True"
                    MemoryMarshal.Write<ulong>(MemoryMarshal.AsBytes(destination), ref true_val);
                    charsWritten = 4;
                    return true;
                }
            }
            else
            {
                if (destination.Length > 4)
                {
                    ulong fals_val = BitConverter.IsLittleEndian ? 0x73006C00610046ul : 0x460061006C0073ul; // "Fals"
                    MemoryMarshal.Write<ulong>(MemoryMarshal.AsBytes(destination), ref fals_val);
                    destination[4] = 'e';
                    charsWritten = 5;
                    return true;
                }
            }

            charsWritten = 0;
            return false;
        }

        // Determines whether two Boolean objects are equal.
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            // If it's not a boolean, we're definitely not equal
            if (!(obj is bool))
            {
                return false;
            }

            return m_value == ((bool)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(bool obj)
        {
            return m_value == obj;
        }

        // Compares this object to another object, returning an integer that
        // indicates the relationship. For booleans, false sorts before true.
        // null is considered to be less than any instance.
        // If object is not of type boolean, this method throws an ArgumentException.
        //
        // Returns a value less than zero if this  object
        //
        public int CompareTo(object? obj)
        {
            if (obj == null)
            {
                return 1;
            }
            if (!(obj is bool))
            {
                throw new ArgumentException(SR.Arg_MustBeBoolean);
            }

            if (m_value == ((bool)obj).m_value)
            {
                return 0;
            }
            else if (m_value == false)
            {
                return -1;
            }
            return 1;
        }

        public int CompareTo(bool value)
        {
            if (m_value == value)
            {
                return 0;
            }
            else if (m_value == false)
            {
                return -1;
            }
            return 1;
        }

        //
        // Static Methods
        //

        // Custom string compares for early application use by config switches, etc
        //
        internal static bool IsTrueStringIgnoreCase(ReadOnlySpan<char> value)
        {
            // "true" as a ulong, each char |'d with 0x0020 for case-insensitivity
            ulong true_val = BitConverter.IsLittleEndian ? 0x65007500720074ul : 0x74007200750065ul;
            return value.Length == 4 &&
                   (MemoryMarshal.Read<ulong>(MemoryMarshal.AsBytes(value)) | 0x0020002000200020) == true_val;
        }

        internal static bool IsFalseStringIgnoreCase(ReadOnlySpan<char> value)
        {
            // "fals" as a ulong, each char |'d with 0x0020 for case-insensitivity
            ulong fals_val = BitConverter.IsLittleEndian ? 0x73006C00610066ul : 0x660061006C0073ul;
            return value.Length == 5 &&
                   (((MemoryMarshal.Read<ulong>(MemoryMarshal.AsBytes(value)) | 0x0020002000200020) == fals_val) &
                    ((value[4] | 0x20) == 'e'));
        }

        // Determines whether a String represents true or false.
        //
        public static bool Parse(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return Parse(value.AsSpan());
        }

        public static bool Parse(ReadOnlySpan<char> value) =>
            TryParse(value, out bool result) ? result : throw new FormatException(SR.Format(SR.Format_BadBoolean, new string(value)));

        // Determines whether a String represents true or false.
        //
        public static bool TryParse([NotNullWhen(true)] string? value, out bool result) =>
            TryParse(value.AsSpan(), out result);

        public static bool TryParse(ReadOnlySpan<char> value, out bool result)
        {
            // Boolean.{Try}Parse allows for optional whitespace/null values before and
            // after the case-insensitive "true"/"false", but we don't expect those to
            // be the common case. We check for "true"/"false" case-insensitive in the
            // fast, inlined call path, and then only if neither match do we fall back
            // to trimming and making a second post-trimming attempt at matching those
            // same strings.

            if (IsTrueStringIgnoreCase(value))
            {
                result = true;
                return true;
            }

            if (IsFalseStringIgnoreCase(value))
            {
                result = false;
                return true;
            }

            return TryParseUncommon(value, out result);

            static bool TryParseUncommon(ReadOnlySpan<char> value, out bool result)
            {
                // With "true" being 4 characters, even if we trim something from <= 4 chars,
                // it can't possibly match "true" or "false".
                int originalLength = value.Length;
                if (originalLength >= 5)
                {
                    value = TrimWhiteSpaceAndNull(value);
                    if (value.Length != originalLength)
                    {
                        // Something was trimmed.  Try matching again.
                        if (IsTrueStringIgnoreCase(value))
                        {
                            result = true;
                            return true;
                        }

                        result = false;
                        return IsFalseStringIgnoreCase(value);
                    }
                }

                result = false;
                return false;
            }
        }

        private static ReadOnlySpan<char> TrimWhiteSpaceAndNull(ReadOnlySpan<char> value)
        {
            int start = 0;
            while (start < value.Length)
            {
                if (!char.IsWhiteSpace(value[start]) && value[start] != '\0')
                {
                    break;
                }
                start++;
            }

            int end = value.Length - 1;
            while (end >= start)
            {
                if (!char.IsWhiteSpace(value[end]) && value[end] != '\0')
                {
                    break;
                }
                end--;
            }

            return value.Slice(start, end - start + 1);
        }

        //
        // IConvertible implementation
        //

        public TypeCode GetTypeCode()
        {
            return TypeCode.Boolean;
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return m_value;
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Boolean", "Char"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            return Convert.ToSByte(m_value);
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            return Convert.ToByte(m_value);
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            return Convert.ToInt16(m_value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            return Convert.ToUInt16(m_value);
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            return Convert.ToInt32(m_value);
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            return Convert.ToUInt32(m_value);
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            return Convert.ToInt64(m_value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return Convert.ToUInt64(m_value);
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            return Convert.ToSingle(m_value);
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            return Convert.ToDouble(m_value);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Boolean", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
