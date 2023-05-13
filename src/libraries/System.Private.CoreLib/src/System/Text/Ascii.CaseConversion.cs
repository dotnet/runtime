// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Unicode;

namespace System.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// ASCII letters to uppercase during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which uppercase text is written.</param>
        /// <param name="bytesWritten">The number of bytes actually written to <paramref name="destination"/>. It's the same as the number of bytes actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        /// <remarks>In-place conversion is prohibited, please use <see cref="ToUpperInPlace(Span{byte}, out int)"/> for that.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            => ChangeCase<byte, byte, ToUpperConversion>(source, destination, out bytesWritten);

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// ASCII letters to uppercase during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which uppercase text is written.</param>
        /// <param name="charsWritten">The number of characters actually written to <paramref name="destination"/>. It's the same as the number of characters actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        /// <remarks>In-place conversion is prohibited, please use <see cref="ToUpperInPlace(Span{char}, out int)"/> for that.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten)
            => ChangeCase<ushort, ushort, ToUpperConversion>(MemoryMarshal.Cast<char, ushort>(source), MemoryMarshal.Cast<char, ushort>(destination), out charsWritten);

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// ASCII letters to uppercase during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which uppercase text is written.</param>
        /// <param name="charsWritten">The number of characters actually written to <paramref name="destination"/>. It's the same as the number of bytes actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
            => ChangeCase<byte, ushort, ToUpperConversion>(source, MemoryMarshal.Cast<char, ushort>(destination), out charsWritten);

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// ASCII letters to uppercase during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which uppercase text is written.</param>
        /// <param name="bytesWritten">The number of bytes actually written to <paramref name="destination"/>. It's the same as the number of characters actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
            => ChangeCase<ushort, byte, ToUpperConversion>(MemoryMarshal.Cast<char, ushort>(source), destination, out bytesWritten);

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// ASCII letters to lowercase during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which lowercase text is written.</param>
        /// <param name="bytesWritten">The number of bytes actually written to <paramref name="destination"/>. It's the same as the number of bytes actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        /// <remarks>In-place conversion is prohibited, please use <see cref="ToLowerInPlace(Span{byte}, out int)"/> for that.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
            => ChangeCase<byte, byte, ToLowerConversion>(source, destination, out bytesWritten);

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// ASCII letters to lowercase during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which lowercase text is written.</param>
        /// <param name="charsWritten">The number of characters actually written to <paramref name="destination"/>. It's the same as the number of characters actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        /// <remarks>In-place conversion is prohibited, please use <see cref="ToLowerInPlace(Span{char}, out int)"/> for that.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten)
            => ChangeCase<ushort, ushort, ToLowerConversion>(MemoryMarshal.Cast<char, ushort>(source), MemoryMarshal.Cast<char, ushort>(destination), out charsWritten);

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// ASCII letters to lowercase during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which lowercase text is written.</param>
        /// <param name="charsWritten">The number of characters actually written to <paramref name="destination"/>. It's the same as the number of bytes actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
            => ChangeCase<byte, ushort, ToLowerConversion>(source, MemoryMarshal.Cast<char, ushort>(destination), out charsWritten);

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// ASCII letters to lowercase during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which lowercase text is written.</param>
        /// <param name="bytesWritten">The number of bytes actually written to <paramref name="destination"/>. It's the same as the number of characters actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
            => ChangeCase<ushort, byte, ToLowerConversion>(MemoryMarshal.Cast<char, ushort>(source), destination, out bytesWritten);

        /// <summary>
        /// Performs in-place uppercase conversion.
        /// </summary>
        /// <param name="value">The ASCII text buffer.</param>
        /// <param name="bytesWritten">The number of processed bytes.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLowerInPlace(Span<byte> value, out int bytesWritten)
            => ChangeCase<byte, ToLowerConversion>(value, out bytesWritten);

        /// <summary>
        /// Performs in-place uppercase conversion.
        /// </summary>
        /// <param name="value">The ASCII text buffer.</param>
        /// <param name="charsWritten">The number of processed characters.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLowerInPlace(Span<char> value, out int charsWritten)
            => ChangeCase<ushort, ToLowerConversion>(MemoryMarshal.Cast<char, ushort>(value), out charsWritten);

        /// <summary>
        /// Performs in-place lowercase conversion.
        /// </summary>
        /// <param name="value">The ASCII text buffer.</param>
        /// <param name="bytesWritten">The number of processed bytes.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpperInPlace(Span<byte> value, out int bytesWritten)
            => ChangeCase<byte, ToUpperConversion>(value, out bytesWritten);

        /// <summary>
        /// Performs in-place lowercase conversion.
        /// </summary>
        /// <param name="value">The ASCII text buffer.</param>
        /// <param name="charsWritten">The number of processed characters.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpperInPlace(Span<char> value, out int charsWritten)
            => ChangeCase<ushort, ToUpperConversion>(MemoryMarshal.Cast<char, ushort>(value), out charsWritten);

        private static unsafe OperationStatus ChangeCase<TFrom, TTo, TCasing>(ReadOnlySpan<TFrom> source, Span<TTo> destination, out int destinationElementsWritten)
            where TFrom : unmanaged, IBinaryInteger<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
            where TCasing : struct
        {
            if (MemoryMarshal.AsBytes(source).Overlaps(MemoryMarshal.AsBytes(destination)))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_SpanOverlappedOperation);
            }

            nuint numElementsToConvert;
            OperationStatus statusToReturnOnSuccess;

            if (source.Length <= destination.Length)
            {
                numElementsToConvert = (uint)source.Length;
                statusToReturnOnSuccess = OperationStatus.Done;
            }
            else
            {
                numElementsToConvert = (uint)destination.Length;
                statusToReturnOnSuccess = OperationStatus.DestinationTooSmall;
            }

            fixed (TFrom* pSource = &MemoryMarshal.GetReference(source))
            fixed (TTo* pDestination = &MemoryMarshal.GetReference(destination))
            {
                nuint numElementsActuallyConverted = ChangeCase<TFrom, TTo, TCasing>(pSource, pDestination, numElementsToConvert);
                Debug.Assert(numElementsActuallyConverted <= numElementsToConvert);

                destinationElementsWritten = (int)numElementsActuallyConverted;
                return (numElementsToConvert == numElementsActuallyConverted) ? statusToReturnOnSuccess : OperationStatus.InvalidData;
            }
        }

        private static unsafe OperationStatus ChangeCase<T, TCasing>(Span<T> buffer, out int elementsWritten)
            where T : unmanaged, IBinaryInteger<T>
            where TCasing : struct
        {
            fixed (T* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                nuint numElementsActuallyConverted = ChangeCase<T, T, TCasing>(pBuffer, pBuffer, (nuint)buffer.Length);
                Debug.Assert(numElementsActuallyConverted <= (nuint)buffer.Length);

                elementsWritten = (int)numElementsActuallyConverted;
                return elementsWritten == buffer.Length ? OperationStatus.Done : OperationStatus.InvalidData;
            }
        }

        private static unsafe nuint ChangeCase<TFrom, TTo, TCasing>(TFrom* pSrc, TTo* pDest, nuint elementCount)
            where TFrom : unmanaged, IBinaryInteger<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
            where TCasing : struct
        {
            Debug.Assert(typeof(TFrom) == typeof(byte) || typeof(TFrom) == typeof(ushort));
            Debug.Assert(typeof(TTo) == typeof(byte) || typeof(TTo) == typeof(ushort));
            Debug.Assert(typeof(TCasing) == typeof(ToUpperConversion) || typeof(TCasing) == typeof(ToLowerConversion));

            bool sourceIsAscii = (sizeof(TFrom) == 1); // JIT turns this into a const
            bool destIsAscii = (sizeof(TTo) == 1); // JIT turns this into a const
            bool conversionIsWidening = sourceIsAscii && !destIsAscii; // JIT turns this into a const
            bool conversionIsNarrowing = !sourceIsAscii && destIsAscii; // JIT turns this into a const
            bool conversionIsWidthPreserving = typeof(TFrom) == typeof(TTo); // JIT turns this into a const
            bool conversionIsToUpper = (typeof(TCasing) == typeof(ToUpperConversion)); // JIT turns this into a const
            uint numInputElementsToConsumeEachVectorizedLoopIteration = (uint)(sizeof(Vector128<byte>) / sizeof(TFrom)); // JIT turns this into a const

            nuint i = 0;

            // The only situation we can't easily optimize is non-hardware-accelerated
            // widening or narrowing. In this case, fall back to a naive element-by-element
            // loop.

            if (!conversionIsWidthPreserving && !Vector128.IsHardwareAccelerated)
            {
                goto DrainRemaining;
            }

            // Process the input as a series of 128-bit blocks.

            if (Vector128.IsHardwareAccelerated && elementCount >= numInputElementsToConsumeEachVectorizedLoopIteration)
            {
                // Unaligned read and check for non-ASCII data.

                Vector128<TFrom> srcVector = Vector128.LoadUnsafe(ref *pSrc);
                if (VectorContainsNonAsciiChar(srcVector))
                {
                    goto Drain64;
                }

                // Now find matching characters and perform case conversion.
                // Basically, the (A <= value && value <= Z) check is converted to:
                // (value - CONST) <= (Z - A), but using signed instead of unsigned arithmetic.

                TFrom SourceSignedMinValue = TFrom.CreateTruncating(1 << (8 * sizeof(TFrom) - 1));
                Vector128<TFrom> subtractionVector = Vector128.Create(conversionIsToUpper ? (SourceSignedMinValue + TFrom.CreateTruncating('a')) : (SourceSignedMinValue + TFrom.CreateTruncating('A')));
                Vector128<TFrom> comparisionVector = Vector128.Create(SourceSignedMinValue + TFrom.CreateTruncating(26 /* A..Z or a..z */));
                Vector128<TFrom> caseConversionVector = Vector128.Create(TFrom.CreateTruncating(0x20)); // works both directions

                Vector128<TFrom> matches = SignedLessThan((srcVector - subtractionVector), comparisionVector);
                srcVector ^= (matches & caseConversionVector);

                // Now write to the destination.

                ChangeWidthAndWriteTo(srcVector, pDest, 0);

                // Now that the first conversion is out of the way, calculate how
                // many elements we should skip in order to have future writes be
                // aligned.

                uint expectedWriteAlignment = numInputElementsToConsumeEachVectorizedLoopIteration * (uint)sizeof(TTo); // JIT turns this into a const
                i = numInputElementsToConsumeEachVectorizedLoopIteration - ((uint)pDest % expectedWriteAlignment) / (uint)sizeof(TTo);
                Debug.Assert((nuint)(&pDest[i]) % expectedWriteAlignment == 0, "Destination buffer wasn't properly aligned!");

                // Future iterations of this loop will be aligned,
                // except for the last iteration.

                while (true)
                {
                    Debug.Assert(i <= elementCount, "We overran a buffer somewhere.");

                    if ((elementCount - i) < numInputElementsToConsumeEachVectorizedLoopIteration)
                    {
                        // If we're about to enter the final iteration of the loop, back up so that
                        // we can read one unaligned block. If we've already consumed all the data,
                        // jump straight to the end.

                        if (i == elementCount)
                        {
                            goto Return;
                        }

                        i = elementCount - numInputElementsToConsumeEachVectorizedLoopIteration;
                    }

                    // Unaligned read & check for non-ASCII data.

                    srcVector = Vector128.LoadUnsafe(ref *pSrc, i);
                    if (VectorContainsNonAsciiChar(srcVector))
                    {
                        goto Drain64;
                    }

                    // Now find matching characters and perform case conversion.

                    matches = SignedLessThan((srcVector - subtractionVector), comparisionVector);
                    srcVector ^= (matches & caseConversionVector);

                    // Now write to the destination.
                    // We expect this write to be aligned except for the last run through the loop.

                    ChangeWidthAndWriteTo(srcVector, pDest, i);
                    i += numInputElementsToConsumeEachVectorizedLoopIteration;
                }
            }

        Drain64:

            // Attempt to process blocks of 64 input bits.

            if (IntPtr.Size >= 8 && (elementCount - i) >= (nuint)(8 / sizeof(TFrom)))
            {
                ulong nextBlockAsUInt64 = Unsafe.ReadUnaligned<ulong>(&pSrc[i]);
                if (sourceIsAscii)
                {
                    if (!Utf8Utility.AllBytesInUInt64AreAscii(nextBlockAsUInt64))
                    {
                        goto Drain32;
                    }
                    nextBlockAsUInt64 = (conversionIsToUpper)
                        ? Utf8Utility.ConvertAllAsciiBytesInUInt64ToUppercase(nextBlockAsUInt64)
                        : Utf8Utility.ConvertAllAsciiBytesInUInt64ToLowercase(nextBlockAsUInt64);
                }
                else
                {
                    if (!Utf16Utility.AllCharsInUInt64AreAscii(nextBlockAsUInt64))
                    {
                        goto Drain32;
                    }
                    nextBlockAsUInt64 = (conversionIsToUpper)
                        ? Utf16Utility.ConvertAllAsciiCharsInUInt64ToUppercase(nextBlockAsUInt64)
                        : Utf16Utility.ConvertAllAsciiCharsInUInt64ToLowercase(nextBlockAsUInt64);
                }

                if (conversionIsWidthPreserving)
                {
                    Unsafe.WriteUnaligned<ulong>(&pDest[i], nextBlockAsUInt64);
                }
                else
                {
                    Debug.Assert(Vector128.IsHardwareAccelerated);

                    Vector128<ulong> blockAsVectorOfUInt64 = Vector128.CreateScalarUnsafe(nextBlockAsUInt64);
                    if (conversionIsWidening)
                    {
                        Vector128.StoreUnsafe(Vector128.WidenLower(blockAsVectorOfUInt64.AsByte()), ref *(ushort*)pDest, i);
                    }
                    else
                    {
                        Vector128<ushort> blockAsVectorOfUInt16 = blockAsVectorOfUInt64.AsUInt16();
                        Vector128<uint> narrowedBlock = Vector128.Narrow(blockAsVectorOfUInt16, blockAsVectorOfUInt16).AsUInt32();
                        Unsafe.WriteUnaligned<uint>(&pDest[i], narrowedBlock.ToScalar());
                    }
                }

                i += (nuint)(8 / sizeof(TFrom));

                // If vectorization is not accelerated, turn this into a while loop.

                if (!Vector128.IsHardwareAccelerated)
                {
                    goto Drain64;
                }
            }

        Drain32:

            // Attempt to process blocks of 32 input bits.

            if ((elementCount - i) >= (nuint)(4 / sizeof(TFrom)))
            {
                uint nextBlockAsUInt32 = Unsafe.ReadUnaligned<uint>(&pSrc[i]);
                if (sourceIsAscii)
                {
                    if (!Utf8Utility.AllBytesInUInt32AreAscii(nextBlockAsUInt32))
                    {
                        goto DrainRemaining;
                    }
                    nextBlockAsUInt32 = (conversionIsToUpper)
                        ? Utf8Utility.ConvertAllAsciiBytesInUInt32ToUppercase(nextBlockAsUInt32)
                        : Utf8Utility.ConvertAllAsciiBytesInUInt32ToLowercase(nextBlockAsUInt32);
                }
                else
                {
                    if (!Utf16Utility.AllCharsInUInt32AreAscii(nextBlockAsUInt32))
                    {
                        goto DrainRemaining;
                    }
                    nextBlockAsUInt32 = (conversionIsToUpper)
                        ? Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(nextBlockAsUInt32)
                        : Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(nextBlockAsUInt32);
                }

                if (conversionIsWidthPreserving)
                {
                    Unsafe.WriteUnaligned<uint>(&pDest[i], nextBlockAsUInt32);
                }
                else
                {
                    Debug.Assert(Vector128.IsHardwareAccelerated);

                    Vector128<uint> blockAsVectorOfUInt32 = Vector128.CreateScalarUnsafe(nextBlockAsUInt32);
                    if (conversionIsWidening)
                    {
                        Vector128<ulong> widenedBlock = Vector128.WidenLower(blockAsVectorOfUInt32.AsByte()).AsUInt64();
                        Unsafe.WriteUnaligned<ulong>(&pDest[i], widenedBlock.ToScalar());
                    }
                    else
                    {
                        Vector128<ushort> blockAsVectorOfUInt16 = blockAsVectorOfUInt32.AsUInt16();
                        Vector128<ushort> narrowedBlock = Vector128.Narrow(blockAsVectorOfUInt16, blockAsVectorOfUInt16).AsUInt16();
                        Unsafe.WriteUnaligned<ushort>(&pDest[i], narrowedBlock.ToScalar());
                    }
                }

                i += (nuint)(4 / sizeof(TFrom));

                // If vectorization is not accelerated or we're on 32-bit,
                // turn this into a while loop.

                if (IntPtr.Size < 8 || !Vector128.IsHardwareAccelerated)
                {
                    goto Drain32;
                }
            }

        DrainRemaining:

            // Process single elements at a time.

            for (; i < elementCount; i++)
            {
                uint element = uint.CreateTruncating(pSrc[i]);
                if (!UnicodeUtility.IsAsciiCodePoint(element))
                {
                    break;
                }

                if (conversionIsToUpper)
                {
                    if (UnicodeUtility.IsInRangeInclusive(element, 'a', 'z'))
                    {
                        element -= 0x20u; // lowercase to uppercase
                    }
                }
                else
                {
                    if (UnicodeUtility.IsInRangeInclusive(element, 'A', 'Z'))
                    {
                        element += 0x20u; // uppercase to lowercase
                    }
                }
                pDest[i] = TTo.CreateTruncating(element);
            }

        Return:

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ChangeWidthAndWriteTo<TFrom, TTo>(Vector128<TFrom> vector, TTo* pDest, nuint elementOffset)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (sizeof(TFrom) == sizeof(TTo))
            {
                // no width change needed
                Vector128.StoreUnsafe(vector.As<TFrom, TTo>(), ref *pDest, elementOffset);
            }
            else if (sizeof(TFrom) == 1 && sizeof(TTo) == 2)
            {
                // widening operation required
                if (Vector256.IsHardwareAccelerated)
                {
                    Vector256<ushort> wide = Vector256.WidenLower(vector.AsByte().ToVector256Unsafe());
                    Vector256.StoreUnsafe(wide, ref *(ushort*)pDest, elementOffset);
                }
                else
                {
                    Vector128.StoreUnsafe(Vector128.WidenLower(vector.AsByte()), ref *(ushort*)pDest, elementOffset);
                    Vector128.StoreUnsafe(Vector128.WidenUpper(vector.AsByte()), ref *(ushort*)pDest, elementOffset + 8);
                }
            }
            else if (sizeof(TFrom) == 2 && sizeof(TTo) == 1)
            {
                // narrowing operation required, we know data is all-ASCII so use extract helper
                Vector128<byte> narrow = ExtractAsciiVector(vector.AsUInt16(), vector.AsUInt16());
                narrow.StoreLowerUnsafe(ref *(byte*)pDest, elementOffset);
            }
            else
            {
                Debug.Fail("Unknown types.");
                throw new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector128<T> SignedLessThan<T>(Vector128<T> left, Vector128<T> right)
            where T : unmanaged
        {
            if (sizeof(T) == 1)
            {
                return Vector128.LessThan(left.AsSByte(), right.AsSByte()).As<sbyte, T>();
            }
            else if (sizeof(T) == 2)
            {
                return Vector128.LessThan(left.AsInt16(), right.AsInt16()).As<short, T>();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private struct ToUpperConversion { }
        private struct ToLowerConversion { }
    }
}
