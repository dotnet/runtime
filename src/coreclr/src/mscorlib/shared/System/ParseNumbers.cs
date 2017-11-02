// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>Methods for parsing numbers and strings.</summary>
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

        public static unsafe long StringToLong(ReadOnlySpan<char> s, int radix, int flags)
        {
            int pos = 0;
            return StringToLong(s, radix, flags, ref pos);
        }

        public static long StringToLong(ReadOnlySpan<char> s, int radix, int flags, ref int currPos)
        {
            int i = currPos;

            // Do some radix checking.
            // A radix of -1 says to use whatever base is spec'd on the number.
            // Parse in Base10 until we figure out what the base actually is.
            int r = (-1 == radix) ? 10 : radix;

            if (r != 2 && r != 10 && r != 8 && r != 16)
                throw new ArgumentException(SR.Arg_InvalidBase, nameof(radix));

            int length = s.Length;

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
            int sign = 1;
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

            int grabNumbersStart = i;
            long result = GrabLongs(r, s, ref i, (flags & TreatAsUnsigned) != 0);

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
            {
                result *= sign;
            }

            return result;
        }

        public static int StringToInt(ReadOnlySpan<char> s, int radix, int flags)
        {
            int pos = 0;
            return StringToInt(s, radix, flags, ref pos);
        }

        public static int StringToInt(ReadOnlySpan<char> s, int radix, int flags, ref int currPos)
        {
            // They're requied to tell me where to start parsing.
            int i = currPos;

            // Do some radix checking.
            // A radix of -1 says to use whatever base is spec'd on the number.
            // Parse in Base10 until we figure out what the base actually is.
            int r = (-1 == radix) ? 10 : radix;

            if (r != 2 && r != 10 && r != 8 && r != 16)
                throw new ArgumentException(SR.Arg_InvalidBase, nameof(radix));

            int length = s.Length;

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
            int sign = 1;
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

            int grabNumbersStart = i;
            int result = GrabInts(r, s, ref i, ((flags & TreatAsUnsigned) != 0));

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
            {
                result *= sign;
            }

            return result;
        }

        public static string IntToString(int n, int radix, int width, char paddingChar, int flags)
        {
            Span<char> buffer = stackalloc char[66]; // Longest possible string length for an integer in binary notation with prefix

            if (radix < MinRadix || radix > MaxRadix)
                throw new ArgumentException(SR.Arg_InvalidBase, nameof(radix));

            // If the number is negative, make it positive and remember the sign.
            // If the number is MIN_VALUE, this will still be negative, so we'll have to
            // special case this later.
            bool isNegative = false;
            uint l;
            if (n < 0)
            {
                isNegative = true;

                // For base 10, write out -num, but other bases write out the
                // 2's complement bit pattern
                l = (10 == radix) ? (uint)-n : (uint)n;
            }
            else
            {
                l = (uint)n;
            }

            // The conversion to a uint will sign extend the number.  In order to ensure
            // that we only get as many bits as we expect, we chop the number.
            if ((flags & PrintAsI1) != 0)
            {
                l &= 0xFF;
            }
            else if ((flags & PrintAsI2) != 0)
            {
                l &= 0xFFFF;
            }

            // Special case the 0.
            int index;
            if (0 == l)
            {
                buffer[0] = '0';
                index = 1;
            }
            else
            {
                index = 0;
                for (int i = 0; i < buffer.Length; i++) // for(...;i<buffer.Length;...) loop instead of do{...}while(l!=0) to help JIT eliminate span bounds checks
                {
                    uint div = l / (uint)radix; // TODO https://github.com/dotnet/coreclr/issues/3439
                    uint charVal = l - (div * (uint)radix);
                    l = div;

                    buffer[i] = (charVal < 10) ?
                        (char)(charVal + '0') :
                        (char)(charVal + 'a' - 10);

                    if (l == 0)
                    {
                        index = i + 1;
                        break;
                    }
                }

                Debug.Assert(l == 0, $"Expected {l} == 0");
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
                {
                    buffer[index++] = '-';
                }
                else if ((flags & PrintSign) != 0)
                {
                    buffer[index++] = '+';
                }
                else if ((flags & PrefixSpace) != 0)
                {
                    buffer[index++] = ' ';
                }
            }

            // Figure out the size of and allocate the resulting string
            string result = string.FastAllocateString(Math.Max(width, index));
            unsafe
            {
                // Put the characters into the string in reverse order.
                // Fill the remaining space, if there is any, with the correct padding character.
                fixed (char* resultPtr = result)
                {
                    char* p = resultPtr;
                    int padding = result.Length - index;

                    if ((flags & LeftAlign) != 0)
                    {
                        for (int i = 0; i < padding; i++)
                        {
                            *p++ = paddingChar;
                        }

                        for (int i = 0; i < index; i++)
                        {
                            *p++ = buffer[index - i - 1];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < index; i++)
                        {
                            *p++ = buffer[index - i - 1];
                        }

                        for (int i = 0; i < padding; i++)
                        {
                            *p++ = paddingChar;
                        }
                    }

                    Debug.Assert((p - resultPtr) == result.Length, $"Expected {p - resultPtr} == {result.Length}");
                }
            }
            return result;
        }

        public static string LongToString(long n, int radix, int width, char paddingChar, int flags)
        {
            Span<char> buffer = stackalloc char[67]; // Longest possible string length for an integer in binary notation with prefix

            if (radix < MinRadix || radix > MaxRadix)
                throw new ArgumentException(SR.Arg_InvalidBase, nameof(radix));

            //If the number is negative, make it positive and remember the sign.
            ulong ul;
            bool isNegative = false;
            if (n < 0)
            {
                isNegative = true;

                // For base 10, write out -num, but other bases write out the
                // 2's complement bit pattern
                ul = (10 == radix) ? (ulong)(-n) : (ulong)n;
            }
            else
            {
                ul = (ulong)n;
            }

            if ((flags & PrintAsI1) != 0)
            {
                ul = ul & 0xFF;
            }
            else if ((flags & PrintAsI2) != 0)
            {
                ul = ul & 0xFFFF;
            }
            else if ((flags & PrintAsI4) != 0)
            {
                ul = ul & 0xFFFFFFFF;
            }

            //Special case the 0.
            int index;
            if (0 == ul)
            {
                buffer[0] = '0';
                index = 1;
            }
            else
            {
                index = 0;
                for (int i = 0; i < buffer.Length; i++) // for loop instead of do{...}while(l!=0) to help JIT eliminate span bounds checks
                {
                    ulong div = ul / (ulong)radix; // TODO https://github.com/dotnet/coreclr/issues/3439
                    int charVal = (int)(ul - (div * (ulong)radix));
                    ul = div;

                    buffer[i] = (charVal < 10) ?
                        (char)(charVal + '0') :
                        (char)(charVal + 'a' - 10);

                    if (ul == 0)
                    {
                        index = i + 1;
                        break;
                    }
                }
                Debug.Assert(ul == 0, $"Expected {ul} == 0");
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

            // Figure out the size of and allocate the resulting string
            string result = string.FastAllocateString(Math.Max(width, index));
            unsafe
            {
                // Put the characters into the string in reverse order.
                // Fill the remaining space, if there is any, with the correct padding character.
                fixed (char* resultPtr = result)
                {
                    char* p = resultPtr;
                    int padding = result.Length - index;

                    if ((flags & LeftAlign) != 0)
                    {
                        for (int i = 0; i < padding; i++)
                        {
                            *p++ = paddingChar;
                        }

                        for (int i = 0; i < index; i++)
                        {
                            *p++ = buffer[index - i - 1];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < index; i++)
                        {
                            *p++ = buffer[index - i - 1];
                        }

                        for (int i = 0; i < padding; i++)
                        {
                            *p++ = paddingChar;
                        }
                    }

                    Debug.Assert((p - resultPtr) == result.Length, $"Expected {p - resultPtr} == {result.Length}");
                }
            }
            return result;
        }

        private static void EatWhiteSpace(ReadOnlySpan<char> s, ref int i)
        {
            int localIndex = i;
            for (; localIndex < s.Length && char.IsWhiteSpace(s[localIndex]); localIndex++);
            i = localIndex;
        }

        private static long GrabLongs(int radix, ReadOnlySpan<char> s, ref int i, bool isUnsigned)
        {
            ulong result = 0;
            ulong maxVal;

            // Allow all non-decimal numbers to set the sign bit.
            if (radix == 10 && !isUnsigned)
            {
                maxVal = 0x7FFFFFFFFFFFFFFF / 10;

                // Read all of the digits and convert to a number
                while (i < s.Length && IsDigit(s[i], radix, out int value))
                {
                    // Check for overflows - this is sufficient & correct.
                    if (result > maxVal || ((long)result) < 0)
                    {
                        ThrowOverflowInt64Exception();
                    }

                    result = result * (ulong)radix + (ulong)value;
                    i++;
                }

                if ((long)result < 0 && result != 0x8000000000000000)
                {
                    ThrowOverflowInt64Exception();
                }
            }
            else
            {
                Debug.Assert(radix == 2 || radix == 8 || radix == 10 || radix == 16);
                maxVal =
                    radix == 10 ? 0xffffffffffffffff / 10 :
                    radix == 16 ? 0xffffffffffffffff / 16 :
                    radix == 8 ? 0xffffffffffffffff / 8 :
                    0xffffffffffffffff / 2;

                // Read all of the digits and convert to a number
                while (i < s.Length && IsDigit(s[i], radix, out int value))
                {
                    // Check for overflows - this is sufficient & correct.
                    if (result > maxVal)
                    {
                        ThrowOverflowUInt64Exception();
                    }

                    ulong temp = result * (ulong)radix + (ulong)value;

                    if (temp < result) // this means overflow as well
                    {
                        ThrowOverflowUInt64Exception();
                    }

                    result = temp;
                    i++;
                }
            }

            return (long)result;
        }

        private static int GrabInts(int radix, ReadOnlySpan<char> s, ref int i, bool isUnsigned)
        {
            uint result = 0;
            uint maxVal;

            // Allow all non-decimal numbers to set the sign bit.
            if (radix == 10 && !isUnsigned)
            {
                maxVal = (0x7FFFFFFF / 10);

                // Read all of the digits and convert to a number
                while (i < s.Length && IsDigit(s[i], radix, out int value))
                {
                    // Check for overflows - this is sufficient & correct.
                    if (result > maxVal || (int)result < 0)
                    {
                        ThrowOverflowInt32Exception();
                    }
                    result = result * (uint)radix + (uint)value;
                    i++;
                }
                if ((int)result < 0 && result != 0x80000000)
                {
                    ThrowOverflowInt32Exception();
                }
            }
            else
            {
                Debug.Assert(radix == 2 || radix == 8 || radix == 10 || radix == 16);
                maxVal =
                    radix == 10 ? 0xffffffff / 10 :
                    radix == 16 ? 0xffffffff / 16 :
                    radix == 8 ? 0xffffffff / 8 :
                    0xffffffff / 2;

                // Read all of the digits and convert to a number
                while (i < s.Length && IsDigit(s[i], radix, out int value))
                {
                    // Check for overflows - this is sufficient & correct.
                    if (result > maxVal)
                    {
                        throw new OverflowException(SR.Overflow_UInt32);
                    }

                    uint temp = result * (uint)radix + (uint)value;

                    if (temp < result) // this means overflow as well
                    {
                        ThrowOverflowUInt32Exception();
                    }

                    result = temp;
                    i++;
                }
            }

            return (int)result;
        }

        private static void ThrowOverflowInt32Exception() => throw new OverflowException(SR.Overflow_Int32);
        private static void ThrowOverflowInt64Exception() => throw new OverflowException(SR.Overflow_Int64);
        private static void ThrowOverflowUInt32Exception() => throw new OverflowException(SR.Overflow_UInt32);
        private static void ThrowOverflowUInt64Exception() => throw new OverflowException(SR.Overflow_UInt64);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit(char c, int radix, out int result)
        {
            int tmp;
            if ((uint)(c - '0') <= 9)
            {
                result = tmp = c - '0';
            }
            else if ((uint)(c - 'A') <= 'Z' - 'A')
            {
                result = tmp = c - 'A' + 10;
            }
            else if ((uint)(c - 'a') <= 'z' - 'a')
            {
                result = tmp = c - 'a' + 10;
            }
            else
            {
                result = -1;
                return false;
            }

            return tmp < radix;
        }
    }
}
