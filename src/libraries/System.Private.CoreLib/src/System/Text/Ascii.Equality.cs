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
        public static bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            => left.Length == right.Length
            && Equals<byte, byte, PlainLoader<byte>>(ref MemoryMarshal.GetReference(left), ref MemoryMarshal.GetReference(right), (uint)right.Length);

        /// <inheritdoc cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
            => left.Length == right.Length
            && Equals<byte, ushort, WideningLoader>(ref MemoryMarshal.GetReference(left), ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right)), (uint)right.Length);

        /// <inheritdoc cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        public static bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<byte> right)
            => Equals(right, left);

        /// <inheritdoc cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        public static bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => left.Length == right.Length
            && Equals<ushort, ushort, PlainLoader<ushort>>(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(left)), ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right)), (uint)right.Length);

        private static bool Equals<TLeft, TRight, TLoader>(ref TLeft left, ref TRight right, nuint length)
            where TLeft : unmanaged, INumberBase<TLeft>
            where TRight : unmanaged, INumberBase<TRight>
            where TLoader : struct, ILoader<TLeft, TRight>
        {
            Debug.Assert(
                (typeof(TLeft) == typeof(byte) && typeof(TRight) == typeof(byte))
             || (typeof(TLeft) == typeof(byte) && typeof(TRight) == typeof(ushort))
             || (typeof(TLeft) == typeof(ushort) && typeof(TRight) == typeof(ushort)));

            if (!Vector128.IsHardwareAccelerated || length < (uint)Vector128<TLeft>.Count)
            {
                for (nuint i = 0; i < length; ++i)
                {
                    uint valueA = uint.CreateTruncating(Unsafe.Add(ref left, i));
                    uint valueB = uint.CreateTruncating(Unsafe.Add(ref right, i));

                    if (valueA != valueB || !UnicodeUtility.IsAsciiCodePoint(valueA))
                    {
                        return false;
                    }
                }
            }
            else if (Vector512.IsHardwareAccelerated && length >= (uint)Vector512<TLeft>.Count)
            {
                ref TLeft currentLeftSearchSpace = ref left;
                ref TRight currentRightSearchSpace = ref right;
                // Add Vector512<TLeft>.Count because TLeft == TRight
                // Or we are in the Widen case where we iterate 2 * TRight.Count which is the same as TLeft.Count
                Debug.Assert(Vector512<TLeft>.Count == Vector512<TRight>.Count
                    || (typeof(TLoader) == typeof(WideningLoader) && Vector512<TLeft>.Count == Vector512<TRight>.Count * 2));
                ref TRight oneVectorAwayFromRightEnd = ref Unsafe.Add(ref currentRightSearchSpace, length - (uint)Vector512<TLeft>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    if (!TLoader.EqualAndAscii512(ref currentLeftSearchSpace, ref currentRightSearchSpace))
                    {
                        return false;
                    }

                    currentRightSearchSpace = ref Unsafe.Add(ref currentRightSearchSpace, Vector512<TLeft>.Count);
                    currentLeftSearchSpace = ref Unsafe.Add(ref currentLeftSearchSpace, Vector512<TLeft>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentRightSearchSpace, ref oneVectorAwayFromRightEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector512<TLeft>.Count != 0)
                {
                    ref TLeft oneVectorAwayFromLeftEnd = ref Unsafe.Add(ref left, length - (uint)Vector512<TLeft>.Count);
                    return TLoader.EqualAndAscii512(ref oneVectorAwayFromLeftEnd, ref oneVectorAwayFromRightEnd);
                }
            }
            else if (Avx.IsSupported && length >= (uint)Vector256<TLeft>.Count)
            {
                ref TLeft currentLeftSearchSpace = ref left;
                ref TRight currentRightSearchSpace = ref right;
                // Add Vector256<TLeft>.Count because TLeft == TRight
                // Or we are in the Widen case where we iterate 2 * TRight.Count which is the same as TLeft.Count
                Debug.Assert(Vector256<TLeft>.Count == Vector256<TRight>.Count
                    || (typeof(TLoader) == typeof(WideningLoader) && Vector256<TLeft>.Count == Vector256<TRight>.Count * 2));
                ref TRight oneVectorAwayFromRightEnd = ref Unsafe.Add(ref currentRightSearchSpace, length - (uint)Vector256<TLeft>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    if (!TLoader.EqualAndAscii256(ref currentLeftSearchSpace, ref currentRightSearchSpace))
                    {
                        return false;
                    }

                    currentRightSearchSpace = ref Unsafe.Add(ref currentRightSearchSpace, Vector256<TLeft>.Count);
                    currentLeftSearchSpace = ref Unsafe.Add(ref currentLeftSearchSpace, Vector256<TLeft>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentRightSearchSpace, ref oneVectorAwayFromRightEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector256<TLeft>.Count != 0)
                {
                    ref TLeft oneVectorAwayFromLeftEnd = ref Unsafe.Add(ref left, length - (uint)Vector256<TLeft>.Count);
                    return TLoader.EqualAndAscii256(ref oneVectorAwayFromLeftEnd, ref oneVectorAwayFromRightEnd);
                }
            }
            else
            {
                ref TLeft currentLeftSearchSpace = ref left;
                ref TLeft oneVectorAwayFromLeftEnd = ref Unsafe.Add(ref currentLeftSearchSpace, length - TLoader.Count128);
                ref TRight currentRightSearchSpace = ref right;
                ref TRight oneVectorAwayFromRightEnd = ref Unsafe.Add(ref currentRightSearchSpace, length - (uint)Vector128<TRight>.Count);

                Vector128<TRight> leftValues;
                Vector128<TRight> rightValues;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    leftValues = TLoader.Load128(ref currentLeftSearchSpace);
                    rightValues = Vector128.LoadUnsafe(ref currentRightSearchSpace);

                    if (leftValues != rightValues || !AllCharsInVectorAreAscii(leftValues))
                    {
                        return false;
                    }

                    currentRightSearchSpace = ref Unsafe.Add(ref currentRightSearchSpace, (uint)Vector128<TRight>.Count);
                    currentLeftSearchSpace = ref Unsafe.Add(ref currentLeftSearchSpace, TLoader.Count128);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentRightSearchSpace, ref oneVectorAwayFromRightEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector128<TRight>.Count != 0)
                {
                    leftValues = TLoader.Load128(ref oneVectorAwayFromLeftEnd);
                    rightValues = Vector128.LoadUnsafe(ref oneVectorAwayFromRightEnd);

                    if (leftValues != rightValues || !AllCharsInVectorAreAscii(leftValues))
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
            && EqualsIgnoreCase<byte, byte, PlainLoader<byte>>(ref MemoryMarshal.GetReference(left), ref MemoryMarshal.GetReference(right), (uint)right.Length);

        /// <inheritdoc cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
            => left.Length == right.Length
            && EqualsIgnoreCase<byte, ushort, WideningLoader>(ref MemoryMarshal.GetReference(left), ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right)), (uint)right.Length);

        /// <inheritdoc cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<byte> right)
            => EqualsIgnoreCase(right, left);

        /// <inheritdoc cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        public static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => left.Length == right.Length
            && EqualsIgnoreCase<ushort, ushort, PlainLoader<ushort>>(ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(left)), ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(right)), (uint)right.Length);

        internal static bool EqualsIgnoreCase(ref char left, ref char right, nuint length) =>
            EqualsIgnoreCase<ushort, ushort, PlainLoader<ushort>>(ref Unsafe.As<char, ushort>(ref left), ref Unsafe.As<char, ushort>(ref right), length);

        private static bool EqualsIgnoreCase<TLeft, TRight, TLoader>(ref TLeft left, ref TRight right, nuint length)
            where TLeft : unmanaged, INumberBase<TLeft>
            where TRight : unmanaged, INumberBase<TRight>
            where TLoader : ILoader<TLeft, TRight>
        {
            Debug.Assert(
                (typeof(TLeft) == typeof(byte) && typeof(TRight) == typeof(byte))
             || (typeof(TLeft) == typeof(byte) && typeof(TRight) == typeof(ushort))
             || (typeof(TLeft) == typeof(ushort) && typeof(TRight) == typeof(ushort)));

            if (!Vector128.IsHardwareAccelerated || length < (uint)Vector128<TRight>.Count)
            {
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
            }
            else if (Vector512.IsHardwareAccelerated && length >= (uint)Vector512<TRight>.Count)
            {
                ref TLeft currentLeftSearchSpace = ref left;
                ref TLeft oneVectorAwayFromLeftEnd = ref Unsafe.Add(ref currentLeftSearchSpace, length - TLoader.Count512);
                ref TRight currentRightSearchSpace = ref right;
                ref TRight oneVectorAwayFromRightEnd = ref Unsafe.Add(ref currentRightSearchSpace, length - (uint)Vector512<TRight>.Count);

                Vector512<TRight> leftValues;
                Vector512<TRight> rightValues;

                Vector512<TRight> loweringMask = Vector512.Create(TRight.CreateTruncating(0x20));
                Vector512<TRight> vecA = Vector512.Create(TRight.CreateTruncating('a'));
                Vector512<TRight> vecZMinusA = Vector512.Create(TRight.CreateTruncating(('z' - 'a')));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    leftValues = TLoader.Load512(ref currentLeftSearchSpace);
                    rightValues = Vector512.LoadUnsafe(ref currentRightSearchSpace);
                    if (!AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }

                    Vector512<TRight> notEquals = ~Vector512.Equals(leftValues, rightValues);

                    if (notEquals != Vector512<TRight>.Zero)
                    {
                        // not exact match

                        leftValues |= loweringMask;
                        rightValues |= loweringMask;

                        if (Vector512.GreaterThanAny((leftValues - vecA) & notEquals, vecZMinusA) || leftValues != rightValues)
                        {
                            return false; // first input isn't in [A-Za-z], and not exact match of lowered
                        }
                    }

                    currentRightSearchSpace = ref Unsafe.Add(ref currentRightSearchSpace, (uint)Vector512<TRight>.Count);
                    currentLeftSearchSpace = ref Unsafe.Add(ref currentLeftSearchSpace, TLoader.Count512);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentRightSearchSpace, ref oneVectorAwayFromRightEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector512<TRight>.Count != 0)
                {
                    leftValues = TLoader.Load512(ref oneVectorAwayFromLeftEnd);
                    rightValues = Vector512.LoadUnsafe(ref oneVectorAwayFromRightEnd);

                    if (!AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }

                    Vector512<TRight> notEquals = ~Vector512.Equals(leftValues, rightValues);

                    if (notEquals != Vector512<TRight>.Zero)
                    {
                        // not exact match

                        leftValues |= loweringMask;
                        rightValues |= loweringMask;

                        if (Vector512.GreaterThanAny((leftValues - vecA) & notEquals, vecZMinusA) || leftValues != rightValues)
                        {
                            return false; // first input isn't in [A-Za-z], and not exact match of lowered
                        }
                    }
                }
            }
            else if (Avx.IsSupported && length >= (uint)Vector256<TRight>.Count)
            {
                ref TLeft currentLeftSearchSpace = ref left;
                ref TLeft oneVectorAwayFromLeftEnd = ref Unsafe.Add(ref currentLeftSearchSpace, length - TLoader.Count256);
                ref TRight currentRightSearchSpace = ref right;
                ref TRight oneVectorAwayFromRightEnd = ref Unsafe.Add(ref currentRightSearchSpace, length - (uint)Vector256<TRight>.Count);

                Vector256<TRight> leftValues;
                Vector256<TRight> rightValues;

                Vector256<TRight> loweringMask = Vector256.Create(TRight.CreateTruncating(0x20));
                Vector256<TRight> vecA = Vector256.Create(TRight.CreateTruncating('a'));
                Vector256<TRight> vecZMinusA = Vector256.Create(TRight.CreateTruncating(('z' - 'a')));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    leftValues = TLoader.Load256(ref currentLeftSearchSpace);
                    rightValues = Vector256.LoadUnsafe(ref currentRightSearchSpace);

                    if (!AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }

                    Vector256<TRight> notEquals = ~Vector256.Equals(leftValues, rightValues);

                    if (notEquals != Vector256<TRight>.Zero)
                    {
                        // not exact match

                        leftValues |= loweringMask;
                        rightValues |= loweringMask;

                        if (Vector256.GreaterThanAny((leftValues - vecA) & notEquals, vecZMinusA) || leftValues != rightValues)
                        {
                            return false; // first input isn't in [A-Za-z], and not exact match of lowered
                        }
                    }

                    currentRightSearchSpace = ref Unsafe.Add(ref currentRightSearchSpace, (uint)Vector256<TRight>.Count);
                    currentLeftSearchSpace = ref Unsafe.Add(ref currentLeftSearchSpace, TLoader.Count256);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentRightSearchSpace, ref oneVectorAwayFromRightEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector256<TRight>.Count != 0)
                {
                    leftValues = TLoader.Load256(ref oneVectorAwayFromLeftEnd);
                    rightValues = Vector256.LoadUnsafe(ref oneVectorAwayFromRightEnd);

                    if (!AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }

                    Vector256<TRight> notEquals = ~Vector256.Equals(leftValues, rightValues);

                    if (notEquals != Vector256<TRight>.Zero)
                    {
                        // not exact match

                        leftValues |= loweringMask;
                        rightValues |= loweringMask;

                        if (Vector256.GreaterThanAny((leftValues - vecA) & notEquals, vecZMinusA) || leftValues != rightValues)
                        {
                            return false; // first input isn't in [A-Za-z], and not exact match of lowered
                        }
                    }
                }
            }
            else
            {
                ref TLeft currentLeftSearchSpace = ref left;
                ref TLeft oneVectorAwayFromLeftEnd = ref Unsafe.Add(ref currentLeftSearchSpace, length - TLoader.Count128);
                ref TRight currentRightSearchSpace = ref right;
                ref TRight oneVectorAwayFromRightEnd = ref Unsafe.Add(ref currentRightSearchSpace, length - (uint)Vector128<TRight>.Count);

                Vector128<TRight> leftValues;
                Vector128<TRight> rightValues;

                Vector128<TRight> loweringMask = Vector128.Create(TRight.CreateTruncating(0x20));
                Vector128<TRight> vecA = Vector128.Create(TRight.CreateTruncating('a'));
                Vector128<TRight> vecZMinusA = Vector128.Create(TRight.CreateTruncating(('z' - 'a')));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    // it's OK to widen the bytes, it's NOT OK to narrow the chars (we could loose some information)
                    leftValues = TLoader.Load128(ref currentLeftSearchSpace);
                    rightValues = Vector128.LoadUnsafe(ref currentRightSearchSpace);

                    if (!AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }

                    Vector128<TRight> notEquals = ~Vector128.Equals(leftValues, rightValues);

                    if (notEquals != Vector128<TRight>.Zero)
                    {
                        // not exact match

                        leftValues |= loweringMask;
                        rightValues |= loweringMask;

                        if (Vector128.GreaterThanAny((leftValues - vecA) & notEquals, vecZMinusA) || leftValues != rightValues)
                        {
                            return false; // first input isn't in [A-Za-z], and not exact match of lowered
                        }
                    }

                    currentRightSearchSpace = ref Unsafe.Add(ref currentRightSearchSpace, (uint)Vector128<TRight>.Count);
                    currentLeftSearchSpace = ref Unsafe.Add(ref currentLeftSearchSpace, TLoader.Count128);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentRightSearchSpace, ref oneVectorAwayFromRightEnd));

                // If any elements remain, process the last vector in the search space.
                if (length % (uint)Vector128<TRight>.Count != 0)
                {
                    leftValues = TLoader.Load128(ref oneVectorAwayFromLeftEnd);
                    rightValues = Vector128.LoadUnsafe(ref oneVectorAwayFromRightEnd);

                    if (!AllCharsInVectorAreAscii(leftValues | rightValues))
                    {
                        return false;
                    }

                    Vector128<TRight> notEquals = ~Vector128.Equals(leftValues, rightValues);

                    if (notEquals != Vector128<TRight>.Zero)
                    {
                        // not exact match

                        leftValues |= loweringMask;
                        rightValues |= loweringMask;

                        if (Vector128.GreaterThanAny((leftValues - vecA) & notEquals, vecZMinusA) || leftValues != rightValues)
                        {
                            return false; // first input isn't in [A-Za-z], and not exact match of lowered
                        }
                    }
                }
            }

            return true;
        }

        private interface ILoader<TLeft, TRight>
            where TLeft : unmanaged, INumberBase<TLeft>
            where TRight : unmanaged, INumberBase<TRight>
        {
            static abstract nuint Count128 { get; }
            static abstract nuint Count256 { get; }
            static abstract nuint Count512 { get; }
            static abstract Vector128<TRight> Load128(ref TLeft ptr);
            static abstract Vector256<TRight> Load256(ref TLeft ptr);
            static abstract Vector512<TRight> Load512(ref TLeft ptr);
            static abstract bool EqualAndAscii256(ref TLeft left, ref TRight right);
            static abstract bool EqualAndAscii512(ref TLeft left, ref TRight right);
        }

        private readonly struct PlainLoader<T> : ILoader<T, T> where T : unmanaged, INumberBase<T>
        {
            public static nuint Count128 => (uint)Vector128<T>.Count;
            public static nuint Count256 => (uint)Vector256<T>.Count;
            public static nuint Count512 => (uint)Vector512<T>.Count;
            public static Vector128<T> Load128(ref T ptr) => Vector128.LoadUnsafe(ref ptr);
            public static Vector256<T> Load256(ref T ptr) => Vector256.LoadUnsafe(ref ptr);
            public static Vector512<T> Load512(ref T ptr) => Vector512.LoadUnsafe(ref ptr);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx))]
            public static bool EqualAndAscii256(ref T left, ref T right)
            {
                Vector256<T> leftValues = Vector256.LoadUnsafe(ref left);
                Vector256<T> rightValues = Vector256.LoadUnsafe(ref right);

                if (leftValues != rightValues || !AllCharsInVectorAreAscii(leftValues))
                {
                    return false;
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool EqualAndAscii512(ref T left, ref T right)
            {
                Vector512<T> leftValues = Vector512.LoadUnsafe(ref left);
                Vector512<T> rightValues = Vector512.LoadUnsafe(ref right);

                if (leftValues != rightValues || !AllCharsInVectorAreAscii(leftValues))
                {
                    return false;
                }

                return true;
            }
        }

        private readonly struct WideningLoader : ILoader<byte, ushort>
        {
            public static nuint Count128 => sizeof(long);
            public static nuint Count256 => (uint)Vector128<byte>.Count;
            public static nuint Count512 => (uint)Vector256<byte>.Count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector128<ushort> Load128(ref byte ptr)
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector256<ushort> Load256(ref byte ptr)
            {
                (Vector128<ushort> lower, Vector128<ushort> upper) = Vector128.Widen(Vector128.LoadUnsafe(ref ptr));
                return Vector256.Create(lower, upper);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector512<ushort> Load512(ref byte ptr)
            {
                return Vector512.WidenLower(Vector256.LoadUnsafe(ref ptr).ToVector512());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [CompExactlyDependsOn(typeof(Avx))]
            public static bool EqualAndAscii256(ref byte utf8, ref ushort utf16)
            {
                // We widen the utf8 param so we can compare it to utf16, this doubles how much of the utf16 vector we search
                Debug.Assert(Vector256<byte>.Count == Vector256<ushort>.Count * 2);

                Vector256<byte> leftNotWidened = Vector256.LoadUnsafe(ref utf8);
                if (!AllCharsInVectorAreAscii(leftNotWidened))
                {
                    return false;
                }

                (Vector256<ushort> leftLower, Vector256<ushort> leftUpper) = Vector256.Widen(leftNotWidened);
                Vector256<ushort> right = Vector256.LoadUnsafe(ref utf16);
                Vector256<ushort> rightNext = Vector256.LoadUnsafe(ref utf16, (uint)Vector256<ushort>.Count);

                // A branchless version of "leftLower != right || leftUpper != rightNext"
                if (((leftLower ^ right) | (leftUpper ^ rightNext)) != Vector256<ushort>.Zero)
                {
                    return false;
                }

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool EqualAndAscii512(ref byte utf8, ref ushort utf16)
            {
                // We widen the utf8 param so we can compare it to utf16, this doubles how much of the utf16 vector we search
                Debug.Assert(Vector512<byte>.Count == Vector512<ushort>.Count * 2);

                Vector512<byte> leftNotWidened = Vector512.LoadUnsafe(ref utf8);
                if (!AllCharsInVectorAreAscii(leftNotWidened))
                {
                    return false;
                }

                (Vector512<ushort> leftLower, Vector512<ushort> leftUpper) = Vector512.Widen(leftNotWidened);
                Vector512<ushort> right = Vector512.LoadUnsafe(ref utf16);
                Vector512<ushort> rightNext = Vector512.LoadUnsafe(ref utf16, (uint)Vector512<ushort>.Count);

                // A branchless version of "leftLower != right || leftUpper != rightNext"
                if (((leftLower ^ right) | (leftUpper ^ rightNext)) != Vector512<ushort>.Zero)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
