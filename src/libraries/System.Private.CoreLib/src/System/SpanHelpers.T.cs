// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#pragma warning disable IDE0060 // https://github.com/dotnet/roslyn-analyzers/issues/6228

#pragma warning disable 8500 // sizeof of managed types

namespace System
{
    internal static partial class SpanHelpers // .T
    {
        public static unsafe void Fill<T>(ref T refData, nuint numElements, T value)
        {
            // Early checks to see if it's even possible to vectorize - JIT will turn these checks into consts.
            // - T cannot contain references (GC can't track references in vectors)
            // - Vectorization must be hardware-accelerated
            // - T's size must not exceed the vector's size
            // - T's size must be a whole power of 2

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) { goto CannotVectorize; }
            if (!Vector.IsHardwareAccelerated) { goto CannotVectorize; }
            if (sizeof(T) > Vector<byte>.Count) { goto CannotVectorize; }
            if (!BitOperations.IsPow2(sizeof(T))) { goto CannotVectorize; }

            if (numElements >= (uint)(Vector<byte>.Count / sizeof(T)))
            {
                // We have enough data for at least one vectorized write.

                T tmp = value; // Avoid taking address of the "value" argument. It would regress performance of the loops below.
                Vector<byte> vector;

                if (sizeof(T) == 1)
                {
                    vector = new Vector<byte>(Unsafe.As<T, byte>(ref tmp));
                }
                else if (sizeof(T) == 2)
                {
                    vector = (Vector<byte>)(new Vector<ushort>(Unsafe.As<T, ushort>(ref tmp)));
                }
                else if (sizeof(T) == 4)
                {
                    // special-case float since it's already passed in a SIMD reg
                    vector = (typeof(T) == typeof(float))
                        ? (Vector<byte>)(new Vector<float>((float)(object)tmp!))
                        : (Vector<byte>)(new Vector<uint>(Unsafe.As<T, uint>(ref tmp)));
                }
                else if (sizeof(T) == 8)
                {
                    // special-case double since it's already passed in a SIMD reg
                    vector = (typeof(T) == typeof(double))
                        ? (Vector<byte>)(new Vector<double>((double)(object)tmp!))
                        : (Vector<byte>)(new Vector<ulong>(Unsafe.As<T, ulong>(ref tmp)));
                }
                else if (sizeof(T) == 16)
                {
                    Vector128<byte> vec128 = Unsafe.As<T, Vector128<byte>>(ref tmp);
                    if (Vector<byte>.Count == 16)
                    {
                        vector = vec128.AsVector();
                    }
                    else if (Vector<byte>.Count == 32)
                    {
                        vector = Vector256.Create(vec128, vec128).AsVector();
                    }
                    else
                    {
                        Debug.Fail("Vector<T> isn't 128 or 256 bits in size?");
                        goto CannotVectorize;
                    }
                }
                else if (sizeof(T) == 32)
                {
                    if (Vector<byte>.Count == 32)
                    {
                        vector = Unsafe.As<T, Vector256<byte>>(ref tmp).AsVector();
                    }
                    else
                    {
                        Debug.Fail("Vector<T> isn't 256 bits in size?");
                        goto CannotVectorize;
                    }
                }
                else
                {
                    Debug.Fail("Vector<T> is greater than 256 bits in size?");
                    goto CannotVectorize;
                }

                ref byte refDataAsBytes = ref Unsafe.As<T, byte>(ref refData);
                nuint totalByteLength = numElements * (nuint)sizeof(T); // get this calculation ready ahead of time
                nuint stopLoopAtOffset = totalByteLength & (nuint)(nint)(2 * (int)-Vector<byte>.Count); // intentional sign extension carries the negative bit
                nuint offset = 0;

                // Loop, writing 2 vectors at a time.
                // Compare 'numElements' rather than 'stopLoopAtOffset' because we don't want a dependency
                // on the very recently calculated 'stopLoopAtOffset' value.

                if (numElements >= (uint)(2 * Vector<byte>.Count / sizeof(T)))
                {
                    do
                    {
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref refDataAsBytes, offset), vector);
                        Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref refDataAsBytes, offset + (nuint)Vector<byte>.Count), vector);
                        offset += (uint)(2 * Vector<byte>.Count);
                    } while (offset < stopLoopAtOffset);
                }

                // At this point, if any data remains to be written, it's strictly less than
                // 2 * sizeof(Vector) bytes. The loop above had us write an even number of vectors.
                // If the total byte length instead involves us writing an odd number of vectors, write
                // one additional vector now. The bit check below tells us if we're in an "odd vector
                // count" situation.

                if ((totalByteLength & (nuint)Vector<byte>.Count) != 0)
                {
                    Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref refDataAsBytes, offset), vector);
                }

                // It's possible that some small buffer remains to be populated - something that won't
                // fit an entire vector's worth of data. Instead of falling back to a loop, we'll write
                // a vector at the very end of the buffer. This may involve overwriting previously
                // populated data, which is fine since we're splatting the same value for all entries.
                // There's no need to perform a length check here because we already performed this
                // check before entering the vectorized code path.

                Unsafe.WriteUnaligned(ref Unsafe.AddByteOffset(ref refDataAsBytes, totalByteLength - (nuint)Vector<byte>.Count), vector);

                // And we're done!

                return;
            }

        CannotVectorize:

            // If we reached this point, we cannot vectorize this T, or there are too few
            // elements for us to vectorize. Fall back to an unrolled loop.

            nuint i = 0;

            // Write 8 elements at a time

            if (numElements >= 8)
            {
                nuint stopLoopAtOffset = numElements & ~(nuint)7;
                do
                {
                    Unsafe.Add(ref refData, (nint)i + 0) = value;
                    Unsafe.Add(ref refData, (nint)i + 1) = value;
                    Unsafe.Add(ref refData, (nint)i + 2) = value;
                    Unsafe.Add(ref refData, (nint)i + 3) = value;
                    Unsafe.Add(ref refData, (nint)i + 4) = value;
                    Unsafe.Add(ref refData, (nint)i + 5) = value;
                    Unsafe.Add(ref refData, (nint)i + 6) = value;
                    Unsafe.Add(ref refData, (nint)i + 7) = value;
                } while ((i += 8) < stopLoopAtOffset);
            }

            // Write next 4 elements if needed

            if ((numElements & 4) != 0)
            {
                Unsafe.Add(ref refData, (nint)i + 0) = value;
                Unsafe.Add(ref refData, (nint)i + 1) = value;
                Unsafe.Add(ref refData, (nint)i + 2) = value;
                Unsafe.Add(ref refData, (nint)i + 3) = value;
                i += 4;
            }

            // Write next 2 elements if needed

            if ((numElements & 2) != 0)
            {
                Unsafe.Add(ref refData, (nint)i + 0) = value;
                Unsafe.Add(ref refData, (nint)i + 1) = value;
                i += 2;
            }

            // Write final element if needed

            if ((numElements & 1) != 0)
            {
                Unsafe.Add(ref refData, (nint)i) = value;
            }
        }

        public static int IndexOf<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>?
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return 0;  // A zero-length sequence is always treated as "found" at the start of the search space.

            T valueHead = value;
            ref T valueTail = ref Unsafe.Add(ref value, 1);
            int valueTailLength = valueLength - 1;

            int index = 0;
            while (true)
            {
                Debug.Assert(0 <= index && index <= searchSpaceLength); // Ensures no deceptive underflows in the computation of "remainingSearchSpaceLength".
                int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Do a quick search for the first element of "value".
                int relativeIndex = IndexOf(ref Unsafe.Add(ref searchSpace, index), valueHead, remainingSearchSpaceLength);
                if (relativeIndex < 0)
                    break;
                index += relativeIndex;

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(ref Unsafe.Add(ref searchSpace, index + 1), ref valueTail, valueTailLength))
                    return index;  // The tail matched. Return a successful find.

                index++;
            }
            return -1;
        }

        // Adapted from IndexOf(...)
        public static unsafe bool Contains<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>?
        {
            Debug.Assert(length >= 0);

            nint index = 0; // Use nint for arithmetic to avoid unnecessary 64->32->64 truncations

            if (default(T) != null || (object?)value != null)
            {
                Debug.Assert(value is not null);

                while (length >= 8)
                {
                    length -= 8;

                    if (value.Equals(Unsafe.Add(ref searchSpace, index + 0)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 1)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 2)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 3)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 4)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 5)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 6)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 7)))
                    {
                        goto Found;
                    }

                    index += 8;
                }

                if (length >= 4)
                {
                    length -= 4;

                    if (value.Equals(Unsafe.Add(ref searchSpace, index + 0)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 1)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 2)) ||
                        value.Equals(Unsafe.Add(ref searchSpace, index + 3)))
                    {
                        goto Found;
                    }

                    index += 4;
                }

                while (length > 0)
                {
                    length--;

                    if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                        goto Found;

                    index += 1;
                }
            }
            else
            {
                nint len = length;
                for (index = 0; index < len; index++)
                {
                    if ((object?)Unsafe.Add(ref searchSpace, index) is null)
                    {
                        goto Found;
                    }
                }
            }

            return false;

        Found:
            return true;
        }

        public static unsafe int IndexOf<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>?
        {
            Debug.Assert(length >= 0);

            nint index = 0; // Use nint for arithmetic to avoid unnecessary 64->32->64 truncations
            if (default(T) != null || (object?)value != null)
            {
                Debug.Assert(value is not null);

                while (length >= 8)
                {
                    length -= 8;

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

                    index += 8;
                }

                if (length >= 4)
                {
                    length -= 4;

                    if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                        goto Found;
                    if (value.Equals(Unsafe.Add(ref searchSpace, index + 1)))
                        goto Found1;
                    if (value.Equals(Unsafe.Add(ref searchSpace, index + 2)))
                        goto Found2;
                    if (value.Equals(Unsafe.Add(ref searchSpace, index + 3)))
                        goto Found3;

                    index += 4;
                }

                while (length > 0)
                {
                    if (value.Equals(Unsafe.Add(ref searchSpace, index)))
                        goto Found;

                    index += 1;
                    length--;
                }
            }
            else
            {
                nint len = (nint)length;
                for (index = 0; index < len; index++)
                {
                    if ((object?)Unsafe.Add(ref searchSpace, index) is null)
                    {
                        goto Found;
                    }
                }
            }
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

        public static int IndexOfAny<T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>?
        {
            Debug.Assert(length >= 0);

            T lookUp;
            int index = 0;
            if (default(T) != null || ((object?)value0 != null && (object?)value1 != null))
            {
                Debug.Assert(value0 is not null && value1 is not null);

                while ((length - index) >= 8)
                {
                    lookUp = Unsafe.Add(ref searchSpace, index);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found;
                    lookUp = Unsafe.Add(ref searchSpace, index + 1);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found1;
                    lookUp = Unsafe.Add(ref searchSpace, index + 2);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found2;
                    lookUp = Unsafe.Add(ref searchSpace, index + 3);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found3;
                    lookUp = Unsafe.Add(ref searchSpace, index + 4);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found4;
                    lookUp = Unsafe.Add(ref searchSpace, index + 5);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found5;
                    lookUp = Unsafe.Add(ref searchSpace, index + 6);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found6;
                    lookUp = Unsafe.Add(ref searchSpace, index + 7);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found7;

                    index += 8;
                }

                if ((length - index) >= 4)
                {
                    lookUp = Unsafe.Add(ref searchSpace, index);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found;
                    lookUp = Unsafe.Add(ref searchSpace, index + 1);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found1;
                    lookUp = Unsafe.Add(ref searchSpace, index + 2);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found2;
                    lookUp = Unsafe.Add(ref searchSpace, index + 3);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found3;

                    index += 4;
                }

                while (index < length)
                {
                    lookUp = Unsafe.Add(ref searchSpace, index);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found;

                    index++;
                }
            }
            else
            {
                for (index = 0; index < length; index++)
                {
                    lookUp = Unsafe.Add(ref searchSpace, index);
                    if ((object?)lookUp is null)
                    {
                        if ((object?)value0 is null || (object?)value1 is null)
                        {
                            goto Found;
                        }
                    }
                    else if (lookUp.Equals(value0) || lookUp.Equals(value1))
                    {
                        goto Found;
                    }
                }
            }

            return -1;

        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return index;
        Found1:
            return index + 1;
        Found2:
            return index + 2;
        Found3:
            return index + 3;
        Found4:
            return index + 4;
        Found5:
            return index + 5;
        Found6:
            return index + 6;
        Found7:
            return index + 7;
        }

        public static int IndexOfAny<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>?
        {
            Debug.Assert(length >= 0);

            T lookUp;
            int index = 0;
            if (default(T) != null || ((object?)value0 != null && (object?)value1 != null && (object?)value2 != null))
            {
                Debug.Assert(value0 is not null && value1 is not null && value2 is not null);

                while ((length - index) >= 8)
                {
                    lookUp = Unsafe.Add(ref searchSpace, index);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found;
                    lookUp = Unsafe.Add(ref searchSpace, index + 1);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found1;
                    lookUp = Unsafe.Add(ref searchSpace, index + 2);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found2;
                    lookUp = Unsafe.Add(ref searchSpace, index + 3);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found3;
                    lookUp = Unsafe.Add(ref searchSpace, index + 4);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found4;
                    lookUp = Unsafe.Add(ref searchSpace, index + 5);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found5;
                    lookUp = Unsafe.Add(ref searchSpace, index + 6);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found6;
                    lookUp = Unsafe.Add(ref searchSpace, index + 7);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found7;

                    index += 8;
                }

                if ((length - index) >= 4)
                {
                    lookUp = Unsafe.Add(ref searchSpace, index);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found;
                    lookUp = Unsafe.Add(ref searchSpace, index + 1);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found1;
                    lookUp = Unsafe.Add(ref searchSpace, index + 2);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found2;
                    lookUp = Unsafe.Add(ref searchSpace, index + 3);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found3;

                    index += 4;
                }

                while (index < length)
                {
                    lookUp = Unsafe.Add(ref searchSpace, index);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found;

                    index++;
                }
            }
            else
            {
                for (index = 0; index < length; index++)
                {
                    lookUp = Unsafe.Add(ref searchSpace, index);
                    if ((object?)lookUp is null)
                    {
                        if ((object?)value0 is null || (object?)value1 is null || (object?)value2 is null)
                        {
                            goto Found;
                        }
                    }
                    else if (lookUp.Equals(value0) || lookUp.Equals(value1) || lookUp.Equals(value2))
                    {
                        goto Found;
                    }
                }
            }
            return -1;

        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return index;
        Found1:
            return index + 1;
        Found2:
            return index + 2;
        Found3:
            return index + 3;
        Found4:
            return index + 4;
        Found5:
            return index + 5;
        Found6:
            return index + 6;
        Found7:
            return index + 7;
        }

        public static int IndexOfAny<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>?
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return -1;  // A zero-length set of values is always treated as "not found".

            // For the following paragraph, let:
            //   n := length of haystack
            //   i := index of first occurrence of any needle within haystack
            //   l := length of needle array
            //
            // We use a naive non-vectorized search because we want to bound the complexity of IndexOfAny
            // to O(i * l) rather than O(n * l), or just O(n * l) if no needle is found. The reason for
            // this is that it's common for callers to invoke IndexOfAny immediately before slicing,
            // and when this is called in a loop, we want the entire loop to be bounded by O(n * l)
            // rather than O(n^2 * l).

            if (typeof(T).IsValueType)
            {
                // Calling ValueType.Equals (devirtualized), which takes 'this' byref. We'll make
                // a byval copy of the candidate from the search space in the outer loop, then in
                // the inner loop we'll pass a ref (as 'this') to each element in the needle.

                for (int i = 0; i < searchSpaceLength; i++)
                {
                    T candidate = Unsafe.Add(ref searchSpace, i);
                    for (int j = 0; j < valueLength; j++)
                    {
                        if (Unsafe.Add(ref value, j)!.Equals(candidate))
                        {
                            return i;
                        }
                    }
                }
            }
            else
            {
                // Calling IEquatable<T>.Equals (virtual dispatch). We'll perform the null check
                // in the outer loop instead of in the inner loop to save some branching.

                for (int i = 0; i < searchSpaceLength; i++)
                {
                    T candidate = Unsafe.Add(ref searchSpace, i);
                    if (candidate is not null)
                    {
                        for (int j = 0; j < valueLength; j++)
                        {
                            if (candidate.Equals(Unsafe.Add(ref value, j)))
                            {
                                return i;
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < valueLength; j++)
                        {
                            if (Unsafe.Add(ref value, j) is null)
                            {
                                return i;
                            }
                        }
                    }
                }
            }

            return -1; // not found
        }

        public static int LastIndexOf<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>?
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return searchSpaceLength;  // A zero-length sequence is always treated as "found" at the end of the search space.

            int valueTailLength = valueLength - 1;
            if (valueTailLength == 0)
            {
                return LastIndexOf(ref searchSpace, value, searchSpaceLength);
            }

            int index = 0;

            T valueHead = value;
            ref T valueTail = ref Unsafe.Add(ref value, 1);

            while (true)
            {
                Debug.Assert(0 <= index && index <= searchSpaceLength); // Ensures no deceptive underflows in the computation of "remainingSearchSpaceLength".
                int remainingSearchSpaceLength = searchSpaceLength - index - valueTailLength;
                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Do a quick search for the first element of "value".
                int relativeIndex = LastIndexOf(ref searchSpace, valueHead, remainingSearchSpaceLength);
                if (relativeIndex < 0)
                    break;

                // Found the first element of "value". See if the tail matches.
                if (SequenceEqual(ref Unsafe.Add(ref searchSpace, relativeIndex + 1), ref valueTail, valueTailLength))
                    return relativeIndex;  // The tail matched. Return a successful find.

                index += remainingSearchSpaceLength - relativeIndex;
            }
            return -1;
        }

        public static int LastIndexOf<T>(ref T searchSpace, T value, int length) where T : IEquatable<T>?
        {
            Debug.Assert(length >= 0);

            if (default(T) != null || (object?)value != null)
            {
                Debug.Assert(value is not null);

                while (length >= 8)
                {
                    length -= 8;

                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 7)))
                        goto Found7;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 6)))
                        goto Found6;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 5)))
                        goto Found5;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 4)))
                        goto Found4;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 3)))
                        goto Found3;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 2)))
                        goto Found2;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 1)))
                        goto Found1;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length)))
                        goto Found;
                }

                if (length >= 4)
                {
                    length -= 4;

                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 3)))
                        goto Found3;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 2)))
                        goto Found2;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length + 1)))
                        goto Found1;
                    if (value.Equals(Unsafe.Add(ref searchSpace, length)))
                        goto Found;
                }

                while (length > 0)
                {
                    length--;

                    if (value.Equals(Unsafe.Add(ref searchSpace, length)))
                        goto Found;
                }
            }
            else
            {
                for (length--; length >= 0; length--)
                {
                    if ((object?)Unsafe.Add(ref searchSpace, length) is null)
                    {
                        goto Found;
                    }
                }
            }

            return -1;

        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return length;
        Found1:
            return length + 1;
        Found2:
            return length + 2;
        Found3:
            return length + 3;
        Found4:
            return length + 4;
        Found5:
            return length + 5;
        Found6:
            return length + 6;
        Found7:
            return length + 7;
        }

        public static int LastIndexOfAny<T>(ref T searchSpace, T value0, T value1, int length) where T : IEquatable<T>?
        {
            Debug.Assert(length >= 0);

            T lookUp;
            if (default(T) != null || ((object?)value0 != null && (object?)value1 != null))
            {
                Debug.Assert(value0 is not null && value1 is not null);

                while (length >= 8)
                {
                    length -= 8;

                    lookUp = Unsafe.Add(ref searchSpace, length + 7);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found7;
                    lookUp = Unsafe.Add(ref searchSpace, length + 6);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found6;
                    lookUp = Unsafe.Add(ref searchSpace, length + 5);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found5;
                    lookUp = Unsafe.Add(ref searchSpace, length + 4);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found4;
                    lookUp = Unsafe.Add(ref searchSpace, length + 3);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found3;
                    lookUp = Unsafe.Add(ref searchSpace, length + 2);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found2;
                    lookUp = Unsafe.Add(ref searchSpace, length + 1);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found1;
                    lookUp = Unsafe.Add(ref searchSpace, length);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found;
                }

                if (length >= 4)
                {
                    length -= 4;

                    lookUp = Unsafe.Add(ref searchSpace, length + 3);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found3;
                    lookUp = Unsafe.Add(ref searchSpace, length + 2);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found2;
                    lookUp = Unsafe.Add(ref searchSpace, length + 1);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found1;
                    lookUp = Unsafe.Add(ref searchSpace, length);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found;
                }

                while (length > 0)
                {
                    length--;

                    lookUp = Unsafe.Add(ref searchSpace, length);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp))
                        goto Found;
                }
            }
            else
            {
                for (length--; length >= 0; length--)
                {
                    lookUp = Unsafe.Add(ref searchSpace, length);
                    if ((object?)lookUp is null)
                    {
                        if ((object?)value0 is null || (object?)value1 is null)
                        {
                            goto Found;
                        }
                    }
                    else if (lookUp.Equals(value0) || lookUp.Equals(value1))
                    {
                        goto Found;
                    }
                }
            }

            return -1;

        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return length;
        Found1:
            return length + 1;
        Found2:
            return length + 2;
        Found3:
            return length + 3;
        Found4:
            return length + 4;
        Found5:
            return length + 5;
        Found6:
            return length + 6;
        Found7:
            return length + 7;
        }

        public static int LastIndexOfAny<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : IEquatable<T>?
        {
            Debug.Assert(length >= 0);

            T lookUp;
            if (default(T) != null || ((object?)value0 != null && (object?)value1 != null && (object?)value2 != null))
            {
                Debug.Assert(value0 is not null && value1 is not null && value2 is not null);

                while (length >= 8)
                {
                    length -= 8;

                    lookUp = Unsafe.Add(ref searchSpace, length + 7);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found7;
                    lookUp = Unsafe.Add(ref searchSpace, length + 6);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found6;
                    lookUp = Unsafe.Add(ref searchSpace, length + 5);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found5;
                    lookUp = Unsafe.Add(ref searchSpace, length + 4);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found4;
                    lookUp = Unsafe.Add(ref searchSpace, length + 3);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found3;
                    lookUp = Unsafe.Add(ref searchSpace, length + 2);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found2;
                    lookUp = Unsafe.Add(ref searchSpace, length + 1);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found1;
                    lookUp = Unsafe.Add(ref searchSpace, length);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found;
                }

                if (length >= 4)
                {
                    length -= 4;

                    lookUp = Unsafe.Add(ref searchSpace, length + 3);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found3;
                    lookUp = Unsafe.Add(ref searchSpace, length + 2);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found2;
                    lookUp = Unsafe.Add(ref searchSpace, length + 1);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found1;
                    lookUp = Unsafe.Add(ref searchSpace, length);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found;
                }

                while (length > 0)
                {
                    length--;

                    lookUp = Unsafe.Add(ref searchSpace, length);
                    if (value0.Equals(lookUp) || value1.Equals(lookUp) || value2.Equals(lookUp))
                        goto Found;
                }
            }
            else
            {
                for (length--; length >= 0; length--)
                {
                    lookUp = Unsafe.Add(ref searchSpace, length);
                    if ((object?)lookUp is null)
                    {
                        if ((object?)value0 is null || (object?)value1 is null || (object?)value2 is null)
                        {
                            goto Found;
                        }
                    }
                    else if (lookUp.Equals(value0) || lookUp.Equals(value1) || lookUp.Equals(value2))
                    {
                        goto Found;
                    }
                }
            }

            return -1;

        Found: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return length;
        Found1:
            return length + 1;
        Found2:
            return length + 2;
        Found3:
            return length + 3;
        Found4:
            return length + 4;
        Found5:
            return length + 5;
        Found6:
            return length + 6;
        Found7:
            return length + 7;
        }

        public static int LastIndexOfAny<T>(ref T searchSpace, int searchSpaceLength, ref T value, int valueLength) where T : IEquatable<T>?
        {
            Debug.Assert(searchSpaceLength >= 0);
            Debug.Assert(valueLength >= 0);

            if (valueLength == 0)
                return -1;  // A zero-length set of values is always treated as "not found".

            // See comments in IndexOfAny(ref T, int, ref T, int) above regarding algorithmic complexity concerns.
            // This logic is similar, but it runs backward.

            if (typeof(T).IsValueType)
            {
                for (int i = searchSpaceLength - 1; i >= 0; i--)
                {
                    T candidate = Unsafe.Add(ref searchSpace, i);
                    for (int j = 0; j < valueLength; j++)
                    {
                        if (Unsafe.Add(ref value, j)!.Equals(candidate))
                        {
                            return i;
                        }
                    }
                }
            }
            else
            {
                for (int i = searchSpaceLength - 1; i >= 0; i--)
                {
                    T candidate = Unsafe.Add(ref searchSpace, i);
                    if (candidate is not null)
                    {
                        for (int j = 0; j < valueLength; j++)
                        {
                            if (candidate.Equals(Unsafe.Add(ref value, j)))
                            {
                                return i;
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < valueLength; j++)
                        {
                            if (Unsafe.Add(ref value, j) is null)
                            {
                                return i;
                            }
                        }
                    }
                }
            }

            return -1; // not found
        }

        internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, int length)
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

            for (int i = 0; i < length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(Unsafe.Add(ref searchSpace, i), value0))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int LastIndexOfAnyExcept<T>(ref T searchSpace, T value0, int length)
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

            for (int i = length - 1; i >= 0; i--)
            {
                if (!EqualityComparer<T>.Default.Equals(Unsafe.Add(ref searchSpace, i), value0))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, int length)
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

            for (int i = 0; i < length; i++)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if (!EqualityComparer<T>.Default.Equals(current, value0) && !EqualityComparer<T>.Default.Equals(current, value1))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int LastIndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, int length)
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

            for (int i = length - 1; i >= 0; i--)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if (!EqualityComparer<T>.Default.Equals(current, value0) && !EqualityComparer<T>.Default.Equals(current, value1))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, T value2, int length)
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

            for (int i = 0; i < length; i++)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if (!EqualityComparer<T>.Default.Equals(current, value0)
                    && !EqualityComparer<T>.Default.Equals(current, value1)
                    && !EqualityComparer<T>.Default.Equals(current, value2))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int LastIndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, T value2, int length)
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

            for (int i = length - 1; i >= 0; i--)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if (!EqualityComparer<T>.Default.Equals(current, value0)
                    && !EqualityComparer<T>.Default.Equals(current, value1)
                    && !EqualityComparer<T>.Default.Equals(current, value2))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int IndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length)
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

            for (int i = 0; i < length; i++)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if (!EqualityComparer<T>.Default.Equals(current, value0)
                    && !EqualityComparer<T>.Default.Equals(current, value1)
                    && !EqualityComparer<T>.Default.Equals(current, value2)
                    && !EqualityComparer<T>.Default.Equals(current, value3))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int LastIndexOfAnyExcept<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length)
        {
            Debug.Assert(length >= 0, "Expected non-negative length");

            for (int i = length - 1; i >= 0; i--)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if (!EqualityComparer<T>.Default.Equals(current, value0)
                    && !EqualityComparer<T>.Default.Equals(current, value1)
                    && !EqualityComparer<T>.Default.Equals(current, value2)
                    && !EqualityComparer<T>.Default.Equals(current, value3))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool SequenceEqual<T>(ref T first, ref T second, int length) where T : IEquatable<T>?
        {
            Debug.Assert(length >= 0);

            if (Unsafe.AreSame(ref first, ref second))
                goto Equal;

            nint index = 0; // Use nint for arithmetic to avoid unnecessary 64->32->64 truncations
            T lookUp0;
            T lookUp1;
            while (length >= 8)
            {
                length -= 8;

                lookUp0 = Unsafe.Add(ref first, index);
                lookUp1 = Unsafe.Add(ref second, index);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 1);
                lookUp1 = Unsafe.Add(ref second, index + 1);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 2);
                lookUp1 = Unsafe.Add(ref second, index + 2);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 3);
                lookUp1 = Unsafe.Add(ref second, index + 3);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 4);
                lookUp1 = Unsafe.Add(ref second, index + 4);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 5);
                lookUp1 = Unsafe.Add(ref second, index + 5);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 6);
                lookUp1 = Unsafe.Add(ref second, index + 6);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 7);
                lookUp1 = Unsafe.Add(ref second, index + 7);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;

                index += 8;
            }

            if (length >= 4)
            {
                length -= 4;

                lookUp0 = Unsafe.Add(ref first, index);
                lookUp1 = Unsafe.Add(ref second, index);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 1);
                lookUp1 = Unsafe.Add(ref second, index + 1);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 2);
                lookUp1 = Unsafe.Add(ref second, index + 2);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                lookUp0 = Unsafe.Add(ref first, index + 3);
                lookUp1 = Unsafe.Add(ref second, index + 3);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;

                index += 4;
            }

            while (length > 0)
            {
                lookUp0 = Unsafe.Add(ref first, index);
                lookUp1 = Unsafe.Add(ref second, index);
                if (!(lookUp0?.Equals(lookUp1) ?? (object?)lookUp1 is null))
                    goto NotEqual;
                index += 1;
                length--;
            }

        Equal:
            return true;

        NotEqual: // Workaround for https://github.com/dotnet/runtime/issues/8795
            return false;
        }

        public static int SequenceCompareTo<T>(ref T first, int firstLength, ref T second, int secondLength)
            where T : IComparable<T>?
        {
            Debug.Assert(firstLength >= 0);
            Debug.Assert(secondLength >= 0);

            int minLength = firstLength;
            if (minLength > secondLength)
                minLength = secondLength;
            for (int i = 0; i < minLength; i++)
            {
                T lookUp = Unsafe.Add(ref second, i);
                int result = (Unsafe.Add(ref first, i)?.CompareTo(lookUp) ?? (((object?)lookUp is null) ? 0 : -1));
                if (result != 0)
                    return result;
            }
            return firstLength.CompareTo(secondLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool ContainsValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
        {
            if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(T) == typeof(short) && PackedSpanHelpers.CanUsePackedIndexOf(value))
            {
                return PackedSpanHelpers.Contains(ref Unsafe.As<T, short>(ref searchSpace), *(short*)&value, length);
            }

            return NonPackedContainsValueType(ref searchSpace, value, length);
        }

        internal static bool NonPackedContainsValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
            {
                nuint offset = 0;

                while (length >= 8)
                {
                    length -= 8;

                    if (Unsafe.Add(ref searchSpace, offset) == value
                     || Unsafe.Add(ref searchSpace, offset + 1) == value
                     || Unsafe.Add(ref searchSpace, offset + 2) == value
                     || Unsafe.Add(ref searchSpace, offset + 3) == value
                     || Unsafe.Add(ref searchSpace, offset + 4) == value
                     || Unsafe.Add(ref searchSpace, offset + 5) == value
                     || Unsafe.Add(ref searchSpace, offset + 6) == value
                     || Unsafe.Add(ref searchSpace, offset + 7) == value)
                    {
                        return true;
                    }

                    offset += 8;
                }

                if (length >= 4)
                {
                    length -= 4;

                    if (Unsafe.Add(ref searchSpace, offset) == value
                     || Unsafe.Add(ref searchSpace, offset + 1) == value
                     || Unsafe.Add(ref searchSpace, offset + 2) == value
                     || Unsafe.Add(ref searchSpace, offset + 3) == value)
                    {
                        return true;
                    }

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    if (Unsafe.Add(ref searchSpace, offset) == value) return true;

                    offset += 1;
                }
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<T>.Count)
            {
                Vector512<T> current, values = Vector512.Create(value);
                ref T currentSearchSpace = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector512<T>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector512.LoadUnsafe(ref currentSearchSpace);

                    if (Vector512.EqualsAny(values, current))
                    {
                        return true;
                    }

                    currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<T>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector512<T>.Count != 0)
                {
                    current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);

                    if (Vector512.EqualsAny(values, current))
                    {
                        return true;
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<T>.Count)
            {
                Vector256<T> equals, values = Vector256.Create(value);
                ref T currentSearchSpace = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector256<T>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    equals = Vector256.Equals(values, Vector256.LoadUnsafe(ref currentSearchSpace));
                    if (equals == Vector256<T>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<T>.Count);
                        continue;
                    }

                    return true;
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<T>.Count != 0)
                {
                    equals = Vector256.Equals(values, Vector256.LoadUnsafe(ref oneVectorAwayFromEnd));
                    if (equals != Vector256<T>.Zero)
                    {
                        return true;
                    }
                }
            }
            else
            {
                Vector128<T> equals, values = Vector128.Create(value);
                ref T currentSearchSpace = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector128<T>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    equals = Vector128.Equals(values, Vector128.LoadUnsafe(ref currentSearchSpace));
                    if (equals == Vector128<T>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<T>.Count);
                        continue;
                    }

                    return true;
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<T>.Count != 0)
                {
                    equals = Vector128.Equals(values, Vector128.LoadUnsafe(ref oneVectorAwayFromEnd));
                    if (equals != Vector128<T>.Zero)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfChar(ref char searchSpace, char value, int length)
            => IndexOfValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int NonPackedIndexOfChar(ref char searchSpace, char value, int length) =>
            NonPackedIndexOfValueType<short, DontNegate<short>>(ref Unsafe.As<char, short>(ref searchSpace), (short)value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => IndexOfValueType<T, DontNegate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => IndexOfValueType<T, Negate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int IndexOfValueType<TValue, TNegator>(ref TValue searchSpace, TValue value, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(TValue) == typeof(short) && PackedSpanHelpers.CanUsePackedIndexOf(value))
            {
                return typeof(TNegator) == typeof(DontNegate<short>)
                    ? PackedSpanHelpers.IndexOf(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)&value, length)
                    : PackedSpanHelpers.IndexOfAnyExcept(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)&value, length);
            }

            return NonPackedIndexOfValueType<TValue, TNegator>(ref searchSpace, value, length);
        }

        internal static int NonPackedIndexOfValueType<TValue, TNegator>(ref TValue searchSpace, TValue value, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = 0;

                while (length >= 8)
                {
                    length -= 8;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 1) == value)) goto Found1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 2) == value)) goto Found2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 3) == value)) goto Found3;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 4) == value)) goto Found4;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 5) == value)) goto Found5;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 6) == value)) goto Found6;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 7) == value)) goto Found7;

                    offset += 8;
                }

                if (length >= 4)
                {
                    length -= 4;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 1) == value)) goto Found1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 2) == value)) goto Found2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 3) == value)) goto Found3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;

                    offset += 1;
                }
                return -1;
            Found7:
                return (int)(offset + 7);
            Found6:
                return (int)(offset + 6);
            Found5:
                return (int)(offset + 5);
            Found4:
                return (int)(offset + 4);
            Found3:
                return (int)(offset + 3);
            Found2:
                return (int)(offset + 2);
            Found1:
                return (int)(offset + 1);
            Found:
                return (int)(offset);
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> current, values = Vector512.Create(value);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector512.LoadUnsafe(ref currentSearchSpace);

                    if (TNegator.HasMatch(values, current))
                    {
                        return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, TNegator.GetMatchMask(values, current));
                    }

                    currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<TValue>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector512<TValue>.Count != 0)
                {
                    current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);

                    if (TNegator.HasMatch(values, current))
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, TNegator.GetMatchMask(values, current));
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, values = Vector256.Create(value);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values, Vector256.LoadUnsafe(ref currentSearchSpace)));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values, Vector256.LoadUnsafe(ref oneVectorAwayFromEnd)));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, values = Vector128.Create(value);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values, Vector128.LoadUnsafe(ref currentSearchSpace)));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values, Vector128.LoadUnsafe(ref oneVectorAwayFromEnd)));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyChar(ref char searchSpace, char value0, char value1, int length)
            => IndexOfAnyValueType(ref Unsafe.As<char, short>(ref searchSpace), (short)value0, (short)value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(TValue) == typeof(short) && PackedSpanHelpers.CanUsePackedIndexOf(value0) && PackedSpanHelpers.CanUsePackedIndexOf(value1))
            {
                if ((*(char*)&value0 ^ *(char*)&value1) == 0x20)
                {
                    char lowerCase = (char)Math.Max(*(char*)&value0, *(char*)&value1);

                    return typeof(TNegator) == typeof(DontNegate<short>)
                        ? PackedSpanHelpers.IndexOfAnyIgnoreCase(ref Unsafe.As<TValue, char>(ref searchSpace), lowerCase, length)
                        : PackedSpanHelpers.IndexOfAnyExceptIgnoreCase(ref Unsafe.As<TValue, char>(ref searchSpace), lowerCase, length);
                }

                return typeof(TNegator) == typeof(DontNegate<short>)
                    ? PackedSpanHelpers.IndexOfAny(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)&value0, *(char*)&value1, length)
                    : PackedSpanHelpers.IndexOfAnyExcept(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)&value0, *(char*)&value1, length);
            }

            return NonPackedIndexOfAnyValueType<TValue, TNegator>(ref searchSpace, value0, value1, length);
        }

        // having INumber<T> constraint here allows to use == operator and get better perf compared to .Equals
        internal static int NonPackedIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = 0;
                TValue lookUp;

                if (typeof(TValue) == typeof(byte)) // this optimization is beneficial only to byte
                {
                    while (length >= 8)
                    {
                        length -= 8;

                        ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                        lookUp = current;
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
                        lookUp = Unsafe.Add(ref current, 1);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found1;
                        lookUp = Unsafe.Add(ref current, 2);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found2;
                        lookUp = Unsafe.Add(ref current, 3);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found3;
                        lookUp = Unsafe.Add(ref current, 4);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found4;
                        lookUp = Unsafe.Add(ref current, 5);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found5;
                        lookUp = Unsafe.Add(ref current, 6);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found6;
                        lookUp = Unsafe.Add(ref current, 7);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found7;

                        offset += 8;
                    }
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;

                    offset += 1;
                }
                return -1;
            Found7:
                return (int)(offset + 7);
            Found6:
                return (int)(offset + 6);
            Found5:
                return (int)(offset + 5);
            Found4:
                return (int)(offset + 4);
            Found3:
                return (int)(offset + 3);
            Found2:
                return (int)(offset + 2);
            Found1:
                return (int)(offset + 1);
            Found:
                return (int)(offset);
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector512.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current));
                    if (equals == Vector512<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector512<TValue>.Count != 0)
                {
                    current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current));
                    if (equals != Vector512<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(TValue) == typeof(short) && PackedSpanHelpers.CanUsePackedIndexOf(value0) && PackedSpanHelpers.CanUsePackedIndexOf(value1) && PackedSpanHelpers.CanUsePackedIndexOf(value2))
            {
                return typeof(TNegator) == typeof(DontNegate<short>)
                    ? PackedSpanHelpers.IndexOfAny(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)&value0, *(char*)&value1, *(char*)&value2, length)
                    : PackedSpanHelpers.IndexOfAnyExcept(ref Unsafe.As<TValue, char>(ref searchSpace), *(char*)&value0, *(char*)&value1, *(char*)&value2, length);
            }

            return NonPackedIndexOfAnyValueType<TValue, TNegator>(ref searchSpace, value0, value1, value2, length);
        }

        internal static int NonPackedIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = 0;
                TValue lookUp;

                if (typeof(TValue) == typeof(byte)) // this optimization is beneficial only to byte
                {
                    while (length >= 8)
                    {
                        length -= 8;

                        ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                        lookUp = current;
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found;
                        lookUp = Unsafe.Add(ref current, 1);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found1;
                        lookUp = Unsafe.Add(ref current, 2);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found2;
                        lookUp = Unsafe.Add(ref current, 3);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found3;
                        lookUp = Unsafe.Add(ref current, 4);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found4;
                        lookUp = Unsafe.Add(ref current, 5);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found5;
                        lookUp = Unsafe.Add(ref current, 6);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found6;
                        lookUp = Unsafe.Add(ref current, 7);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found7;

                        offset += 8;
                    }
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found;

                    offset += 1;
                }
                return -1;
            Found7:
                return (int)(offset + 7);
            Found6:
                return (int)(offset + 6);
            Found5:
                return (int)(offset + 5);
            Found4:
                return (int)(offset + 4);
            Found3:
                return (int)(offset + 3);
            Found2:
                return (int)(offset + 2);
            Found1:
                return (int)(offset + 1);
            Found:
                return (int)(offset);
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1), values2 = Vector512.Create(value2);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector512.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current) | Vector512.Equals(values2, current));
                    if (equals == Vector512<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector512<TValue>.Count != 0)
                {
                    current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current) | Vector512.Equals(values2, current));
                    if (equals != Vector512<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1), values2 = Vector256.Create(value2);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current) | Vector256.Equals(values2, current));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current) | Vector256.Equals(values2, current));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1), values2 = Vector128.Create(value2);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current) | Vector128.Equals(values2, current));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current) | Vector128.Equals(values2, current));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, length);

        private static int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, TValue value3, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = 0;
                TValue lookUp;

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto Found;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto Found1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto Found2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto Found3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto Found;

                    offset += 1;
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
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1), values2 = Vector512.Create(value2), values3 = Vector512.Create(value3);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector512<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector512.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current)
                        | Vector512.Equals(values2, current) | Vector512.Equals(values3, current));
                    if (equals == Vector512<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector512<TValue>.Count != 0)
                {
                    current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current)
                        | Vector512.Equals(values2, current) | Vector512.Equals(values3, current));
                    if (equals != Vector512<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1), values2 = Vector256.Create(value2), values3 = Vector256.Create(value3);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current)
                        | Vector256.Equals(values2, current) | Vector256.Equals(values3, current));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current)
                        | Vector256.Equals(values2, current) | Vector256.Equals(values3, current));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1), values2 = Vector128.Create(value2), values3 = Vector128.Create(value3);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current)
                        | Vector128.Equals(values2, current) | Vector128.Equals(values3, current));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current)
                        | Vector128.Equals(values2, current) | Vector128.Equals(values3, current));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, value4, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length) where T : struct, INumber<T>
            => IndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, value4, length);

        private static int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, TValue value3, TValue value4, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = 0;
                TValue lookUp;

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) goto Found;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) goto Found1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) goto Found2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) goto Found3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) goto Found;

                    offset += 1;
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
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1),
                    values2 = Vector512.Create(value2), values3 = Vector512.Create(value3), values4 = Vector512.Create(value4);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector512<TValue>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector512.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current) | Vector512.Equals(values2, current)
                           | Vector512.Equals(values3, current) | Vector512.Equals(values4, current));
                    if (equals == Vector512<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector512<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector512<TValue>.Count != 0)
                {
                    current = Vector512.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(values0, current) | Vector512.Equals(values1, current) | Vector512.Equals(values2, current)
                           | Vector512.Equals(values3, current) | Vector512.Equals(values4, current));
                    if (equals != Vector512<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1),
                    values2 = Vector256.Create(value2), values3 = Vector256.Create(value3), values4 = Vector256.Create(value4);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector256<TValue>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current) | Vector256.Equals(values2, current)
                           | Vector256.Equals(values3, current) | Vector256.Equals(values4, current));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values0, current) | Vector256.Equals(values1, current) | Vector256.Equals(values2, current)
                           | Vector256.Equals(values3, current) | Vector256.Equals(values4, current));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1),
                    values2 = Vector128.Create(value2), values3 = Vector128.Create(value3), values4 = Vector128.Create(value4);
                ref TValue currentSearchSpace = ref searchSpace;
                ref TValue oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector128<TValue>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current) | Vector128.Equals(values2, current)
                           | Vector128.Equals(values3, current) | Vector128.Equals(values4, current));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values0, current) | Vector128.Equals(values1, current) | Vector128.Equals(values2, current)
                           | Vector128.Equals(values3, current) | Vector128.Equals(values4, current));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => LastIndexOfValueType<T, DontNegate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => LastIndexOfValueType<T, Negate<T>>(ref searchSpace, value, length);

        private static int LastIndexOfValueType<TValue, TNegator>(ref TValue searchSpace, TValue value, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = (nuint)length - 1;

                while (length >= 8)
                {
                    length -= 8;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 1) == value)) goto FoundM1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 2) == value)) goto FoundM2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 3) == value)) goto FoundM3;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 4) == value)) goto FoundM4;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 5) == value)) goto FoundM5;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 6) == value)) goto FoundM6;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 7) == value)) goto FoundM7;

                    offset -= 8;
                }

                if (length >= 4)
                {
                    length -= 4;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 1) == value)) goto FoundM1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 2) == value)) goto FoundM2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 3) == value)) goto FoundM3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) goto Found;

                    offset -= 1;
                }
                return -1;
            FoundM7:
                return (int)(offset - 7);
            FoundM6:
                return (int)(offset - 6);
            FoundM5:
                return (int)(offset - 5);
            FoundM4:
                return (int)(offset - 4);
            FoundM3:
                return (int)(offset - 3);
            FoundM2:
                return (int)(offset - 2);
            FoundM1:
                return (int)(offset - 1);
            Found:
                return (int)(offset);
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                return SimdImpl<Vector512<TValue>>(ref searchSpace, value, length);
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                return SimdImpl<Vector256<TValue>>(ref searchSpace, value, length);
            }
            else
            {
                return SimdImpl<Vector128<TValue>>(ref searchSpace, value, length);
            }

            static int SimdImpl<TVector>(ref TValue searchSpace, TValue value, int length)
                where TVector : struct, ISimdVector<TVector, TValue>
            {
                TVector current;
                TVector values = TVector.Create(value);

                int offset = length - TVector.Count;

                // Loop until either we've finished all elements -or- there's one or less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = TVector.LoadUnsafe(ref searchSpace, (uint)(offset));

                    if (TNegator.HasMatch(values, current))
                    {
                        return offset + TVector.IndexOfLastMatch(TNegator.GetMatchMask(values, current));
                    }

                    offset -= TVector.Count;
                }

                // Process the first vector in the search space.

                current = TVector.LoadUnsafe(ref searchSpace);

                if (TNegator.HasMatch(values, current))
                {
                    return TVector.IndexOfLastMatch(TNegator.GetMatchMask(values, current));
                }

                return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, length);

        private static int LastIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = (nuint)length - 1;
                TValue lookUp;

                if (typeof(TValue) == typeof(byte)) // this optimization is beneficial only to byte
                {
                    while (length >= 8)
                    {
                        length -= 8;

                        ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                        lookUp = current;
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
                        lookUp = Unsafe.Add(ref current, -1);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM1;
                        lookUp = Unsafe.Add(ref current, -2);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM2;
                        lookUp = Unsafe.Add(ref current, -3);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM3;
                        lookUp = Unsafe.Add(ref current, -4);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM4;
                        lookUp = Unsafe.Add(ref current, -5);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM5;
                        lookUp = Unsafe.Add(ref current, -6);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM6;
                        lookUp = Unsafe.Add(ref current, -7);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM7;

                        offset -= 8;
                    }
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;
                    lookUp = Unsafe.Add(ref current, -1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM1;
                    lookUp = Unsafe.Add(ref current, -2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM2;
                    lookUp = Unsafe.Add(ref current, -3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto FoundM3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) goto Found;

                    offset -= 1;
                }
                return -1;
            FoundM7:
                return (int)(offset - 7);
            FoundM6:
                return (int)(offset - 6);
            FoundM5:
                return (int)(offset - 5);
            FoundM4:
                return (int)(offset - 4);
            FoundM3:
                return (int)(offset - 3);
            FoundM2:
                return (int)(offset - 2);
            FoundM1:
                return (int)(offset - 1);
            Found:
                return (int)(offset);
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1);
                nint offset = length - Vector512<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector512.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(current, values0) | Vector512.Equals(current, values1));

                    if (equals == Vector512<TValue>.Zero)
                    {
                        offset -= Vector512<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector512.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector512.Equals(current, values0) | Vector512.Equals(current, values1));

                if (equals != Vector512<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1);
                nint offset = length - Vector256<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1));

                    if (equals == Vector256<TValue>.Zero)
                    {
                        offset -= Vector256<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector256.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1));

                if (equals != Vector256<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1);
                nint offset = length - Vector128<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        offset -= Vector128<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector128.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1));

                if (equals != Vector128<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, length);

        private static int LastIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = (nuint)length - 1;
                TValue lookUp;

                if (typeof(TValue) == typeof(byte)) // this optimization is beneficial only to byte
                {
                    while (length >= 8)
                    {
                        length -= 8;

                        ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                        lookUp = current;
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found;
                        lookUp = Unsafe.Add(ref current, -1);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM1;
                        lookUp = Unsafe.Add(ref current, -2);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM2;
                        lookUp = Unsafe.Add(ref current, -3);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM3;
                        lookUp = Unsafe.Add(ref current, -4);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM4;
                        lookUp = Unsafe.Add(ref current, -5);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM5;
                        lookUp = Unsafe.Add(ref current, -6);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM6;
                        lookUp = Unsafe.Add(ref current, -7);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM7;

                        offset -= 8;
                    }
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found;
                    lookUp = Unsafe.Add(ref current, -1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM1;
                    lookUp = Unsafe.Add(ref current, -2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM2;
                    lookUp = Unsafe.Add(ref current, -3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto FoundM3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) goto Found;

                    offset -= 1;
                }
                return -1;
            FoundM7:
                return (int)(offset - 7);
            FoundM6:
                return (int)(offset - 6);
            FoundM5:
                return (int)(offset - 5);
            FoundM4:
                return (int)(offset - 4);
            FoundM3:
                return (int)(offset - 3);
            FoundM2:
                return (int)(offset - 2);
            FoundM1:
                return (int)(offset - 1);
            Found:
                return (int)(offset);
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1), values2 = Vector512.Create(value2);
                nint offset = length - Vector512<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector512.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(current, values0) | Vector512.Equals(current, values1) | Vector512.Equals(current, values2));

                    if (equals == Vector512<TValue>.Zero)
                    {
                        offset -= Vector512<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector512.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector512.Equals(current, values0) | Vector512.Equals(current, values1) | Vector512.Equals(current, values2));

                if (equals != Vector512<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1), values2 = Vector256.Create(value2);
                nint offset = length - Vector256<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1) | Vector256.Equals(current, values2));

                    if (equals == Vector256<TValue>.Zero)
                    {
                        offset -= Vector256<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector256.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1) | Vector256.Equals(current, values2));

                if (equals != Vector256<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1), values2 = Vector128.Create(value2);
                nint offset = length - Vector128<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1) | Vector128.Equals(current, values2));

                    if (equals == Vector128<TValue>.Zero)
                    {
                        offset -= Vector128<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector128.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1) | Vector128.Equals(current, values2));

                if (equals != Vector128<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, length);

        private static int LastIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, TValue value3, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = (nuint)length - 1;
                TValue lookUp;

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto Found;
                    lookUp = Unsafe.Add(ref current, -1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto FoundM1;
                    lookUp = Unsafe.Add(ref current, -2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto FoundM2;
                    lookUp = Unsafe.Add(ref current, -3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto FoundM3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) goto Found;

                    offset -= 1;
                }
                return -1;
            FoundM3:
                return (int)(offset - 3);
            FoundM2:
                return (int)(offset - 2);
            FoundM1:
                return (int)(offset - 1);
            Found:
                return (int)(offset);
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1), values2 = Vector512.Create(value2), values3 = Vector512.Create(value3);
                nint offset = length - Vector512<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector512.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(current, values0) | Vector512.Equals(current, values1)
                                            | Vector512.Equals(current, values2) | Vector512.Equals(current, values3));
                    if (equals == Vector512<TValue>.Zero)
                    {
                        offset -= Vector512<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector512.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector512.Equals(current, values0) | Vector512.Equals(current, values1) | Vector512.Equals(current, values2) | Vector512.Equals(current, values3));

                if (equals != Vector512<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1), values2 = Vector256.Create(value2), values3 = Vector256.Create(value3);
                nint offset = length - Vector256<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1)
                                            | Vector256.Equals(current, values2) | Vector256.Equals(current, values3));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        offset -= Vector256<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector256.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1) | Vector256.Equals(current, values2) | Vector256.Equals(current, values3));

                if (equals != Vector256<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1), values2 = Vector128.Create(value2), values3 = Vector128.Create(value3);
                nint offset = length - Vector128<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1) | Vector128.Equals(current, values2) | Vector128.Equals(current, values3));

                    if (equals == Vector128<TValue>.Zero)
                    {
                        offset -= Vector128<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector128.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1) | Vector128.Equals(current, values2) | Vector128.Equals(current, values3));

                if (equals != Vector128<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }

            return -1;
        }

        public static void Replace<T>(ref T src, ref T dst, T oldValue, T newValue, nuint length) where T : IEquatable<T>?
        {
            if (default(T) is not null || oldValue is not null)
            {
                Debug.Assert(oldValue is not null);

                for (nuint idx = 0; idx < length; ++idx)
                {
                    T original = Unsafe.Add(ref src, idx);
                    Unsafe.Add(ref dst, idx) = oldValue.Equals(original) ? newValue : original;
                }
            }
            else
            {
                for (nuint idx = 0; idx < length; ++idx)
                {
                    T original = Unsafe.Add(ref src, idx);
                    Unsafe.Add(ref dst, idx) = original is null ? newValue : original;
                }
            }
        }

        public static void ReplaceValueType<T>(ref T src, ref T dst, T oldValue, T newValue, nuint length) where T : struct
        {
            if (!Vector128.IsHardwareAccelerated || length < (uint)Vector128<T>.Count)
            {
                for (nuint idx = 0; idx < length; ++idx)
                {
                    T original = Unsafe.Add(ref src, idx);
                    Unsafe.Add(ref dst, idx) = EqualityComparer<T>.Default.Equals(original, oldValue) ? newValue : original;
                }
            }
            else
            {
                Debug.Assert(Vector128.IsHardwareAccelerated && Vector128<T>.IsSupported, "Vector128 is not HW-accelerated or not supported");

                nuint idx = 0;

                if (!Vector256.IsHardwareAccelerated || length < (uint)Vector256<T>.Count)
                {
                    nuint lastVectorIndex = length - (uint)Vector128<T>.Count;
                    Vector128<T> oldValues = Vector128.Create(oldValue);
                    Vector128<T> newValues = Vector128.Create(newValue);
                    Vector128<T> original, mask, result;

                    do
                    {
                        original = Vector128.LoadUnsafe(ref src, idx);
                        mask = Vector128.Equals(oldValues, original);
                        result = Vector128.ConditionalSelect(mask, newValues, original);
                        result.StoreUnsafe(ref dst, idx);

                        idx += (uint)Vector128<T>.Count;
                    }
                    while (idx < lastVectorIndex);

                    // There are (0, Vector128<T>.Count] elements remaining now.
                    // As the operation is idempotent, and we know that in total there are at least Vector128<T>.Count
                    // elements available, we read a vector from the very end, perform the replace and write to the
                    // the resulting vector at the very end.
                    // Thus we can eliminate the scalar processing of the remaining elements.
                    original = Vector128.LoadUnsafe(ref src, lastVectorIndex);
                    mask = Vector128.Equals(oldValues, original);
                    result = Vector128.ConditionalSelect(mask, newValues, original);
                    result.StoreUnsafe(ref dst, lastVectorIndex);
                }
                else if (!Vector512.IsHardwareAccelerated || length < (uint)Vector512<T>.Count)
                {
                    nuint lastVectorIndex = length - (uint)Vector256<T>.Count;
                    Vector256<T> oldValues = Vector256.Create(oldValue);
                    Vector256<T> newValues = Vector256.Create(newValue);
                    Vector256<T> original, mask, result;

                    do
                    {
                        original = Vector256.LoadUnsafe(ref src, idx);
                        mask = Vector256.Equals(oldValues, original);
                        result = Vector256.ConditionalSelect(mask, newValues, original);
                        result.StoreUnsafe(ref dst, idx);

                        idx += (uint)Vector256<T>.Count;
                    }
                    while (idx < lastVectorIndex);

                    original = Vector256.LoadUnsafe(ref src, lastVectorIndex);
                    mask = Vector256.Equals(oldValues, original);
                    result = Vector256.ConditionalSelect(mask, newValues, original);
                    result.StoreUnsafe(ref dst, lastVectorIndex);
                }
                else
                {
                    Debug.Assert(Vector512.IsHardwareAccelerated && Vector512<T>.IsSupported, "Vector512 is not HW-accelerated or not supported");

                    nuint lastVectorIndex = length - (uint)Vector512<T>.Count;
                    Vector512<T> oldValues = Vector512.Create(oldValue);
                    Vector512<T> newValues = Vector512.Create(newValue);
                    Vector512<T> original, mask, result;

                    do
                    {
                        original = Vector512.LoadUnsafe(ref src, idx);
                        mask = Vector512.Equals(oldValues, original);
                        result = Vector512.ConditionalSelect(mask, newValues, original);
                        result.StoreUnsafe(ref dst, idx);

                        idx += (uint)Vector512<T>.Count;
                    }
                    while (idx < lastVectorIndex);

                    original = Vector512.LoadUnsafe(ref src, lastVectorIndex);
                    mask = Vector512.Equals(oldValues, original);
                    result = Vector512.ConditionalSelect(mask, newValues, original);
                    result.StoreUnsafe(ref dst, lastVectorIndex);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, value2, value3, value4, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, value2, value3, value4, length);

        private static int LastIndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, TValue value3, TValue value4, int length)
            where TValue : struct, INumber<TValue>
            where TNegator : struct, INegator<TValue>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<TValue>.Count)
            {
                nuint offset = (nuint)length - 1;
                TValue lookUp;

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) return (int)offset;
                    lookUp = Unsafe.Add(ref current, -1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) return (int)offset - 1;
                    lookUp = Unsafe.Add(ref current, -2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) return (int)offset - 2;
                    lookUp = Unsafe.Add(ref current, -3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) return (int)offset - 3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4)) return (int)offset;

                    offset -= 1;
                }
            }
            else if (Vector512.IsHardwareAccelerated && length >= Vector512<TValue>.Count)
            {
                Vector512<TValue> equals, current, values0 = Vector512.Create(value0), values1 = Vector512.Create(value1),
                    values2 = Vector512.Create(value2), values3 = Vector512.Create(value3), values4 = Vector512.Create(value4);
                nint offset = length - Vector512<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector512.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector512.Equals(current, values0) | Vector512.Equals(current, values1) | Vector512.Equals(current, values2)
                        | Vector512.Equals(current, values3) | Vector512.Equals(current, values4));
                    if (equals == Vector512<TValue>.Zero)
                    {
                        offset -= Vector512<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector512.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector512.Equals(current, values0) | Vector512.Equals(current, values1) | Vector512.Equals(current, values2)
                    | Vector512.Equals(current, values3) | Vector512.Equals(current, values4));

                if (equals != Vector512<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1),
                    values2 = Vector256.Create(value2), values3 = Vector256.Create(value3), values4 = Vector256.Create(value4);
                nint offset = length - Vector256<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector256.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1) | Vector256.Equals(current, values2)
                        | Vector256.Equals(current, values3) | Vector256.Equals(current, values4));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        offset -= Vector256<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector256.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1) | Vector256.Equals(current, values2)
                    | Vector256.Equals(current, values3) | Vector256.Equals(current, values4));

                if (equals != Vector256<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1),
                    values2 = Vector128.Create(value2), values3 = Vector128.Create(value3), values4 = Vector128.Create(value4);
                nint offset = length - Vector128<TValue>.Count;

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                while (offset > 0)
                {
                    current = Vector128.LoadUnsafe(ref searchSpace, (nuint)(offset));
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1) | Vector128.Equals(current, values2)
                        | Vector128.Equals(current, values3) | Vector128.Equals(current, values4));

                    if (equals == Vector128<TValue>.Zero)
                    {
                        offset -= Vector128<TValue>.Count;
                        continue;
                    }

                    return ComputeLastIndex(offset, equals);
                }

                // Process the first vector in the search space.

                current = Vector128.LoadUnsafe(ref searchSpace);
                equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1) | Vector128.Equals(current, values2)
                    | Vector128.Equals(current, values3) | Vector128.Equals(current, values4));

                if (equals != Vector128<TValue>.Zero)
                {
                    return ComputeLastIndex(offset: 0, equals);
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector128<T> equals) where T : struct
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector256<T> equals) where T : struct
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector512<T> equals) where T : struct
        {
            ulong notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref current) / (nuint)sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeLastIndex<T>(nint offset, Vector128<T> equals) where T : struct
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = 31 - BitOperations.LeadingZeroCount(notEqualsElements); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
            return (int)offset + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeLastIndex<T>(nint offset, Vector256<T> equals) where T : struct
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = 31 - BitOperations.LeadingZeroCount(notEqualsElements); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
            return (int)offset + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeLastIndex<T>(nint offset, Vector512<T> equals) where T : struct
        {
            ulong notEqualsElements = equals.ExtractMostSignificantBits();
            int index = 63 - BitOperations.LeadingZeroCount(notEqualsElements); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
            return (int)offset + index;
        }

        internal interface INegator<T> where T : struct
        {
            static abstract bool NegateIfNeeded(bool equals);
            static abstract Vector128<T> NegateIfNeeded(Vector128<T> equals);
            static abstract Vector256<T> NegateIfNeeded(Vector256<T> equals);
            static abstract Vector512<T> NegateIfNeeded(Vector512<T> equals);

            // The generic vector APIs assume use for IndexOf where `DontNegate` is
            // for `IndexOfAny` and `Negate` is for `IndexOfAnyExcept`

            static abstract bool HasMatch<TVector>(TVector left, TVector right)
                where TVector : struct, ISimdVector<TVector, T>;

            static abstract TVector GetMatchMask<TVector>(TVector left, TVector right)
                where TVector : struct, ISimdVector<TVector, T>;
        }

        internal readonly struct DontNegate<T> : INegator<T>
            where T : struct
        {
            public static bool NegateIfNeeded(bool equals) => equals;
            public static Vector128<T> NegateIfNeeded(Vector128<T> equals) => equals;
            public static Vector256<T> NegateIfNeeded(Vector256<T> equals) => equals;
            public static Vector512<T> NegateIfNeeded(Vector512<T> equals) => equals;

            // The generic vector APIs assume use for `IndexOfAny` where we
            // want "HasMatch" to mean any of the two elements match.

            public static bool HasMatch<TVector>(TVector left, TVector right)
                where TVector : struct, ISimdVector<TVector, T>
            {
                return TVector.EqualsAny(left, right);
            }

            public static TVector GetMatchMask<TVector>(TVector left, TVector right)
                where TVector : struct, ISimdVector<TVector, T>
            {
                return TVector.Equals(left, right);
            }
        }

        internal readonly struct Negate<T> : INegator<T>
            where T : struct
        {
            public static bool NegateIfNeeded(bool equals) => !equals;
            public static Vector128<T> NegateIfNeeded(Vector128<T> equals) => ~equals;
            public static Vector256<T> NegateIfNeeded(Vector256<T> equals) => ~equals;
            public static Vector512<T> NegateIfNeeded(Vector512<T> equals) => ~equals;

            // The generic vector APIs assume use for `IndexOfAnyExcept` where we
            // want "HasMatch" to mean any of the two elements don't match

            public static bool HasMatch<TVector>(TVector left, TVector right)
                where TVector : struct, ISimdVector<TVector, T>
            {
                return !TVector.EqualsAll(left, right);
            }

            public static TVector GetMatchMask<TVector>(TVector left, TVector right)
                where TVector : struct, ISimdVector<TVector, T>
            {
                return ~TVector.Equals(left, right);
            }
        }

        internal static int IndexOfAnyInRange<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : IComparable<T>
        {
            for (int i = 0; i < length; i++)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if ((lowInclusive.CompareTo(current) <= 0) && (highInclusive.CompareTo(current) >= 0))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int IndexOfAnyExceptInRange<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : IComparable<T>
        {
            for (int i = 0; i < length; i++)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if ((lowInclusive.CompareTo(current) > 0) || (highInclusive.CompareTo(current) < 0))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int IndexOfAnyInRangeUnsignedNumber<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool> =>
            IndexOfAnyInRangeUnsignedNumber<T, DontNegate<T>>(ref searchSpace, lowInclusive, highInclusive, length);

        internal static int IndexOfAnyExceptInRangeUnsignedNumber<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool> =>
            IndexOfAnyInRangeUnsignedNumber<T, Negate<T>>(ref searchSpace, lowInclusive, highInclusive, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int IndexOfAnyInRangeUnsignedNumber<T, TNegator>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool>
            where TNegator : struct, INegator<T>
        {
            if (PackedSpanHelpers.PackedIndexOfIsSupported && typeof(T) == typeof(ushort) && PackedSpanHelpers.CanUsePackedIndexOf(lowInclusive) && PackedSpanHelpers.CanUsePackedIndexOf(highInclusive) && highInclusive >= lowInclusive)
            {
                ref char charSearchSpace = ref Unsafe.As<T, char>(ref searchSpace);
                char charLowInclusive = *(char*)&lowInclusive;
                char charRange = (char)(*(char*)&highInclusive - charLowInclusive);

                return typeof(TNegator) == typeof(DontNegate<ushort>)
                    ? PackedSpanHelpers.IndexOfAnyInRange(ref charSearchSpace, charLowInclusive, charRange, length)
                    : PackedSpanHelpers.IndexOfAnyExceptInRange(ref charSearchSpace, charLowInclusive, charRange, length);
            }

            return NonPackedIndexOfAnyInRangeUnsignedNumber<T, TNegator>(ref searchSpace, lowInclusive, highInclusive, length);
        }

        internal static int NonPackedIndexOfAnyInRangeUnsignedNumber<T, TNegator>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool>
            where TNegator : struct, INegator<T>
        {
            // T must be a type whose comparison operator semantics match that of Vector128/256.

            if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
            {
                T rangeInclusive = highInclusive - lowInclusive;
                for (int i = 0; i < length; i++)
                {
                    ref T current = ref Unsafe.Add(ref searchSpace, i);
                    if (TNegator.NegateIfNeeded((current - lowInclusive) <= rangeInclusive))
                    {
                        return i;
                    }
                }
            }
            else if (!Vector256.IsHardwareAccelerated || length < Vector256<T>.Count)
            {
                Vector128<T> lowVector = Vector128.Create(lowInclusive);
                Vector128<T> rangeVector = Vector128.Create(highInclusive - lowInclusive);
                Vector128<T> inRangeVector;

                ref T current = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector128<T>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    inRangeVector = TNegator.NegateIfNeeded(Vector128.LessThanOrEqual(Vector128.LoadUnsafe(ref current) - lowVector, rangeVector));
                    if (inRangeVector != Vector128<T>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref current, inRangeVector);
                    }

                    current = ref Unsafe.Add(ref current, Vector128<T>.Count);
                }
                while (Unsafe.IsAddressLessThan(ref current, ref oneVectorAwayFromEnd));

                // Process the last vector in the search space (which might overlap with already processed elements).
                inRangeVector = TNegator.NegateIfNeeded(Vector128.LessThanOrEqual(Vector128.LoadUnsafe(ref oneVectorAwayFromEnd) - lowVector, rangeVector));
                if (inRangeVector != Vector128<T>.Zero)
                {
                    return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, inRangeVector);
                }
            }
            else if (!Vector512.IsHardwareAccelerated || length < (uint)Vector512<T>.Count)
            {
                Vector256<T> lowVector = Vector256.Create(lowInclusive);
                Vector256<T> rangeVector = Vector256.Create(highInclusive - lowInclusive);
                Vector256<T> inRangeVector;

                ref T current = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector256<T>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    inRangeVector = TNegator.NegateIfNeeded(Vector256.LessThanOrEqual(Vector256.LoadUnsafe(ref current) - lowVector, rangeVector));
                    if (inRangeVector != Vector256<T>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref current, inRangeVector);
                    }

                    current = ref Unsafe.Add(ref current, Vector256<T>.Count);
                }
                while (Unsafe.IsAddressLessThan(ref current, ref oneVectorAwayFromEnd));

                // Process the last vector in the search space (which might overlap with already processed elements).
                inRangeVector = TNegator.NegateIfNeeded(Vector256.LessThanOrEqual(Vector256.LoadUnsafe(ref oneVectorAwayFromEnd) - lowVector, rangeVector));
                if (inRangeVector != Vector256<T>.Zero)
                {
                    return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, inRangeVector);
                }
            }
            else
            {
                Vector512<T> lowVector = Vector512.Create(lowInclusive);
                Vector512<T> rangeVector = Vector512.Create(highInclusive - lowInclusive);
                Vector512<T> inRangeVector;

                ref T current = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, (uint)(length - Vector512<T>.Count));

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    inRangeVector = TNegator.NegateIfNeeded(Vector512.LessThanOrEqual(Vector512.LoadUnsafe(ref current) - lowVector, rangeVector));
                    if (inRangeVector != Vector512<T>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref current, inRangeVector);
                    }

                    current = ref Unsafe.Add(ref current, Vector256<T>.Count);
                }
                while (Unsafe.IsAddressLessThan(ref current, ref oneVectorAwayFromEnd));

                // Process the last vector in the search space (which might overlap with already processed elements).
                inRangeVector = TNegator.NegateIfNeeded(Vector512.LessThanOrEqual(Vector512.LoadUnsafe(ref oneVectorAwayFromEnd) - lowVector, rangeVector));
                if (inRangeVector != Vector512<T>.Zero)
                {
                    return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, inRangeVector);
                }
            }

            return -1;
        }

        internal static int LastIndexOfAnyInRange<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : IComparable<T>
        {
            for (int i = length - 1; i >= 0; i--)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if ((lowInclusive.CompareTo(current) <= 0) && (highInclusive.CompareTo(current) >= 0))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int LastIndexOfAnyExceptInRange<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : IComparable<T>
        {
            for (int i = length - 1; i >= 0; i--)
            {
                ref T current = ref Unsafe.Add(ref searchSpace, i);
                if ((lowInclusive.CompareTo(current) > 0) || (highInclusive.CompareTo(current) < 0))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int LastIndexOfAnyInRangeUnsignedNumber<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool> =>
            LastIndexOfAnyInRangeUnsignedNumber<T, DontNegate<T>>(ref searchSpace, lowInclusive, highInclusive, length);

        internal static int LastIndexOfAnyExceptInRangeUnsignedNumber<T>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool> =>
            LastIndexOfAnyInRangeUnsignedNumber<T, Negate<T>>(ref searchSpace, lowInclusive, highInclusive, length);

        private static int LastIndexOfAnyInRangeUnsignedNumber<T, TNegator>(ref T searchSpace, T lowInclusive, T highInclusive, int length)
            where T : struct, IUnsignedNumber<T>, IComparisonOperators<T, T, bool>
            where TNegator : struct, INegator<T>
        {
            // T must be a type whose comparison operator semantics match that of Vector128/256.

            if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
            {
                T rangeInclusive = highInclusive - lowInclusive;
                for (int i = length - 1; i >= 0; i--)
                {
                    ref T current = ref Unsafe.Add(ref searchSpace, i);
                    if (TNegator.NegateIfNeeded((current - lowInclusive) <= rangeInclusive))
                    {
                        return i;
                    }
                }
            }
            else if (!Vector256.IsHardwareAccelerated || length < Vector256<T>.Count)
            {
                Vector128<T> lowVector = Vector128.Create(lowInclusive);
                Vector128<T> rangeVector = Vector128.Create(highInclusive - lowInclusive);
                Vector128<T> inRangeVector;

                nint offset = length - Vector128<T>.Count;

                // Loop until either we've finished all elements or there's a vector's-worth or less remaining.
                while (offset > 0)
                {
                    inRangeVector = TNegator.NegateIfNeeded(Vector128.LessThanOrEqual(Vector128.LoadUnsafe(ref searchSpace, (nuint)offset) - lowVector, rangeVector));
                    if (inRangeVector != Vector128<T>.Zero)
                    {
                        return ComputeLastIndex(offset, inRangeVector);
                    }

                    offset -= Vector128<T>.Count;
                }

                // Process the first vector in the search space.
                inRangeVector = TNegator.NegateIfNeeded(Vector128.LessThanOrEqual(Vector128.LoadUnsafe(ref searchSpace) - lowVector, rangeVector));
                if (inRangeVector != Vector128<T>.Zero)
                {
                    return ComputeLastIndex(offset: 0, inRangeVector);
                }
            }
            else if (!Vector512.IsHardwareAccelerated || length < Vector512<T>.Count)
            {
                Vector256<T> lowVector = Vector256.Create(lowInclusive);
                Vector256<T> rangeVector = Vector256.Create(highInclusive - lowInclusive);
                Vector256<T> inRangeVector;

                nint offset = length - Vector256<T>.Count;

                // Loop until either we've finished all elements or there's a vector's-worth or less remaining.
                while (offset > 0)
                {
                    inRangeVector = TNegator.NegateIfNeeded(Vector256.LessThanOrEqual(Vector256.LoadUnsafe(ref searchSpace, (nuint)offset) - lowVector, rangeVector));
                    if (inRangeVector != Vector256<T>.Zero)
                    {
                        return ComputeLastIndex(offset, inRangeVector);
                    }

                    offset -= Vector256<T>.Count;
                }

                // Process the first vector in the search space.
                inRangeVector = TNegator.NegateIfNeeded(Vector256.LessThanOrEqual(Vector256.LoadUnsafe(ref searchSpace) - lowVector, rangeVector));
                if (inRangeVector != Vector256<T>.Zero)
                {
                    return ComputeLastIndex(offset: 0, inRangeVector);
                }
            }
            else
            {
                Vector512<T> lowVector = Vector512.Create(lowInclusive);
                Vector512<T> rangeVector = Vector512.Create(highInclusive - lowInclusive);
                Vector512<T> inRangeVector;

                nint offset = length - Vector512<T>.Count;

                // Loop until either we've finished all elements or there's a vector's-worth or less remaining.
                while (offset > 0)
                {
                    inRangeVector = TNegator.NegateIfNeeded(Vector512.LessThanOrEqual(Vector512.LoadUnsafe(ref searchSpace, (nuint)offset) - lowVector, rangeVector));
                    if (inRangeVector != Vector512<T>.Zero)
                    {
                        return ComputeLastIndex(offset, inRangeVector);
                    }

                    offset -= Vector512<T>.Count;
                }

                // Process the first vector in the search space.
                inRangeVector = TNegator.NegateIfNeeded(Vector512.LessThanOrEqual(Vector512.LoadUnsafe(ref searchSpace) - lowVector, rangeVector));
                if (inRangeVector != Vector512<T>.Zero)
                {
                    return ComputeLastIndex(offset: 0, inRangeVector);
                }
            }

            return -1;
        }

        public static int Count<T>(ref T current, T value, int length) where T : IEquatable<T>?
        {
            int count = 0;

            ref T end = ref Unsafe.Add(ref current, length);
            if (value is not null)
            {
                while (Unsafe.IsAddressLessThan(ref current, ref end))
                {
                    if (value.Equals(current))
                    {
                        count++;
                    }

                    current = ref Unsafe.Add(ref current, 1);
                }
            }
            else
            {
                while (Unsafe.IsAddressLessThan(ref current, ref end))
                {
                    if (current is null)
                    {
                        count++;
                    }

                    current = ref Unsafe.Add(ref current, 1);
                }
            }

            return count;
        }

        public static int CountValueType<T>(ref T current, T value, int length) where T : struct, IEquatable<T>?
        {
            int count = 0;
            ref T end = ref Unsafe.Add(ref current, length);

            if (Vector128.IsHardwareAccelerated && length >= Vector128<T>.Count)
            {
                if (Vector512.IsHardwareAccelerated && length >= Vector512<T>.Count)
                {
                    Vector512<T> targetVector = Vector512.Create(value);
                    ref T oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector512<T>.Count);
                    do
                    {
                        count += BitOperations.PopCount(Vector512.Equals(Vector512.LoadUnsafe(ref current), targetVector).ExtractMostSignificantBits());
                        current = ref Unsafe.Add(ref current, Vector512<T>.Count);
                    }
                    while (!Unsafe.IsAddressGreaterThan(ref current, ref oneVectorAwayFromEnd));

                    // If there are just a few elements remaining, then processing these elements by the scalar loop
                    // is cheaper than doing bitmask + popcount on the full last vector. To avoid complicated type
                    // based checks, other remainder-count based logic to determine the correct cut-off, for simplicity
                    // a half-vector size is chosen (based on benchmarks).
                    uint remaining = (uint)Unsafe.ByteOffset(ref current, ref end) / (uint)Unsafe.SizeOf<T>();
                    if (remaining > Vector512<T>.Count / 2)
                    {
                        ulong mask = Vector512.Equals(Vector512.LoadUnsafe(ref oneVectorAwayFromEnd), targetVector).ExtractMostSignificantBits();

                        // The mask contains some elements that may be double-checked, so shift them away in order to get the correct pop-count.
                        uint overlaps = (uint)Vector512<T>.Count - remaining;
                        mask >>= (int)overlaps;
                        count += BitOperations.PopCount(mask);

                        return count;
                    }
                }
                else if (Vector256.IsHardwareAccelerated && length >= Vector256<T>.Count)
                {
                    Vector256<T> targetVector = Vector256.Create(value);
                    ref T oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector256<T>.Count);
                    do
                    {
                        count += BitOperations.PopCount(Vector256.Equals(Vector256.LoadUnsafe(ref current), targetVector).ExtractMostSignificantBits());
                        current = ref Unsafe.Add(ref current, Vector256<T>.Count);
                    }
                    while (!Unsafe.IsAddressGreaterThan(ref current, ref oneVectorAwayFromEnd));

                    // If there are just a few elements remaining, then processing these elements by the scalar loop
                    // is cheaper than doing bitmask + popcount on the full last vector. To avoid complicated type
                    // based checks, other remainder-count based logic to determine the correct cut-off, for simplicity
                    // a half-vector size is chosen (based on benchmarks).
                    uint remaining = (uint)Unsafe.ByteOffset(ref current, ref end) / (uint)Unsafe.SizeOf<T>();
                    if (remaining > Vector256<T>.Count / 2)
                    {
                        uint mask = Vector256.Equals(Vector256.LoadUnsafe(ref oneVectorAwayFromEnd), targetVector).ExtractMostSignificantBits();

                        // The mask contains some elements that may be double-checked, so shift them away in order to get the correct pop-count.
                        uint overlaps = (uint)Vector256<T>.Count - remaining;
                        mask >>= (int)overlaps;
                        count += BitOperations.PopCount(mask);

                        return count;
                    }
                }
                else
                {
                    Vector128<T> targetVector = Vector128.Create(value);
                    ref T oneVectorAwayFromEnd = ref Unsafe.Subtract(ref end, Vector128<T>.Count);
                    do
                    {
                        count += BitOperations.PopCount(Vector128.Equals(Vector128.LoadUnsafe(ref current), targetVector).ExtractMostSignificantBits());
                        current = ref Unsafe.Add(ref current, Vector128<T>.Count);
                    }
                    while (!Unsafe.IsAddressGreaterThan(ref current, ref oneVectorAwayFromEnd));

                    uint remaining = (uint)Unsafe.ByteOffset(ref current, ref end) / (uint)Unsafe.SizeOf<T>();
                    if (remaining > Vector128<T>.Count / 2)
                    {
                        uint mask = Vector128.Equals(Vector128.LoadUnsafe(ref oneVectorAwayFromEnd), targetVector).ExtractMostSignificantBits();

                        // The mask contains some elements that may be double-checked, so shift them away in order to get the correct pop-count.
                        uint overlaps = (uint)Vector128<T>.Count - remaining;
                        mask >>= (int)overlaps;
                        count += BitOperations.PopCount(mask);

                        return count;
                    }
                }
            }

            while (Unsafe.IsAddressLessThan(ref current, ref end))
            {
                if (current.Equals(value))
                {
                    count++;
                }

                current = ref Unsafe.Add(ref current, 1);
            }

            return count;
        }
    }
}
