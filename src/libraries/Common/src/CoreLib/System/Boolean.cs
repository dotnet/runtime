// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: The boolean class serves as a wrapper for the primitive
** type boolean.
**
** 
===========================================================*/

#nullable enable
using System.Runtime.CompilerServices;
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
        // Overriden Instance Methods
        //
        /*=================================GetHashCode==================================
        **Args:  None
        **Returns: 1 or 0 depending on whether this instance represents true or false.
        **Exceptions: None
        **Overriden From: Value
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
                if ((uint)destination.Length > 3) // uint cast, per https://github.com/dotnet/coreclr/issues/18688
                {
                    destination[0] = 'T';
                    destination[1] = 'r';
                    destination[2] = 'u';
                    destination[3] = 'e';
                    charsWritten = 4;
                    return true;
                }
            }
            else
            {
                if ((uint)destination.Length > 4)
                {
                    destination[0] = 'F';
                    destination[1] = 'a';
                    destination[2] = 'l';
                    destination[3] = 's';
                    destination[4] = 'e';
                    charsWritten = 5;
                    return true;
                }
            }

            charsWritten = 0;
            return false;
        }

        // Determines whether two Boolean objects are equal.
        public override bool Equals(object? obj)
        {
            //If it's not a boolean, we're definitely not equal
            if (!(obj is bool))
            {
                return false;
            }

            return (m_value == ((bool)obj).m_value);
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
            return (value.Length == 4 &&
                    (value[0] == 't' || value[0] == 'T') &&
                    (value[1] == 'r' || value[1] == 'R') &&
                    (value[2] == 'u' || value[2] == 'U') &&
                    (value[3] == 'e' || value[3] == 'E'));
        }

        internal static bool IsFalseStringIgnoreCase(ReadOnlySpan<char> value)
        {
            return (value.Length == 5 &&
                    (value[0] == 'f' || value[0] == 'F') &&
                    (value[1] == 'a' || value[1] == 'A') &&
                    (value[2] == 'l' || value[2] == 'L') &&
                    (value[3] == 's' || value[3] == 'S') &&
                    (value[4] == 'e' || value[4] == 'E'));
        }

        // Determines whether a String represents true or false.
        // 
        public static bool Parse(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Parse(value.AsSpan());
        }

        public static bool Parse(ReadOnlySpan<char> value) =>
            TryParse(value, out bool result) ? result : throw new FormatException(SR.Format(SR.Format_BadBoolean, new string(value)));

        // Determines whether a String represents true or false.
        // 
        public static bool TryParse(string? value, out bool result)
        {
            if (value == null)
            {
                result = false;
                return false;
            }

            return TryParse(value.AsSpan(), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> value, out bool result)
        {
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

            // Special case: Trim whitespace as well as null characters.
            value = TrimWhiteSpaceAndNull(value);

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

            result = false;
            return false;
        }

        private static ReadOnlySpan<char> TrimWhiteSpaceAndNull(ReadOnlySpan<char> value)
        {
            const char nullChar = (char)0x0000;

            int start = 0;
            while (start < value.Length)
            {
                if (!char.IsWhiteSpace(value[start]) && value[start] != nullChar)
                {
                    break;
                }
                start++;
            }

            int end = value.Length - 1;
            while (end >= start)
            {
                if (!char.IsWhiteSpace(value[end]) && value[end] != nullChar)
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
