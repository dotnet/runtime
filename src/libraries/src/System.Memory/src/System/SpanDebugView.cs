// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    internal sealed class SpanDebugView<T>
    {
        private readonly T[] _pinnable;
        private readonly IntPtr _byteOffset;
        private readonly int _length;

        public SpanDebugView(Span<T> collection)
        {
            _pinnable = (T[])(object)collection.Pinnable;
            _byteOffset = collection.ByteOffset;
            _length = collection.Length;
        }

        public SpanDebugView(ReadOnlySpan<T> collection)
        {
            _pinnable = (T[])(object)collection.Pinnable;
            _byteOffset = collection.ByteOffset;
            _length = collection.Length;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public unsafe T[] Items
        {
            get
            {
                int elementSize = typeof(T).GetTypeInfo().IsValueType ? Unsafe.SizeOf<T>() : IntPtr.Size;//Workaround to VIL bug where Unsafe.SizeOf<T> where T is a class, returns the size of its fields instead of IntPtr.Size
                T[] result = new T[_length];

                if (_pinnable == null)
                {
                    byte* source = (byte*)_byteOffset.ToPointer();

                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = Unsafe.Read<T>(source);
                        source = source + elementSize;
                    }
                }
                else
                {
                    long byteOffsetInt = _byteOffset.ToInt64();
                    long arrayAdjustment = SpanHelpers.PerTypeValues<T>.ArrayAdjustment.ToInt64();
                    int sourceIndex = (int)((byteOffsetInt - arrayAdjustment) / elementSize);

                    Array.Copy(_pinnable, sourceIndex, result, 0, _length);
                }

                return result;
            }
        }
    }
}
