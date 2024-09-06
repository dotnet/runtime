// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace Claunia.PropertyList
{
    public partial class NSArray : IList<NSObject>
    {
        /// <inheritdoc />
        public NSObject this[int index]
        {
            get => array[index];

            set => array[index] = value;
        }

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public void Add(NSObject item) => array.Add(item);

        /// <inheritdoc />
        public void Clear() => array.Clear();

        /// <inheritdoc />
        public bool Contains(NSObject item) => array.Contains(item);

        /// <inheritdoc />
        public void CopyTo(NSObject[] array, int arrayIndex) => this.array.CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<NSObject> GetEnumerator() => array.GetEnumerator();

        /// <inheritdoc />
        public int IndexOf(NSObject item) => array.IndexOf(item);

        /// <inheritdoc />
        public void Insert(int index, NSObject item) => array.Insert(index, item);

        /// <inheritdoc />
        public bool Remove(NSObject item) => array.Remove(item);

        /// <inheritdoc />
        public void RemoveAt(int index) => array.RemoveAt(index);

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => array.GetEnumerator();

        public void Add(object item) => Add(Wrap(item));

        public bool Contains(object item) => Contains(Wrap(item));

        public int IndexOf(object item) => array.IndexOf(Wrap(item));

        public void Insert(int index, object item) => Insert(index, Wrap(item));

        public bool Remove(object item) => Remove(Wrap(item));
    }
}
