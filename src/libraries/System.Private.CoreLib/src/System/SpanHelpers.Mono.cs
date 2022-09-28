// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System
{
    internal static partial class SpanHelpers // helpers used by Mono
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int IndexOfValueType(ref byte searchSpace, byte value, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue = value; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Vector128.IsHardwareAccelerated)
            {
                // Avx2 branch also operates on Sse2 sizes, so check is combined.
                if (length >= Vector128<byte>.Count * 2)
                {
                    lengthToExamine = UnalignedCountVector128(ref searchSpace);
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                if (length >= Vector<byte>.Count * 2)
                {
                    lengthToExamine = UnalignedCountVector(ref searchSpace);
                }
            }
        SequentialScan:
            while (lengthToExamine >= 8)
            {
                lengthToExamine -= 8;

                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset))
                    goto Found;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 1))
                    goto Found1;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 2))
                    goto Found2;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 3))
                    goto Found3;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 4))
                    goto Found4;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 5))
                    goto Found5;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 6))
                    goto Found6;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 7))
                    goto Found7;

                offset += 8;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;

                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset))
                    goto Found;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 1))
                    goto Found1;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 2))
                    goto Found2;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 3))
                    goto Found3;

                offset += 4;
            }

            while (lengthToExamine > 0)
            {
                lengthToExamine -= 1;

                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset))
                    goto Found;

                offset += 1;
            }

            // We get past SequentialScan only if IsHardwareAccelerated is true; and remain length is greater than Vector length.
            // However, we still have the redundant check to allow the JIT to see that the code is unreachable and eliminate it when the platform does not
            // have hardware accelerated. After processing Vector lengths we return to SequentialScan to finish any remaining.
            if (Vector256.IsHardwareAccelerated)
            {
                if (offset < (nuint)(uint)length)
                {
                    if ((((nuint)(uint)Unsafe.AsPointer(ref searchSpace) + offset) & (nuint)(Vector256<byte>.Count - 1)) != 0)
                    {
                        // Not currently aligned to Vector256 (is aligned to Vector128); this can cause a problem for searches
                        // with no upper bound e.g. String.strlen.
                        // Start with a check on Vector128 to align to Vector256, before moving to processing Vector256.
                        // This ensures we do not fault across memory pages while searching for an end of string.
                        Vector128<byte> values = Vector128.Create(value);
                        Vector128<byte> search = Vector128.LoadUnsafe(ref searchSpace, offset);

                        // Same method as below
                        uint matches = Vector128.Equals(values, search).ExtractMostSignificantBits();
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += (nuint)Vector128<byte>.Count;
                        }
                        else
                        {
                            // Find bitflag offset of first match and add to current offset
                            return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                        }
                    }

                    lengthToExamine = GetByteVector256SpanLength(offset, length);
                    if (lengthToExamine > offset)
                    {
                        Vector256<byte> values = Vector256.Create(value);
                        do
                        {
                            Vector256<byte> search = Vector256.LoadUnsafe(ref searchSpace, offset);
                            uint matches = Vector256.Equals(values, search).ExtractMostSignificantBits();
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // Zero flags set so no matches
                                offset += (nuint)Vector256<byte>.Count;
                                continue;
                            }

                            // Find bitflag offset of first match and add to current offset
                            return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                        } while (lengthToExamine > offset);
                    }

                    lengthToExamine = GetByteVector128SpanLength(offset, length);
                    if (lengthToExamine > offset)
                    {
                        Vector128<byte> values = Vector128.Create(value);
                        Vector128<byte> search = Vector128.LoadUnsafe(ref searchSpace, offset);

                        // Same method as above
                        uint matches = Vector128.Equals(values, search).ExtractMostSignificantBits();
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += (nuint)Vector128<byte>.Count;
                        }
                        else
                        {
                            // Find bitflag offset of first match and add to current offset
                            return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                        }
                    }

                    if (offset < (nuint)(uint)length)
                    {
                        lengthToExamine = ((nuint)(uint)length - offset);
                        goto SequentialScan;
                    }
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                if (offset < (nuint)(uint)length)
                {
                    lengthToExamine = GetByteVector128SpanLength(offset, length);

                    Vector128<byte> values = Vector128.Create(value);
                    while (lengthToExamine > offset)
                    {
                        Vector128<byte> search = Vector128.LoadUnsafe(ref searchSpace, offset);

                        // Same method as above
                        Vector128<byte> compareResult = Vector128.Equals(values, search);
                        if (compareResult == Vector128<byte>.Zero)
                        {
                            // Zero flags set so no matches
                            offset += (nuint)Vector128<byte>.Count;
                            continue;
                        }

                        // Find bitflag offset of first match and add to current offset
                        uint matches = compareResult.ExtractMostSignificantBits();
                        return (int)(offset + (uint)BitOperations.TrailingZeroCount(matches));
                    }

                    if (offset < (nuint)(uint)length)
                    {
                        lengthToExamine = ((nuint)(uint)length - offset);
                        goto SequentialScan;
                    }
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                if (offset < (nuint)(uint)length)
                {
                    lengthToExamine = GetByteVectorSpanLength(offset, length);

                    Vector<byte> values = new Vector<byte>(value);

                    while (lengthToExamine > offset)
                    {
                        var matches = Vector.Equals(values, LoadVector(ref searchSpace, offset));
                        if (Vector<byte>.Zero.Equals(matches))
                        {
                            offset += (nuint)Vector<byte>.Count;
                            continue;
                        }

                        // Find offset of first match and add to current offset
                        return (int)offset + LocateFirstFoundByte(matches);
                    }

                    if (offset < (nuint)(uint)length)
                    {
                        lengthToExamine = ((nuint)(uint)length - offset);
                        goto SequentialScan;
                    }
                }
            }
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int IndexOfValueType(ref short searchSpace, short value, int length)
            => IndexOfChar(ref Unsafe.As<short, char>(ref searchSpace), Unsafe.As<short, char>(ref value), length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int IndexOfChar(ref char searchSpace, char value, int length)
        {
            Debug.Assert(length >= 0);

            nint offset = 0;
            nint lengthToExamine = length;

            if (((int)Unsafe.AsPointer(ref searchSpace) & 1) != 0)
            {
                // Input isn't char aligned, we won't be able to align it to a Vector
            }
            else if (Sse2.IsSupported || AdvSimd.Arm64.IsSupported)
            {
                // Avx2 branch also operates on Sse2 sizes, so check is combined.
                // Needs to be double length to allow us to align the data first.
                if (length >= Vector128<ushort>.Count * 2)
                {
                    lengthToExamine = UnalignedCountVector128(ref searchSpace);
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Needs to be double length to allow us to align the data first.
                if (length >= Vector<ushort>.Count * 2)
                {
                    lengthToExamine = UnalignedCountVector(ref searchSpace);
                }
            }

        SequentialScan:
            // In the non-vector case lengthToExamine is the total length.
            // In the vector case lengthToExamine first aligns to Vector,
            // then in a second pass after the Vector lengths is the
            // remaining data that is shorter than a Vector length.
            while (lengthToExamine >= 4)
            {
                ref char current = ref Unsafe.Add(ref searchSpace, offset);

                if (value == current)
                    goto Found;
                if (value == Unsafe.Add(ref current, 1))
                    goto Found1;
                if (value == Unsafe.Add(ref current, 2))
                    goto Found2;
                if (value == Unsafe.Add(ref current, 3))
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                if (value == Unsafe.Add(ref searchSpace, offset))
                    goto Found;

                offset++;
                lengthToExamine--;
            }

            // We get past SequentialScan only if IsHardwareAccelerated or intrinsic .IsSupported is true. However, we still have the redundant check to allow
            // the JIT to see that the code is unreachable and eliminate it when the platform does not have hardware accelerated.
            if (Avx2.IsSupported)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector128<ushort>.Count);
                    if (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref searchSpace, (nint)offset)) & (nint)(Vector256<byte>.Count - 1)) != 0)
                    {
                        // Not currently aligned to Vector256 (is aligned to Vector128); this can cause a problem for searches
                        // with no upper bound e.g. String.wcslen. Start with a check on Vector128 to align to Vector256,
                        // before moving to processing Vector256.

                        // If the input searchSpan has been fixed or pinned, this ensures we do not fault across memory pages
                        // while searching for an end of string. Specifically that this assumes that the length is either correct
                        // or that the data is pinned otherwise it may cause an AccessViolation from crossing a page boundary into an
                        // unowned page. If the search is unbounded (e.g. null terminator in wcslen) and the search value is not found,
                        // again this will likely cause an AccessViolation. However, correctly bounded searches will return -1 rather
                        // than ever causing an AV.

                        // If the searchSpan has not been fixed or pinned the GC can relocate it during the execution of this
                        // method, so the alignment only acts as best endeavour. The GC cost is likely to dominate over
                        // the misalignment that may occur after; to we default to giving the GC a free hand to relocate and
                        // its up to the caller whether they are operating over fixed data.
                        Vector128<ushort> values = Vector128.Create((ushort)value);
                        Vector128<ushort> search = LoadVector128(ref searchSpace, offset);

                        // Same method as below
                        int matches = Sse2.MoveMask(Sse2.CompareEqual(values, search).AsByte());
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += Vector128<ushort>.Count;
                        }
                        else
                        {
                            // Find bitflag offset of first match and add to current offset
                            return (int)(offset + ((uint)BitOperations.TrailingZeroCount(matches) / sizeof(char)));
                        }
                    }

                    lengthToExamine = GetCharVector256SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        Vector256<ushort> values = Vector256.Create((ushort)value);
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector256<ushort>.Count);

                            Vector256<ushort> search = LoadVector256(ref searchSpace, offset);
                            int matches = Avx2.MoveMask(Avx2.CompareEqual(values, search).AsByte());
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // Zero flags set so no matches
                                offset += Vector256<ushort>.Count;
                                lengthToExamine -= Vector256<ushort>.Count;
                                continue;
                            }

                            // Find bitflag offset of first match and add to current offset,
                            // flags are in bytes so divide for chars
                            return (int)(offset + ((uint)BitOperations.TrailingZeroCount(matches) / sizeof(char)));
                        } while (lengthToExamine > 0);
                    }

                    lengthToExamine = GetCharVector128SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        Debug.Assert(lengthToExamine >= Vector128<ushort>.Count);

                        Vector128<ushort> values = Vector128.Create((ushort)value);
                        Vector128<ushort> search = LoadVector128(ref searchSpace, offset);

                        // Same method as above
                        int matches = Sse2.MoveMask(Sse2.CompareEqual(values, search).AsByte());
                        if (matches == 0)
                        {
                            // Zero flags set so no matches
                            offset += Vector128<ushort>.Count;
                            // Don't need to change lengthToExamine here as we don't use its current value again.
                        }
                        else
                        {
                            // Find bitflag offset of first match and add to current offset,
                            // flags are in bytes so divide for chars
                            return (int)(offset + ((uint)BitOperations.TrailingZeroCount(matches) / sizeof(char)));
                        }
                    }

                    if (offset < length)
                    {
                        lengthToExamine = length - offset;
                        goto SequentialScan;
                    }
                }
            }
            else if (Sse2.IsSupported)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector128<ushort>.Count);

                    lengthToExamine = GetCharVector128SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        Vector128<ushort> values = Vector128.Create((ushort)value);
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector128<ushort>.Count);

                            Vector128<ushort> search = LoadVector128(ref searchSpace, offset);

                            // Same method as above
                            int matches = Sse2.MoveMask(Sse2.CompareEqual(values, search).AsByte());
                            if (matches == 0)
                            {
                                // Zero flags set so no matches
                                offset += Vector128<ushort>.Count;
                                lengthToExamine -= Vector128<ushort>.Count;
                                continue;
                            }

                            // Find bitflag offset of first match and add to current offset,
                            // flags are in bytes so divide for chars
                            return (int)(offset + ((uint)BitOperations.TrailingZeroCount(matches) / sizeof(char)));
                        } while (lengthToExamine > 0);
                    }

                    if (offset < length)
                    {
                        lengthToExamine = length - offset;
                        goto SequentialScan;
                    }
                }
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector128<ushort>.Count);

                    lengthToExamine = GetCharVector128SpanLength(offset, length);
                    if (lengthToExamine > 0)
                    {
                        Vector128<ushort> values = Vector128.Create((ushort)value);
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector128<ushort>.Count);

                            Vector128<ushort> search = LoadVector128(ref searchSpace, offset);
                            Vector128<ushort> compareResult = AdvSimd.CompareEqual(values, search);

                            if (compareResult == Vector128<ushort>.Zero)
                            {
                                offset += Vector128<ushort>.Count;
                                lengthToExamine -= Vector128<ushort>.Count;
                                continue;
                            }

                            return (int)(offset + FindFirstMatchedLane(compareResult));
                        } while (lengthToExamine > 0);
                    }

                    if (offset < length)
                    {
                        lengthToExamine = length - offset;
                        goto SequentialScan;
                    }
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                if (offset < length)
                {
                    Debug.Assert(length - offset >= Vector<ushort>.Count);

                    lengthToExamine = GetCharVectorSpanLength(offset, length);

                    if (lengthToExamine > 0)
                    {
                        Vector<ushort> values = new Vector<ushort>((ushort)value);
                        do
                        {
                            Debug.Assert(lengthToExamine >= Vector<ushort>.Count);

                            var matches = Vector.Equals(values, LoadVector(ref searchSpace, offset));
                            if (Vector<ushort>.Zero.Equals(matches))
                            {
                                offset += Vector<ushort>.Count;
                                lengthToExamine -= Vector<ushort>.Count;
                                continue;
                            }

                            // Find offset of first match
                            return (int)(offset + LocateFirstFoundChar(matches));
                        } while (lengthToExamine > 0);
                    }

                    if (offset < length)
                    {
                        lengthToExamine = length - offset;
                        goto SequentialScan;
                    }
                }
            }
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)(offset);
        }

        internal static unsafe int IndexOfValueType<T>(ref T searchSpace, T value, int length) where T : struct, IEquatable<T>
        {
            Debug.Assert(length >= 0);

            nint index = 0; // Use nint for arithmetic to avoid unnecessary 64->32->64 truncations
            if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported && (Vector<T>.Count * 2) <= length)
            {
                Vector<T> valueVector = new Vector<T>(value);
                Vector<T> compareVector;
                Vector<T> matchVector;
                if ((uint)length % (uint)Vector<T>.Count != 0)
                {
                    // Number of elements is not a multiple of Vector<T>.Count, so do one
                    // check and shift only enough for the remaining set to be a multiple
                    // of Vector<T>.Count.
                    compareVector = Unsafe.As<T, Vector<T>>(ref Unsafe.Add(ref searchSpace, index));
                    matchVector = Vector.Equals(valueVector, compareVector);
                    if (matchVector != Vector<T>.Zero)
                    {
                        goto VectorMatch;
                    }
                    index += length % Vector<T>.Count;
                    length -= length % Vector<T>.Count;
                }
                while (length > 0)
                {
                    compareVector = Unsafe.As<T, Vector<T>>(ref Unsafe.Add(ref searchSpace, index));
                    matchVector = Vector.Equals(valueVector, compareVector);
                    if (matchVector != Vector<T>.Zero)
                    {
                        goto VectorMatch;
                    }
                    index += Vector<T>.Count;
                    length -= Vector<T>.Count;
                }
                goto NotFound;
            VectorMatch:
                for (int i = 0; i < Vector<T>.Count; i++)
                    if (compareVector[i].Equals(value))
                        return (int)(index + i);
            }

            while (length >= 8)
            {
                if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                    goto Found;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 1)))
                    goto Found1;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 2)))
                    goto Found2;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 3)))
                    goto Found3;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 4)))
                    goto Found4;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 5)))
                    goto Found5;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 6)))
                    goto Found6;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 7)))
                    goto Found7;

                length -= 8;
                index += 8;
            }

            while (length >= 4)
            {
                if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                    goto Found;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 1)))
                    goto Found1;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 2)))
                    goto Found2;
                if (value.Equals(Unsafe.Add(ref searchSpace, index + 3)))
                    goto Found3;

                length -= 4;
                index += 4;
            }

            while (length > 0)
            {
                if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                    goto Found;

                index += 1;
                length--;
            }
        NotFound:
            return -1;

        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return (int)index;
        Found1:
            return (int)(index + 1);
        Found2:
            return (int)(index + 2);
        Found3:
            return (int)(index + 3);
        Found4:
            return (int)(index + 4);
        Found5:
            return (int)(index + 5);
        Found6:
            return (int)(index + 6);
        Found7:
            return (int)(index + 7);
        }

        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, int length) where T : struct, IEquatable<T>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
            {
                for (int i = 0; i < length; i++)
                {
                    if (!Unsafe.Add(ref searchSpace, i).Equals(value0))
                    {
                        return i;
                    }
                }
            }
            else
            {
                Vector128<T> notEquals, value0Vector = Vector128.Create(value0);
                ref T current = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<T>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    notEquals = ~Vector128.Equals(value0Vector, Vector128.LoadUnsafe(ref current));
                    if (notEquals != Vector128<T>.Zero)
                    {
                        return ComputeIndex(ref searchSpace, ref current, notEquals);
                    }

                    current = ref Unsafe.Add(ref current, Vector128<T>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref current, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector128<T>.Count != 0)
                {
                    notEquals = ~Vector128.Equals(value0Vector, Vector128.LoadUnsafe(ref oneVectorAwayFromEnd));
                    if (notEquals != Vector128<T>.Zero)
                    {
                        return ComputeIndex(ref searchSpace, ref oneVectorAwayFromEnd, notEquals);
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static int ComputeIndex(ref T searchSpace, ref T current, Vector128<T> notEquals)
                {
                    uint notEqualsElements = notEquals.ExtractMostSignificantBits();
                    int index = BitOperations.TrailingZeroCount(notEqualsElements);
                    return index + (int)(Unsafe.ByteOffset(ref searchSpace, ref current) / Unsafe.SizeOf<T>());
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static int LastIndexOfValueType(ref byte searchSpace, byte value, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue = value; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            nuint offset = (nuint)(uint)length; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                lengthToExamine = UnalignedCountVectorFromEnd(ref searchSpace, length);
            }
        SequentialScan:
            while (lengthToExamine >= 8)
            {
                lengthToExamine -= 8;
                offset -= 8;

                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 7))
                    goto Found7;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 6))
                    goto Found6;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 5))
                    goto Found5;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 4))
                    goto Found4;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 3))
                    goto Found3;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 2))
                    goto Found2;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 1))
                    goto Found1;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset))
                    goto Found;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;
                offset -= 4;

                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 3))
                    goto Found3;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 2))
                    goto Found2;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset + 1))
                    goto Found1;
                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset))
                    goto Found;
            }

            while (lengthToExamine > 0)
            {
                lengthToExamine -= 1;
                offset -= 1;

                if (uValue == Unsafe.AddByteOffset(ref searchSpace, offset))
                    goto Found;
            }

            if (Vector.IsHardwareAccelerated && (offset > 0))
            {
                lengthToExamine = (offset & (nuint)~(Vector<byte>.Count - 1));

                Vector<byte> values = new Vector<byte>(value);

                while (lengthToExamine > (nuint)(Vector<byte>.Count - 1))
                {
                    var matches = Vector.Equals(values, LoadVector(ref searchSpace, offset - (nuint)Vector<byte>.Count));
                    if (Vector<byte>.Zero.Equals(matches))
                    {
                        offset -= (nuint)Vector<byte>.Count;
                        lengthToExamine -= (nuint)Vector<byte>.Count;
                        continue;
                    }

                    // Find offset of first match and add to current offset
                    return (int)(offset) - Vector<byte>.Count + LocateLastFoundByte(matches);
                }
                if (offset > 0)
                {
                    lengthToExamine = offset;
                    goto SequentialScan;
                }
            }
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int LastIndexOfValueType(ref short searchSpace, short value, int length)
            => LastIndexOfValueType(ref Unsafe.As<short, char>(ref searchSpace), Unsafe.As<short, char>(ref value), length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int LastIndexOfValueType(ref char searchSpace, char value, int length)
        {
            Debug.Assert(length >= 0);

            fixed (char* pChars = &searchSpace)
            {
                char* pCh = pChars + length;
                char* pEndCh = pChars;

                if (Vector.IsHardwareAccelerated && length >= Vector<ushort>.Count * 2)
                {
                    // Figure out how many characters to read sequentially from the end until we are vector aligned
                    // This is equivalent to: length = ((int)pCh % Unsafe.SizeOf<Vector<ushort>>()) / elementsPerByte
                    const int elementsPerByte = sizeof(ushort) / sizeof(byte);
                    length = ((int)pCh & (Unsafe.SizeOf<Vector<ushort>>() - 1)) / elementsPerByte;
                }

            SequentialScan:
                while (length >= 4)
                {
                    length -= 4;
                    pCh -= 4;

                    if (*(pCh + 3) == value)
                        goto Found3;
                    if (*(pCh + 2) == value)
                        goto Found2;
                    if (*(pCh + 1) == value)
                        goto Found1;
                    if (*pCh == value)
                        goto Found;
                }

                while (length > 0)
                {
                    length--;
                    pCh--;

                    if (*pCh == value)
                        goto Found;
                }

                // We get past SequentialScan only if IsHardwareAccelerated is true. However, we still have the redundant check to allow
                // the JIT to see that the code is unreachable and eliminate it when the platform does not have hardware accelerated.
                if (Vector.IsHardwareAccelerated && pCh > pEndCh)
                {
                    // Get the highest multiple of Vector<ushort>.Count that is within the search space.
                    // That will be how many times we iterate in the loop below.
                    // This is equivalent to: length = Vector<ushort>.Count * ((int)(pCh - pEndCh) / Vector<ushort>.Count)
                    length = (int)((pCh - pEndCh) & ~(Vector<ushort>.Count - 1));

                    // Get comparison Vector
                    Vector<ushort> vComparison = new Vector<ushort>(value);

                    while (length > 0)
                    {
                        char* pStart = pCh - Vector<ushort>.Count;
                        // Using Unsafe.Read instead of ReadUnaligned since the search space is pinned and pCh (and hence pSart) is always vector aligned
                        Debug.Assert(((int)pStart & (Unsafe.SizeOf<Vector<ushort>>() - 1)) == 0);
                        Vector<ushort> vMatches = Vector.Equals(vComparison, Unsafe.Read<Vector<ushort>>(pStart));
                        if (Vector<ushort>.Zero.Equals(vMatches))
                        {
                            pCh -= Vector<ushort>.Count;
                            length -= Vector<ushort>.Count;
                            continue;
                        }
                        // Find offset of last match
                        return (int)(pStart - pEndCh) + LocateLastFoundChar(vMatches);
                    }

                    if (pCh > pEndCh)
                    {
                        length = (int)(pCh - pEndCh);
                        goto SequentialScan;
                    }
                }

                return -1;
            Found:
                return (int)(pCh - pEndCh);
            Found1:
                return (int)(pCh - pEndCh) + 1;
            Found2:
                return (int)(pCh - pEndCh) + 2;
            Found3:
                return (int)(pCh - pEndCh) + 3;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int LastIndexOfValueType<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>?
            => LastIndexOf(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static int IndexOfAnyValueType(ref byte searchSpace, byte value0, byte value1, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
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

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found3;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 4);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found4;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 5);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found5;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 6);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found6;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 7);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found7;

                offset += 8;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found3;

                offset += 4;
            }

            while (lengthToExamine > 0)
            {

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp)
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
                    Vector256<byte> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<byte>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<byte>.Count
                    if (lengthToExamine >= (nuint)Vector128<byte>.Count)
                    {
                        Vector256<byte> values0 = Vector256.Create(value0);
                        Vector256<byte> values1 = Vector256.Create(value1);

                        // Subtract Vector128<byte>.Count so we have now subtracted Vector256<byte>.Count
                        lengthToExamine -= (nuint)Vector128<byte>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchSpace, offset);
                            // Bitwise Or to combine the flagged matches for the second value to our match flags
                            matches = Avx2.MoveMask(
                                            Avx2.Or(
                                                Avx2.CompareEqual(values0, search),
                                                Avx2.CompareEqual(values1, search)));
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

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchSpace, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(
                                    Avx2.Or(
                                        Avx2.CompareEqual(values0, search),
                                        Avx2.CompareEqual(values1, search)));
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
                    Vector128<byte> search;
                    Vector128<byte> values0 = Vector128.Create(value0);
                    Vector128<byte> values1 = Vector128.Create(value1);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchSpace, offset);

                        matches = Sse2.MoveMask(
                            Sse2.Or(
                                Sse2.CompareEqual(values0, search),
                                Sse2.CompareEqual(values1, search))
                            .AsByte());
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
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchSpace, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(
                        Sse2.Or(
                            Sse2.CompareEqual(values0, search),
                            Sse2.CompareEqual(values1, search)));
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
                Vector128<byte> search;
                Vector128<byte> matches;
                Vector128<byte> values0 = Vector128.Create(value0);
                Vector128<byte> values1 = Vector128.Create(value1);
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector128(ref searchSpace, offset);

                    matches = AdvSimd.Or(
                            AdvSimd.CompareEqual(values0, search),
                            AdvSimd.CompareEqual(values1, search));

                    if (matches == Vector128<byte>.Zero)
                    {
                        offset += (nuint)Vector128<byte>.Count;
                        continue;
                    }

                    // Find bitflag offset of first match and add to current offset
                    offset += FindFirstMatchedLane(matches);

                    goto Found;
                }

                // Move to Vector length from end for final compare
                search = LoadVector128(ref searchSpace, lengthToExamine);
                offset = lengthToExamine;
                // Same as method as above
                matches = AdvSimd.Or(
                        AdvSimd.CompareEqual(values0, search),
                        AdvSimd.CompareEqual(values1, search));

                if (matches == Vector128<byte>.Zero)
                {
                    // None matched
                    goto NotFound;
                }

                // Find bitflag offset of first match and add to current offset
                offset += FindFirstMatchedLane(matches);

                goto Found;
            }
            else if (Vector.IsHardwareAccelerated)
            {
                Vector<byte> values0 = new Vector<byte>(value0);
                Vector<byte> values1 = new Vector<byte>(value1);

                Vector<byte> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchSpace, offset);
                    search = Vector.BitwiseOr(
                                    Vector.Equals(search, values0),
                                    Vector.Equals(search, values1));
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
                                Vector.Equals(search, values0),
                                Vector.Equals(search, values1));
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
        internal static unsafe int IndexOfAnyValueType(ref short searchSpace, short value0, short value1, int length)
            => IndexOfAnyChar(ref Unsafe.As<short, char>(ref searchSpace), Unsafe.As<short, char>(ref value0), Unsafe.As<short, char>(ref value1), length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int IndexOfAnyChar(ref char searchStart, char value0, char value1, int length)
        {
            Debug.Assert(length >= 0);

            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Sse2.IsSupported)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector128<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // >= Sse2 intrinsics are supported and length is enough to use them, so use that path.
                    // We jump forward to the intrinsics at the end of them method so a naive branch predict
                    // will choose the non-intrinsic path so short lengths which don't gain anything aren't
                    // overly disadvantaged by having to jump over a lot of code. Whereas the longer lengths
                    // more than make this back from the intrinsics.
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto VectorCompare;
                }
            }

            int lookUp;
            while (lengthToExamine >= 4)
            {
                ref char current = ref Add(ref searchStart, offset);

                lookUp = current;
                if (value0 == lookUp || value1 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref current, 1);
                if (value0 == lookUp || value1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref current, 2);
                if (value0 == lookUp || value1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref current, 3);
                if (value0 == lookUp || value1 == lookUp)
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = Add(ref searchStart, offset);
                if (value0 == lookUp || value1 == lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)offset;

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
                    Vector256<ushort> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<ushort>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<ushort>.Count
                    if (lengthToExamine >= (nuint)Vector128<ushort>.Count)
                    {
                        Vector256<ushort> values0 = Vector256.Create((ushort)value0);
                        Vector256<ushort> values1 = Vector256.Create((ushort)value1);

                        // Subtract Vector128<ushort>.Count so we have now subtracted Vector256<ushort>.Count
                        lengthToExamine -= (nuint)Vector128<ushort>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchStart, offset);
                            // Bitwise Or to combine the flagged matches for the second value to our match flags
                            matches = Avx2.MoveMask(
                                            Avx2.Or(
                                                Avx2.CompareEqual(values0, search),
                                                Avx2.CompareEqual(values1, search))
                                            .AsByte());
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // None matched
                                offset += (nuint)Vector256<ushort>.Count;
                                continue;
                            }

                            goto IntrinsicsMatch;
                        }

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchStart, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(
                                    Avx2.Or(
                                        Avx2.CompareEqual(values0, search),
                                        Avx2.CompareEqual(values1, search))
                                    .AsByte());
                        if (matches == 0)
                        {
                            // None matched
                            goto NotFound;
                        }

                        goto IntrinsicsMatch;
                    }
                }

                // Initial size check was done on method entry.
                Debug.Assert(length >= Vector128<ushort>.Count);
                {
                    Vector128<ushort> search;
                    Vector128<ushort> values0 = Vector128.Create((ushort)value0);
                    Vector128<ushort> values1 = Vector128.Create((ushort)value1);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchStart, offset);

                        matches = Sse2.MoveMask(
                            Sse2.Or(
                                Sse2.CompareEqual(values0, search),
                                Sse2.CompareEqual(values1, search))
                            .AsByte());
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // None matched
                            offset += (nuint)Vector128<ushort>.Count;
                            continue;
                        }

                        goto IntrinsicsMatch;
                    }
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchStart, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(
                        Sse2.Or(
                            Sse2.CompareEqual(values0, search),
                            Sse2.CompareEqual(values1, search))
                        .AsByte());
                    if (matches == 0)
                    {
                        // None matched
                        goto NotFound;
                    }
                }

            IntrinsicsMatch:
                // Find bitflag offset of first difference and add to current offset,
                // flags are in bytes so divide by 2 for chars (shift right by 1)
                offset += (nuint)(uint)BitOperations.TrailingZeroCount(matches) >> 1;
                goto Found;
            }

        VectorCompare:
            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
            {
                Vector<ushort> values0 = new Vector<ushort>(value0);
                Vector<ushort> values1 = new Vector<ushort>(value1);

                Vector<ushort> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchStart, offset);
                    search = Vector.BitwiseOr(
                                    Vector.Equals(search, values0),
                                    Vector.Equals(search, values1));
                    if (Vector<ushort>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<ushort>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchStart, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                                Vector.Equals(search, values0),
                                Vector.Equals(search, values1));
                if (Vector<ushort>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)(uint)LocateFirstFoundChar(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length)
            => IndexOfAnyExcept(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static int IndexOfAnyValueType(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue2 = value2; // Use uint for comparisons to avoid unnecessary 8->32 extensions
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

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found3;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 4);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found4;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 5);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found5;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 6);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found6;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 7);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found7;

                offset += 8;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found3;

                offset += 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
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
                    Vector256<byte> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<byte>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<byte>.Count
                    if (lengthToExamine >= (nuint)Vector128<byte>.Count)
                    {
                        Vector256<byte> values0 = Vector256.Create(value0);
                        Vector256<byte> values1 = Vector256.Create(value1);
                        Vector256<byte> values2 = Vector256.Create(value2);

                        // Subtract Vector128<byte>.Count so we have now subtracted Vector256<byte>.Count
                        lengthToExamine -= (nuint)Vector128<byte>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchSpace, offset);
                            // Bitwise Or to combine the flagged matches for the second value to our match flags
                            matches = Avx2.MoveMask(
                                        Avx2.Or(
                                            Avx2.Or(
                                                Avx2.CompareEqual(values0, search),
                                                Avx2.CompareEqual(values1, search)),
                                            Avx2.CompareEqual(values2, search)));
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

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchSpace, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(
                                    Avx2.Or(
                                        Avx2.Or(
                                            Avx2.CompareEqual(values0, search),
                                            Avx2.CompareEqual(values1, search)),
                                        Avx2.CompareEqual(values2, search)));
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
                    Vector128<byte> search;
                    Vector128<byte> values0 = Vector128.Create(value0);
                    Vector128<byte> values1 = Vector128.Create(value1);
                    Vector128<byte> values2 = Vector128.Create(value2);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchSpace, offset);

                        matches = Sse2.MoveMask(
                                    Sse2.Or(
                                        Sse2.Or(
                                            Sse2.CompareEqual(values0, search),
                                            Sse2.CompareEqual(values1, search)),
                                        Sse2.CompareEqual(values2, search)));
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
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchSpace, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(
                                Sse2.Or(
                                    Sse2.Or(
                                        Sse2.CompareEqual(values0, search),
                                        Sse2.CompareEqual(values1, search)),
                                    Sse2.CompareEqual(values2, search)));
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
                Vector128<byte> search;
                Vector128<byte> matches;
                Vector128<byte> values0 = Vector128.Create(value0);
                Vector128<byte> values1 = Vector128.Create(value1);
                Vector128<byte> values2 = Vector128.Create(value2);
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector128(ref searchSpace, offset);

                    matches = AdvSimd.Or(
                                AdvSimd.Or(
                                    AdvSimd.CompareEqual(values0, search),
                                    AdvSimd.CompareEqual(values1, search)),
                                AdvSimd.CompareEqual(values2, search));

                    if (matches == Vector128<byte>.Zero)
                    {
                        offset += (nuint)Vector128<byte>.Count;
                        continue;
                    }

                    // Find bitflag offset of first match and add to current offset
                    offset += FindFirstMatchedLane(matches);

                    goto Found;
                }

                // Move to Vector length from end for final compare
                search = LoadVector128(ref searchSpace, lengthToExamine);
                offset = lengthToExamine;
                // Same as method as above
                matches = AdvSimd.Or(
                            AdvSimd.Or(
                                AdvSimd.CompareEqual(values0, search),
                                AdvSimd.CompareEqual(values1, search)),
                            AdvSimd.CompareEqual(values2, search));

                if (matches == Vector128<byte>.Zero)
                {
                    // None matched
                    goto NotFound;
                }

                // Find bitflag offset of first match and add to current offset
                offset += FindFirstMatchedLane(matches);

                goto Found;
            }
            else if (Vector.IsHardwareAccelerated)
            {
                Vector<byte> values0 = new Vector<byte>(value0);
                Vector<byte> values1 = new Vector<byte>(value1);
                Vector<byte> values2 = new Vector<byte>(value2);

                Vector<byte> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchSpace, offset);
                    search = Vector.BitwiseOr(
                                Vector.BitwiseOr(
                                    Vector.Equals(search, values0),
                                    Vector.Equals(search, values1)),
                                Vector.Equals(search, values2));
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
                            Vector.Equals(search, values2));
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
        internal static unsafe int IndexOfAnyValueType(ref short searchSpace, short value0, short value1, short value2, int length)
            => IndexOfAnyValueType(
                ref Unsafe.As<short, char>(ref searchSpace),
                Unsafe.As<short, char>(ref value0),
                Unsafe.As<short, char>(ref value1),
                Unsafe.As<short, char>(ref value2),
                length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int IndexOfAnyValueType(ref char searchStart, char value0, char value1, char value2, int length)
        {
            Debug.Assert(length >= 0);

            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Sse2.IsSupported)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector128<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // >= Sse2 intrinsics are supported and length is enough to use them, so use that path.
                    // We jump forward to the intrinsics at the end of them method so a naive branch predict
                    // will choose the non-intrinsic path so short lengths which don't gain anything aren't
                    // overly disadvantaged by having to jump over a lot of code. Whereas the longer lengths
                    // more than make this back from the intrinsics.
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto VectorCompare;
                }
            }

            int lookUp;
            while (lengthToExamine >= 4)
            {
                ref char current = ref Add(ref searchStart, offset);

                lookUp = current;
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref current, 1);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref current, 2);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref current, 3);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = Add(ref searchStart, offset);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)offset;

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
                    Vector256<ushort> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<ushort>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<ushort>.Count
                    if (lengthToExamine >= (nuint)Vector128<ushort>.Count)
                    {
                        Vector256<ushort> values0 = Vector256.Create((ushort)value0);
                        Vector256<ushort> values1 = Vector256.Create((ushort)value1);
                        Vector256<ushort> values2 = Vector256.Create((ushort)value2);

                        // Subtract Vector128<ushort>.Count so we have now subtracted Vector256<ushort>.Count
                        lengthToExamine -= (nuint)Vector128<ushort>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchStart, offset);
                            // Bitwise Or to combine the flagged matches for the second value to our match flags
                            matches = Avx2.MoveMask(
                                            Avx2.Or(
                                                Avx2.Or(
                                                    Avx2.CompareEqual(values0, search),
                                                    Avx2.CompareEqual(values1, search)),
                                                Avx2.CompareEqual(values2, search))
                                            .AsByte());
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // None matched
                                offset += (nuint)Vector256<ushort>.Count;
                                continue;
                            }

                            goto IntrinsicsMatch;
                        }

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchStart, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(
                                    Avx2.Or(
                                        Avx2.Or(
                                            Avx2.CompareEqual(values0, search),
                                            Avx2.CompareEqual(values1, search)),
                                        Avx2.CompareEqual(values2, search))
                                    .AsByte());
                        if (matches == 0)
                        {
                            // None matched
                            goto NotFound;
                        }

                        goto IntrinsicsMatch;
                    }
                }

                // Initial size check was done on method entry.
                Debug.Assert(length >= Vector128<ushort>.Count);
                {
                    Vector128<ushort> search;
                    Vector128<ushort> values0 = Vector128.Create((ushort)value0);
                    Vector128<ushort> values1 = Vector128.Create((ushort)value1);
                    Vector128<ushort> values2 = Vector128.Create((ushort)value2);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchStart, offset);

                        matches = Sse2.MoveMask(
                                    Sse2.Or(
                                        Sse2.Or(
                                            Sse2.CompareEqual(values0, search),
                                            Sse2.CompareEqual(values1, search)),
                                        Sse2.CompareEqual(values2, search))
                                    .AsByte());
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // None matched
                            offset += (nuint)Vector128<ushort>.Count;
                            continue;
                        }

                        goto IntrinsicsMatch;
                    }
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchStart, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(
                                    Sse2.Or(
                                        Sse2.Or(
                                            Sse2.CompareEqual(values0, search),
                                            Sse2.CompareEqual(values1, search)),
                                        Sse2.CompareEqual(values2, search))
                                    .AsByte());
                    if (matches == 0)
                    {
                        // None matched
                        goto NotFound;
                    }
                }

            IntrinsicsMatch:
                // Find bitflag offset of first difference and add to current offset,
                // flags are in bytes so divide by 2 for chars (shift right by 1)
                offset += (nuint)(uint)BitOperations.TrailingZeroCount(matches) >> 1;
                goto Found;
            }

        VectorCompare:
            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
            {
                Vector<ushort> values0 = new Vector<ushort>(value0);
                Vector<ushort> values1 = new Vector<ushort>(value1);
                Vector<ushort> values2 = new Vector<ushort>(value2);

                Vector<ushort> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchStart, offset);
                    search = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(search, values0),
                                        Vector.Equals(search, values1)),
                                    Vector.Equals(search, values2));
                    if (Vector<ushort>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<ushort>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchStart, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(search, values0),
                                        Vector.Equals(search, values1)),
                                    Vector.Equals(search, values2));
                if (Vector<ushort>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)(uint)LocateFirstFoundChar(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length)
            => IndexOfAnyExcept(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int IndexOfAnyValueType(ref short searchSpace, short value0, short value1, short value2, short value3, int length)
            => IndexOfAnyValueType(
                ref Unsafe.As<short, char>(ref searchSpace),
                Unsafe.As<short, char>(ref value0),
                Unsafe.As<short, char>(ref value1),
                Unsafe.As<short, char>(ref value2),
                Unsafe.As<short, char>(ref value3),
                length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int IndexOfAnyValueType(ref char searchStart, char value0, char value1, char value2, char value3, int length)
        {
            Debug.Assert(length >= 0);

            nuint offset = 0; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Sse2.IsSupported)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector128<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // >= Sse2 intrinsics are supported and length is enough to use them, so use that path.
                    // We jump forward to the intrinsics at the end of them method so a naive branch predict
                    // will choose the non-intrinsic path so short lengths which don't gain anything aren't
                    // overly disadvantaged by having to jump over a lot of code. Whereas the longer lengths
                    // more than make this back from the intrinsics.
                    lengthToExamine = (nuint)vectorDiff;
                    goto IntrinsicsCompare;
                }
            }
            else if (Vector.IsHardwareAccelerated)
            {
                // Calculate lengthToExamine here for test, rather than just testing as it used later, rather than doing it twice.
                nint vectorDiff = (nint)length - Vector<ushort>.Count;
                if (vectorDiff >= 0)
                {
                    // Similar as above for Vector version
                    lengthToExamine = (nuint)vectorDiff;
                    goto VectorCompare;
                }
            }

            int lookUp;
            while (lengthToExamine >= 4)
            {
                ref char current = ref Add(ref searchStart, offset);

                lookUp = current;
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found;
                lookUp = Unsafe.Add(ref current, 1);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found1;
                lookUp = Unsafe.Add(ref current, 2);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found2;
                lookUp = Unsafe.Add(ref current, 3);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found3;

                offset += 4;
                lengthToExamine -= 4;
            }

            while (lengthToExamine > 0)
            {
                lookUp = Add(ref searchStart, offset);
                if (value0 == lookUp || value1 == lookUp || value2 == lookUp || value3 == lookUp)
                    goto Found;

                offset += 1;
                lengthToExamine -= 1;
            }

        NotFound:
            return -1;
        Found3:
            return (int)(offset + 3);
        Found2:
            return (int)(offset + 2);
        Found1:
            return (int)(offset + 1);
        Found:
            return (int)offset;

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
                    Vector256<ushort> search;
                    // Guard as we may only have a valid size for Vector128; when we will move to the Sse2
                    // We have already subtracted Vector128<ushort>.Count from lengthToExamine so compare against that
                    // to see if we have double the size for Vector256<ushort>.Count
                    if (lengthToExamine >= (nuint)Vector128<ushort>.Count)
                    {
                        Vector256<ushort> values0 = Vector256.Create((ushort)value0);
                        Vector256<ushort> values1 = Vector256.Create((ushort)value1);
                        Vector256<ushort> values2 = Vector256.Create((ushort)value2);
                        Vector256<ushort> values3 = Vector256.Create((ushort)value3);

                        // Subtract Vector128<ushort>.Count so we have now subtracted Vector256<ushort>.Count
                        lengthToExamine -= (nuint)Vector128<ushort>.Count;
                        // First time this checks again against 0, however we will move into final compare if it fails.
                        while (lengthToExamine > offset)
                        {
                            search = LoadVector256(ref searchStart, offset);
                            // We preform the Or at non-Vector level as we are using the maximum number of non-preserved registers,
                            // and more causes them first to be pushed to stack and then popped on exit to preseve their values.
                            matches = Avx2.MoveMask(Avx2.CompareEqual(values0, search).AsByte());
                            // Bitwise Or to combine the flagged matches for the second, third and fourth values to our match flags
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values1, search).AsByte());
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values2, search).AsByte());
                            matches |= Avx2.MoveMask(Avx2.CompareEqual(values3, search).AsByte());
                            // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                            // So the bit position in 'matches' corresponds to the element offset.
                            if (matches == 0)
                            {
                                // None matched
                                offset += (nuint)Vector256<ushort>.Count;
                                continue;
                            }

                            goto IntrinsicsMatch;
                        }

                        // Move to Vector length from end for final compare
                        search = LoadVector256(ref searchStart, lengthToExamine);
                        offset = lengthToExamine;
                        // Same as method as above
                        matches = Avx2.MoveMask(Avx2.CompareEqual(values0, search).AsByte());
                        // Bitwise Or to combine the flagged matches for the second, third and fourth values to our match flags
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values1, search).AsByte());
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values2, search).AsByte());
                        matches |= Avx2.MoveMask(Avx2.CompareEqual(values3, search).AsByte());
                        if (matches == 0)
                        {
                            // None matched
                            goto NotFound;
                        }

                        goto IntrinsicsMatch;
                    }
                }

                // Initial size check was done on method entry.
                Debug.Assert(length >= Vector128<ushort>.Count);
                {
                    Vector128<ushort> search;
                    Vector128<ushort> values0 = Vector128.Create((ushort)value0);
                    Vector128<ushort> values1 = Vector128.Create((ushort)value1);
                    Vector128<ushort> values2 = Vector128.Create((ushort)value2);
                    Vector128<ushort> values3 = Vector128.Create((ushort)value3);
                    // First time this checks against 0 and we will move into final compare if it fails.
                    while (lengthToExamine > offset)
                    {
                        search = LoadVector128(ref searchStart, offset);

                        matches = Sse2.MoveMask(Sse2.CompareEqual(values0, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values1, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values2, search).AsByte());
                        matches |= Sse2.MoveMask(Sse2.CompareEqual(values3, search).AsByte());
                        // Note that MoveMask has converted the equal vector elements into a set of bit flags,
                        // So the bit position in 'matches' corresponds to the element offset.
                        if (matches == 0)
                        {
                            // None matched
                            offset += (nuint)Vector128<ushort>.Count;
                            continue;
                        }

                        goto IntrinsicsMatch;
                    }
                    // Move to Vector length from end for final compare
                    search = LoadVector128(ref searchStart, lengthToExamine);
                    offset = lengthToExamine;
                    // Same as method as above
                    matches = Sse2.MoveMask(Sse2.CompareEqual(values0, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values1, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values2, search).AsByte());
                    matches |= Sse2.MoveMask(Sse2.CompareEqual(values3, search).AsByte());
                    if (matches == 0)
                    {
                        // None matched
                        goto NotFound;
                    }
                }

            IntrinsicsMatch:
                // Find bitflag offset of first difference and add to current offset,
                // flags are in bytes so divide by 2 for chars (shift right by 1)
                offset += (nuint)(uint)BitOperations.TrailingZeroCount(matches) >> 1;
                goto Found;
            }

        VectorCompare:
            // We include the Supported check again here even though path will not be taken, so the asm isn't generated if not supported.
            if (!Sse2.IsSupported && Vector.IsHardwareAccelerated)
            {
                Vector<ushort> values0 = new Vector<ushort>(value0);
                Vector<ushort> values1 = new Vector<ushort>(value1);
                Vector<ushort> values2 = new Vector<ushort>(value2);
                Vector<ushort> values3 = new Vector<ushort>(value3);

                Vector<ushort> search;
                // First time this checks against 0 and we will move into final compare if it fails.
                while (lengthToExamine > offset)
                {
                    search = LoadVector(ref searchStart, offset);
                    search = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.BitwiseOr(
                                            Vector.Equals(search, values0),
                                            Vector.Equals(search, values1)),
                                        Vector.Equals(search, values2)),
                                    Vector.Equals(search, values3));
                    if (Vector<ushort>.Zero.Equals(search))
                    {
                        // None matched
                        offset += (nuint)Vector<ushort>.Count;
                        continue;
                    }

                    goto VectorMatch;
                }

                // Move to Vector length from end for final compare
                search = LoadVector(ref searchStart, lengthToExamine);
                offset = lengthToExamine;
                search = Vector.BitwiseOr(
                                Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(search, values0),
                                        Vector.Equals(search, values1)),
                                    Vector.Equals(search, values2)),
                                Vector.Equals(search, values3));
                if (Vector<ushort>.Zero.Equals(search))
                {
                    // None matched
                    goto NotFound;
                }

            VectorMatch:
                offset += (nuint)(uint)LocateFirstFoundChar(search);
                goto Found;
            }

            Debug.Fail("Unreachable");
            goto NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length)
            => IndexOfAnyExcept(ref searchSpace, value0, value1, value2, value3, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value, int length)
            => LastIndexOfAnyExcept(ref searchSpace, value, length);

        internal static int LastIndexOfAnyValueType(ref byte searchSpace, byte value0, byte value1, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1;
            nuint offset = (nuint)(uint)length; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                lengthToExamine = UnalignedCountVectorFromEnd(ref searchSpace, length);
            }
        SequentialScan:
            uint lookUp;
            while (lengthToExamine >= 8)
            {
                lengthToExamine -= 8;
                offset -= 8;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 7);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found7;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 6);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found6;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 5);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found5;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 4);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found4;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found3;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;
                offset -= 4;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found3;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
            }

            while (lengthToExamine > 0)
            {
                lengthToExamine -= 1;
                offset -= 1;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp)
                    goto Found;
            }

            if (Vector.IsHardwareAccelerated && (offset > 0))
            {
                lengthToExamine = (offset & (nuint)~(Vector<byte>.Count - 1));

                Vector<byte> values0 = new Vector<byte>(value0);
                Vector<byte> values1 = new Vector<byte>(value1);

                while (lengthToExamine > (nuint)(Vector<byte>.Count - 1))
                {
                    Vector<byte> search = LoadVector(ref searchSpace, offset - (nuint)Vector<byte>.Count);
                    var matches = Vector.BitwiseOr(
                                    Vector.Equals(search, values0),
                                    Vector.Equals(search, values1));
                    if (Vector<byte>.Zero.Equals(matches))
                    {
                        offset -= (nuint)Vector<byte>.Count;
                        lengthToExamine -= (nuint)Vector<byte>.Count;
                        continue;
                    }

                    // Find offset of first match and add to current offset
                    return (int)(offset) - Vector<byte>.Count + LocateLastFoundByte(matches);
                }

                if (offset > 0)
                {
                    lengthToExamine = offset;
                    goto SequentialScan;
                }
            }
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType(ref short searchSpace, short value0, short value1, int length)
            => LastIndexOfAny(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length)
            => LastIndexOfAnyExcept(ref searchSpace, value0, value1, length);

        internal static int LastIndexOfAnyValueType(ref byte searchSpace, byte value0, byte value1, byte value2, int length)
        {
            Debug.Assert(length >= 0);

            uint uValue0 = value0; // Use uint for comparisons to avoid unnecessary 8->32 extensions
            uint uValue1 = value1;
            uint uValue2 = value2;
            nuint offset = (nuint)(uint)length; // Use nuint for arithmetic to avoid unnecessary 64->32->64 truncations
            nuint lengthToExamine = (nuint)(uint)length;

            if (Vector.IsHardwareAccelerated && length >= Vector<byte>.Count * 2)
            {
                lengthToExamine = UnalignedCountVectorFromEnd(ref searchSpace, length);
            }
        SequentialScan:
            uint lookUp;
            while (lengthToExamine >= 8)
            {
                lengthToExamine -= 8;
                offset -= 8;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 7);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found7;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 6);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found6;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 5);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found5;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 4);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found4;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found3;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
            }

            if (lengthToExamine >= 4)
            {
                lengthToExamine -= 4;
                offset -= 4;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 3);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found3;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 2);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found2;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset + 1);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found1;
                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
            }

            while (lengthToExamine > 0)
            {
                lengthToExamine -= 1;
                offset -= 1;

                lookUp = Unsafe.AddByteOffset(ref searchSpace, offset);
                if (uValue0 == lookUp || uValue1 == lookUp || uValue2 == lookUp)
                    goto Found;
            }

            if (Vector.IsHardwareAccelerated && (offset > 0))
            {
                lengthToExamine = (offset & (nuint)~(Vector<byte>.Count - 1));

                Vector<byte> values0 = new Vector<byte>(value0);
                Vector<byte> values1 = new Vector<byte>(value1);
                Vector<byte> values2 = new Vector<byte>(value2);

                while (lengthToExamine > (nuint)(Vector<byte>.Count - 1))
                {
                    Vector<byte> search = LoadVector(ref searchSpace, offset - (nuint)Vector<byte>.Count);

                    var matches = Vector.BitwiseOr(
                                    Vector.BitwiseOr(
                                        Vector.Equals(search, values0),
                                        Vector.Equals(search, values1)),
                                    Vector.Equals(search, values2));

                    if (Vector<byte>.Zero.Equals(matches))
                    {
                        offset -= (nuint)Vector<byte>.Count;
                        lengthToExamine -= (nuint)Vector<byte>.Count;
                        continue;
                    }

                    // Find offset of first match and add to current offset
                    return (int)(offset) - Vector<byte>.Count + LocateLastFoundByte(matches);
                }

                if (offset > 0)
                {
                    lengthToExamine = offset;
                    goto SequentialScan;
                }
            }
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType(ref short searchSpace, short value0, short value1, short value2, int length)
            => LastIndexOfAny(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length)
            => LastIndexOfAnyExcept(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length)
            => LastIndexOfAnyExcept(ref searchSpace, value0, value1, value2, value3, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ushort> LoadVector128(ref char start, nint offset)
            => Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ushort> LoadVector128(ref char start, nuint offset)
            => Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ushort> LoadVector256(ref char start, nint offset)
            => Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ushort> LoadVector256(ref char start, nuint offset)
            => Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, (nint)offset)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref char Add(ref char start, nuint offset) => ref Unsafe.Add(ref start, (nint)offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FindFirstMatchedLane(Vector128<byte> compareResult)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            // Mask to help find the first lane in compareResult that is set.
            // MSB 0x10 corresponds to 1st lane, 0x01 corresponds to 0th lane and so forth.
            Vector128<byte> mask = Vector128.Create((ushort)0x1001).AsByte();

            // Find the first lane that is set inside compareResult.
            Vector128<byte> maskedSelectedLanes = AdvSimd.And(compareResult, mask);
            Vector128<byte> pairwiseSelectedLane = AdvSimd.Arm64.AddPairwise(maskedSelectedLanes, maskedSelectedLanes);
            ulong selectedLanes = pairwiseSelectedLane.AsUInt64().ToScalar();

            // It should be handled by compareResult != Vector.Zero
            Debug.Assert(selectedLanes != 0);

            // Find the first lane that is set inside compareResult.
            return (uint)BitOperations.TrailingZeroCount(selectedLanes) >> 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindFirstMatchedLane(Vector128<ushort> compareResult)
        {
            Debug.Assert(AdvSimd.Arm64.IsSupported);

            Vector128<byte> pairwiseSelectedLane = AdvSimd.Arm64.AddPairwise(compareResult.AsByte(), compareResult.AsByte());
            ulong selectedLanes = pairwiseSelectedLane.AsUInt64().ToScalar();

            // It should be handled by compareResult != Vector.Zero
            Debug.Assert(selectedLanes != 0);

            return BitOperations.TrailingZeroCount(selectedLanes) >> 3;
        }

        // Vector sub-search adapted from https://github.com/aspnet/KestrelHttpServer/pull/1138
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateLastFoundChar(Vector<ushort> match)
        {
            var vector64 = Vector.AsVectorUInt64(match);
            ulong candidate = 0;
            int i = Vector<ulong>.Count - 1;

            // This pattern is only unrolled by the Jit if the limit is Vector<T>.Count
            // As such, we need a dummy iteration variable for that condition to be satisfied
            for (int j = 0; j < Vector<ulong>.Count; j++)
            {
                candidate = vector64[i];
                if (candidate != 0)
                {
                    break;
                }

                i--;
            }

            // Single LEA instruction with jitted const (using function result)
            return i * 4 + LocateLastFoundChar(candidate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LocateLastFoundChar(ulong match)
            => BitOperations.Log2(match) >> 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe nuint UnalignedCountVectorFromEnd(ref byte searchSpace, int length)
        {
            nint unaligned = (nint)Unsafe.AsPointer(ref searchSpace) & (Vector<byte>.Count - 1);
            return (nuint)(uint)(((length & (Vector<byte>.Count - 1)) + unaligned) & (Vector<byte>.Count - 1));
        }
    }
}
