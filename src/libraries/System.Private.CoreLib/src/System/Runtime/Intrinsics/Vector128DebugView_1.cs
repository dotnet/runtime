// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics
{
    internal readonly struct Vector128DebugView<T>
    {
        private readonly Vector128<T> _value;

        public Vector128DebugView(Vector128<T> value)
        {
            _value = value;
        }

        public byte[] ByteView
        {
            get
            {
                var items = new byte[Vector128<byte>.Count];
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(items), _value);
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                var items = new double[Vector128<double>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public short[] Int16View
        {
            get
            {
                var items = new short[Vector128<short>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public int[] Int32View
        {
            get
            {
                var items = new int[Vector128<int>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public long[] Int64View
        {
            get
            {
                var items = new long[Vector128<long>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public nint[] NIntView
        {
            get
            {
                var items = new nint[Vector128<nint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public nuint[] NUIntView
        {
            get
            {
                var items = new nuint[Vector128<nuint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<nuint, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                var items = new sbyte[Vector128<sbyte>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public float[] SingleView
        {
            get
            {
                var items = new float[Vector128<float>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public ushort[] UInt16View
        {
            get
            {
                var items = new ushort[Vector128<ushort>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public uint[] UInt32View
        {
            get
            {
                var items = new uint[Vector128<uint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }

        public ulong[] UInt64View
        {
            get
            {
                var items = new ulong[Vector128<ulong>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref MemoryMarshal.GetArrayDataReference(items)), _value);
                return items;
            }
        }
    }
}
