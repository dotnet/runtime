// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
**
** Purpose: List for exceptions.
**
**
===========================================================*/

namespace System.Collections
{
    ///    This is a simple implementation of IDictionary that is empty and readonly.
    internal sealed class EmptyReadOnlyDictionaryInternal : IDictionary
    {
        // Note that this class must be agile with respect to AppDomains.  See its usage in
        // System.Exception to understand why this is the case.

        public EmptyReadOnlyDictionaryInternal()
        {
        }

        // IEnumerable members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new NodeEnumerator();
        }

        // ICollection members

        public void CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);

            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (array.Length - index < this.Count)
                throw new ArgumentException(SR.ArgumentOutOfRange_IndexMustBeLessOrEqual, nameof(index));

            // the actual copy is a NOP
        }

        public int Count => 0;

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        // IDictionary members

        public object? this[object key]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);

                return null;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(key);

                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        public ICollection Keys => Array.Empty<object>();

        public ICollection Values => Array.Empty<object>();

        public bool Contains(object key)
        {
            return false;
        }

        public void Add(object key, object? value)
        {
            ArgumentNullException.ThrowIfNull(key);

            throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
        }

        public void Clear()
        {
            throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public IDictionaryEnumerator GetEnumerator()
        {
            return new NodeEnumerator();
        }

        public void Remove(object key)
        {
            throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
        }

        private sealed class NodeEnumerator : IDictionaryEnumerator
        {
            public NodeEnumerator()
            {
            }

            // IEnumerator members

            public bool MoveNext()
            {
                return false;
            }

            public object? Current => throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

            public void Reset()
            {
            }

            // IDictionaryEnumerator members

            public object Key => throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

            public object? Value => throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);

            public DictionaryEntry Entry => throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
        }
    }
}
