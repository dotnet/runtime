// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal readonly struct VectorDebugView<T>
        where T : struct
    {
        private readonly Vector<T> _value;

        public VectorDebugView(Vector<T> value)
        {
            _value = value;
        }

        public byte[] ByteView
        {
            get
            {
                var items = new byte[Vector<byte>.Count];
                Unsafe.WriteUnaligned(ref items[0], _value);
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                var items = new double[Vector<double>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref items[0]), _value);
                return items;
            }
        }

        public short[] Int16View
        {
            get
            {
                var items = new short[Vector<short>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref items[0]), _value);
                return items;
            }
        }

        public int[] Int32View
        {
            get
            {
                var items = new int[Vector<int>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref items[0]), _value);
                return items;
            }
        }

        public long[] Int64View
        {
            get
            {
                var items = new long[Vector<long>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref items[0]), _value);
                return items;
            }
        }

        public nint[] NIntView
        {
            get
            {
                var items = new nint[Vector<nint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref items[0]), _value);
                return items;
            }
        }

        public nuint[] NUIntView
        {
            get
            {
                var items = new nuint[Vector<nuint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<nuint, byte>(ref items[0]), _value);
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                var items = new sbyte[Vector<sbyte>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref items[0]), _value);
                return items;
            }
        }

        public float[] SingleView
        {
            get
            {
                var items = new float[Vector<float>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref items[0]), _value);
                return items;
            }
        }

        public ushort[] UInt16View
        {
            get
            {
                var items = new ushort[Vector<ushort>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref items[0]), _value);
                return items;
            }
        }

        public uint[] UInt32View
        {
            get
            {
                var items = new uint[Vector<uint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref items[0]), _value);
                return items;
            }
        }

        public ulong[] UInt64View
        {
            get
            {
                var items = new ulong[Vector<ulong>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref items[0]), _value);
                return items;
            }
        }
    }
}
