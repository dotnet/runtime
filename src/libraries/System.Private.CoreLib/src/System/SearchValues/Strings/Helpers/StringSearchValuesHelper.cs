// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace System.Buffers
{
    // Provides implementations for helpers shared across multiple SearchValues<string> implementations,
    // such as normalizing and matching values under different case sensitivity rules.
    internal static class StringSearchValuesHelper
    {
        [Conditional("DEBUG")]
        public static void ValidateReadPosition(ref char searchSpaceStart, int searchSpaceLength, ref char searchSpace, int offset = 0)
        {
            Debug.Assert(searchSpaceLength >= 0);

            ValidateReadPosition(MemoryMarshal.CreateReadOnlySpan(ref searchSpaceStart, searchSpaceLength), ref searchSpace, offset);
        }

        [Conditional("DEBUG")]
        public static void ValidateReadPosition(ReadOnlySpan<char> span, ref char searchSpace, int offset = 0)
        {
            Debug.Assert(offset >= 0);

            nint currentByteOffset = Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref searchSpace);
            Debug.Assert(currentByteOffset >= 0);
            Debug.Assert((currentByteOffset & 1) == 0);

            int currentOffset = (int)(currentByteOffset / 2);
            int availableLength = span.Length - currentOffset;
            Debug.Assert(offset <= availableLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<TCaseSensitivity>(ref char matchStart, int lengthRemaining, string[] candidates)
            where TCaseSensitivity : struct, ICaseSensitivity
        {
            foreach (string candidate in candidates)
            {
                if (StartsWith<TCaseSensitivity>(ref matchStart, lengthRemaining, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<TCaseSensitivity>(ref char matchStart, int lengthRemaining, string candidate)
            where TCaseSensitivity : struct, ICaseSensitivity
        {
            Debug.Assert(lengthRemaining > 0);

            if (lengthRemaining < candidate.Length)
            {
                return false;
            }

            return TCaseSensitivity.Equals<ValueLength8OrLongerOrUnknown>(ref matchStart, candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarEquals<TCaseSensitivity>(ref char matchStart, string candidate)
            where TCaseSensitivity : struct, ICaseSensitivity
        {
            for (int i = 0; i < candidate.Length; i++)
            {
                if (TCaseSensitivity.TransformInput(Unsafe.Add(ref matchStart, i)) != candidate[i])
                {
                    return false;
                }
            }

            return true;
        }

        public interface IValueLength
        {
            static abstract bool AtLeast4Chars { get; }
            static abstract bool AtLeast8CharsOrUnknown { get; }
        }

        public readonly struct ValueLengthLessThan4 : IValueLength
        {
            public static bool AtLeast4Chars => false;
            public static bool AtLeast8CharsOrUnknown => false;
        }

        public readonly struct ValueLength4To7 : IValueLength
        {
            public static bool AtLeast4Chars => true;
            public static bool AtLeast8CharsOrUnknown => false;
        }

        // "Unknown" is currently only used by Teddy when confirming matches.
        public readonly struct ValueLength8OrLongerOrUnknown : IValueLength
        {
            public static bool AtLeast4Chars => true;
            public static bool AtLeast8CharsOrUnknown => true;
        }

        public interface ICaseSensitivity
        {
            static abstract char TransformInput(char input);
            static abstract Vector128<byte> TransformInput(Vector128<byte> input);
            static abstract Vector256<byte> TransformInput(Vector256<byte> input);
            static abstract Vector512<byte> TransformInput(Vector512<byte> input);
            static abstract bool Equals<TValueLength>(ref char matchStart, string candidate) where TValueLength : struct, IValueLength;
        }

        // Performs no case transformations.
        public readonly struct CaseSensitive : ICaseSensitivity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static char TransformInput(char input) => input;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<byte> TransformInput(Vector128<byte> input) => input;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<byte> TransformInput(Vector256<byte> input) => input;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<byte> TransformInput(Vector512<byte> input) => input;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Equals<TValueLength>(ref char matchStart, string candidate)
                where TValueLength : struct, IValueLength
            {
                Debug.Assert(candidate.Length > 1);

                ref byte first = ref Unsafe.As<char, byte>(ref matchStart);
                ref byte second = ref Unsafe.As<char, byte>(ref candidate.GetRawStringData());
                nuint byteLength = (nuint)(uint)candidate.Length * 2;

                if (TValueLength.AtLeast8CharsOrUnknown)
                {
                    return SpanHelpers.SequenceEqual(ref first, ref second, byteLength);
                }

                Debug.Assert(matchStart == candidate[0], "This should only be called after the first character has been checked");

                if (TValueLength.AtLeast4Chars)
                {
                    nuint offset = byteLength - sizeof(ulong);
                    ulong differentBits = Unsafe.ReadUnaligned<ulong>(ref first) - Unsafe.ReadUnaligned<ulong>(ref second);
                    differentBits |= Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref first, offset)) - Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref second, offset));
                    return differentBits == 0;
                }
                else
                {
                    Debug.Assert(candidate.Length is 2 or 3);

                    // We know that the candidate is 2 or 3 characters long, and that the first character has already been checked.
                    // We only have to to check the last 2 characters also match.
                    nuint offset = byteLength - sizeof(uint);

                    return Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref first, offset))
                        == Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref second, offset));
                }
            }
        }

        // Transforms inputs to their uppercase variants with the assumption that all input characters are ASCII letters.
        // These helpers may produce wrong results for other characters, and the callers must account for that.
        public readonly struct CaseInsensitiveAsciiLetters : ICaseSensitivity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static char TransformInput(char input) => (char)(input & ~0x20);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<byte> TransformInput(Vector128<byte> input) => input & Vector128.Create(unchecked((byte)~0x20));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<byte> TransformInput(Vector256<byte> input) => input & Vector256.Create(unchecked((byte)~0x20));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<byte> TransformInput(Vector512<byte> input) => input & Vector512.Create(unchecked((byte)~0x20));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Equals<TValueLength>(ref char matchStart, string candidate)
                where TValueLength : struct, IValueLength
            {
                Debug.Assert(candidate.Length > 1);
                Debug.Assert(Ascii.IsValid(candidate));
                Debug.Assert(candidate.ToUpperInvariant() == candidate);

                if (TValueLength.AtLeast8CharsOrUnknown)
                {
                    return Ascii.EqualsIgnoreCase(ref matchStart, ref candidate.GetRawStringData(), (uint)candidate.Length);
                }

                ref byte first = ref Unsafe.As<char, byte>(ref matchStart);
                ref byte second = ref Unsafe.As<char, byte>(ref candidate.GetRawStringData());
                nuint byteLength = (nuint)(uint)candidate.Length * 2;

                if (TValueLength.AtLeast4Chars)
                {
                    const ulong CaseMask = ~0x20002000200020u;
                    nuint offset = byteLength - sizeof(ulong);
                    ulong differentBits = (Unsafe.ReadUnaligned<ulong>(ref first) & CaseMask) - Unsafe.ReadUnaligned<ulong>(ref second);
                    differentBits |= (Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref first, offset)) & CaseMask) - Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref second, offset));
                    return differentBits == 0;
                }
                else
                {
                    const uint CaseMask = ~0x200020u;
                    nuint offset = byteLength - sizeof(uint);
                    uint differentBits = (Unsafe.ReadUnaligned<uint>(ref first) & CaseMask) - Unsafe.ReadUnaligned<uint>(ref second);
                    differentBits |= (Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref first, offset)) & CaseMask) - Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref second, offset));
                    return differentBits == 0;
                }
            }
        }

        // Transforms inputs to their uppercase variants with the assumption that all input characters are ASCII.
        // These helpers may produce wrong results for non-ASCII inputs, and the callers must account for that.
        public readonly struct CaseInsensitiveAscii : ICaseSensitivity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static char TransformInput(char input) => TextInfo.ToUpperAsciiInvariant(input);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<byte> TransformInput(Vector128<byte> input)
            {
                Vector128<byte> subtraction = Vector128.Create((byte)(128 + 'a'));
                Vector128<byte> comparison = Vector128.Create((byte)(128 + 26));
                Vector128<byte> caseConversion = Vector128.Create((byte)0x20);

                Vector128<byte> matches = Vector128.LessThan((input - subtraction).AsSByte(), comparison.AsSByte()).AsByte();
                return input ^ (matches & caseConversion);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<byte> TransformInput(Vector256<byte> input)
            {
                Vector256<byte> subtraction = Vector256.Create((byte)(128 + 'a'));
                Vector256<byte> comparison = Vector256.Create((byte)(128 + 26));
                Vector256<byte> caseConversion = Vector256.Create((byte)0x20);

                Vector256<byte> matches = Vector256.LessThan((input - subtraction).AsSByte(), comparison.AsSByte()).AsByte();
                return input ^ (matches & caseConversion);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<byte> TransformInput(Vector512<byte> input)
            {
                Vector512<byte> subtraction = Vector512.Create((byte)(128 + 'a'));
                Vector512<byte> comparison = Vector512.Create((byte)(128 + 26));
                Vector512<byte> caseConversion = Vector512.Create((byte)0x20);

                Vector512<byte> matches = Vector512.LessThan((input - subtraction).AsSByte(), comparison.AsSByte()).AsByte();
                return input ^ (matches & caseConversion);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Equals<TValueLength>(ref char matchStart, string candidate)
                where TValueLength : struct, IValueLength
            {
                Debug.Assert(candidate.Length > 1);
                Debug.Assert(Ascii.IsValid(candidate));
                Debug.Assert(candidate.ToUpperInvariant() == candidate);

                if (TValueLength.AtLeast8CharsOrUnknown)
                {
                    return Ascii.EqualsIgnoreCase(ref matchStart, ref candidate.GetRawStringData(), (uint)candidate.Length);
                }

                return ScalarEquals<CaseInsensitiveAscii>(ref matchStart, candidate);
            }
        }

        // We can't efficiently map non-ASCII inputs to their Ordinal uppercase variants,
        // so this helper is only used for the verification of the whole input.
        public readonly struct CaseInsensitiveUnicode : ICaseSensitivity
        {
            public static char TransformInput(char input) => throw new UnreachableException();
            public static Vector128<byte> TransformInput(Vector128<byte> input) => throw new UnreachableException();
            public static Vector256<byte> TransformInput(Vector256<byte> input) => throw new UnreachableException();
            public static Vector512<byte> TransformInput(Vector512<byte> input) => throw new UnreachableException();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Equals<TValueLength>(ref char matchStart, string candidate)
                where TValueLength : struct, IValueLength
            {
                if (TValueLength.AtLeast8CharsOrUnknown)
                {
                    return Ordinal.EqualsIgnoreCase(ref matchStart, ref candidate.GetRawStringData(), candidate.Length);
                }

                return Ordinal.EqualsIgnoreCase_Scalar(ref matchStart, ref candidate.GetRawStringData(), candidate.Length);
            }
        }
    }
}
