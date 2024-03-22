// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;


#pragma warning disable 8500 // sizeof of managed types

namespace System.Numerics.Tensors
{
    internal static partial class SpanHelpers // .T
    {
        public static unsafe void Memmove<T>(ref T[] destination, ref T source, nint length)
        {
            fixed (T* dst = destination)
            fixed (T* src = &source)
            {
                NativeMemory.Copy(src, dst, (nuint)length * (nuint)sizeof(T));
            }
        }

        public static unsafe void Memmove<T>(ref T destination, ref T source, nint length)
        {
            fixed (T* dst = &destination)
            fixed (T* src = &source)
            {
                NativeMemory.Copy(src, dst, (nuint)length * (nuint)sizeof(T));
            }
        }

        //[Intrinsic] // Unrolled for small sizes
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

        public static bool SequenceEqual<T>(ref T first, ref T second, nint length) where T : IEquatable<T>?
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
    }
}
