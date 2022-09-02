// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Unicode;

#pragma warning disable SA1121 // Use built-in type alias
using SkipChecks = System.Boolean;
using CheckBytes = System.Byte;
using CheckChars = System.Char;

namespace System.Buffers.Text
{
    public static partial class Ascii
    {
        public static bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
            => left.Length == right.Length && Equals<SkipChecks>(right, left) == EqualsResult.Match;

        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            => left.Length == right.Length && EqualsIgnoreCase<SkipChecks>(left, right) == EqualsResult.Match;

        public static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => left.Length == right.Length && Ordinal.EqualsIgnoreCase(ref MemoryMarshal.GetReference(left), ref MemoryMarshal.GetReference(right), left.Length);

        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
            => left.Length == right.Length && Equals<SkipChecks>(right, left) == EqualsResult.Match;

        public static unsafe bool StartsWith(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(Equals<CheckChars>(value, text.Slice(0, value.Length))));

        public static unsafe bool EndsWith(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(Equals<CheckChars>(value, text.Slice(text.Length - value.Length))));

        public static unsafe bool StartsWith(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(Equals<CheckBytes>(text.Slice(0, value.Length), value)));

        public static unsafe bool EndsWith(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(Equals<CheckBytes>(text.Slice(text.Length - value.Length), value)));

        public static bool StartsWithIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(EqualsIgnoreCase<CheckBytes>(text.Slice(0, value.Length), value)));

        public static bool EndsWithIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(EqualsIgnoreCase<CheckBytes>(text.Slice(text.Length - value.Length), value)));

        // TODO adsitnik: discuss whether this overload should exists, as the only difference with ROS.StartsWith(ROS, StringComparison.OrdinalIgnoreCase)
        // is throwing an exception for non-ASCII characters found in value
        public static bool StartsWithIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<char> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(EqualsIgnoreCase<CheckChars>(text.Slice(0, value.Length), value)));

        public static bool EndsWithIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<char> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(EqualsIgnoreCase<CheckChars>(text.Slice(text.Length - value.Length), value)));

        public static unsafe bool StartsWithIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(EqualsIgnoreCase<CheckChars>(value, text.Slice(0, value.Length))));

        public static unsafe bool EndsWithIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(EqualsIgnoreCase<CheckChars>(value, text.Slice(text.Length - value.Length))));

        public static unsafe bool StartsWithIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(EqualsIgnoreCase<CheckBytes>(text.Slice(0, value.Length), value)));

        public static unsafe bool EndsWithIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => value.IsEmpty || (text.Length >= value.Length && Map(EqualsIgnoreCase<CheckBytes>(text.Slice(text.Length - value.Length), value)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Map(EqualsResult equalsResult)
            => equalsResult switch
            {
                EqualsResult.NonAsciiFound => ThrowNonAsciiFound(),
                EqualsResult.Match => true,
                _ => false
            };

        [DoesNotReturn]
        private static bool ThrowNonAsciiFound() => throw new ArgumentException(SR.Arg_ContainsNonAscii, "value");

        private static EqualsResult Equals<TCheck>(ReadOnlySpan<char> chars, ReadOnlySpan<byte> bytes) where TCheck : struct
        {
            Debug.Assert(typeof(TCheck) == typeof(CheckBytes) || typeof(TCheck) == typeof(CheckChars) || typeof(TCheck) == typeof(SkipChecks));
            Debug.Assert(chars.Length == bytes.Length);

            if (!Vector128.IsHardwareAccelerated || chars.Length < Vector128<ushort>.Count)
            {
                for (int i = 0; i < chars.Length; i++)
                {
                    char c = chars[i];
                    byte b = bytes[i];

                    if (typeof(TCheck) == typeof(CheckChars))
                    {
                        if (!UnicodeUtility.IsAsciiCodePoint(c))
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }
                    else if (typeof(TCheck) == typeof(CheckBytes))
                    {
                        if (!UnicodeUtility.IsAsciiCodePoint(b))
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }

                    if (c != b)
                    {
                        return EqualsResult.NoMatch;
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && chars.Length >= Vector256<ushort>.Count)
            {
                ref ushort currentCharsSearchSpace = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(chars));
                ref ushort oneVectorAwayFromCharsEnd = ref Unsafe.Add(ref currentCharsSearchSpace, chars.Length - Vector256<ushort>.Count);
                ref byte currentBytesSearchSpace = ref MemoryMarshal.GetReference(bytes);
                ref byte oneVectorAwayFromBytesEnd = ref Unsafe.Add(ref currentBytesSearchSpace, bytes.Length - Vector128<byte>.Count);

                Vector128<byte> byteValues;
                Vector256<ushort> charValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    charValues = Vector256.LoadUnsafe(ref currentCharsSearchSpace);
                    byteValues = Vector128.LoadUnsafe(ref currentBytesSearchSpace);

                    if (typeof(TCheck) == typeof(CheckChars))
                    {
                        if (charValues.AsByte().ExtractMostSignificantBits() != 0)
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }
                    else if (typeof(TCheck) == typeof(CheckBytes))
                    {
                        if (byteValues.ExtractMostSignificantBits() != 0)
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }

                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    if (Vector256.Equals(Widen(byteValues), charValues) != Vector256<ushort>.AllBitsSet)
                    {
                        return EqualsResult.NoMatch;
                    }

                    currentCharsSearchSpace = ref Unsafe.Add(ref currentCharsSearchSpace, Vector256<ushort>.Count);
                    currentBytesSearchSpace = ref Unsafe.Add(ref currentBytesSearchSpace, Vector128<byte>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentCharsSearchSpace, ref oneVectorAwayFromCharsEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)chars.Length % Vector256<ushort>.Count != 0)
                {
                    charValues = Vector256.LoadUnsafe(ref oneVectorAwayFromCharsEnd);
                    byteValues = Vector128.LoadUnsafe(ref oneVectorAwayFromBytesEnd);

                    if (typeof(TCheck) == typeof(CheckChars))
                    {
                        if (charValues.AsByte().ExtractMostSignificantBits() != 0)
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }
                    else if (typeof(TCheck) == typeof(CheckBytes))
                    {
                        if (byteValues.ExtractMostSignificantBits() != 0)
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }

                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    if (Vector256.Equals(Widen(byteValues), charValues) != Vector256<ushort>.AllBitsSet)
                    {
                        return EqualsResult.NoMatch;
                    }
                }
            }
            else
            {
                ref ushort currentCharsSearchSpace = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(chars));
                ref ushort oneVectorAwayFromCharsEnd = ref Unsafe.Add(ref currentCharsSearchSpace, chars.Length - Vector128<ushort>.Count);
                ref byte currentBytesSearchSpace = ref MemoryMarshal.GetReference(bytes);
                ref byte oneVectorAwayFromBytesEnd = ref Unsafe.Add(ref currentBytesSearchSpace, bytes.Length - Vector64<byte>.Count);

                Vector64<byte> byteValues;
                Vector128<ushort> charValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    charValues = Vector128.LoadUnsafe(ref currentCharsSearchSpace);
                    byteValues = Vector64.LoadUnsafe(ref currentBytesSearchSpace);

                    if (typeof(TCheck) == typeof(CheckChars))
                    {
                        if (ASCIIUtility.VectorContainsNonAsciiChar(charValues))
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }
                    else if (typeof(TCheck) == typeof(CheckBytes))
                    {
                        if (VectorContainsNonAsciiChar(byteValues))
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }

                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    if (Vector128.Equals(Widen(byteValues), charValues) != Vector128<ushort>.AllBitsSet)
                    {
                        return EqualsResult.NoMatch;
                    }

                    currentCharsSearchSpace = ref Unsafe.Add(ref currentCharsSearchSpace, Vector128<ushort>.Count);
                    currentBytesSearchSpace = ref Unsafe.Add(ref currentBytesSearchSpace, Vector64<byte>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentCharsSearchSpace, ref oneVectorAwayFromCharsEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)chars.Length % Vector128<ushort>.Count != 0)
                {
                    charValues = Vector128.LoadUnsafe(ref oneVectorAwayFromCharsEnd);
                    byteValues = Vector64.LoadUnsafe(ref oneVectorAwayFromBytesEnd);

                    if (typeof(TCheck) == typeof(CheckChars))
                    {
                        if (ASCIIUtility.VectorContainsNonAsciiChar(charValues))
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }
                    else if (typeof(TCheck) == typeof(CheckBytes))
                    {
                        if (VectorContainsNonAsciiChar(byteValues))
                        {
                            return EqualsResult.NonAsciiFound;
                        }
                    }

                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    if (Vector128.Equals(Widen(byteValues), charValues) != Vector128<ushort>.AllBitsSet)
                    {
                        return EqualsResult.NoMatch;
                    }
                }
            }

            return EqualsResult.Match;
        }

        private static EqualsResult EqualsIgnoreCase<TCheck>(ReadOnlySpan<char> chars, ReadOnlySpan<byte> bytes) where TCheck : struct
        {
            Debug.Assert(chars.Length == bytes.Length);

            for (int i = 0; i < chars.Length; i++)
            {
                uint valueA = chars[i];
                uint valueB = bytes[i];

                if (typeof(TCheck) == typeof(CheckChars))
                {
                    if (!UnicodeUtility.IsAsciiCodePoint(valueA))
                    {
                        return EqualsResult.NonAsciiFound;
                    }
                }
                else if (typeof(TCheck) == typeof(CheckBytes))
                {
                    if (!UnicodeUtility.IsAsciiCodePoint(valueB))
                    {
                        return EqualsResult.NonAsciiFound;
                    }
                }

                if (valueA == valueB)
                {
                    continue; // exact match
                }

                valueA |= 0x20u;
                if ((uint)(valueA - 'a') > (uint)('z' - 'a'))
                {
                    return EqualsResult.NoMatch; // not exact match, and first input isn't in [A-Za-z]
                }

                if (valueA != (valueB | 0x20u))
                {
                    return EqualsResult.NoMatch;
                }
            }

            return EqualsResult.Match;
        }

        private static EqualsResult EqualsIgnoreCase<TCheck>(ReadOnlySpan<char> text, ReadOnlySpan<char> value) where TCheck : struct
        {
            Debug.Assert(text.Length == value.Length);

            for (int i = 0; i < text.Length; i++)
            {
                uint valueA = text[i];
                uint valueB = value[i];

                if (typeof(TCheck) == typeof(CheckChars))
                {
                    if (!UnicodeUtility.IsAsciiCodePoint(valueB))
                    {
                        return EqualsResult.NonAsciiFound; // value must not contain non-ASCII characters
                    }
                }

                if (valueA == valueB)
                {
                    continue; // exact match
                }

                valueA |= 0x20u;
                if ((uint)(valueA - 'a') > (uint)('z' - 'a'))
                {
                    return EqualsResult.NoMatch; // not exact match, and first input isn't in [A-Za-z]
                }

                if (valueA != (valueB | 0x20u))
                {
                    return EqualsResult.NoMatch;
                }
            }

            return EqualsResult.Match;
        }

        private static EqualsResult EqualsIgnoreCase<TCheck>(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value) where TCheck : struct
        {
            Debug.Assert(text.Length == value.Length);

            for (int i = 0; i < text.Length; i++)
            {
                uint valueA = text[i];
                uint valueB = value[i];

                if (typeof(TCheck) == typeof(CheckBytes))
                {
                    if (!UnicodeUtility.IsAsciiCodePoint(valueB))
                    {
                        return EqualsResult.NonAsciiFound; // value must not contain non-ASCII characters
                    }
                }

                if (valueA == valueB)
                {
                    continue; // exact match
                }

                valueA |= 0x20u;
                if ((uint)(valueA - 'a') > (uint)('z' - 'a'))
                {
                    return EqualsResult.NoMatch; // not exact match, and first input isn't in [A-Za-z]
                }

                if (valueA != (valueB | 0x20u))
                {
                    return EqualsResult.NoMatch;
                }
            }

            return EqualsResult.Match;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ushort> Widen(Vector64<byte> bytes)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.ZeroExtendWideningLower(bytes);
            }
            else
            {
                (Vector64<ushort> lower, Vector64<ushort> upper) = Vector64.Widen(bytes);
                return Vector128.Create(lower, upper);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ushort> Widen(Vector128<byte> bytes)
        {
            (Vector128<ushort> lower, Vector128<ushort> upper) = Vector128.Widen(bytes);
            return Vector256.Create(lower, upper);
        }

        private static bool VectorContainsNonAsciiChar(Vector64<byte> bytes)
            => !Utf8Utility.AllBytesInUInt64AreAscii(bytes.AsUInt64().ToScalar());

        private enum EqualsResult
        {
            NoMatch,
            Match,
            NonAsciiFound
        }
    }
}
#pragma warning restore SA1121 // Use built-in type alias
