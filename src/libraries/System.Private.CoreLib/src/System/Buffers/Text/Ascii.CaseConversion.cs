// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
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
            => ChangeCase<char, char, ToUpperConversion>(source, destination, out charsConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten)
            => ChangeCase<byte, char, ToUpperConversion>(source, destination, out bytesConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
            => ChangeCase<char, byte, ToUpperConversion>(source, destination, out charsConsumed, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => ChangeCase<byte, byte, ToLowerConversion>(source, destination, out bytesConsumed, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten)
            => ChangeCase<char, char, ToLowerConversion>(source, destination, out charsConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten)
            => ChangeCase<byte, char, ToLowerConversion>(source, destination, out bytesConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
            => ChangeCase<char, byte, ToLowerConversion>(source, destination, out charsConsumed, out bytesWritten);

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

            // Attempt to process 128 input bits.

            if (Vector128.IsHardwareAccelerated && elementCount >= (nuint)(16 / sizeof(TFrom)))
            {
                Vector128<TFrom> srcVector = Vector128.LoadUnsafe(ref *pSrc);

                // First, check for non-ASCII data. If we see any, immediately
                // exit the vectorized logic and fall back to the slower drain paths.

                if (VectorContainsAnyNonAsciiData(srcVector))
                {
                    goto Drain64;
                }

                // Now find matching characters and perform case conversion.

                Vector128<TFrom> searchValuesLowerExclusive = Vector128.Create(TFrom.CreateTruncating(ConversionIsToUpper ? '`' : '@')); // just before 'a' and 'A'
                Vector128<TFrom> searchValuesUpperExclusive = Vector128.Create(TFrom.CreateTruncating(ConversionIsToUpper ? '{' : '[')); // just after 'z' and 'Z'
                Vector128<TFrom> caseConversionVector = Vector128.Create(TFrom.CreateTruncating(0x20)); // works both directions

                Vector128<TFrom> matches = Vector128.LessThan(srcVector, searchValuesUpperExclusive)
                    & Vector128.LessThan(searchValuesLowerExclusive, srcVector);
                srcVector ^= (matches & caseConversionVector);

                // Now narrow or widen the vector as needed and write to the destination.

                if (ConversionIsNarrowing)
                {
                    Vector128<ushort> wide = srcVector.AsUInt16();
                    Vector128<byte> narrow = Vector128.Narrow(wide, wide);
                    Unsafe.WriteUnaligned<ulong>(pDest, narrow.AsUInt64().ToScalar());
                }
                else if (ConversionIsWidening)
                {
                    Vector128<byte> narrow = srcVector.AsByte();
                    Vector128.WidenLower(narrow).StoreUnsafe(ref *(ushort*)pDest);
                    Vector128.WidenUpper(narrow).StoreUnsafe(ref *(ushort*)pDest, 8);
                }
                else
                {
                    srcVector.As<TFrom, TTo>().StoreUnsafe(ref *pDest);
                }
            }


        Drain64:

            // Attempt to process 64 input bits.

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

            // Attempt to process 32 input bits.

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
