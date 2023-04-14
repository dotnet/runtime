// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Text.Unicode;
using System.Numerics;

namespace System.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// Determines whether the provided buffers contain equal ASCII characters.
        /// </summary>
        /// <param name="left">The buffer to compare with <paramref name="right" />.</param>
        /// <param name="right">The buffer to compare with <paramref name="left" />.</param>
        /// <returns><see langword="true" /> if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal and ASCII. <see langword="false" /> otherwise.</returns>
        /// <remarks>If both buffers contain equal, but non-ASCII characters, the method returns <see langword="false" />.</remarks>
        public static bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            if (!Vector128.IsHardwareAccelerated || right.Length < Vector128<ushort>.Count)
            {
                for (int i = 0; i < right.Length; i++)
                {
                    char c = right[i];
                    byte b = left[i];

                    if (c != b)
                    {
                        return false;
                    }

                    if (!UnicodeUtility.IsAsciiCodePoint(c) || !UnicodeUtility.IsAsciiCodePoint(b))
                    {
                        return false;
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && right.Length >= Vector256<ushort>.Count)
            {
                ref ushort currentCharsSearchSpace = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right));
                ref ushort oneVectorAwayFromCharsEnd = ref Unsafe.Add(ref currentCharsSearchSpace, right.Length - Vector256<ushort>.Count);
                ref byte currentBytesSearchSpace = ref MemoryMarshal.GetReference(left);
                ref byte oneVectorAwayFromBytesEnd = ref Unsafe.Add(ref currentBytesSearchSpace, left.Length - Vector128<byte>.Count);

                Vector128<byte> byteValues;
                Vector256<ushort> charValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    charValues = Vector256.LoadUnsafe(ref currentCharsSearchSpace);
                    byteValues = Vector128.LoadUnsafe(ref currentBytesSearchSpace);

                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    if (Vector256.Equals(Widen(byteValues), charValues) != Vector256<ushort>.AllBitsSet)
                    {
                        return false;
                    }

                    if (charValues.AsByte().ExtractMostSignificantBits() != 0 || byteValues.ExtractMostSignificantBits() != 0)
                    {
                        return false;
                    }

                    currentCharsSearchSpace = ref Unsafe.Add(ref currentCharsSearchSpace, Vector256<ushort>.Count);
                    currentBytesSearchSpace = ref Unsafe.Add(ref currentBytesSearchSpace, Vector128<byte>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentCharsSearchSpace, ref oneVectorAwayFromCharsEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)right.Length % Vector256<ushort>.Count != 0)
                {
                    charValues = Vector256.LoadUnsafe(ref oneVectorAwayFromCharsEnd);
                    byteValues = Vector128.LoadUnsafe(ref oneVectorAwayFromBytesEnd);

                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    if (Vector256.Equals(Widen(byteValues), charValues) != Vector256<ushort>.AllBitsSet)
                    {
                        return false;
                    }

                    if (charValues.AsByte().ExtractMostSignificantBits() != 0 || byteValues.ExtractMostSignificantBits() != 0)
                    {
                        return false;
                    }
                }
            }
            else
            {
                ref ushort currentCharsSearchSpace = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right));
                ref ushort oneVectorAwayFromCharsEnd = ref Unsafe.Add(ref currentCharsSearchSpace, right.Length - Vector128<ushort>.Count);
                ref byte currentBytesSearchSpace = ref MemoryMarshal.GetReference(left);
                ref byte oneVectorAwayFromBytesEnd = ref Unsafe.Add(ref currentBytesSearchSpace, left.Length - Vector64<byte>.Count);

                Vector64<byte> byteValues;
                Vector128<ushort> charValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    charValues = Vector128.LoadUnsafe(ref currentCharsSearchSpace);
                    byteValues = Vector64.LoadUnsafe(ref currentBytesSearchSpace);

                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    if (Vector128.Equals(Widen(byteValues), charValues) != Vector128<ushort>.AllBitsSet)
                    {
                        return false;
                    }

                    if (VectorContainsNonAsciiChar(charValues) || VectorContainsNonAsciiChar(byteValues))
                    {
                        return false;
                    }

                    currentCharsSearchSpace = ref Unsafe.Add(ref currentCharsSearchSpace, Vector128<ushort>.Count);
                    currentBytesSearchSpace = ref Unsafe.Add(ref currentBytesSearchSpace, Vector64<byte>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentCharsSearchSpace, ref oneVectorAwayFromCharsEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)right.Length % Vector128<ushort>.Count != 0)
                {
                    charValues = Vector128.LoadUnsafe(ref oneVectorAwayFromCharsEnd);
                    byteValues = Vector64.LoadUnsafe(ref oneVectorAwayFromBytesEnd);

                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    if (Vector128.Equals(Widen(byteValues), charValues) != Vector128<ushort>.AllBitsSet)
                    {
                        return false;
                    }

                    if (VectorContainsNonAsciiChar(charValues) || VectorContainsNonAsciiChar(byteValues))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the provided buffers contain equal ASCII characters, ignoring case considerations.
        /// </summary>
        /// <param name="left">The buffer to compare with <paramref name="right" />.</param>
        /// <param name="right">The buffer to compare with <paramref name="left" />.</param>
        /// <returns><see langword="true" /> if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal ignoring case considerations and ASCII. <see langword="false" /> otherwise.</returns>
        /// <remarks>If both buffers contain equal, but non-ASCII characters, the method returns <see langword="false" />.</remarks>
        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            => left.Length == right.Length && SequenceEqualIgnoreCase(left, right);

        /// <inheritdoc cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
            => left.Length == right.Length && SequenceEqualIgnoreCase(right, left);

        /// <inheritdoc cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => left.Length == right.Length && SequenceEqualIgnoreCase(right, left);

        private static bool SequenceEqualIgnoreCase<TText, TValue>(ReadOnlySpan<TText> text, ReadOnlySpan<TValue> value)
            where TText : unmanaged, INumberBase<TText>
            where TValue : unmanaged, INumberBase<TValue>
        {
            Debug.Assert(text.Length == value.Length);

            for (int i = 0; i < text.Length; i++)
            {
                uint valueA = uint.CreateTruncating(text[i]);
                uint valueB = uint.CreateTruncating(value[i]);

                if (!UnicodeUtility.IsAsciiCodePoint(valueA) || !UnicodeUtility.IsAsciiCodePoint(valueB))
                {
                    return false;
                }

                if (valueA == valueB)
                {
                    continue; // exact match
                }

                valueA |= 0x20u;
                if ((uint)(valueA - 'a') > (uint)('z' - 'a'))
                {
                    return false; // not exact match, and first input isn't in [A-Za-z]
                }

                if (valueA != (valueB | 0x20u))
                {
                    return false;
                }
            }

            return true;
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
    }
}
