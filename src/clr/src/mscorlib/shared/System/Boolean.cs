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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public struct Boolean : IComparable, IConvertible, IComparable<Boolean>, IEquatable<Boolean>
    {
        //
        // Member Variables
        //
        private bool m_value; // Do not rename (binary serialization)

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
        internal const String TrueLiteral = "True";

        // The internal string representation of false.
        // 
        internal const String FalseLiteral = "False";


        //
        // Public Constants
        //

        // The public string representation of true.
        // 
        public static readonly String TrueString = TrueLiteral;

        // The public string representation of false.
        // 
        public static readonly String FalseString = FalseLiteral;

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
        public override String ToString()
        {
            if (false == m_value)
            {
                return FalseLiteral;
            }
            return TrueLiteral;
        }

        public String ToString(IFormatProvider provider)
        {
            return ToString();
        }

        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            string s = m_value ? TrueLiteral : FalseLiteral;

            if (s.Length <= destination.Length)
            {
                bool copied = s.AsReadOnlySpan().TryCopyTo(destination);
                Debug.Assert(copied);
                charsWritten = s.Length;
                return true;
            }
            else
            {
                charsWritten = 0;
                return false;
            }
        }

        // Determines whether two Boolean objects are equal.
        public override bool Equals(Object obj)
        {
            //If it's not a boolean, we're definitely not equal
            if (!(obj is Boolean))
            {
                return false;
            }

            return (m_value == ((Boolean)obj).m_value);
        }

        [NonVersionable]
        public bool Equals(Boolean obj)
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
        public int CompareTo(Object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            if (!(obj is Boolean))
            {
                throw new ArgumentException(SR.Arg_MustBeBoolean);
            }

            if (m_value == ((Boolean)obj).m_value)
            {
                return 0;
            }
            else if (m_value == false)
            {
                return -1;
            }
            return 1;
        }

        public int CompareTo(Boolean value)
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

        // Determines whether a String represents true or false.
        // 
        public static Boolean Parse(String value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Parse(value.AsReadOnlySpan());
        }

        public static bool Parse(ReadOnlySpan<char> value) =>
            TryParse(value, out bool result) ? result : throw new FormatException(SR.Format_BadBoolean);

        // Determines whether a String represents true or false.
        // 
        public static Boolean TryParse(String value, out Boolean result)
        {
            if (value == null)
            {
                result = false;
                return false;
            }

            return TryParse(value.AsReadOnlySpan(), out result);
        }

        public static bool TryParse(ReadOnlySpan<char> value, out bool result)
        {
            ReadOnlySpan<char> trueSpan = TrueLiteral.AsReadOnlySpan();
            if (StringSpanHelpers.Equals(trueSpan, value, StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            ReadOnlySpan<char> falseSpan = FalseLiteral.AsReadOnlySpan();
            if (StringSpanHelpers.Equals(falseSpan, value, StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            // Special case: Trim whitespace as well as null characters.
            value = TrimWhiteSpaceAndNull(value);

            if (StringSpanHelpers.Equals(trueSpan, value, StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (StringSpanHelpers.Equals(falseSpan, value, StringComparison.OrdinalIgnoreCase))
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
                if (!Char.IsWhiteSpace(value[start]) && value[start] != nullChar)
                {
                    break;
                }
                start++;
            }

            int end = value.Length - 1;
            while (end >= start)
            {
                if (!Char.IsWhiteSpace(value[end]) && value[end] != nullChar)
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


        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return m_value;
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Boolean", "Char"));
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return Convert.ToSByte(m_value);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return Convert.ToByte(m_value);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return Convert.ToInt16(m_value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return Convert.ToUInt16(m_value);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return Convert.ToInt32(m_value);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return Convert.ToUInt32(m_value);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return Convert.ToInt64(m_value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return Convert.ToUInt64(m_value);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return Convert.ToSingle(m_value);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return Convert.ToDouble(m_value);
        }

        Decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return Convert.ToDecimal(m_value);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Boolean", "DateTime"));
        }

        Object IConvertible.ToType(Type type, IFormatProvider provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}
