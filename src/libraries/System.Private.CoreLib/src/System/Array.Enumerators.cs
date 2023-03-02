// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace System
{
    internal sealed class ArrayEnumerator : IEnumerator, ICloneable
    {
        private readonly Array _array;
        private nint _index;

        internal ArrayEnumerator(Array array)
        {
            _array = array;
            _index = -1;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public bool MoveNext()
        {
            nint index = _index + 1;
            nuint length = _array.NativeLength;
            if ((nuint)index >= length)
            {
                _index = (nint)length;
                return false;
            }
            _index = index;
            return true;
        }

        public object? Current
        {
            get
            {
                nint index = _index;
                Array array = _array;

                if ((nuint)index >= array.NativeLength)
                {
                    if (index < 0)
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                    }
                    else
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
                    }
                }

                return array.InternalGetValue(index);
            }
        }

        public void Reset()
        {
            _index = -1;
        }
    }

    internal class SZGenericArrayEnumeratorBase
    {
        protected readonly Array _array;
        protected int _index;

        protected SZGenericArrayEnumeratorBase(Array array)
        {
            Debug.Assert(array != null);

            _array = array;
            _index = -1;
        }

        public bool MoveNext()
        {
            int index = _index + 1;
            uint length = (uint)_array.NativeLength;
            if ((uint)index >= length)
            {
                _index = (int)length;
                return false;
            }
            _index = index;
            return true;
        }

        public void Reset() => _index = -1;

#pragma warning disable CA1822 // https://github.com/dotnet/roslyn-analyzers/issues/5911
        public void Dispose()
        {
        }
#pragma warning restore CA1822
    }

    internal sealed class SZGenericArrayEnumerator<T> : SZGenericArrayEnumeratorBase, IEnumerator<T>
    {
        // Array.Empty is intentionally omitted here, since we don't want to pay for generic instantiations that
        // wouldn't have otherwise been used.
#pragma warning disable CA1825
        internal static readonly SZGenericArrayEnumerator<T> Empty = new SZGenericArrayEnumerator<T>(new T[0]);
#pragma warning restore CA1825

        public SZGenericArrayEnumerator(T[] array)
            : base(array)
        {
        }

        public T Current
        {
            get
            {
                int index = _index;
                T[] array = Unsafe.As<T[]>(_array);

                if ((uint)index >= (uint)array.Length)
                {
                    ThrowHelper.ThrowInvalidOperationException_EnumCurrent(index);
                }

                return array[index];
            }
        }

        object? IEnumerator.Current => Current;
    }
}
