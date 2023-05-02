// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

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

            ref byte currentBytesSearchSpace = ref MemoryMarshal.GetReference(left);
            ref ushort currentCharsSearchSpace = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right));
            nuint length = (uint)right.Length;

            if (!Vector128.IsHardwareAccelerated || right.Length < Vector128<ushort>.Count)
            {
                for (nuint i = 0; i < length; ++i)
                {
                    byte b = Unsafe.Add(ref currentBytesSearchSpace, i);
                    ushort c = Unsafe.Add(ref currentCharsSearchSpace, i);

                    if (b != c || !UnicodeUtility.IsAsciiCodePoint((uint)(b | c)))
                    {
                        return false;
                    }
                }
            }
            else if (!Vector256.IsHardwareAccelerated || right.Length < Vector256<ushort>.Count)
            {
                ref byte oneVectorAwayFromBytesEnd = ref Unsafe.Add(ref currentBytesSearchSpace, length - sizeof(long));
                ref ushort oneVectorAwayFromCharsEnd = ref Unsafe.Add(ref currentCharsSearchSpace, length - (uint)Vector128<ushort>.Count);

                Vector128<ushort> widenedByteValues;
                Vector128<ushort> charValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    widenedByteValues = LoadAndWiden128(ref currentBytesSearchSpace);
                    charValues = Vector128.LoadUnsafe(ref currentCharsSearchSpace);

                    if (widenedByteValues != charValues || !AllCharsInVectorAreAscii(widenedByteValues | charValues))
                    {
                        return false;
                    }

                    currentBytesSearchSpace = ref Unsafe.Add(ref currentBytesSearchSpace, sizeof(long));
                    currentCharsSearchSpace = ref Unsafe.Add(ref currentCharsSearchSpace, Vector128<ushort>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentCharsSearchSpace, ref oneVectorAwayFromCharsEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector128<ushort>.Count != 0)
                {
                    widenedByteValues = LoadAndWiden128(ref oneVectorAwayFromBytesEnd);
                    charValues = Vector128.LoadUnsafe(ref oneVectorAwayFromCharsEnd);

                    if (widenedByteValues != charValues || !AllCharsInVectorAreAscii(widenedByteValues | charValues))
                    {
                        return false;
                    }
                }
            }
            else
            {
                ref byte oneVectorAwayFromBytesEnd = ref Unsafe.Add(ref currentBytesSearchSpace, length - (uint)Vector128<byte>.Count);
                ref ushort oneVectorAwayFromCharsEnd = ref Unsafe.Add(ref currentCharsSearchSpace, length - (uint)Vector256<ushort>.Count);

                Vector256<ushort> widenedByteValues;
                Vector256<ushort> charValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    widenedByteValues = LoadAndWiden256(ref currentBytesSearchSpace);
                    charValues = Vector256.LoadUnsafe(ref currentCharsSearchSpace);

                    if (widenedByteValues != charValues || !AllCharsInVectorAreAscii(widenedByteValues | charValues))
                    {
                        return false;
                    }

                    currentBytesSearchSpace = ref Unsafe.Add(ref currentBytesSearchSpace, Vector128<byte>.Count);
                    currentCharsSearchSpace = ref Unsafe.Add(ref currentCharsSearchSpace, Vector256<ushort>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentBytesSearchSpace, ref oneVectorAwayFromBytesEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector256<ushort>.Count != 0)
                {
                    widenedByteValues = LoadAndWiden256(ref oneVectorAwayFromBytesEnd);
                    charValues = Vector256.LoadUnsafe(ref oneVectorAwayFromCharsEnd);

                    if (widenedByteValues != charValues || !AllCharsInVectorAreAscii(widenedByteValues | charValues))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <inheritdoc cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        public static bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            => left.Length == right.Length
            && Equals(ref MemoryMarshal.GetReference(left), ref MemoryMarshal.GetReference(right), (uint)left.Length);

        /// <inheritdoc cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        public static bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<byte> right)
            => Equals(right, left);

        /// <inheritdoc cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        public static bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => left.Length == right.Length
            && Equals(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(left)), ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right)), (uint)left.Length);

        private static bool Equals<T>(ref T left, ref T right, nuint length) where T : unmanaged, INumberBase<T>
        {
            Debug.Assert(typeof(T) == typeof(byte) || typeof(T) == typeof(ushort));

            if (!Vector128.IsHardwareAccelerated || length < (uint)Vector128<T>.Count)
            {
                for (nuint i = 0; i < length; ++i)
                {
                    uint valueA = uint.CreateTruncating(Unsafe.Add(ref left, i));
                    uint valueB = uint.CreateTruncating(Unsafe.Add(ref right, i));

                    if (valueA != valueB || !UnicodeUtility.IsAsciiCodePoint(valueA | valueB))
                    {
                        return false;
                    }
                }
            }
            else if (!Vector256.IsHardwareAccelerated || length < (uint)Vector256<T>.Count)
            {
                ref T currentLeftSearchSpace = ref left;
                ref T oneVectorAwayFromLeftEnd = ref Unsafe.Add(ref currentLeftSearchSpace, length - (uint)Vector128<T>.Count);
                ref T currentRightSearchSpace = ref right;
                ref T oneVectorAwayFromRightEnd = ref Unsafe.Add(ref currentRightSearchSpace, length - (uint)Vector128<T>.Count);

                Vector128<T> leftValues;
                Vector128<T> rightValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    leftValues = Vector128.LoadUnsafe(ref currentLeftSearchSpace);
                    rightValues = Vector128.LoadUnsafe(ref currentRightSearchSpace);

                    if (leftValues != rightValues || !AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }

                    currentRightSearchSpace = ref Unsafe.Add(ref currentRightSearchSpace, Vector128<T>.Count);
                    currentLeftSearchSpace = ref Unsafe.Add(ref currentLeftSearchSpace, Vector128<T>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentRightSearchSpace, ref oneVectorAwayFromRightEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector128<T>.Count != 0)
                {
                    leftValues = Vector128.LoadUnsafe(ref oneVectorAwayFromLeftEnd);
                    rightValues = Vector128.LoadUnsafe(ref oneVectorAwayFromRightEnd);

                    if (leftValues != rightValues || !AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }
                }
            }
            else
            {
                ref T currentLeftSearchSpace = ref left;
                ref T oneVectorAwayFromLeftEnd = ref Unsafe.Add(ref currentLeftSearchSpace, length - (uint)Vector256<T>.Count);
                ref T currentRightSearchSpace = ref right;
                ref T oneVectorAwayFromRightEnd = ref Unsafe.Add(ref currentRightSearchSpace, length - (uint)Vector256<T>.Count);

                Vector256<T> leftValues;
                Vector256<T> rightValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    leftValues = Vector256.LoadUnsafe(ref currentLeftSearchSpace);
                    rightValues = Vector256.LoadUnsafe(ref currentRightSearchSpace);

                    if (leftValues != rightValues || !AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }

                    currentRightSearchSpace = ref Unsafe.Add(ref currentRightSearchSpace, Vector256<T>.Count);
                    currentLeftSearchSpace = ref Unsafe.Add(ref currentLeftSearchSpace, Vector256<T>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentRightSearchSpace, ref oneVectorAwayFromRightEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector256<T>.Count != 0)
                {
                    rightValues = Vector256.LoadUnsafe(ref oneVectorAwayFromRightEnd);
                    leftValues = Vector256.LoadUnsafe(ref oneVectorAwayFromLeftEnd);

                    if (leftValues != rightValues || !AllCharsInVectorAreAscii(leftValues | rightValues))
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
            => left.Length == right.Length
            && EqualsIgnoreCase(ref MemoryMarshal.GetReference(left), ref MemoryMarshal.GetReference(right), (uint)left.Length);

        /// <inheritdoc cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<byte> right)
            => EqualsIgnoreCase(right, left);

        /// <inheritdoc cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
            => left.Length == right.Length
            && EqualsIgnoreCase(ref MemoryMarshal.GetReference(left), ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right)), (uint)left.Length);

        /// <inheritdoc cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => left.Length == right.Length
            && EqualsIgnoreCase(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(left)), ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right)), (uint)left.Length);

        private static bool EqualsIgnoreCase<TLeft, TRight>(ref TLeft left, ref TRight right, nuint length)
            where TLeft : unmanaged, INumberBase<TLeft>
            where TRight : unmanaged, INumberBase<TRight>
        {
            Debug.Assert(typeof(TLeft) == typeof(byte) || typeof(TLeft) == typeof(ushort));
            Debug.Assert(typeof(TRight) == typeof(byte) || typeof(TRight) == typeof(ushort));

            for (nuint i = 0; i < length; ++i)
            {
                uint valueA = uint.CreateTruncating(Unsafe.Add(ref left, i));
                uint valueB = uint.CreateTruncating(Unsafe.Add(ref right, i));

                if (!UnicodeUtility.IsAsciiCodePoint(valueA | valueB))
                {
                    return false;
                }

                if (valueA == valueB)
                {
                    continue; // exact match
                }

                valueA |= 0x20u;
                if (valueA - 'a' > 'z' - 'a')
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
        private static Vector256<ushort> LoadAndWiden256(ref byte ptr)
        {
            (Vector128<ushort> lower, Vector128<ushort> upper) = Vector128.Widen(Vector128.LoadUnsafe(ref ptr));
            return Vector256.Create(lower, upper);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ushort> LoadAndWiden128(ref byte ptr)
        {
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.ZeroExtendWideningLower(Vector64.LoadUnsafe(ref ptr));
            }
            else if (Sse2.IsSupported)
            {
                Vector128<byte> vec = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<long>(ref ptr)).AsByte();
                return Sse2.UnpackLow(vec, Vector128<byte>.Zero).AsUInt16();
            }
            else
            {
                (Vector64<ushort> lower, Vector64<ushort> upper) = Vector64.Widen(Vector64.LoadUnsafe(ref ptr));
                return Vector128.Create(lower, upper);
            }
        }
    }
}
