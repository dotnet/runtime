// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>[To be supplied.]</para>
    /// </summary>
    public class ListSortDescriptionCollection : IList
    {
        private ArrayList _sorts = new ArrayList();

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListSortDescriptionCollection()
        {
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListSortDescriptionCollection(ListSortDescription[] sorts)
        {
            if (sorts != null)
            {
                for (int i = 0; i < sorts.Length; i++)
                {
                    _sorts.Add(sorts[i]);
                }
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public ListSortDescription this[int index]
        {
            get
            {
                return (ListSortDescription)_sorts[index];
            }
            set
            {
                throw new InvalidOperationException(SR.CantModifyListSortDescriptionCollection);
            }
        }

        // IList implementation
        //

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        bool IList.IsFixedSize => true;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        bool IList.IsReadOnly => true;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                throw new InvalidOperationException(SR.CantModifyListSortDescriptionCollection);
            }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        int IList.Add(object value)
        {
            throw new InvalidOperationException(SR.CantModifyListSortDescriptionCollection);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void IList.Clear()
        {
            throw new InvalidOperationException(SR.CantModifyListSortDescriptionCollection);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public bool Contains(object value)
        {
            return ((IList)_sorts).Contains(value);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public int IndexOf(object value)
        {
            return ((IList)_sorts).IndexOf(value);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void IList.Insert(int index, object value)
        {
            throw new InvalidOperationException(SR.CantModifyListSortDescriptionCollection);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void IList.Remove(object value)
        {
            throw new InvalidOperationException(SR.CantModifyListSortDescriptionCollection);
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        void IList.RemoveAt(int index)
        {
            throw new InvalidOperationException(SR.CantModifyListSortDescriptionCollection);
        }

        // ICollection
        //

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public int Count => _sorts.Count;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        bool ICollection.IsSynchronized => true;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        object ICollection.SyncRoot => this;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void CopyTo(Array array, int index)
        {
            _sorts.CopyTo(array, index);
        }

        // IEnumerable
        //

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _sorts.GetEnumerator();
        }
    }
}
