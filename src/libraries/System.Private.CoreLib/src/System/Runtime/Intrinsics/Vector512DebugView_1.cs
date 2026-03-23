// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    internal readonly struct Vector512DebugView<T>
    {
        private readonly Vector512<T> _value;

        public Vector512DebugView(Vector512<T> value)
        {
            _value = value;
        }

        public byte[] ByteView
        {
            get
            {
                var items = new byte[Vector512<byte>.Count];
                Unsafe.WriteUnaligned(ref items[0], _value);
                return items;
            }
        }

        public double[] DoubleView
        {
            get
            {
                var items = new double[Vector512<double>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public short[] Int16View
        {
            get
            {
                var items = new short[Vector512<short>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public int[] Int32View
        {
            get
            {
                var items = new int[Vector512<int>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public long[] Int64View
        {
            get
            {
                var items = new long[Vector512<long>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<long, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public nint[] NIntView
        {
            get
            {
                var items = new nint[Vector512<nint>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nint, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public nuint[] NUIntView
        {
            get
            {
                var items = new nuint[Vector512<nuint>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<nuint, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public sbyte[] SByteView
        {
            get
            {
                var items = new sbyte[Vector512<sbyte>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public float[] SingleView
        {
            get
            {
                var items = new float[Vector512<float>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public ushort[] UInt16View
        {
            get
            {
                var items = new ushort[Vector512<ushort>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<ushort, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public uint[] UInt32View
        {
            get
            {
                var items = new uint[Vector512<uint>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref items[0]), _value);
                }
                return items;
            }
        }

        public ulong[] UInt64View
        {
            get
            {
                var items = new ulong[Vector512<ulong>.Count];
                // TODO(unsafe): Baselining unsafe usage
                unsafe
                {
                    Unsafe.WriteUnaligned(ref Unsafe.As<ulong, byte>(ref items[0]), _value);
                }
                return items;
            }
        }
    }
}
