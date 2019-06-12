// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // These stubs will be used when a call via ICollection<KeyValuePair<K, V>> is made in managed code.
    // This can mean two things - either the underlying unmanaged object implements IMap<K, V> or it
    // implements IVector<IKeyValuePair<K, V>> and we cannot determine this statically in the general
    // case so we have to cast at run-time. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not MapToCollectionAdapter objects. Rather, they are of type
    // IVector<KeyValuePair<K, V>> or IMap<K, V>. No actual MapToCollectionAdapter object is ever
    // instantiated. Thus, you will see a lot of expressions that cast "this" to "IVector<KeyValuePair<K, V>>"
    // or "IMap<K, V>".
    internal sealed class MapToCollectionAdapter
    {
        private MapToCollectionAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // int Count { get }
        internal int Count<K, V>() where K : notnull
        {
            object _this = Unsafe.As<object>(this);

            if (_this is IMap<K, V> _this_map)
            {
                uint size = _this_map.Size;

                if (((uint)int.MaxValue) < size)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingDictionaryTooLarge);
                }

                return (int)size;
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = Unsafe.As<IVector<KeyValuePair<K, V>>>(this);
                uint size = _this_vector.Size;

                if (((uint)int.MaxValue) < size)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
                }

                return (int)size;
            }
        }

        // bool IsReadOnly { get }
        internal bool IsReadOnly<K, V>() where K : notnull
        {
            return false;
        }

        // void Add(T item)
        internal void Add<K, V>(KeyValuePair<K, V> item) where K : notnull
        {
            object _this = Unsafe.As<object>(this);

            if (_this is IDictionary<K, V> _this_dictionary)
            {
                _this_dictionary.Add(item.Key, item.Value);
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = Unsafe.As<IVector<KeyValuePair<K, V>>>(this);
                _this_vector.Append(item);
            }
        }

        // void Clear()
        internal void Clear<K, V>() where K : notnull
        {
            object _this = Unsafe.As<object>(this);

            if (_this is IMap<K, V> _this_map)
            {
                _this_map.Clear();
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = Unsafe.As<IVector<KeyValuePair<K, V>>>(this);
                _this_vector.Clear();
            }
        }

        // bool Contains(T item)
        internal bool Contains<K, V>(KeyValuePair<K, V> item) where K : notnull
        {
            object _this = Unsafe.As<object>(this);

            if (_this is IDictionary<K, V> _this_dictionary)
            {
                V value;
                bool hasKey = _this_dictionary.TryGetValue(item.Key, out value);

                if (!hasKey)
                    return false;

                return EqualityComparer<V>.Default.Equals(value, item.Value);
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = Unsafe.As<IVector<KeyValuePair<K, V>>>(this);

                uint index;
                return _this_vector.IndexOf(item, out index);
            }
        }

        // void CopyTo(T[] array, int arrayIndex)
        internal void CopyTo<K, V>(KeyValuePair<K, V>[] array, int arrayIndex) where K : notnull
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length <= arrayIndex && Count<K, V>() > 0)
                throw new ArgumentException(SR.Argument_IndexOutOfArrayBounds);

            if (array.Length - arrayIndex < Count<K, V>())
                throw new ArgumentException(SR.Argument_InsufficientSpaceToCopyCollection);


            IIterable<KeyValuePair<K, V>> _this = Unsafe.As<IIterable<KeyValuePair<K, V>>>(this);
            foreach (KeyValuePair<K, V> mapping in _this)
            {
                array[arrayIndex++] = mapping;
            }
        }

        // bool Remove(T item)
        internal bool Remove<K, V>(KeyValuePair<K, V> item) where K : notnull
        {
            object _this = Unsafe.As<object>(this);

            if (_this is IDictionary<K, V> _this_dictionary)
            {
                return _this_dictionary.Remove(item.Key);
            }
            else
            {
                IVector<KeyValuePair<K, V>> _this_vector = Unsafe.As<IVector<KeyValuePair<K, V>>>(this);
                uint index;
                bool exists = _this_vector.IndexOf(item, out index);

                if (!exists)
                    return false;

                if (((uint)int.MaxValue) < index)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_CollectionBackingListTooLarge);
                }

                VectorToListAdapter.RemoveAtHelper<KeyValuePair<K, V>>(_this_vector, index);
                return true;
            }
        }
    }
}
