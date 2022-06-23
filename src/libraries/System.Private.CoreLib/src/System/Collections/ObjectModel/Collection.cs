// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.ObjectModel
{
    [Serializable]
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class Collection<T> : IList<T>, IList, IReadOnlyList<T>
    {
        private readonly IList<T> items; // Do not rename (binary serialization)

        public Collection()
        {
            items = new List<T>();
        }

        public Collection(IList<T> list)
        {
            if (list == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.list);
            }
            items = list;
        }

        public int Count => items.Count;

        protected IList<T> Items => items;

        public T this[int index]
        {
            get => items[index];
            set
            {
                if (items.IsReadOnly)
                {
                    ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
                }

                if ((uint)index >= (uint)items.Count)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
                }

                SetItem(index, value);
            }
        }

        public void Add(T item)
        {
            if (items.IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            }

            int index = items.Count;
            InsertItem(index, item);
        }

        public void Clear()
        {
            if (items.IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            }

            ClearItems();
        }

        public void CopyTo(T[] array, int index)
        {
            items.CopyTo(array, index);
        }

        public bool Contains(T item)
        {
            return items.Contains(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (items.IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            }

            if ((uint)index > (uint)items.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            InsertItem(index, item);
        }

        public bool Remove(T item)
        {
            if (items.IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            }

            int index = items.IndexOf(item);
            if (index < 0) return false;
            RemoveItem(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (items.IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            }

            if ((uint)index >= (uint)items.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
            }

            RemoveItem(index);
        }

        protected virtual void ClearItems()
        {
            items.Clear();
        }

        protected virtual void InsertItem(int index, T item)
        {
            items.Insert(index, item);
        }

        protected virtual void RemoveItem(int index)
        {
            items.RemoveAt(index);
        }

        protected virtual void SetItem(int index, T item)
        {
            items[index] = item;
        }

        bool ICollection<T>.IsReadOnly => items.IsReadOnly;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)items).GetEnumerator();
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => items is ICollection coll ? coll.SyncRoot : this;

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (array.Rank != 1)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            }

            if (array.GetLowerBound(0) != 0)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
            }

            if (index < 0)
            {
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            }

            if (array.Length - index < Count)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            if (array is T[] tArray)
            {
                items.CopyTo(tArray, index);
            }
            else
            {
                //
                // Catch the obvious case assignment will fail.
                // We can't find all possible problems by doing the check though.
                // For example, if the element type of the Array is derived from T,
                // we can't figure out if we can successfully copy the element beforehand.
                //
                Type targetType = array.GetType().GetElementType()!;
                Type sourceType = typeof(T);
                if (!(targetType.IsAssignableFrom(sourceType) || sourceType.IsAssignableFrom(targetType)))
                {
                    ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
                }

                //
                // We can't cast array of value type to object[], so we don't support
                // widening of primitive types here.
                //
                object?[]? objects = array as object[];
                if (objects == null)
                {
                    ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
                }

                int count = items.Count;
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        objects[index++] = items[i];
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    ThrowHelper.ThrowArgumentException_Argument_InvalidArrayType();
                }
            }
        }

        object? IList.this[int index]
        {
            get => items[index];
            set
            {
                ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);

                T? item = default;

                try
                {
                    item = (T)value!;
                }
                catch (InvalidCastException)
                {
                    ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));
                }

                this[index] = item;
            }
        }

        bool IList.IsReadOnly => items.IsReadOnly;

        bool IList.IsFixedSize
        {
            get
            {
                // There is no IList<T>.IsFixedSize, so we must assume that only
                // readonly collections are fixed size, if our internal item
                // collection does not implement IList.  Note that Array implements
                // IList, and therefore T[] and U[] will be fixed-size.
                if (items is IList list)
                {
                    return list.IsFixedSize;
                }
                return items.IsReadOnly;
            }
        }

        int IList.Add(object? value)
        {
            if (items.IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            }
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);

            T? item = default;

            try
            {
                item = (T)value!;
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));
            }

            Add(item);

            return this.Count - 1;
        }

        bool IList.Contains(object? value)
        {
            if (IsCompatibleObject(value))
            {
                return Contains((T)value!);
            }
            return false;
        }

        int IList.IndexOf(object? value)
        {
            if (IsCompatibleObject(value))
            {
                return IndexOf((T)value!);
            }
            return -1;
        }

        void IList.Insert(int index, object? value)
        {
            if (items.IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            }
            ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);

            T? item = default;

            try
            {
                item = (T)value!;
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));
            }

            Insert(index, item);
        }

        void IList.Remove(object? value)
        {
            if (items.IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            }

            if (IsCompatibleObject(value))
            {
                Remove((T)value!);
            }
        }

        private static bool IsCompatibleObject(object? value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return (value is T) || (value == null && default(T) == null);
        }
    }
}
