// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;


namespace System.Xml.Serialization
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlAnyElementAttributes : IList
    {
        private List<XmlAnyElementAttribute> _list = new List<XmlAnyElementAttribute>();

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute this[int index]
        {
            get { return _list[index]; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _list[index] = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlAnyElementAttribute value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int index = _list.Count;
            _list.Add(value);
            return index;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlAnyElementAttribute value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _list.Insert(index, value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlAnyElementAttribute value)
        {
            return _list.IndexOf(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlAnyElementAttribute value)
        {
            return _list.Contains(value);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlAnyElementAttribute value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!_list.Remove(value))
            {
                throw new ArgumentException(SR.Arg_RemoveArgNotFound);
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlAnyElementAttribute[] array, int index)
        {
            _list.CopyTo(array, index);
        }

        private IList List
        {
            get { return _list; }
        }

        public int Count
        {
            get
            {
                return _list == null ? 0 : _list.Count;
            }
        }

        public void Clear()
        {
            _list.Clear();
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        bool IList.IsReadOnly
        {
            get { return List.IsReadOnly; }
        }

        bool IList.IsFixedSize
        {
            get { return List.IsFixedSize; }
        }

        bool ICollection.IsSynchronized
        {
            get { return List.IsSynchronized; }
        }

        Object ICollection.SyncRoot
        {
            get { return List.SyncRoot; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            List.CopyTo(array, index);
        }

        Object IList.this[int index]
        {
            get
            {
                return List[index];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                List[index] = value;
            }
        }

        bool IList.Contains(Object value)
        {
            return List.Contains(value);
        }

        int IList.Add(Object value)
        {
            if (value == null)
            {
                throw new ArgumentException(nameof(value));
            }

            return List.Add(value);
        }

        void IList.Remove(Object value)
        {
            if (value == null)
            {
                throw new ArgumentException(nameof(value));
            }

            var attribute = value as XmlAnyElementAttribute;
            if (attribute == null)
            {
                throw new ArgumentException(SR.Arg_RemoveArgNotFound);
            }
            Remove(attribute);
        }

        int IList.IndexOf(Object value)
        {
            return List.IndexOf(value);
        }

        void IList.Insert(int index, Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            List.Insert(index, value);
        }

        public IEnumerator GetEnumerator()
        {
            return List.GetEnumerator();
        }
    }
}
