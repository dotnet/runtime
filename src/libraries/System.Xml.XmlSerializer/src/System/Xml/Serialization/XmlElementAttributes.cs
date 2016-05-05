// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlElementAttributes : IList
    {
        private List<XmlElementAttribute> _list = new List<XmlElementAttribute>();

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute this[int index]
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

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Add"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlElementAttribute value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int index = _list.Count;
            _list.Add(value);
            return index;
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Insert"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlElementAttribute value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _list.Insert(index, value);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlElementAttribute value)
        {
            return _list.IndexOf(value);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlElementAttribute value)
        {
            return _list.Contains(value);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Remove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlElementAttribute value)
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

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlElementAttribute[] array, int index)
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
                throw new ArgumentNullException(nameof(value));
            }

            return List.Add(value);
        }

        void IList.Remove(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var attribute = value as XmlElementAttribute;
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
