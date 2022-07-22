// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Unicode;

namespace System.Buffers.Text
{
    public static partial class Ascii
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => ChangeCase<byte, byte, ToUpperConversion>(source, destination, out bytesConsumed, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten)
            => ChangeCase<ushort, ushort, ToUpperConversion>(MemoryMarshal.Cast<char, ushort>(source), MemoryMarshal.Cast<char, ushort>(destination), out charsConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten)
            => ChangeCase<byte, ushort, ToUpperConversion>(source, MemoryMarshal.Cast<char, ushort>(destination), out bytesConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
            => ChangeCase<ushort, byte, ToUpperConversion>(MemoryMarshal.Cast<char, ushort>(source), destination, out charsConsumed, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => ChangeCase<byte, byte, ToLowerConversion>(source, destination, out bytesConsumed, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten)
            => ChangeCase<ushort, ushort, ToLowerConversion>(MemoryMarshal.Cast<char, ushort>(source), MemoryMarshal.Cast<char, ushort>(destination), out charsConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten)
            => ChangeCase<byte, ushort, ToLowerConversion>(source, MemoryMarshal.Cast<char, ushort>(destination), out bytesConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
            => ChangeCase<ushort, byte, ToLowerConversion>(MemoryMarshal.Cast<char, ushort>(source), destination, out charsConsumed, out bytesWritten);

        private static unsafe OperationStatus ChangeCase<TFrom, TTo, TCasing>(ReadOnlySpan<TFrom> source, Span<TTo> destination, out int sourceElementsConsumed, out int destinationElementsWritten)
            where TFrom : unmanaged, IBinaryInteger<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
            where TCasing : struct
        {
            if (typeof(TFrom) == typeof(TTo) && source.Overlaps(MemoryMarshal.Cast<TTo, TFrom>(destination)))
            {
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
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

                sourceElementsConsumed = (int)numElementsActuallyConverted;
                destinationElementsWritten = (int)numElementsActuallyConverted;
                return (numElementsToConvert == numElementsActuallyConverted) ? statusToReturnOnSuccess : OperationStatus.InvalidData;
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

            bool SourceIsAscii = (typeof(TFrom) == typeof(byte)); // JIT turns this into a const
            bool DestIsAscii = (typeof(TTo) == typeof(byte)); // JIT turns this into a const
            bool ConversionIsWidening = SourceIsAscii && !DestIsAscii; // JIT turns this into a const
            bool ConversionIsNarrowing = !SourceIsAscii && DestIsAscii; // JIT turns this into a const
            bool ConversionIsWidthPreserving = typeof(TFrom) == typeof(TTo); // JIT turns this into a const
            bool ConversionIsToUpper = (typeof(TCasing) == typeof(ToUpperConversion)); // JIT turns this into a const

            // Is there enough data to perform vectorized operations?

            nuint i = 0;

            // The only situation we can't easily optimize is non-hardware-accelerated
            // widening or narrowing. In this case, fall back to a naive element-by-element
            // loop.

            if (!ConversionIsWidthPreserving && Vector128.IsHardwareAccelerated)
            {
                goto DrainRemaining;
            }

            // Attempt to process blocks of 128 input bits.

            if (Vector128.IsHardwareAccelerated && elementCount >= (nuint)(16 / sizeof(TFrom)))
            {
                // The first iteration of this loop will be unaligned.

                Vector128<TFrom> srcVector = Vector128.LoadUnsafe(ref *pSrc, i);

                // First, check for non-ASCII data. If we see any, immediately
                // exit the vectorized logic and fall back to the slower drain paths.

                if (VectorContainsAnyNonAsciiData(srcVector))
                {
                    goto Drain64;
                }

                // Now find matching characters and perform case conversion.
                // Basically, the (A <= value && value <= Z) check is converted to:
                // (value - CONST) <= (Z - A), but using signed instead of unsigned arithmetic.

                Vector128<TFrom> subtractionVector = Vector128.Create(TFrom.CreateTruncating((ConversionIsToUpper ? 'a' : 'A') + 0x80));
                Vector128<TFrom> comparisionVector = Vector128.Create(TFrom.CreateTruncating(26 /* a..z or A..Z */ - 0x80));
                Vector128<TFrom> caseConversionVector = Vector128.Create(TFrom.CreateTruncating(0x20)); // works both directions

                Vector128<TFrom> matches = SignedLessThan((srcVector - subtractionVector), comparisionVector);
                srcVector ^= (matches & caseConversionVector);

                // Now narrow or widen the vector as needed and write to the destination.

                if (ConversionIsNarrowing)
                {
                    Narrow16To8AndAndWriteTo(srcVector.AsUInt16(), (byte*)pDest, 0);
                }
                else if (ConversionIsWidening)
                {
                    Widen8To16AndAndWriteTo(srcVector.AsByte(), (char*)pDest, 0);
                }
                else
                {
                    srcVector.As<TFrom, TTo>().StoreUnsafe(ref *pDest);
                }

                // Now that the first conversion is out of the way, calculate how
                // many elements we should skip in order to have future writes be
                // aligned.

                uint expectedWriteAlignment = ConversionIsNarrowing ? 8u : 16u; // JIT turns this into a const
                i = expectedWriteAlignment - ((uint)pDest & (expectedWriteAlignment - 1)) / (uint)sizeof(TTo);
                Debug.Assert((nuint)(&pDest[i]) % expectedWriteAlignment == 0, "Destination buffer wasn't properly aligned!");

                // Future iterations of this loop will be aligned.

                for (; (elementCount - i) >= (nuint)(16 / sizeof(TFrom)); i += (nuint)(16 / sizeof(TFrom)))
                {
                    // Unaligned read & check for non-ASCII data.

                    srcVector = Vector128.LoadUnsafe(ref *pSrc, i);
                    if (VectorContainsAnyNonAsciiData(srcVector))
                    {
                        goto Drain64;
                    }

                    // Now find matching characters and perform case conversion.

                    matches = SignedLessThan((srcVector - subtractionVector), comparisionVector);
                    srcVector ^= (matches & caseConversionVector);

                    // Now narrow or widen the vector as needed and write to the destination.
                    // We expect this write to be aligned.

                    if (ConversionIsNarrowing)
                    {
                        Narrow16To8AndAndWriteTo(srcVector.AsUInt16(), (byte*)pDest, i);
                    }
                    else if (ConversionIsWidening)
                    {
                        Widen8To16AndAndWriteTo(srcVector.AsByte(), (char*)pDest, i);
                    }
                    else
                    {
                        srcVector.As<TFrom, TTo>().StoreUnsafe(ref *pDest, i);
                    }
                }
            }

        Drain64:

            // Attempt to process blocks of 64 input bits.

            if (IntPtr.Size >= 8 && (elementCount - i) >= (nuint)(8 / sizeof(TFrom)))
            {
                ulong nextBlockAsUInt64 = Unsafe.ReadUnaligned<ulong>(&pSrc[i]);
                if (SourceIsAscii)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (!Utf16Utility.AllCharsInUInt64AreAscii(nextBlockAsUInt64))
                    {
                        goto Drain32;
                    }
                    nextBlockAsUInt64 = (ConversionIsToUpper)
                        ? Utf16Utility.ConvertAllAsciiCharsInUInt64ToUppercase(nextBlockAsUInt64)
                        : throw new NotImplementedException();
                }

                if (ConversionIsWidthPreserving)
                {
                    Unsafe.WriteUnaligned<ulong>(&pDest[i], nextBlockAsUInt64);
                }
                else
                {
                    Debug.Assert(Vector128.IsHardwareAccelerated);

                    Vector128<ulong> blockAsVectorOfUInt64 = Vector128.CreateScalarUnsafe(nextBlockAsUInt64);
                    if (ConversionIsWidening)
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
                if (SourceIsAscii)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (!Utf16Utility.AllCharsInUInt32AreAscii(nextBlockAsUInt32))
                    {
                        goto DrainRemaining;
                    }
                    nextBlockAsUInt32 = (ConversionIsToUpper)
                        ? Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(nextBlockAsUInt32)
                        : Utf16Utility.ConvertAllAsciiCharsInUInt32ToLowercase(nextBlockAsUInt32);
                }

                if (ConversionIsWidthPreserving)
                {
                    Unsafe.WriteUnaligned<uint>(&pDest[i], nextBlockAsUInt32);
                }
                else
                {
                    Debug.Assert(Vector128.IsHardwareAccelerated);

                    Vector128<uint> blockAsVectorOfUInt32 = Vector128.CreateScalarUnsafe(nextBlockAsUInt32);
                    if (ConversionIsWidening)
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
                if (!UnicodeUtility.IsAsciiCodePoint(element)) { break; }
                if (ConversionIsToUpper)
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

            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool VectorContainsAnyNonAsciiData<T>(Vector128<T> vector)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
                if (vector.ExtractMostSignificantBits() != 0) { return true; }
            }
            else if (typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
            {
                if (ASCIIUtility.VectorContainsNonAsciiChar(vector.AsUInt16())) { return true; }
            }
            else
            {
                Debug.Fail("Unknown types provided.");
                throw new NotSupportedException();
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Widen8To16AndAndWriteTo(Vector128<byte> narrowVector, char* pDest, nuint destOffset)
        {
            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<ushort> wide = Vector256.WidenLower(narrowVector.ToVector256Unsafe());
                wide.StoreUnsafe(ref *(ushort*)pDest, destOffset);
            }
            else
            {
                Vector128.WidenLower(narrowVector).StoreUnsafe(ref *(ushort*)pDest, destOffset);
                Vector128.WidenUpper(narrowVector).StoreUnsafe(ref *(ushort*)pDest, destOffset + 8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void Narrow16To8AndAndWriteTo(Vector128<ushort> wideVector, byte* pDest, nuint destOffset)
        {
            Vector128<byte> narrow = Vector128.Narrow(wideVector, wideVector);

            if (Sse2.IsSupported)
            {
                // MOVQ is supported even on x86, unaligned accesses allowed
                Sse2.StoreScalar((ulong*)(pDest + destOffset), narrow.AsUInt64());
            }
            else if (Vector64.IsHardwareAccelerated)
            {
                narrow.GetLower().StoreUnsafe(ref *pDest, destOffset);
            }
            else
            {
                Unsafe.WriteUnaligned<ulong>(pDest + destOffset, narrow.AsUInt64().ToScalar());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe Vector128<T> SignedLessThan<T>(Vector128<T> left, Vector128<T> right)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte))
            {
                return Vector128.LessThan(left.AsSByte(), right.AsSByte()).As<sbyte, T>();
            }
            else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(short))
            {
                return Vector128.LessThan(left.AsInt16(), right.AsInt16()).As<short, T>();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<TTo> NarrowOrWidenLowerVector<TFrom, TTo>(Vector128<TFrom> vector)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            if (typeof(TFrom) == typeof(byte) && typeof(TTo) == typeof(ushort))
            {
                return Vector128.WidenLower(vector.AsByte()).As<ushort, TTo>();
            }
            else if (typeof(TFrom) == typeof(ushort) && typeof(TTo) == typeof(byte))
            {
                return Vector128.Narrow(vector.AsUInt16(), vector.AsUInt16()).As<byte, TTo>();
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
