// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    internal readonly struct Vector128DebugView<T> where T : struct
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
                var items = new byte[16];
                Unsafe.WriteUnaligned(ref items[0], _value);
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                var items = new double[2];
                Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref items[0]), _value);
                return items;
            }
        }

        public short[] Int16View
        {
            get
            {
                var items = new short[8];
                Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref items[0]), _value);
                return items;
            }
        }

        public int[] Int32View
        {
            get
            {
                var items = new int[4];
                Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref items[0]), _value);
                return items;
            }
        }

        public long[] Int64View
        {
            get
            {
                var items = new long[2];
                Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref items[0]), _value);
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                var items = new sbyte[16];
                Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref items[0]), _value);
                return items;
            }
        }

        public float[] SingleView
        {
            get
            {
                var items = new float[4];
                Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref items[0]), _value);
                return items;
            }
        }

        public ushort[] UInt16View
        {
            get
            {
                var items = new ushort[8];
                Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref items[0]), _value);
                return items;
            }
        }

        public uint[] UInt32View
        {
            get
            {
                var items = new uint[4];
                Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref items[0]), _value);
                return items;
            }
        }

        public ulong[] UInt64View
        {
            get
            {
                var items = new ulong[2];
                Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref items[0]), _value);
                return items;
            }
        }
    }
}
