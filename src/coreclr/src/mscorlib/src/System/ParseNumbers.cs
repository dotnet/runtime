// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Methods for Parsing numbers and Strings.
**
** 
===========================================================*/

using System.Text;

namespace System
{
    internal static class ParseNumbers
    {
        internal const int LeftAlign = 0x0001;
        internal const int RightAlign = 0x0004;
        internal const int PrefixSpace = 0x0008;
        internal const int PrintSign = 0x0010;
        internal const int PrintBase = 0x0020;
        internal const int PrintAsI1 = 0x0040;
        internal const int PrintAsI2 = 0x0080;
        internal const int PrintAsI4 = 0x0100;
        internal const int TreatAsUnsigned = 0x0200;
        internal const int TreatAsI1 = 0x0400;
        internal const int TreatAsI2 = 0x0800;
        internal const int IsTight = 0x1000;
        internal const int NoSpace = 0x2000;
        internal const int PrintRadixBase = 0x4000;

        private const int MinRadix = 2;
        private const int MaxRadix = 36;

        public static unsafe long StringToLong(System.String s, int radix, int flags)
        {
            int pos = 0;
            return StringToLong(s, radix, flags, ref pos);
        }

        public static long StringToLong(string s, int radix, int flags, ref int currPos)
        {
            long result = 0;

            int sign = 1;
            int length;
            int i;
            int grabNumbersStart = 0;
            int r;

            if (s != null)
            {
                i = currPos;

                // Do some radix checking.
                // A radix of -1 says to use whatever base is spec'd on the number.
                // Parse in Base10 until we figure out what the base actually is.
                r = (-1 == radix) ? 10 : radix;

                if (r != 2 && r != 10 && r != 8 && r != 16)
                    throw new ArgumentException(SR.Arg_InvalidBase, nameof(radix));

                length = s.Length;

                if (i < 0 || i >= length)
                    throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_Index);

                // Get rid of the whitespace and then check that we've still got some digits to parse.
                if (((flags & IsTight) == 0) && ((flags & NoSpace) == 0))
                {
                    EatWhiteSpace(s, ref i);
                    if (i == length)
                        throw new FormatException(SR.Format_EmptyInputString);
                }

                // Check for a sign
                if (s[i] == '-')
                {
                    if (r != 10)
                        throw new ArgumentException(SR.Arg_CannotHaveNegativeValue);

                    if ((flags & TreatAsUnsigned) != 0)
                        throw new OverflowException(SR.Overflow_NegativeUnsigned);

                    sign = -1;
                    i++;
                }
                else if (s[i] == '+')
                {
                    i++;
                }

                if ((radix == -1 || radix == 16) && (i + 1 < length) && s[i] == '0')
                {
                    if (s[i + 1] == 'x' || s[i + 1] == 'X')
                    {
                        r = 16;
                        i += 2;
                    }
                }

                grabNumbersStart = i;
                result = GrabLongs(r, s, ref i, (flags & TreatAsUnsigned) != 0);

                // Check if they passed us a string with no parsable digits.
                if (i == grabNumbersStart)
                    throw new FormatException(SR.Format_NoParsibleDigits);

                if ((flags & IsTight) != 0)
                {
                    //If we've got effluvia left at the end of the string, complain.
                    if (i < length)
                        throw new FormatException(SR.Format_ExtraJunkAtEnd);
                }

                // Put the current index back into the correct place.
                currPos = i;

                // Return the value properly signed.
                if ((ulong)result == 0x8000000000000000 && sign == 1 && r == 10 && ((flags & TreatAsUnsigned) == 0))
                    throw new OverflowException(SR.Overflow_Int64);

                if (r == 10)
                    result *= sign;
            }
            else
            {
                result = 0;
            }

            return result;
        }

        public static int StringToInt(string s, int radix, int flags)
        {
            int pos = 0;
            return StringToInt(s, radix, flags, ref pos);
        }

        public static int StringToInt(string s, int radix, int flags, ref int currPos)
        {
            int result = 0;

            int sign = 1;
            int length;
            int i;
            int grabNumbersStart = 0;
            int r;

            if (s != null)
            {
                // They're requied to tell me where to start parsing.
                i = currPos;

                // Do some radix checking.
                // A radix of -1 says to use whatever base is spec'd on the number.
                // Parse in Base10 until we figure out what the base actually is.
                r = (-1 == radix) ? 10 : radix;

                if (r != 2 && r != 10 && r != 8 && r != 16)
                    throw new ArgumentException(SR.Arg_InvalidBase, nameof(radix));

                length = s.Length;

                if (i < 0 || i >= length)
                    throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_Index);

                // Get rid of the whitespace and then check that we've still got some digits to parse.
                if (((flags & IsTight) == 0) && ((flags & NoSpace) == 0))
                {
                    EatWhiteSpace(s, ref i);
                    if (i == length)
                        throw new FormatException(SR.Format_EmptyInputString);
                }

                // Check for a sign
                if (s[i] == '-')
                {
                    if (r != 10)
                        throw new ArgumentException(SR.Arg_CannotHaveNegativeValue);

                    if ((flags & TreatAsUnsigned) != 0)
                        throw new OverflowException(SR.Overflow_NegativeUnsigned);

                    sign = -1;
                    i++;
                }
                else if (s[i] == '+')
                {
                    i++;
                }

                // Consume the 0x if we're in an unknown base or in base-16.
                if ((radix == -1 || radix == 16) && (i + 1 < length) && s[i] == '0')
                {
                    if (s[i + 1] == 'x' || s[i + 1] == 'X')
                    {
                        r = 16;
                        i += 2;
                    }
                }

                grabNumbersStart = i;
                result = GrabInts(r, s, ref i, ((flags & TreatAsUnsigned) != 0));

                // Check if they passed us a string with no parsable digits.
                if (i == grabNumbersStart)
                    throw new FormatException(SR.Format_NoParsibleDigits);

                if ((flags & IsTight) != 0)
                {
                    // If we've got effluvia left at the end of the string, complain.
                    if (i < length)
                        throw new FormatException(SR.Format_ExtraJunkAtEnd);
                }

                // Put the current index back into the correct place.
                currPos = i;

                // Return the value properly signed.
                if ((flags & TreatAsI1) != 0)
                {
                    if ((uint)result > 0xFF)
                        throw new OverflowException(SR.Overflow_SByte);
                }
                else if ((flags & TreatAsI2) != 0)
                {
                    if ((uint)result > 0xFFFF)
                        throw new OverflowException(SR.Overflow_Int16);
                }
                else if ((uint)result == 0x80000000 && sign == 1 && r == 10 && ((flags & TreatAsUnsigned) == 0))
                {
                    throw new OverflowException(SR.Overflow_Int32);
                }

                if (r == 10)
                    result *= sign;
            }
            else
            {
                result = 0;
            }

            return result;
        }

        public static String IntToString(int n, int radix, int width, char paddingChar, int flags)
        {
            bool isNegative = false;
            int index = 0;
            int buffLength;
            int i;
            uint l;
            char[] buffer = new char[66];  // Longest possible string length for an integer in binary notation with prefix

            if (radix < MinRadix || radix > MaxRadix)
                throw new ArgumentException(SR.Arg_InvalidBase, nameof(radix));

            // If the number is negative, make it positive and remember the sign.
            // If the number is MIN_VALUE, this will still be negative, so we'll have to
            // special case this later.
            if (n < 0)
            {
                isNegative = true;

                // For base 10, write out -num, but other bases write out the
                // 2's complement bit pattern
                if (10 == radix)
                    l = (uint)-n;
                else
                    l = (uint)n;
            }
            else
            {
                l = (uint)n;
            }

            // The conversion to a uint will sign extend the number.  In order to ensure
            // that we only get as many bits as we expect, we chop the number.
            if ((flags & PrintAsI1) != 0)
                l &= 0xFF;
            else if ((flags & PrintAsI2) != 0)
                l &= 0xFFFF;

            // Special case the 0.
            if (0 == l)
            {
                buffer[0] = '0';
                index = 1;
            }
            else
            {
                do
                {
                    uint charVal = l % (uint)radix;
                    l /= (uint)radix;
                    if (charVal < 10)
                        buffer[index++] = (char)(charVal + '0');
                    else
                        buffer[index++] = (char)(charVal + 'a' - 10);
                }
                while (l != 0);
            }

            // If they want the base, append that to the string (in reverse order)
            if (radix != 10 && ((flags & PrintBase) != 0))
            {
                if (16 == radix)
                {
                    buffer[index++] = 'x';
                    buffer[index++] = '0';
                }
                else if (8 == radix)
                {
                    buffer[index++] = '0';
                }
            }

            if (10 == radix)
            {
                // If it was negative, append the sign, else if they requested, add the '+'.
                // If they requested a leading space, put it on.
                if (isNegative)
                    buffer[index++] = '-';
                else if ((flags & PrintSign) != 0)
                    buffer[index++] = '+';
                else if ((flags & PrefixSpace) != 0)
                    buffer[index++] = ' ';
            }

            // Figure out the size of our string.
            if (width <= index)
                buffLength = index;
            else
                buffLength = width;

            StringBuilder sb = new StringBuilder(buffLength);

            // Put the characters into the String in reverse order
            // Fill the remaining space -- if there is any --
            // with the correct padding character.
            if ((flags & LeftAlign) != 0)
            {
                for (i = 0; i < index; i++)
                    sb.Append(buffer[index - i - 1]);

                if (buffLength > index)
                    sb.Append(paddingChar, buffLength - index);
            }
            else
            {
                if (buffLength > index)
                    sb.Append(paddingChar, buffLength - index);

                for (i = 0; i < index; i++)
                    sb.Append(buffer[index - i - 1]);
            }

            return sb.ToString();
        }

        public static String LongToString(long n, int radix, int width, char paddingChar, int flags)
        {
            bool isNegative = false;
            int index = 0;
            int charVal;
            ulong ul;
            int i;
            int buffLength = 0;
            char[] buffer = new char[67];//Longest possible string length for an integer in binary notation with prefix

            if (radix < MinRadix || radix > MaxRadix)
                throw new ArgumentException(SR.Arg_InvalidBase, nameof(radix));

            //If the number is negative, make it positive and remember the sign.
            if (n < 0)
            {
                isNegative = true;

                // For base 10, write out -num, but other bases write out the
                // 2's complement bit pattern
                if (10 == radix)
                    ul = (ulong)(-n);
                else
                    ul = (ulong)n;
            }
            else
            {
                ul = (ulong)n;
            }

            if ((flags & PrintAsI1) != 0)
                ul = ul & 0xFF;
            else if ((flags & PrintAsI2) != 0)
                ul = ul & 0xFFFF;
            else if ((flags & PrintAsI4) != 0)
                ul = ul & 0xFFFFFFFF;

            //Special case the 0.
            if (0 == ul)
            {
                buffer[0] = '0';
                index = 1;
            }
            else
            {
                //Pull apart the number and put the digits (in reverse order) into the buffer.
                for (index = 0; ul > 0; ul = ul / (ulong)radix, index++)
                {
                    if ((charVal = (int)(ul % (ulong)radix)) < 10)
                        buffer[index] = (char)(charVal + '0');
                    else
                        buffer[index] = (char)(charVal + 'a' - 10);
                }
            }

            //If they want the base, append that to the string (in reverse order)
            if (radix != 10 && ((flags & PrintBase) != 0))
            {
                if (16 == radix)
                {
                    buffer[index++] = 'x';
                    buffer[index++] = '0';
                }
                else if (8 == radix)
                {
                    buffer[index++] = '0';
                }
                else if ((flags & PrintRadixBase) != 0)
                {
                    buffer[index++] = '#';
                    buffer[index++] = (char)((radix % 10) + '0');
                    buffer[index++] = (char)((radix / 10) + '0');
                }
            }

            if (10 == radix)
            {
                //If it was negative, append the sign.
                if (isNegative)
                {
                    buffer[index++] = '-';
                }

                //else if they requested, add the '+';
                else if ((flags & PrintSign) != 0)
                {
                    buffer[index++] = '+';
                }

                //If they requested a leading space, put it on.
                else if ((flags & PrefixSpace) != 0)
                {
                    buffer[index++] = ' ';
                }
            }

            //Figure out the size of our string.
            if (width <= index)
                buffLength = index;
            else
                buffLength = width;

            StringBuilder sb = new StringBuilder(buffLength);

            //Put the characters into the String in reverse order
            //Fill the remaining space -- if there is any --
            //with the correct padding character.
            if ((flags & LeftAlign) != 0)
            {
                for (i = 0; i < index; i++)
                    sb.Append(buffer[index - i - 1]);

                if (buffLength > index)
                    sb.Append(paddingChar, buffLength - index);
            }
            else
            {
                if (buffLength > index)
                    sb.Append(paddingChar, buffLength - index);

                for (i = 0; i < index; i++)
                    sb.Append(buffer[index - i - 1]);
            }

            return sb.ToString();
        }

        private static void EatWhiteSpace(string s, ref int i)
        {
            for (; i < s.Length && char.IsWhiteSpace(s[i]); i++)
                ;
        }

        private static long GrabLongs(int radix, string s, ref int i, bool isUnsigned)
        {
            ulong result = 0;
            int value;
            ulong maxVal;

            // Allow all non-decimal numbers to set the sign bit.
            if (radix == 10 && !isUnsigned)
            {
                maxVal = 0x7FFFFFFFFFFFFFFF / 10;

                // Read all of the digits and convert to a number
                while (i < s.Length && (IsDigit(s[i], radix, out value)))
                {
                    // Check for overflows - this is sufficient & correct.
                    if (result > maxVal || ((long)result) < 0)
                        throw new OverflowException(SR.Overflow_Int64);
                    result = result * (ulong)radix + (ulong)value;
                    i++;
                }

                if ((long)result < 0 && result != 0x8000000000000000)
                    throw new OverflowException(SR.Overflow_Int64);
            }
            else
            {
                maxVal = 0xffffffffffffffff / (ulong)radix;

                // Read all of the digits and convert to a number
                while (i < s.Length && (IsDigit(s[i], radix, out value)))
                {
                    // Check for overflows - this is sufficient & correct.
                    if (result > maxVal)
                        throw new OverflowException(SR.Overflow_UInt64);

                    ulong temp = result * (ulong)radix + (ulong)value;
                    if (temp < result) // this means overflow as well
                        throw new OverflowException(SR.Overflow_UInt64);
                    result = temp;

                    i++;
                }
            }

            return (long)result;
        }

        private static int GrabInts(int radix, string s, ref int i, bool isUnsigned)
        {
            uint result = 0;
            int value;
            uint maxVal;

            // Allow all non-decimal numbers to set the sign bit.
            if (radix == 10 && !isUnsigned)
            {
                maxVal = (0x7FFFFFFF / 10);

                // Read all of the digits and convert to a number
                while (i < s.Length && (IsDigit(s[i], radix, out value)))
                {
                    // Check for overflows - this is sufficient & correct.
                    if (result > maxVal || (int)result < 0)
                        throw new OverflowException(SR.Overflow_Int32);
                    result = result * (uint)radix + (uint)value;
                    i++;
                }
                if ((int)result < 0 && result != 0x80000000)
                    throw new OverflowException(SR.Overflow_Int32);
            }
            else
            {
                maxVal = 0xffffffff / (uint)radix;

                // Read all of the digits and convert to a number
                while (i < s.Length && (IsDigit(s[i], radix, out value)))
                {
                    // Check for overflows - this is sufficient & correct.
                    if (result > maxVal)
                        throw new OverflowException(SR.Overflow_UInt32);
                    // the above check won't cover 4294967296 to 4294967299
                    uint temp = result * (uint)radix + (uint)value;
                    if (temp < result) // this means overflow as well
                        throw new OverflowException(SR.Overflow_UInt32);

                    result = temp;
                    i++;
                }
            }

            return (int)result;
        }

        private static bool IsDigit(char c, int radix, out int result)
        {
            if (c >= '0' && c <= '9')
                result = c - '0';
            else if (c >= 'A' && c <= 'Z')
                result = c - 'A' + 10;
            else if (c >= 'a' && c <= 'z')
                result = c - 'a' + 10;
            else
                result = -1;

            if ((result >= 0) && (result < radix))
                return true;

            return false;
        }
    }
}
