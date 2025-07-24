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

            return UnknownLengthEquals<TCaseSensitivity>(ref matchStart, candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool UnknownLengthEquals<TCaseSensitivity>(ref char matchStart, string candidate)
            where TCaseSensitivity : struct, ICaseSensitivity
        {
            if (typeof(TCaseSensitivity) == typeof(CaseSensitive))
            {
                return SpanHelpers.SequenceEqual(
                    ref Unsafe.As<char, byte>(ref matchStart),
                    ref candidate.GetRawStringDataAsUInt8(),
                    (uint)candidate.Length * sizeof(char));
            }

            if (typeof(TCaseSensitivity) == typeof(CaseInsensitiveAscii) ||
                typeof(TCaseSensitivity) == typeof(CaseInsensitiveAsciiLetters))
            {
                return Ascii.EqualsIgnoreCase(ref matchStart, ref candidate.GetRawStringData(), (uint)candidate.Length);
            }

            Debug.Assert(typeof(TCaseSensitivity) == typeof(CaseInsensitiveUnicode));
            return Ordinal.EqualsIgnoreCase(ref matchStart, ref candidate.GetRawStringData(), candidate.Length);
        }

        public interface IValueLength { }

        public readonly struct ValueLengthLessThan4 : IValueLength { }

        public readonly struct ValueLength4To8 : IValueLength { }

        public readonly struct ValueLength9To16 : IValueLength { }

        // "Unknown" is currently only used by Teddy when confirming matches.
        public readonly struct ValueLengthLongOrUnknown : IValueLength { }

        public readonly struct SingleValueState
        {
            public readonly string Value;
            public readonly nint SecondReadByteOffset;
            public readonly Vector256<ushort> Value256;
            public readonly Vector256<ushort> ToUpperMask256;

            public readonly ulong Value64_0 => Value256.AsUInt64()[0];
            public readonly ulong Value64_1 => Value256.AsUInt64()[1];
            public readonly uint Value32_0 => Value256.AsUInt32()[0];
            public readonly uint Value32_1 => Value256.AsUInt32()[1];

            public readonly ulong ToUpperMask64_0 => ToUpperMask256.AsUInt64()[0];
            public readonly ulong ToUpperMask64_1 => ToUpperMask256.AsUInt64()[1];
            public readonly uint ToUpperMask32_0 => ToUpperMask256.AsUInt32()[0];
            public readonly uint ToUpperMask32_1 => ToUpperMask256.AsUInt32()[1];

            public SingleValueState(string value, bool ignoreCase)
            {
                Debug.Assert(value.Length >= 2);

                Value = value;

                // We precompute vectors specific to this value to speed up later comparisons.
                // We group values depending on their length (2-3, 4-8, 9-16).
                // For any of those lengths, we can load the whole value with two overlapped reads (e.g. 2x 8 characters for lengths 9-16).
                // For a string "Hello World", we would load
                //              [Hello Wo]
                //                 [lo World]
                // SecondReadByteOffset: 6 bytes (3 characters)
                // We then precompute a mask that converts any potential input to the uppercase variant, specific to this value.
                // We must ensure that the ASCII letter mask only applies to the letters, not the space character.
                // Value256:       [HELLO WOLO WORLD] (note that the value is already converted to uppercase if we're ignoring casing)
                // ToUpperMask256: [xxxxx xxxx xxxxx] (x = ~0x20 for ASCII letters, 0xFFFF otherwise)
                //
                // Given a potential match, we can now confirm whether we found a match by loading the candidate in the same way and applying this mask:
                // Vector256 input = [Vector128.Load(candidate), Vector128.Load(candidate + 6 bytes)];
                // bool matches = (input & ToUpperMask256) == Value256;

                // The two vectors may overlap completely for Length == 2 or Length == 4, and that's fine.
                // The second comparison during validation is redundant in such cases, but the alternative is to introduce more IValueLength specializations.

                if (value.Length <= 16)
                {
                    if (value.Length > 8)
                    {
                        SecondReadByteOffset = (value.Length - 8) * sizeof(char);
                        Value256 = Vector256.Create(
                            Vector128.LoadUnsafe(ref value.GetRawStringDataAsUInt16()),
                            Vector128.LoadUnsafe(ref Unsafe.AddByteOffset(ref value.GetRawStringDataAsUInt16(), SecondReadByteOffset)));
                    }
                    else if (value.Length >= 4)
                    {
                        SecondReadByteOffset = (value.Length - 4) * sizeof(char);
                        Value256 = Vector256.Create(Vector128.Create(
                            Unsafe.ReadUnaligned<ulong>(ref value.GetRawStringDataAsUInt8()),
                            Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref value.GetRawStringDataAsUInt8(), SecondReadByteOffset))
                            )).AsUInt16();
                    }
                    else
                    {
                        Debug.Assert(value.Length is 2 or 3);

                        SecondReadByteOffset = (value.Length - 2) * sizeof(char);
                        Value256 = Vector256.Create(Vector128.Create(Vector64.Create(
                            Unsafe.ReadUnaligned<uint>(ref value.GetRawStringDataAsUInt8()),
                            Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref value.GetRawStringDataAsUInt8(), SecondReadByteOffset))
                            ))).AsUInt16();
                    }

                    if (ignoreCase)
                    {
                        Vector256<ushort> isAsciiLetter =
                            Vector256.GreaterThanOrEqual(Value256, Vector256.Create((ushort)'A')) &
                            Vector256.LessThanOrEqual(Value256, Vector256.Create((ushort)'Z'));

                        ToUpperMask256 = Vector256.ConditionalSelect(isAsciiLetter, Vector256.Create(unchecked((ushort)~0x20)), Vector256.Create(ushort.MaxValue));
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MatchesLength9To16_CaseSensitive(ref char matchStart)
            {
                Debug.Assert(Value.Length is >= 9 and <= 16);
                Debug.Assert(ToUpperMask256 == default);

                if (Vector256.IsHardwareAccelerated)
                {
                    Vector256<ushort> input = Vector256.Create(
                        Vector128.LoadUnsafe(ref matchStart),
                        Vector128.LoadUnsafe(ref Unsafe.AddByteOffset(ref matchStart, SecondReadByteOffset)));

                    return input == Value256;
                }
                else
                {
                    Vector128<ushort> different = Vector128.LoadUnsafe(ref matchStart) ^ Value256.GetLower();
                    different |= Vector128.LoadUnsafe(ref Unsafe.AddByteOffset(ref matchStart, SecondReadByteOffset)) ^ Value256.GetUpper();
                    return different == Vector128<ushort>.Zero;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MatchesLength9To16_CaseInsensitiveAscii(ref char matchStart)
            {
                Debug.Assert(Value.Length is >= 9 and <= 16);
                Debug.Assert(ToUpperMask256 != default);

                if (Vector256.IsHardwareAccelerated)
                {
                    Vector256<ushort> input = Vector256.Create(
                        Vector128.LoadUnsafe(ref matchStart),
                        Vector128.LoadUnsafe(ref Unsafe.AddByteOffset(ref matchStart, SecondReadByteOffset)));

                    return (input & ToUpperMask256) == Value256;
                }
                else
                {
                    Vector128<ushort> different = (Vector128.LoadUnsafe(ref matchStart) & ToUpperMask256.GetLower()) ^ Value256.GetLower();
                    different |= (Vector128.LoadUnsafe(ref Unsafe.AddByteOffset(ref matchStart, SecondReadByteOffset)) & ToUpperMask256.GetUpper()) ^ Value256.GetUpper();
                    return different == Vector128<ushort>.Zero;
                }
            }
        }

        public interface ICaseSensitivity
        {
            static abstract char TransformInput(char input);
            static abstract Vector128<byte> TransformInput(Vector128<byte> input);
            static abstract Vector256<byte> TransformInput(Vector256<byte> input);
            static abstract Vector512<byte> TransformInput(Vector512<byte> input);
            static abstract bool Equals<TValueLength>(ref char matchStart, ref readonly SingleValueState state) where TValueLength : struct, IValueLength;
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
            public static bool Equals<TValueLength>(ref char matchStart, ref readonly SingleValueState state)
                where TValueLength : struct, IValueLength
            {
                if (typeof(TValueLength) == typeof(ValueLengthLongOrUnknown))
                {
                    return UnknownLengthEquals<CaseSensitive>(ref matchStart, state.Value);
                }
                else if (typeof(TValueLength) == typeof(ValueLength9To16))
                {
                    return state.MatchesLength9To16_CaseSensitive(ref matchStart);
                }
                else if (typeof(TValueLength) == typeof(ValueLength4To8))
                {
                    ref byte matchByteStart = ref Unsafe.As<char, byte>(ref matchStart);
                    ulong differentBits = Unsafe.ReadUnaligned<ulong>(ref matchByteStart) - state.Value64_0;
                    differentBits |= Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref matchByteStart, state.SecondReadByteOffset)) - state.Value64_1;
                    return differentBits == 0;
                }
                else
                {
                    Debug.Assert(state.Value.Length is 2 or 3);
                    Debug.Assert(matchStart == state.Value[0], "This should only be called after the first character has been checked");

                    // We know that the candidate is 2 or 3 characters long, and that the first character has already been checked.
                    // We only have to to check whether the last 2 characters also match.
                    ref byte matchByteStart = ref Unsafe.As<char, byte>(ref matchStart);
                    return Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref matchByteStart, state.SecondReadByteOffset)) == state.Value32_1;
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
            public static bool Equals<TValueLength>(ref char matchStart, ref readonly SingleValueState state)
                where TValueLength : struct, IValueLength
            {
                if (typeof(TValueLength) == typeof(ValueLengthLongOrUnknown))
                {
                    return UnknownLengthEquals<CaseInsensitiveAsciiLetters>(ref matchStart, state.Value);
                }
                else if (typeof(TValueLength) == typeof(ValueLength9To16))
                {
                    return state.MatchesLength9To16_CaseInsensitiveAscii(ref matchStart);
                }
                else if (typeof(TValueLength) == typeof(ValueLength4To8))
                {
                    const ulong CaseMask = ~0x20002000200020u;
                    ref byte matchByteStart = ref Unsafe.As<char, byte>(ref matchStart);
                    ulong differentBits = (Unsafe.ReadUnaligned<ulong>(ref matchByteStart) & CaseMask) - state.Value64_0;
                    differentBits |= (Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref matchByteStart, state.SecondReadByteOffset)) & CaseMask) - state.Value64_1;
                    return differentBits == 0;
                }
                else
                {
                    Debug.Assert(state.Value.Length is 2 or 3);
                    Debug.Assert(TransformInput(matchStart) == state.Value[0], "This should only be called after the first character has been checked");

                    // We know that the candidate is 2 or 3 characters long, and that the first character has already been checked.
                    // We only have to to check whether the last 2 characters also match.
                    const uint CaseMask = ~0x200020u;
                    ref byte matchByteStart = ref Unsafe.As<char, byte>(ref matchStart);
                    return (Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref matchByteStart, state.SecondReadByteOffset)) & CaseMask) == state.Value32_1;
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
            public static bool Equals<TValueLength>(ref char matchStart, ref readonly SingleValueState state)
                where TValueLength : struct, IValueLength
            {
                if (typeof(TValueLength) == typeof(ValueLengthLongOrUnknown))
                {
                    return UnknownLengthEquals<CaseInsensitiveAscii>(ref matchStart, state.Value);
                }
                else if (typeof(TValueLength) == typeof(ValueLength9To16))
                {
                    return state.MatchesLength9To16_CaseInsensitiveAscii(ref matchStart);
                }
                else if (typeof(TValueLength) == typeof(ValueLength4To8))
                {
                    ref byte matchByteStart = ref Unsafe.As<char, byte>(ref matchStart);
                    ulong differentBits = (Unsafe.ReadUnaligned<ulong>(ref matchByteStart) & state.ToUpperMask64_0) - state.Value64_0;
                    differentBits |= (Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref matchByteStart, state.SecondReadByteOffset)) & state.ToUpperMask64_1) - state.Value64_1;
                    return differentBits == 0;
                }
                else
                {
                    Debug.Assert(state.Value.Length is 2 or 3);
                    Debug.Assert((matchStart & ~0x20) == (state.Value[0] & ~0x20));

                    ref byte matchByteStart = ref Unsafe.As<char, byte>(ref matchStart);
                    uint differentBits = (Unsafe.ReadUnaligned<uint>(ref matchByteStart) & state.ToUpperMask32_0) - state.Value32_0;
                    differentBits |= (Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref matchByteStart, state.SecondReadByteOffset)) & state.ToUpperMask32_1) - state.Value32_1;
                    return differentBits == 0;
                }
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
            public static bool Equals<TValueLength>(ref char matchStart, ref readonly SingleValueState state)
                where TValueLength : struct, IValueLength
            {
                if (typeof(TValueLength) == typeof(ValueLengthLongOrUnknown))
                {
                    return UnknownLengthEquals<CaseInsensitiveUnicode>(ref matchStart, state.Value);
                }
                else
                {
                    return Ordinal.EqualsIgnoreCase_Scalar(ref matchStart, ref state.Value.GetRawStringData(), state.Value.Length);
                }
            }
        }
    }
}
