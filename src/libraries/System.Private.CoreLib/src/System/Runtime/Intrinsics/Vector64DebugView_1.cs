// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    internal readonly struct Vector64DebugView<T>
        where T : struct
    {
        private readonly Vector64<T> _value;

        public Vector64DebugView(Vector64<T> value)
        {
            _value = value;
        }

        public byte[] ByteView
        {
            get
            {
                var items = new byte[Vector64<byte>.Count];
                Unsafe.WriteUnaligned(ref items[0], _value);
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                var items = new double[Vector64<double>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref items[0]), _value);
                return items;
            }
        }

        public short[] Int16View
        {
            get
            {
                var items = new short[Vector64<short>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref items[0]), _value);
                return items;
            }
        }

        public int[] Int32View
        {
            get
            {
                var items = new int[Vector64<int>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref items[0]), _value);
                return items;
            }
        }

        public long[] Int64View
        {
            get
            {
                var items = new long[Vector64<long>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref items[0]), _value);
                return items;
            }
        }

        public nint[] NIntView
        {
            get
            {
                var items = new nint[Vector64<nint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref items[0]), _value);
                return items;
            }
        }

        public nuint[] NUIntView
        {
            get
            {
                var items = new nuint[Vector64<nuint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<nuint, byte>(ref items[0]), _value);
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                var items = new sbyte[Vector64<sbyte>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref items[0]), _value);
                return items;
            }
        }

        public float[] SingleView
        {
            get
            {
                var items = new float[Vector64<float>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref items[0]), _value);
                return items;
            }
        }

        public ushort[] UInt16View
        {
            get
            {
                var items = new ushort[Vector64<ushort>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref items[0]), _value);
                return items;
            }
        }

        public uint[] UInt32View
        {
            get
            {
                var items = new uint[Vector64<uint>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref items[0]), _value);
                return items;
            }
        }

        public ulong[] UInt64View
        {
            get
            {
                var items = new ulong[Vector64<ulong>.Count];
                Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref items[0]), _value);
                return items;
            }
        }
    }
}
