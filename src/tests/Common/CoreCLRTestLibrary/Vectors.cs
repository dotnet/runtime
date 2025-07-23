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
        public static void VectorToArray<T>(ref T[] to, Vector<T> from)
        {
            long tsize = Unsafe.SizeOf<T>();
            long vsize = Unsafe.SizeOf<Vector<T>>();
            if (to.Length * tsize != vsize)
            {
                throw new ArgumentException("sizeof(to) != sizeof(from)");
            }
            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref to[0]), from);
        }

        public static void ArrayToVector<T>(ref Vector<T> to, T[] from)
        {
            long tsize = Unsafe.SizeOf<T>();
            long vsize = Unsafe.SizeOf<Vector<T>>();
            if (from.Length * tsize != vsize)
            {
                throw new ArgumentException("sizeof(to) != sizeof(from)");
            }
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector<T>, byte>(ref to), ref Unsafe.As<T, byte>(ref from[0]), checked((uint)vsize));
        }

        public static Vector<T> GetRandomVector<T>()
        {
            long vsize = Unsafe.SizeOf<Vector<T>>();

            byte[] data = new byte[vsize];
            for (int i = 0; i < vsize; i++)
            {
                data[i] = TestLibrary.Generator.GetByte();
            }

            Vector<T> vec = new Vector<T>();
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector<T>, byte>(ref vec), ref Unsafe.AsRef<byte>(ref data[0]), checked((uint)vsize));
            return vec;
        }

        public static Vector<T> GetRandomMask<T>()
        {
            long vsize = Unsafe.SizeOf<Vector<T>>();
            long tsize = Unsafe.SizeOf<T>();

            byte[] data = new byte[vsize];
            for (int i = 0; i < vsize; i++)
            {
                data[i] = 0;
            }

            long count = vsize / tsize;
            for (int i = 0; i < count; i++)
            {
                data[i * tsize] |= (byte)(TestLibrary.Generator.GetByte() & 1);
            }

            Vector<T> vec = new Vector<T>();
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Vector<T>, byte>(ref vec), ref Unsafe.AsRef<byte>(ref data[0]), checked((uint)vsize));
            return vec;
        }

        public class PinnedVector<T>
        {
            private byte[] buf;
            private GCHandle inHandle1;
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
                    inHandle1 = GCHandle.Alloc(buf, GCHandleType.Pinned);
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

            public unsafe void* Ptr => Align((byte*)inHandle1.AddrOfPinnedObject().ToPointer(), alignment);

            public void Dispose()
            {
                inHandle1.Free();
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