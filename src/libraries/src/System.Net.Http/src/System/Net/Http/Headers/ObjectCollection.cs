// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Net.Http.Headers
{
    // We need to prevent 'null' values in the collection. Since List<T> allows them, we will create
    // a custom collection class. It is less efficient than List<T> but only used for small collections.
    internal sealed class ObjectCollection<T> : Collection<T> where T : class
    {
        private static readonly Action<T> s_defaultValidator = CheckNotNull;

        private readonly Action<T> _validator;

        public ObjectCollection()
            : this(s_defaultValidator)
        {
        }

        public ObjectCollection(Action<T> validator)
            : base(new List<T>())
        {
            _validator = validator;
        }

        // This is only used internally to enumerate the collection
        // without the enumerator allocation.
        new public List<T>.Enumerator GetEnumerator()
        {
            return ((List<T>)Items).GetEnumerator();
        }

        protected override void InsertItem(int index, T item)
        {
            if (_validator != null)
            {
                _validator(item);
            }
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item)
        {
            if (_validator != null)
            {
                _validator(item);
            }
            base.SetItem(index, item);
        }

        private static void CheckNotNull(T item)
        {
            // Null values cannot be added to the collection.
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
        }
    }
}
