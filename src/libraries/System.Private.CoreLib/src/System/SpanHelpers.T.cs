// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System
{
    internal static partial class SpanHelpers // .T
    {
        public static void Fill<T>(ref T refData, nuint numElements, T value)
        {
            // Early checks to see if it's even possible to vectorize - JIT will turn these checks into consts.
            // - T cannot contain references (GC can't track references in vectors)
            // - Vectorization must be hardware-accelerated
            // - T's size must not exceed the vector's size
            // - T's size must be a whole power of 2

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) { goto CannotVectorize; }
            if (!Vector.IsHardwareAccelerated) { goto CannotVectorize; }
            if (Unsafe.SizeOf<T>() > Vector<byte>.Count) { goto CannotVectorize; }
            if (!BitOperations.IsPow2(Unsafe.SizeOf<T>())) { goto CannotVectorize; }

            if (numElements >= (uint)(Vector<byte>.Count / Unsafe.SizeOf<T>()))
            {
                // We have enough data for at least one vectorized write.

                T tmp = value; // Avoid taking address of the "value" argument. It would regress performance of the loops below.
                Vector<byte> vector;

                if (Unsafe.SizeOf<T>() == 1)
                {
                    vector = new Vector<byte>(Unsafe.As<T, byte>(ref tmp));
                }
                else if (Unsafe.SizeOf<T>() == 2)
                {
                    vector = (Vector<byte>)(new Vector<ushort>(Unsafe.As<T, ushort>(ref tmp)));
                }
                else if (Unsafe.SizeOf<T>() == 4)
                {
                    // special-case float since it's already passed in a SIMD reg
                    vector = (typeof(T) == typeof(float))
                        ? (Vector<byte>)(new Vector<float>((float)(object)tmp!))
                        : (Vector<byte>)(new Vector<uint>(Unsafe.As<T, uint>(ref tmp)));
                }
                else if (Unsafe.SizeOf<T>() == 8)
                {
                    // special-case double since it's already passed in a SIMD reg
                    vector = (typeof(T) == typeof(double))
                        ? (Vector<byte>)(new Vector<double>((double)(object)tmp!))
                        : (Vector<byte>)(new Vector<ulong>(Unsafe.As<T, ulong>(ref tmp)));
                }
                else if (Unsafe.SizeOf<T>() == 16)
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
                else if (Unsafe.SizeOf<T>() == 32)
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
                nuint totalByteLength = numElements * (nuint)Unsafe.SizeOf<T>(); // get this calculation ready ahead of time
                nuint stopLoopAtOffset = totalByteLength & (nuint)(nint)(2 * (int)-Vector<byte>.Count); // intentional sign extension carries the negative bit
                nuint offset = 0;

                // Loop, writing 2 vectors at a time.
                // Compare 'numElements' rather than 'stopLoopAtOffset' because we don't want a dependency
                // on the very recently calculated 'stopLoopAtOffset' value.

                if (numElements >= (uint)(2 * Vector<byte>.Count / Unsafe.SizeOf<T>()))
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

            for (int i = length -1; i >= 0; i--)
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
        internal static bool CanVectorizeAndBenefit<T>(int length) where T : IEquatable<T>?
        {
            if (Vector128.IsHardwareAccelerated && RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (Unsafe.SizeOf<T>() == sizeof(byte))
                {
                    return length >= Vector128<byte>.Count;
                }
                else if (Unsafe.SizeOf<T>() == sizeof(short))
                {
                    return length >= Vector128<short>.Count;
                }
                else if (Unsafe.SizeOf<T>() == sizeof(int))
                {
                    return length >= Vector128<int>.Count;
                }
                else if (Unsafe.SizeOf<T>() == sizeof(long))
                {
                    return length >= Vector128<long>.Count;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static bool ContainsValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
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
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<T>.Count)
            {
                Vector256<T> equals, values = Vector256.Create(value);
                ref T currentSearchSpace = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<T>.Count);

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
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<T>.Count);

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
        internal static int IndexOfValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => IndexOfValueType<T, DontNegate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExceptValueType<T>(ref T searchSpace, T value, int length) where T : struct, INumber<T>
            => IndexOfValueType<T, Negate<T>>(ref searchSpace, value, length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static int IndexOfValueType<TValue, TNegator>(ref TValue searchSpace, TValue value, int length)
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

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) return (int)offset;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 1) == value)) return (int)offset + 1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 2) == value)) return (int)offset + 2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 3) == value)) return (int)offset + 3;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 4) == value)) return (int)offset + 4;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 5) == value)) return (int)offset + 5;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 6) == value)) return (int)offset + 6;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 7) == value)) return (int)offset + 7;

                    offset += 8;
                }

                if (length >= 4)
                {
                    length -= 4;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) return (int)offset;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 1) == value)) return (int)offset + 1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 2) == value)) return (int)offset + 2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset + 3) == value)) return (int)offset + 3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) return (int)offset;

                    offset += 1;
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

        // having INumber<T> constraint here allows to use == operator and get better perf compared to .Equals
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, int length)
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
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset;
                        lookUp = Unsafe.Add(ref current, 1);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 1;
                        lookUp = Unsafe.Add(ref current, 2);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 2;
                        lookUp = Unsafe.Add(ref current, 3);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 3;
                        lookUp = Unsafe.Add(ref current, 4);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 4;
                        lookUp = Unsafe.Add(ref current, 5);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 5;
                        lookUp = Unsafe.Add(ref current, 6);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 6;
                        lookUp = Unsafe.Add(ref current, 7);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 7;

                        offset += 8;
                    }
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset + 3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset;

                    offset += 1;
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static int IndexOfAnyValueType<TValue, TNegator>(ref TValue searchSpace, TValue value0, TValue value1, TValue value2, int length)
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
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset;
                        lookUp = Unsafe.Add(ref current, 1);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 1;
                        lookUp = Unsafe.Add(ref current, 2);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 2;
                        lookUp = Unsafe.Add(ref current, 3);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 3;
                        lookUp = Unsafe.Add(ref current, 4);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 4;
                        lookUp = Unsafe.Add(ref current, 5);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 5;
                        lookUp = Unsafe.Add(ref current, 6);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 6;
                        lookUp = Unsafe.Add(ref current, 7);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 7;

                        offset += 8;
                    }
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset + 3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset;

                    offset += 1;
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset + 1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset + 2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset + 3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset;

                    offset += 1;
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static int IndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, T value2, T value3, T value4, int length)
            where T : struct, INumber<T>
        {
            Debug.Assert(length >= 0, "Expected non-negative length");
            Debug.Assert(value0 is byte or short or int or long, "Expected caller to normalize to one of these types");

            if (!Vector128.IsHardwareAccelerated || length < Vector128<T>.Count)
            {
                nuint offset = 0;
                T lookUp;

                while (length >= 4)
                {
                    length -= 4;

                    ref T current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4) return (int)offset;
                    lookUp = Unsafe.Add(ref current, 1);
                    if (lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4) return (int)offset + 1;
                    lookUp = Unsafe.Add(ref current, 2);
                    if (lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4) return (int)offset + 2;
                    lookUp = Unsafe.Add(ref current, 3);
                    if (lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4) return (int)offset + 3;

                    offset += 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3 || lookUp == value4) return (int)offset;

                    offset += 1;
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<T>.Count)
            {
                Vector256<T> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1),
                    values2 = Vector256.Create(value2), values3 = Vector256.Create(value3), values4 = Vector256.Create(value4);
                ref T currentSearchSpace = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector256<T>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = Vector256.Equals(values0, current) | Vector256.Equals(values1, current) | Vector256.Equals(values2, current)
                           | Vector256.Equals(values3, current) | Vector256.Equals(values4, current);
                    if (equals == Vector256<T>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector256<T>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the last vector in the search space.
                if ((uint)length % Vector256<T>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = Vector256.Equals(values0, current) | Vector256.Equals(values1, current) | Vector256.Equals(values2, current)
                           | Vector256.Equals(values3, current) | Vector256.Equals(values4, current);
                    if (equals != Vector256<T>.Zero)
                    {
                        return ComputeFirstIndex(ref searchSpace, ref oneVectorAwayFromEnd, equals);
                    }
                }
            }
            else
            {
                Vector128<T> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1),
                    values2 = Vector128.Create(value2), values3 = Vector128.Create(value3), values4 = Vector128.Create(value4);
                ref T currentSearchSpace = ref searchSpace;
                ref T oneVectorAwayFromEnd = ref Unsafe.Add(ref searchSpace, length - Vector128<T>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = Vector128.Equals(values0, current) | Vector128.Equals(values1, current) | Vector128.Equals(values2, current)
                           | Vector128.Equals(values3, current) | Vector128.Equals(values4, current);
                    if (equals == Vector128<T>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Add(ref currentSearchSpace, Vector128<T>.Count);
                        continue;
                    }

                    return ComputeFirstIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressGreaterThan(ref currentSearchSpace, ref oneVectorAwayFromEnd));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<T>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref oneVectorAwayFromEnd);
                    equals = Vector128.Equals(values0, current) | Vector128.Equals(values1, current) | Vector128.Equals(values2, current)
                           | Vector128.Equals(values3, current) | Vector128.Equals(values4, current);
                    if (equals != Vector128<T>.Zero)
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) return (int)offset;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 1) == value)) return (int)offset - 1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 2) == value)) return (int)offset - 2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 3) == value)) return (int)offset - 3;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 4) == value)) return (int)offset - 4;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 5) == value)) return (int)offset - 5;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 6) == value)) return (int)offset - 6;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 7) == value)) return (int)offset - 7;

                    offset -= 8;
                }

                if (length >= 4)
                {
                    length -= 4;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) return (int)offset;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 1) == value)) return (int)offset - 1;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 2) == value)) return (int)offset - 2;
                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset - 3) == value)) return (int)offset - 3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    if (TNegator.NegateIfNeeded(Unsafe.Add(ref searchSpace, offset) == value)) return (int)offset;

                    offset -= 1;
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, values = Vector256.Create(value);
                ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values, Vector256.LoadUnsafe(ref currentSearchSpace)));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeLastIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(values, Vector256.LoadUnsafe(ref searchSpace)));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeLastIndex(ref searchSpace, ref searchSpace, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, values = Vector128.Create(value);
                ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values, Vector128.LoadUnsafe(ref currentSearchSpace)));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeLastIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(values, Vector128.LoadUnsafe(ref searchSpace)));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeLastIndex(ref searchSpace, ref searchSpace, equals);
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, DontNegate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExceptValueType<T>(ref T searchSpace, T value0, T value1, int length) where T : struct, INumber<T>
            => LastIndexOfAnyValueType<T, Negate<T>>(ref searchSpace, value0, value1, length);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset;
                        lookUp = Unsafe.Add(ref current, -1);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 1;
                        lookUp = Unsafe.Add(ref current, -2);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 2;
                        lookUp = Unsafe.Add(ref current, -3);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 3;
                        lookUp = Unsafe.Add(ref current, -4);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 4;
                        lookUp = Unsafe.Add(ref current, -5);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 5;
                        lookUp = Unsafe.Add(ref current, -6);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 6;
                        lookUp = Unsafe.Add(ref current, -7);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 7;

                        offset -= 8;
                    }
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset;
                    lookUp = Unsafe.Add(ref current, -1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 1;
                    lookUp = Unsafe.Add(ref current, -2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 2;
                    lookUp = Unsafe.Add(ref current, -3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset - 3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1)) return (int)offset;

                    offset -= 1;
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1);
                ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeLastIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref searchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeLastIndex(ref searchSpace, ref searchSpace, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1);
                ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeLastIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref searchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeLastIndex(ref searchSpace, ref searchSpace, equals);
                    }
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset;
                        lookUp = Unsafe.Add(ref current, -1);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 1;
                        lookUp = Unsafe.Add(ref current, -2);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 2;
                        lookUp = Unsafe.Add(ref current, -3);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 3;
                        lookUp = Unsafe.Add(ref current, -4);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 4;
                        lookUp = Unsafe.Add(ref current, -5);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 5;
                        lookUp = Unsafe.Add(ref current, -6);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 6;
                        lookUp = Unsafe.Add(ref current, -7);
                        if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 7;

                        offset -= 8;
                    }
                }

                while (length >= 4)
                {
                    length -= 4;

                    ref TValue current = ref Unsafe.Add(ref searchSpace, offset);
                    lookUp = current;
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset;
                    lookUp = Unsafe.Add(ref current, -1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 1;
                    lookUp = Unsafe.Add(ref current, -2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 2;
                    lookUp = Unsafe.Add(ref current, -3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset - 3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2)) return (int)offset;

                    offset -= 1;
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1), values2 = Vector256.Create(value2);
                ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1) | Vector256.Equals(current, values2));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeLastIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref searchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1) | Vector256.Equals(current, values2));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeLastIndex(ref searchSpace, ref searchSpace, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1), values2 = Vector128.Create(value2);
                ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1) | Vector128.Equals(current, values2));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeLastIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref searchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1) | Vector128.Equals(current, values2));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeLastIndex(ref searchSpace, ref searchSpace, equals);
                    }
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset;
                    lookUp = Unsafe.Add(ref current, -1);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset - 1;
                    lookUp = Unsafe.Add(ref current, -2);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset - 2;
                    lookUp = Unsafe.Add(ref current, -3);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset - 3;

                    offset -= 4;
                }

                while (length > 0)
                {
                    length -= 1;

                    lookUp = Unsafe.Add(ref searchSpace, offset);
                    if (TNegator.NegateIfNeeded(lookUp == value0 || lookUp == value1 || lookUp == value2 || lookUp == value3)) return (int)offset;

                    offset -= 1;
                }
            }
            else if (Vector256.IsHardwareAccelerated && length >= Vector256<TValue>.Count)
            {
                Vector256<TValue> equals, current, values0 = Vector256.Create(value0), values1 = Vector256.Create(value1), values2 = Vector256.Create(value2), values3 = Vector256.Create(value3);
                ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector256<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector256.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1)
                                            | Vector256.Equals(current, values2) | Vector256.Equals(current, values3));
                    if (equals == Vector256<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector256<TValue>.Count);
                        continue;
                    }

                    return ComputeLastIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector256<TValue>.Count != 0)
                {
                    current = Vector256.LoadUnsafe(ref searchSpace);
                    equals = TNegator.NegateIfNeeded(Vector256.Equals(current, values0) | Vector256.Equals(current, values1)
                                            | Vector256.Equals(current, values2) | Vector256.Equals(current, values3));
                    if (equals != Vector256<TValue>.Zero)
                    {
                        return ComputeLastIndex(ref searchSpace, ref searchSpace, equals);
                    }
                }
            }
            else
            {
                Vector128<TValue> equals, current, values0 = Vector128.Create(value0), values1 = Vector128.Create(value1), values2 = Vector128.Create(value2), values3 = Vector128.Create(value3);
                ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

                // Loop until either we've finished all elements or there's less than a vector's-worth remaining.
                do
                {
                    current = Vector128.LoadUnsafe(ref currentSearchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1)
                                            | Vector128.Equals(current, values2) | Vector128.Equals(current, values3));
                    if (equals == Vector128<TValue>.Zero)
                    {
                        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
                        continue;
                    }

                    return ComputeLastIndex(ref searchSpace, ref currentSearchSpace, equals);
                }
                while (!Unsafe.IsAddressLessThan(ref currentSearchSpace, ref searchSpace));

                // If any elements remain, process the first vector in the search space.
                if ((uint)length % Vector128<TValue>.Count != 0)
                {
                    current = Vector128.LoadUnsafe(ref searchSpace);
                    equals = TNegator.NegateIfNeeded(Vector128.Equals(current, values0) | Vector128.Equals(current, values1)
                                            | Vector128.Equals(current, values2) | Vector128.Equals(current, values3));
                    if (equals != Vector128<TValue>.Zero)
                    {
                        return ComputeLastIndex(ref searchSpace, ref searchSpace, equals);
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector128<T> equals) where T : struct
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)(Unsafe.ByteOffset(ref searchSpace, ref current) / Unsafe.SizeOf<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeFirstIndex<T>(ref T searchSpace, ref T current, Vector256<T> equals) where T : struct
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = BitOperations.TrailingZeroCount(notEqualsElements);
            return index + (int)(Unsafe.ByteOffset(ref searchSpace, ref current) / Unsafe.SizeOf<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeLastIndex<T>(ref T searchSpace, ref T current, Vector128<T> equals) where T : struct
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = 31 - BitOperations.LeadingZeroCount(notEqualsElements); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
            return (int)(Unsafe.ByteOffset(ref searchSpace, ref current) / Unsafe.SizeOf<T>()) + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ComputeLastIndex<T>(ref T searchSpace, ref T current, Vector256<T> equals) where T : struct
        {
            uint notEqualsElements = equals.ExtractMostSignificantBits();
            int index = 31 - BitOperations.LeadingZeroCount(notEqualsElements); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
            return (int)(Unsafe.ByteOffset(ref searchSpace, ref current) / Unsafe.SizeOf<T>()) + index;
        }

        private interface INegator<T> where T : struct
        {
            static abstract bool NegateIfNeeded(bool equals);
            static abstract Vector128<T> NegateIfNeeded(Vector128<T> equals);
            static abstract Vector256<T> NegateIfNeeded(Vector256<T> equals);
        }

        private readonly struct DontNegate<T> : INegator<T> where T : struct
        {
            public static bool NegateIfNeeded(bool equals) => equals;
            public static Vector128<T> NegateIfNeeded(Vector128<T> equals) => equals;
            public static Vector256<T> NegateIfNeeded(Vector256<T> equals) => equals;
        }

        private readonly struct Negate<T> : INegator<T> where T : struct
        {
            public static bool NegateIfNeeded(bool equals) => !equals;
            public static Vector128<T> NegateIfNeeded(Vector128<T> equals) => ~equals;
            public static Vector256<T> NegateIfNeeded(Vector256<T> equals) => ~equals;
        }
    }
}
