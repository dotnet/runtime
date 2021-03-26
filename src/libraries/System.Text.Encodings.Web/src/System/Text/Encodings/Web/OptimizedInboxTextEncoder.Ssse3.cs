// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Text.Encodings.Web
{
    internal sealed partial class OptimizedInboxTextEncoder
    {
        private unsafe nuint GetIndexOfFirstByteToEncodeSsse3(byte* pData, nuint lengthInBytes)
        {
            Debug.Assert(Ssse3.IsSupported);
            Debug.Assert(BitConverter.IsLittleEndian);

            Vector128<byte> vecZero = Vector128<byte>.Zero;
            Vector128<byte> vec0x7 = Vector128.Create((byte)0x7);
            Vector128<byte> vecPowersOfTwo = Vector128.Create(1, 2, 4, 8, 16, 32, 64, 128, 0, 0, 0, 0, 0, 0, 0, 0);
            Vector128<byte> allowedCodePoints = _allowedAsciiCodePoints.AsVector;
            int pmovmskb;

            nuint i = 0;
            if (lengthInBytes >= 16)
            {
                nuint lastLegalIterationFor16CharRead = lengthInBytes & unchecked((nuint)(nint)~0xF);

                do
                {
                    // Read 16 bytes at a time into a single 128-bit vector.

                    Vector128<byte> packed = Sse2.LoadVector128(pData + i); // unaligned read

                    // Each element of the packed vector corresponds to a byte of untrusted source data. It will
                    // have the format [ ..., 0xYZ, ... ]. We use the low nibble of each byte to index into
                    // the 'allowedCodePoints' vector, and we use the high nibble of each byte to select a bit
                    // from the corresponding element in the 'allowedCodePoints' vector.
                    //
                    // Example: let packed := [ ..., 0x6D ('m'), ... ]
                    // The final 'result' vector will contain a non-zero value in the corresponding space iff the
                    // 0xD element in the 'allowedCodePoints' vector has its 1 << 0x6 bit set.
                    //
                    // We rely on the fact that the pshufb operation will turn each non-ASCII byte (high bit set)
                    // into 0x00 in the resulting 'shuffled' vector. That results in the corresponding element
                    // in the 'result' vector also being 0x00, meaning that escaping is required.

                    var allowedCodePointsShuffled = Ssse3.Shuffle(allowedCodePoints, packed);
                    var vecPowersOfTwoShuffled = Ssse3.Shuffle(vecPowersOfTwo, Sse2.And(Sse2.ShiftRightLogical(packed.AsUInt32(), 4).AsByte(), vec0x7));
                    var result = Sse2.And(allowedCodePointsShuffled, vecPowersOfTwoShuffled);

                    // Now, each element of 'result' contains a non-zero value if the corresponding element in
                    // 'packed' is allowed; and it contains a zero value if the corresponding element in 'packed'
                    // is disallowed. We'll compare 'result' against an all-zero vector to normalize 0x00 -> 0xFF
                    // and (anything other than 0x00) -> 0x00. Then 'pmovmskb' will have its nth bit set iff
                    // the nth entry in 'packed' requires escaping. An all-zero pmovmskb means no escaping is required.

                    pmovmskb = Sse2.MoveMask(Sse2.CompareEqual(result, vecZero));
                    if ((pmovmskb & 0xFFFF) != 0)
                    {
                        goto MaskContainsDataWhichRequiresEscaping;
                    }
                } while ((i += 16) < lastLegalIterationFor16CharRead);
            }

            if ((lengthInBytes & 8) != 0)
            {
                // Read 8 bytes at a time into a single 128-bit vector.
                // Same logic as the 16-byte case, but we only care about the low byte of the final pmovmskb value.
                // Everything except the low byte of pmovksmb contains garbage and must be discarded.

                var packed = Sse2.LoadScalarVector128((/* unaligned */ ulong*)(pData + i)).AsByte();
                var allowedCodePointsShuffled = Ssse3.Shuffle(allowedCodePoints, packed);
                var vecPowersOfTwoShuffled = Ssse3.Shuffle(vecPowersOfTwo, Sse2.And(Sse2.ShiftRightLogical(packed.AsUInt32(), 4).AsByte(), vec0x7));
                var result = Sse2.And(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                pmovmskb = Sse2.MoveMask(Sse2.CompareEqual(result, vecZero));
                if ((byte)pmovmskb != 0)
                {
                    goto MaskContainsDataWhichRequiresEscaping;
                }

                i += 8;
            }

            if ((lengthInBytes & 4) != 0)
            {
                // Read 4 bytes at a time into a single 128-bit vector.
                // Same logic as the 16-byte case, but we only care about the low nibble of the final pmovmskb value.
                // Everything except the low nibble of pmovksmb contains garbage and must be discarded.

                var packed = Sse2.LoadScalarVector128((/* unaligned */ uint*)(pData + i)).AsByte();
                var allowedCodePointsShuffled = Ssse3.Shuffle(allowedCodePoints, packed);
                var vecPowersOfTwoShuffled = Ssse3.Shuffle(vecPowersOfTwo, Sse2.And(Sse2.ShiftRightLogical(packed.AsUInt32(), 4).AsByte(), vec0x7));
                var result = Sse2.And(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                pmovmskb = Sse2.MoveMask(Sse2.CompareEqual(result, vecZero));
                if ((pmovmskb & 0xF) != 0)
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

        MaskContainsDataWhichRequiresEscaping:

            Debug.Assert(pmovmskb != 0);
            i += (uint)BitOperations.TrailingZeroCount(pmovmskb); // location of lowest set bit is where we must begin escaping
            goto Return;
        }

        private unsafe nuint GetIndexOfFirstCharToEncodeSsse3(char* pData, nuint lengthInChars)
        {
            // See GetIndexOfFirstByteToEncodeSsse3 for the central logic behind this method.
            // The main difference here is that we need to pack WORDs to BYTEs before performing
            // the main vectorized logic. It doesn't matter if we use signed or unsigned saturation
            // while packing, as saturation will convert out-of-range (non-ASCII char) WORDs to
            // 0x00 or 0x7F..0xFF, all of which are forbidden by the encoder.

            Debug.Assert(Ssse3.IsSupported);
            Debug.Assert(BitConverter.IsLittleEndian);

            Vector128<byte> vecZero = Vector128<byte>.Zero;
            Vector128<byte> vec0x7 = Vector128.Create((byte)0x7);
            Vector128<byte> vecPowersOfTwo = Vector128.Create(1, 2, 4, 8, 16, 32, 64, 128, 0, 0, 0, 0, 0, 0, 0, 0);
            Vector128<byte> allowedCodePoints = _allowedAsciiCodePoints.AsVector;
            int pmovmskb;

            nuint i = 0;
            if (lengthInChars >= 16)
            {
                nuint lastLegalIterationFor16CharRead = lengthInChars & unchecked((nuint)(nint)~0xF);

                do
                {
                    // Read 16 chars at a time into 2x 128-bit vectors, then pack into a single 128-bit vector.

                    var packed = Sse2.PackUnsignedSaturate(
                        Sse2.LoadVector128((/* unaligned */ short*)(pData + i)),
                        Sse2.LoadVector128((/* unaligned */ short*)(pData + 8 + i)));
                    var allowedCodePointsShuffled = Ssse3.Shuffle(allowedCodePoints, packed);
                    var vecPowersOfTwoShuffled = Ssse3.Shuffle(vecPowersOfTwo, Sse2.And(Sse2.ShiftRightLogical(packed.AsUInt32(), 4).AsByte(), vec0x7));
                    var result = Sse2.And(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                    pmovmskb = Sse2.MoveMask(Sse2.CompareEqual(result, vecZero));
                    if ((pmovmskb & 0xFFFF) != 0)
                    {
                        goto MaskContainsDataWhichRequiresEscaping;
                    }
                } while ((i += 16) < lastLegalIterationFor16CharRead);
            }

            if ((lengthInChars & 8) != 0)
            {
                // Read 8 chars at a time into a single 128-bit vector, then pack into low 8 bytes.

                var packed = Sse2.PackUnsignedSaturate(
                    Sse2.LoadVector128((/* unaligned */ short*)(pData + i)),
                    vecZero.AsInt16());
                var allowedCodePointsShuffled = Ssse3.Shuffle(allowedCodePoints, packed);
                var vecPowersOfTwoShuffled = Ssse3.Shuffle(vecPowersOfTwo, Sse2.And(Sse2.ShiftRightLogical(packed.AsUInt32(), 4).AsByte(), vec0x7));
                var result = Sse2.And(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                pmovmskb = Sse2.MoveMask(Sse2.CompareEqual(result, vecZero));
                if ((byte)pmovmskb != 0)
                {
                    goto MaskContainsDataWhichRequiresEscaping;
                }

                i += 8;
            }

            if ((lengthInChars & 4) != 0)
            {
                // Read 4 chars at a time into a single 128-bit vector, then pack into low 4 bytes.
                // Everything except the low nibble of pmovksmb contains garbage and must be discarded.

                var packed = Sse2.PackUnsignedSaturate(
                   Sse2.LoadScalarVector128((/* unaligned */ ulong*)(pData + i)).AsInt16(),
                   vecZero.AsInt16());
                var allowedCodePointsShuffled = Ssse3.Shuffle(allowedCodePoints, packed);
                var vecPowersOfTwoShuffled = Ssse3.Shuffle(vecPowersOfTwo, Sse2.And(Sse2.ShiftRightLogical(packed.AsUInt32(), 4).AsByte(), vec0x7));
                var result = Sse2.And(allowedCodePointsShuffled, vecPowersOfTwoShuffled);
                pmovmskb = Sse2.MoveMask(Sse2.CompareEqual(result, vecZero));
                if ((pmovmskb & 0xF) != 0)
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

        MaskContainsDataWhichRequiresEscaping:

            Debug.Assert(pmovmskb != 0);
            i += (uint)BitOperations.TrailingZeroCount(pmovmskb); // location of lowest set bit is where we must begin escaping
            goto Return;
        }
    }
}
