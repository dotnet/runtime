// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestLibrary
{
    public static class Vectors
    {
        public static void VectorToArray<T>(ref T[] dst, Vector<T> src) where T : struct
        {
            Span<byte> span = MemoryMarshal.AsBytes(dst.AsSpan());
            MemoryMarshal.Write(span, src);
        }

        public static Vector<T> GetRandomVector<T>()
        {
            long vsize = Unsafe.SizeOf<Vector<T>>();
            byte[] data = new byte[vsize];
            for (int i = 0; i < vsize; i++)
            {
                data[i] = TestLibrary.Generator.GetByte();
            }

            // TODO-ARM64-SVE: Some test functions do not support propagation of NaN/Inf values.
            if (typeof(T) == typeof(float))
            {
                for (int i = 0; i < vsize / sizeof(float); i++)
                {
                    // Clear bit 23 to suppress generation of NaN/Inf values.
                    data[i * sizeof(float) + 2] &= byte.CreateTruncating(~(1 << 7));
                }
            }
            else if (typeof(T) == typeof(double))
            {
                for (int i = 0; i < vsize / sizeof(double); i++)
                {
                    // Clear bit 52 to suppress generation of NaN/Inf values.
                    data[i * sizeof(double) + 6] &= byte.CreateTruncating(~(1 << 4));
                }
            }

            return new Vector<T>(data.AsSpan());
        }

        public static Vector<T> GetRandomMask<T>()
        {
            long vsize = Unsafe.SizeOf<Vector<T>>();
            long tsize = Unsafe.SizeOf<T>();

            byte[] data = new byte[vsize];

            long count = vsize / tsize;
            for (int i = 0; i < count; i++)
            {
                // Bias the generator to produces zero values at least 50% of the time.
                // Elements that pass through this choice will be filled with random data.
                if (TestLibrary.Generator.GetBool())
                {
                    for (int j = 0; j < tsize; j++)
                    {
                        data[i * tsize + j] = TestLibrary.Generator.GetByte();
                    }
                }
            }

            return new Vector<T>(data.AsSpan());
        }

        public class PinnedVector<T> where T : struct
        {
            private byte[] buf;
            private GCHandle inHandle;
            private ulong alignment;

            private void Alloc(T[] data, int alignment)
            {
                unsafe
                {
                    int sizeOfinArray1 = data.Length * Unsafe.SizeOf<T>();
                    if ((alignment != 64 && alignment != 16 && alignment != 8) || (alignment * 2) < sizeOfinArray1)
                    {
                        throw new ArgumentException($"Invalid value of alignment: {alignment}, sizeOfinArray1: {sizeOfinArray1}");
                    }

                    buf = new byte[alignment * 2];
                    inHandle = GCHandle.Alloc(buf, GCHandleType.Pinned);
                    this.alignment = (ulong)alignment;
                    Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(Ptr), ref Unsafe.As<T, byte>(ref data[0]), (uint)sizeOfinArray1);
                }
            }

            public PinnedVector(T[] data, int alignment)
            {
                Alloc(data, alignment);
            }

            public PinnedVector(Vector<T> inVector, int alignment)
            {
                long tsize = Unsafe.SizeOf<T>();
                long vsize = Unsafe.SizeOf<Vector<T>>();
                long count = vsize / tsize;
                T[] data = new T[count];
                VectorToArray(ref data, inVector);
                Alloc(data, alignment);
            }

            public unsafe void* Ptr => Align((byte*)inHandle.AddrOfPinnedObject().ToPointer(), alignment);

            public void Dispose()
            {
                inHandle.Free();
            }

            private static unsafe void* Align(byte* buffer, ulong expectedAlignment)
            {
                return (void*)(((ulong)buffer + expectedAlignment - 1) & ~(expectedAlignment - 1));
            }

            public Vector<T> Value
            {
                get
                {
                    unsafe
                    {
                        return Unsafe.Read<Vector<T>>(Ptr);
                    }
                }
                set
                {
                    unsafe
                    {
                        Unsafe.Write(Ptr, value);
                    }
                }
            }
        }
    }
}