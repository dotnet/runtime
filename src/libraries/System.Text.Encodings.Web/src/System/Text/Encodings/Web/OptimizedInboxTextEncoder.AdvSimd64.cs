// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace System.Text.Encodings.Web
{
    internal sealed partial class OptimizedInboxTextEncoder
    {
        private unsafe nuint GetIndexOfFirstByteToEncodeAdvSimd64(byte* pData, nuint lengthInBytes)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);
            Debug.Assert(BitConverter.IsLittleEndian);

            Vector128<byte> vec0xF = Vector128.Create((byte)0xF);
            Vector128<byte> vecPowersOfTwo = Vector128.Create(1, 2, 4, 8, 16, 32, 64, 128, 0, 0, 0, 0, 0, 0, 0, 0);
            Vector128<byte> vecPairwiseAddNibbleBitmask = Vector128.Create((ushort)0xF00F).AsByte(); // little endian only
            Vector128<byte> allowedCodePoints = _allowedAsciiCodePoints.AsVector;
            ulong resultScalar;

            nuint i = 0;
            if (lengthInBytes >= 16)
            {
                nuint lastLegalIterationFor16CharRead = lengthInBytes & unchecked((nuint)(nint)~0xF);

                do
                {
                    // Read 16 bytes at a time into a single 128-bit vector.

                    Vector128<byte> packed = AdvSimd.LoadVector128(pData + i); // unaligned read

                    // Each element of the packed vector corresponds to a byte of untrusted source data. It will
                    // have the format [ ..., 0xYZ, ... ]. We use the low nibble of each byte to index into
                    // the 'allowedCodePoints' vector, and we use the high nibble of each byte to select a bit
                    // from the corresponding element in the 'allowedCodePoints' vector.
                    //
                    // Example: let packed := [ ..., 0x6D ('m'), ... ]
                    // The final 'result' vector will contain a non-zero value in the corresponding space iff the
                    // 0xD element in the 'allowedCodePoints' vector has its 1 << 0x6 bit set.
                    //
                    // We rely on the fact that when we perform an arithmetic shift of vector values to get the
                    // high nibble into the low 4 bits, we'll smear the high (non-ASCII) bit, causing the vector
                    // element value to be in the range [ 128..255 ]. This causes the tbl lookup to return 0x00
                    // for that particular element in the 'vecPowersOfTwoShuffled' vector, meaning that escaping is required.

                    var allowedCodePointsShuffled = AdvSimd.Arm64.VectorTableLookup(allowedCodePoints, AdvSimd.And(packed, vec0xF));
                    var vecPowersOfTwoShuffled = AdvSimd.Arm64.VectorTableLookup(vecPowersOfTwo, AdvSimd.ShiftRightArithmetic(packed.AsSByte(), 4).AsByte());
                    var result = AdvSimd.CompareTest(allowedCodePointsShuffled, vecPowersOfTwoShuffled);

                    // Now, each element of 'result' contains 0xFF if the corresponding element in 'packed' is allowed;
                    // and it contains a zero value if the corresponding element in 'packed' is disallowed. We'll convert
                    // this into a vector where if 0xFF occurs in an even-numbered index, it gets converted to 0x0F; and
                    // if 0xFF occurs in an odd-numbered index, it gets converted to 0xF0. This allows us to collapse
                    // the Vector128<byte> to a 64-bit unsigned integer, where each of the 16 nibbles in the 64-bit integer
                    // corresponds to whether an element in the 'result' vector was originally 0xFF or 0x00.

                    var maskedResult = AdvSimd.And(result, vecPairwiseAddNibbleBitmask);
                    resultScalar = AdvSimd.Arm64.AddPairwise(maskedResult, maskedResult).AsUInt64().ToScalar();

                    if (resultScalar != ulong.MaxValue)
                    {
                        goto PairwiseAddMaskContainsDataWhichRequiresEscaping;
                    }
                } while ((i += 16) < lastLegalIterationFor16CharRead);
            }

            if ((lengthInBytes & 8) != 0)
            {
                // Read 8 bytes at a time into a single 64-bit vector, extended to 128 bits.
                // Same logic as the 16-byte case, but we don't need to worry about the pairwise add step.
                // We'll treat the low 64 bits of the 'result' vector as its own scalar element.

                Vector128<byte> packed = AdvSimd.LoadVector64(pData + i).ToVector128Unsafe(); // unaligned read
                var allowedCodePointsShuffled = AdvSimd.Arm64.VectorTableLookup(allowedCodePoints, AdvSimd.And(packed, vec0xF));
                var vecPowersOfTwoShuffled = AdvSimd.Arm64.VectorTableLookup(vecPowersOfTwo, AdvSimd.ShiftRightArithmetic(packed.AsSByte(), 4).AsByte());
                var result = AdvSimd.CompareTest(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                resultScalar = result.AsUInt64().ToScalar();

                if (resultScalar != ulong.MaxValue)
                {
                    goto MaskContainsDataWhichRequiresEscaping;
                }

                i += 8;
            }

            if ((lengthInBytes & 4) != 0)
            {
                // Read 4 bytes at a time into a single element, extended to a 128-bit vector.
                // Same logic as the 16-byte case, but we don't need to worry about the pairwise add step.
                // We'll treat the low 32 bits of the 'result' vector as its own scalar element.

                Vector128<byte> packed = Vector128.CreateScalarUnsafe(Unsafe.ReadUnaligned<uint>(pData + i)).AsByte();
                var allowedCodePointsShuffled = AdvSimd.Arm64.VectorTableLookup(allowedCodePoints, AdvSimd.And(packed, vec0xF));
                var vecPowersOfTwoShuffled = AdvSimd.Arm64.VectorTableLookup(vecPowersOfTwo, AdvSimd.ShiftRightArithmetic(packed.AsSByte(), 4).AsByte());
                var result = AdvSimd.CompareTest(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                resultScalar = result.AsUInt32().ToScalar(); // n.b. implicit conversion uint -> ulong; high 32 bits will be zeroed

                if (resultScalar != uint.MaxValue)
                {
                    goto MaskContainsDataWhichRequiresEscaping;
                }

                i += 4;
            }

            // Beyond this point, vectorization isn't worthwhile. Just do a normal loop.

            if ((lengthInBytes & 3) != 0)
            {
                Debug.Assert(lengthInBytes - i <= 3);

                do
                {
                    if (!_allowedAsciiCodePoints.IsAllowedAsciiCodePoint(pData[i])) { break; }
                } while (++i != lengthInBytes);
            }

        Return:

            return i;

        PairwiseAddMaskContainsDataWhichRequiresEscaping:

            Debug.Assert(resultScalar != ulong.MaxValue);
            // Each nibble is 4 (1 << 2) bits, so we shr by 2 to account for per-nibble stride.
            i += (uint)BitOperations.TrailingZeroCount(~resultScalar) >> 2; // location of lowest set bit is where we must begin escaping
            goto Return;

        MaskContainsDataWhichRequiresEscaping:

            Debug.Assert(resultScalar != ulong.MaxValue);
            // Each byte is 8 (1 << 3) bits, so we shr by 3 to account for per-byte stride.
            i += (uint)BitOperations.TrailingZeroCount(~resultScalar) >> 3; // location of lowest set bit is where we must begin escaping
            goto Return;
        }

        private unsafe nuint GetIndexOfFirstCharToEncodeAdvSimd64(char* pData, nuint lengthInChars)
        {
            // See GetIndexOfFirstByteToEncodeAdvSimd64 for the central logic behind this method.
            // The main difference here is that we need to pack WORDs to BYTEs before performing
            // the main vectorized logic. It doesn't matter if we use signed or unsigned saturation
            // while packing, as saturation will convert out-of-range (non-ASCII char) WORDs to
            // 0x00 or 0x7F..0xFF, all of which are forbidden by the encoder.

            Debug.Assert(AdvSimd.Arm64.IsSupported);
            Debug.Assert(BitConverter.IsLittleEndian);

            Vector128<byte> vec0xF = Vector128.Create((byte)0xF);
            Vector128<byte> vecPowersOfTwo = Vector128.Create(1, 2, 4, 8, 16, 32, 64, 128, 0, 0, 0, 0, 0, 0, 0, 0);
            Vector128<byte> vecPairwiseAddNibbleBitmask = Vector128.Create((ushort)0xF00F).AsByte(); // little endian only
            Vector128<byte> allowedCodePoints = _allowedAsciiCodePoints.AsVector;
            ulong resultScalar;

            nuint i = 0;
            if (lengthInChars >= 16)
            {
                nuint lastLegalIterationFor16CharRead = lengthInChars & unchecked((nuint)(nint)~0xF);

                do
                {
                    // Read 16 chars at a time into 2x 128-bit vectors, then pack into a single 128-bit vector.
                    // We turn 16 chars (256 bits) into 16 nibbles (64 bits) during this process.

                    Vector128<byte> packed = AdvSimd.ExtractNarrowingSaturateUnsignedUpper(
                        AdvSimd.ExtractNarrowingSaturateUnsignedLower(AdvSimd.LoadVector128((/* unaligned */ short*)(pData + i))),
                        AdvSimd.LoadVector128((/* unaligned */ short*)(pData + 8 + i)));
                    var allowedCodePointsShuffled = AdvSimd.Arm64.VectorTableLookup(allowedCodePoints, AdvSimd.And(packed, vec0xF));
                    var vecPowersOfTwoShuffled = AdvSimd.Arm64.VectorTableLookup(vecPowersOfTwo, AdvSimd.ShiftRightArithmetic(packed.AsSByte(), 4).AsByte());
                    var result = AdvSimd.CompareTest(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                    var maskedResult = AdvSimd.And(result, vecPairwiseAddNibbleBitmask);
                    resultScalar = AdvSimd.Arm64.AddPairwise(maskedResult, maskedResult).AsUInt64().ToScalar();

                    if (resultScalar != ulong.MaxValue)
                    {
                        goto PairwiseAddMaskContainsDataWhichRequiresEscaping;
                    }
                } while ((i += 16) < lastLegalIterationFor16CharRead);
            }

            if ((lengthInChars & 8) != 0)
            {
                // Read 8 chars at a time into a single 128-bit vector, then pack into a 64-bit
                // vector, then extend to 128 bits. We turn 8 chars (128 bits) into 8 bytes (64 bits)
                // during this process. Only the low 64 bits of the 'result' vector have meaningful
                // data.

                Vector128<byte> packed = AdvSimd.ExtractNarrowingSaturateUnsignedLower(AdvSimd.LoadVector128((/* unaligned */ short*)(pData + i))).AsByte().ToVector128Unsafe();
                var allowedCodePointsShuffled = AdvSimd.Arm64.VectorTableLookup(allowedCodePoints, AdvSimd.And(packed, vec0xF));
                var vecPowersOfTwoShuffled = AdvSimd.Arm64.VectorTableLookup(vecPowersOfTwo, AdvSimd.ShiftRightArithmetic(packed.AsSByte(), 4).AsByte());
                var result = AdvSimd.CompareTest(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                resultScalar = result.AsUInt64().ToScalar();

                if (resultScalar != ulong.MaxValue)
                {
                    goto MaskContainsDataWhichRequiresEscaping;
                }

                i += 8;
            }

            if ((lengthInChars & 4) != 0)
            {
                // Read 4 chars at a time into a single 64-bit vector, then pack into the low 32 bits
                // of a 128-bit vector. We turn 4 chars (64 bits) into 4 bytes (32 bits) during this
                // process. Only the low 32 bits of the 'result' vector have meaningful data.

                Vector128<byte> packed = AdvSimd.ExtractNarrowingSaturateUnsignedLower(AdvSimd.LoadVector64((/* unaligned */ short*)(pData + i)).ToVector128Unsafe()).ToVector128Unsafe();
                var allowedCodePointsShuffled = AdvSimd.Arm64.VectorTableLookup(allowedCodePoints, AdvSimd.And(packed, vec0xF));
                var vecPowersOfTwoShuffled = AdvSimd.Arm64.VectorTableLookup(vecPowersOfTwo, AdvSimd.ShiftRightArithmetic(packed.AsSByte(), 4).AsByte());
                var result = AdvSimd.CompareTest(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                resultScalar = result.AsUInt32().ToScalar(); // n.b. implicit conversion uint -> ulong; high 32 bits will be zeroed

                if (resultScalar != uint.MaxValue)
                {
                    goto MaskContainsDataWhichRequiresEscaping;
                }

                i += 4;
            }

            // Beyond this point, vectorization isn't worthwhile. Just do a normal loop.

            if ((lengthInChars & 3) != 0)
            {
                Debug.Assert(lengthInChars - i <= 3);

                do
                {
                    if (!_allowedAsciiCodePoints.IsAllowedAsciiCodePoint(pData[i])) { break; }
                } while (++i != lengthInChars);
            }

        Return:

            return i;

        PairwiseAddMaskContainsDataWhichRequiresEscaping:

            Debug.Assert(resultScalar != ulong.MaxValue);
            // Each nibble is 4 (1 << 2) bits, so we shr by 2 to account for per-nibble stride.
            i += (uint)BitOperations.TrailingZeroCount(~resultScalar) >> 2; // location of lowest set bit is where we must begin escaping
            goto Return;

        MaskContainsDataWhichRequiresEscaping:

            Debug.Assert(resultScalar != ulong.MaxValue);
            // Each byte is 8 (1 << 3) bits, so we shr by 3 to account for per-byte stride.
            i += (uint)BitOperations.TrailingZeroCount(~resultScalar) >> 3; // location of lowest set bit is where we must begin escaping
            goto Return;
        }
    }
}
