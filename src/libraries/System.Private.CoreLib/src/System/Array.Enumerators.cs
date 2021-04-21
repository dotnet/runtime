// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
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
            if ((nuint)index >= (nuint)_array.LongLength)
            {
                _index = (nint)_array.LongLength;
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

                if ((nuint)index >= (nuint)array.LongLength)
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

    internal sealed class SZGenericArrayEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _array;
        private int _index;

        // Array.Empty is intentionally omitted here, since we don't want to pay for generic instantiations that
        // wouldn't have otherwise been used.
#pragma warning disable CA1825
        internal static readonly SZGenericArrayEnumerator<T> Empty = new SZGenericArrayEnumerator<T>(new T[0]);
#pragma warning restore CA1825

        internal SZGenericArrayEnumerator(T[] array)
        {
            Debug.Assert(array != null);

            _array = array;
            _index = -1;
        }

        public bool MoveNext()
        {
            int index = _index + 1;
            if ((uint)index >= (uint)_array.Length)
            {
                _index = _array.Length;
                return false;
            }
            _index = index;
            return true;
        }

        public T Current
        {
            get
            {
                int index = _index;
                T[] array = _array;

                if ((uint)index >= (uint)array.Length)
                {
                    ThrowHelper.ThrowInvalidOperationException_EnumCurrent(index);
                }

                return array[index];
            }
        }

        object? IEnumerator.Current => Current;

        void IEnumerator.Reset() => _index = -1;

        public void Dispose()
        {
        }
    }
}
