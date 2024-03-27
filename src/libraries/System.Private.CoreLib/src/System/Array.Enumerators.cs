// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

    internal abstract class SZGenericArrayEnumeratorBase : IDisposable
    {
        protected int _index;
        protected readonly int _endIndex;

        protected SZGenericArrayEnumeratorBase(int endIndex)
        {
            _index = -1;
            _endIndex = endIndex;
        }

        public bool MoveNext()
        {
            int index = _index + 1;
            if ((uint)index < (uint)_endIndex)
            {
                _index = index;
                return true;
            }
            _index = _endIndex;
            return false;
        }

        public void Reset() => _index = -1;

        public void Dispose()
        {
        }
    }

    internal sealed class SZGenericArrayEnumerator<T> : SZGenericArrayEnumeratorBase, IEnumerator<T>
    {
        private readonly T[]? _array;

        /// <summary>Provides an empty enumerator singleton.</summary>
        /// <remarks>
        /// If the consumer is using SZGenericArrayEnumerator elsewhere or is otherwise likely
        /// to be using T[] elsewhere, this singleton should be used.  Otherwise, GenericEmptyEnumerator's
        /// singleton should be used instead, as it doesn't reference T[] in order to reduce footprint.
        /// </remarks>
        internal static readonly SZGenericArrayEnumerator<T> Empty = new SZGenericArrayEnumerator<T>(null, 0);

        internal SZGenericArrayEnumerator(T[]? array, int endIndex)
            : base(endIndex)
        {
            Debug.Assert(array == null || endIndex == array.Length);
            _array = array;
        }

        public T Current
        {
            get
            {
                if ((uint)_index >= (uint)_endIndex)
                    ThrowHelper.ThrowInvalidOperationException_EnumCurrent(_index);
                return _array![_index];
            }
        }

        object? IEnumerator.Current => Current;
    }

    internal abstract class GenericEmptyEnumeratorBase : IDisposable, IEnumerator
    {
#pragma warning disable CA1822 // https://github.com/dotnet/roslyn-analyzers/issues/5911
        public bool MoveNext() => false;

        public object Current
        {
            get
            {
                ThrowHelper.ThrowInvalidOperationException_EnumCurrent(-1);
                return default;
            }
        }

        public void Reset() { }

        public void Dispose() { }
#pragma warning restore CA1822
    }

    /// <summary>Provides an empty enumerator singleton.</summary>
    /// <remarks>
    /// If the consumer is using SZGenericArrayEnumerator elsewhere or is otherwise likely
    /// to be using T[] elsewhere, SZGenericArrayEnumerator's singleton should be used.  Otherwise,
    /// this singleton should be used, as it doesn't reference T[] in order to reduce footprint.
    /// </remarks>
    internal sealed class GenericEmptyEnumerator<T> : GenericEmptyEnumeratorBase, IEnumerator<T>
    {
        public static readonly GenericEmptyEnumerator<T> Instance = new();

        private GenericEmptyEnumerator() { }

        public new T Current
        {
            get
            {
                ThrowHelper.ThrowInvalidOperationException_EnumCurrent(-1);
                return default;
            }
        }
    }
}
