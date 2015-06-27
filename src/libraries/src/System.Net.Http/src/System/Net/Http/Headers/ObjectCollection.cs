// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Net.Http.Headers
{
    // We need to prevent 'null' values in the collection. Since List<T> allows them, we will create
    // a custom collection class. It is less efficient than List<T> but only used for small collections.
    internal class ObjectCollection<T> : Collection<T> where T : class
    {
        private static readonly Action<T> s_defaultValidator = CheckNotNull;

        private Action<T> _validator;

        public ObjectCollection()
            : this(s_defaultValidator)
        {
        }

        public ObjectCollection(Action<T> validator)
        {
            _validator = validator;
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
                throw new ArgumentNullException("item");
            }
        }
    }
}
