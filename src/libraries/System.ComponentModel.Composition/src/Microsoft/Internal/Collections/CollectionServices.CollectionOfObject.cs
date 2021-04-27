// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Internal.Collections
{
    internal static partial class CollectionServices
    {
        public static ICollection<object> GetCollectionWrapper(Type itemType, object collectionObject)
        {
            if (itemType == null)
            {
                throw new ArgumentNullException(nameof(itemType));
            }

            if (collectionObject == null)
            {
                throw new ArgumentNullException(nameof(collectionObject));
            }

            var underlyingItemType = itemType.UnderlyingSystemType;

            if (underlyingItemType == typeof(object))
            {
                return (ICollection<object>)collectionObject;
            }

            // Most common .NET collections implement IList as well so for those
            // cases we can optimize the wrapping instead of using reflection to create
            // a generic type.
            if (typeof(IList).IsAssignableFrom(collectionObject.GetType()))
            {
                return new CollectionOfObjectList((IList)collectionObject);
            }

            Type collectionType = typeof(CollectionOfObject<>).MakeGenericType(underlyingItemType);

            return (ICollection<object>)Activator.CreateInstance(collectionType, collectionObject)!;
        }

        private sealed class CollectionOfObjectList : ICollection<object>
        {
            private readonly IList _list;

            public CollectionOfObjectList(IList list)
            {
                _list = list;
            }

            public void Add(object item)
            {
                _list.Add(item);
            }

            public void Clear()
            {
                _list.Clear();
            }

            public bool Contains(object item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(object[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { throw new NotImplementedException(); }
            }

            public bool IsReadOnly
            {
                get { return _list.IsReadOnly; }
            }

            public bool Remove(object item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<object> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private sealed class CollectionOfObject<T> : ICollection<object>
        {
            private readonly ICollection<T> _collectionOfT;

            public CollectionOfObject(object collectionOfT)
            {
                _collectionOfT = (ICollection<T>)collectionOfT;
            }

            public void Add(object item)
            {
                _collectionOfT.Add((T)item);
            }

            public void Clear()
            {
                _collectionOfT.Clear();
            }

            public bool Contains(object item)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(object[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public int Count
            {
                get { throw new NotImplementedException(); }
            }

            public bool IsReadOnly
            {
                get { return _collectionOfT.IsReadOnly; }
            }

            public bool Remove(object item)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<object> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
