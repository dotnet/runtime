// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (array.Length - index < this.Count)
                throw new ArgumentException(SR.ArgumentOutOfRange_Index, nameof(index));

            // the actual copy is a NOP
        }

        public int Count
        {
            get
            {
                return 0;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        // IDictionary members

        public object? this[object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
                }
                return null;
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
                }

                if (!key.GetType().IsSerializable)
                    throw new ArgumentException(SR.Argument_NotSerializable, nameof(key));

                if ((value != null) && (!value.GetType().IsSerializable))
                    throw new ArgumentException(SR.Argument_NotSerializable, nameof(value));

                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
        }

        public ICollection Keys
        {
            get
            {
                return Array.Empty<object>();
            }
        }

        public ICollection Values
        {
            get
            {
                return Array.Empty<object>();
            }
        }

        public bool Contains(object key)
        {
            return false;
        }

        public void Add(object key, object? value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), SR.ArgumentNull_Key);
            }

            if (!key.GetType().IsSerializable)
                throw new ArgumentException(SR.Argument_NotSerializable, nameof(key));

            if ((value != null) && (!value.GetType().IsSerializable))
                throw new ArgumentException(SR.Argument_NotSerializable, nameof(value));

            throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
        }

        public void Clear()
        {
            throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return true;
            }
        }

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

            public object? Current
            {
                get
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
            }

            public void Reset()
            {
            }

            // IDictionaryEnumerator members

            public object Key
            {
                get
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
            }

            public object? Value
            {
                get
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                }
            }
        }
    }
}
