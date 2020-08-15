// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        // https://tools.ietf.org/html/rfc8259
        // Does the span contain '"', '\',  or any control characters (i.e. 0 to 31)
        // IndexOfAny(34, 92, < 32)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span)
        {
            if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported || Vector.IsHardwareAccelerated)
            {
                return IndexOfOrLessThan(
                        ref MemoryMarshal.GetReference(span),
                        JsonConstants.Quote,
                        JsonConstants.BackSlash,
                        lessThan: JsonConstants.Space,
                        span.Length);
            }
            else
            {
                return IndexOfOrLessThanNonVector(span);
            }
        }

        // Borrowed and modified from SpanHelpers.Byte:
        // https://github.com/dotnet/runtime/blob/a196d534495a1654ffd158211ca8761b1eba8c05/src/libraries/System.Private.CoreLib/src/System/SpanHelpers.Byte.cs#L916-L1207
        private static int IndexOfOrLessThan(ref byte searchSpace, byte value0, byte value1, byte lessThan, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uLessThan = lessThan; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
            {
                // Avx2 branch also operates on Sse2 sizes, so check is combined.
                nint vectorDiff = (nint)length - Vector128<byte>.Count;
                if (vectorDiff >= 0)
                {
                    // >= Sse2 intrinsics are supported, and length is enough to use them so use that path.
                    // We jump forward to the intrinsics at the end of the method so a naive branch predict
                    // will choose the non-intrinsic path so short lengths which don't gain anything aren't
                    // overly disadvantaged by having to jump over a lot of code. Whereas the longer lengths
                    // more than make this back from the intrinsics.
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, as it is used later
                nint vectorDiff = (nint)length - Vector<byte>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }

            uint lookUp;
            while (lengthToExamine >= 8)
            {
                lengthToExamine -= 8;

                lookUp = AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found;
                lookUp = AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found1;
                lookUp = AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found2;
                lookUp = AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found3;
                lookUp = AddByteOffset(ref searchSpace, offset + 4);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found4;
                lookUp = AddByteOffset(ref searchSpace, offset + 5);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found5;
                lookUp = AddByteOffset(ref searchSpace, offset + 6);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found6;
                lookUp = AddByteOffset(ref searchSpace, offset + 7);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found7;

                offset += 8;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;

                lookUp = AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found;
                lookUp = AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found1;
                lookUp = AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found2;
                lookUp = AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found3;

                offset += 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uLessThan > lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return (int)offset;
        Found1:
            return (int)(offset + 1);
        Found2:
            return (int)(offset + 2);
        Found3:
            return (int)(offset + 3);
        Found4:
            return (int)(offset + 4);
        Found5:
            return (int)(offset + 5);
        Found6:
            return (int)(offset + 6);
        Found7:
            return (int)(offset + 7);

        IntrinsicsCompare:
            // When we move into a Vectorized block, we process everything of Vector size;
            // and then for any remainder we do a final compare of Vector size but starting at
            // the end and forwards, which may overlap on an earlier compare.

            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (Sse2.IsSupported)
            {
                int matches;
                if (Avx2.IsSupported)
                {
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<byte>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<byte>.Count
                    if (lengthToExamine >= (nuint)Vector128<byte>.Count)
                    {
                        Vector256<byte> values0 = Vector256.Create(value0);
                        Vector256<byte> values1 = Vector256.Create(value1);
                        Vector256<sbyte> valuesLessThan = Vector256.Create((sbyte)(uLessThan - 0x80));

                        // Subtract Vector128<byte>.Count so we have now subtracted Vector256<byte>.Count
                        lengthToExamine -= (nuint)Vector128<byte>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            Vector256<byte> search = LoadVector256(ref searchSpace, offset);
                            // Bitwise Or to combine the flagged matches for the second value to our match flags
                            matches = Avx2.MoveMask(
                                            Avx2.Or(
                                                Avx2.CompareGreaterThan(
                                                    valuesLessThan,
                                                    Avx2.Subtract(search, Vector256.Create((byte)0x80)).AsSByte()).AsByte(),
                                                Avx2.Or(
                                                    Avx2.CompareEqual(values0, search),
                                                    Avx2.CompareEqual(values1, search))
                                                )
                                            );
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // None matched
                                offset += (nuint)Vector256<byte>.Count;
                                continue;
                            }

                            goto IntrinsicsMatch;
                        }

                        {
                            // Move to Vector length from end for final compare
                            Vector256<byte> search = LoadVector256(ref searchSpace, lengthToExamine);
                            offset = lengthToExamine;
                            // Same as method as above
                            matches = Avx2.MoveMask(
                                            Avx2.Or(
                                                Avx2.CompareGreaterThan(
                                                    valuesLessThan,
                                                    Avx2.Subtract(search, Vector256.Create((byte)0x80)).AsSByte()).AsByte(),
                                                Avx2.Or(
                                                    Avx2.CompareEqual(values0, search),
                                                    Avx2.CompareEqual(values1, search))
                                                )
                                            );
                        }

                        if (matches == 0)
                        {
                            // None matched
                            goto NotFound;
                        }

                        goto IntrinsicsMatch;
                    }
                }

                // Initial size check was done on method entry.
                Debug.Assert(length >= Vector128<byte>.Count);
                {
                    Vector128<byte> values0 = Vector128.Create(value0);
                    Vector128<byte> values1 = Vector128.Create(value1);
                    Vector128<sbyte> valuesLessThan = Vector128.Create((sbyte)(uLessThan - 0x80));
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        Vector128<byte> search = LoadVector128(ref searchSpace, offset);

                        matches = Sse2.MoveMask(
                            Sse2.Or(
                                Sse2.CompareLessThan(
                                    Sse2.Subtract(search, Vector128.Create((byte)0x80)).AsSByte(),
                                    valuesLessThan).AsByte(),
                                Sse2.Or(
                                    Sse2.CompareEqual(search, values0),
                                    Sse2.CompareEqual(search, values1))
                                )
                            );
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // None matched
                            offset += (nuint)Vector128<byte>.Count;
                            continue;
                        }

                        goto IntrinsicsMatch;
                    }
                    {
                        // Move to Vector length from end for final compare
                        Vector128<byte> search = LoadVector128(ref searchSpace, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Sse2.MoveMask(
                            Sse2.Or(
                                Sse2.CompareLessThan(
                                    Sse2.Subtract(search, Vector128.Create((byte)0x80)).AsSByte(),
                                    valuesLessThan).AsByte(),
                                Sse2.Or(
                                    Sse2.CompareEqual(search, values0),
                                    Sse2.CompareEqual(search, values1))
                                )
                            );
                    }
                    if (matches == 0)
                    {
                        // None matched
                        goto NotFound;
                    }
                }

            IntrinsicsMatch:
                // Find bitflag offset of first difference and add to current offset
                offset += (nuint)BitOperations.TrailingZeroCount(matches);
                goto Found;
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                // Mask to help find the first lane in compareResult that is set.
                // LSB 0x01 corresponds to lane 0, 0x10 - to lane 1, and so on.
                Vector128<byte> mask = Vector128.Create((ushort)0x1001).AsByte();
                int matchedLane = 0;

                Vector128<byte> search;
                Vector128<byte> matches;
                Vector128<byte> values0 = Vector128.Create(value0);
                Vector128<byte> values1 = Vector128.Create(value1);
                Vector128<byte> valuesLessThan = Vector128.Create(lessThan);
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector128(ref searchSpace, offset);

                    matches = AdvSimd.Or(
                                AdvSimd.Or(
                                    AdvSimd.CompareEqual(search, values0),
                                    AdvSimd.CompareEqual(search, values1)),
                                AdvSimd.CompareLessThan(search, valuesLessThan));

                    if (!TryFindFirstMatchedLane(mask, matches, ref matchedLane))
                    {
                        // Zero flags set so no matches
                        offset += (nuint)Vector128<byte>.Count;
                        continue;
                    }

                    // Find bitflag offset of first match and add to current offset
                    offset += (uint)matchedLane;

                    goto Found;
                }

                // Move to Vector length from end for final compare
                search = LoadVector128(ref searchSpace, lengthToExamine);
                offset = lengthToExamine;
                // Same as method as above
                matches = AdvSimd.Or(
                            AdvSimd.Or(
                                AdvSimd.CompareEqual(search, values0),
                                AdvSimd.CompareEqual(search, values1)),
                            AdvSimd.CompareLessThan(search, valuesLessThan));

                if (!TryFindFirstMatchedLane(mask, matches, ref matchedLane))
                {
                    // None matched
                    goto NotFound;
                }

                // Find bitflag offset of first match and add to current offset
                offset += (nuint)(uint)matchedLane;

                goto Found;
            }
            else if (Vector.IsHardwareAccelerated)
            {
                Vector<byte> values0 = new Vector<byte>(value0);
                Vector<byte> values1 = new Vector<byte>(value1);
                Vector<byte> valuesLessThan = new Vector<byte>(lessThan);

                Vector<byte> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchSpace, offset);
                    search = Vector.BitwiseOr(
                                Vector.BitwiseOr(
                                    Vector.Equals(search, values0),
                                    Vector.Equals(search, values1)),
                                Vector.LessThan(search, valuesLessThan));
                    if (Vector<byte>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<byte>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchSpace, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                            Vector.BitwiseOr(
                                Vector.Equals(search, values0),
                                Vector.Equals(search, values1)),
                            Vector.LessThan(search, valuesLessThan));
                if (Vector<byte>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)LocateFirstFoundByte(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryFindFirstMatchedLane(Vector128<byte> mask, Vector128<byte> compareResult, ref int matchedLane)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            // Find the first lane that is set inside compareResult.
            Vector128<byte> maskedSelectedLanes = AdvSimd.And(compareResult, mask);
            Vector128<byte> pairwiseSelectedLane = AdvSimd.Arm64.AddPairwise(maskedSelectedLanes, maskedSelectedLanes);
            ulong selectedLanes = pairwiseSelectedLane.AsUInt64().ToScalar();
            if (selectedLanes == 0)
            {
                // all lanes are zero, so nothing matched.
                return false;
            }

            // Find the first lane that is set inside compareResult.
            matchedLane = BitOperations.TrailingZeroCount(selectedLanes) >> 2;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<byte> LoadVector128(ref byte start, nuint offset)
            => Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.AddByteOffset(ref start, (IntPtr)(nint)offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<byte> LoadVector256(ref byte start, nuint offset)
            => Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.AddByteOffset(ref start, (IntPtr)(nint)offset));
    }
}
