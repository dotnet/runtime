using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    internal static class CharHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAsciiDigit(char c) =>
            (uint)(c - '0') < 10;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAsciiLowercaseLetter(char c) =>
            (uint)(c - 'a') < 26;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAsciiUppercaseLetter(char c) =>
            (uint)(c - 'A') < 26;

        public static bool IsAsciiLetter(char c) =>
            (((uint)c - 'A') & ~0x20) < 26;

        public static bool IsAsciiLetterOrDigit(char c) =>
            ((((uint)c - 'A') & ~0x20) < 26) ||
            (((uint)c - '0') < 10);

        public static bool IsHexDigit(char c) =>
            ((((uint)c - 'A') & ~0x20) < 6) ||
            (((uint)c - '0') < 10);


        public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
        {
            Debug.Assert(char.IsSurrogatePair(highSurrogate, lowSurrogate));

            const char HighSurrogateStart = '\ud800';
            const char LowSurrogateStart = '\udc00';
            const int UnicodePlane01Start = 0x10000;

            // return (((highSurrogate - HighSurrogateStart) * 0x400) + (lowSurrogate - LowSurrogateStart) + UnicodePlane01Start);

            const int Offset = (-HighSurrogateStart * 0x400) - LowSurrogateStart + UnicodePlane01Start;
            // return highSurrogate * 0x400 + lowSurrogate + Offset;

            int result = highSurrogate * 0x400 + lowSurrogate + Offset;

            if (result != char.ConvertToUtf32(highSurrogate, lowSurrogate))
            {
                throw new Exception((int)highSurrogate + " " + (int)lowSurrogate);
            }

            return result;
        }
    }
}
