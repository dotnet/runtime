// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Collections;
using System.ComponentModel;


namespace System.Xml.Serialization
{
    /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlElementAttributes : IList
    {
        private ArrayList _list;

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlElementAttribute this[int index]
        {
            get { return (XmlElementAttribute)List[index]; }
            set { List[index] = value; }
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Add"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlElementAttribute attribute)
        {
            return List.Add(attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Insert"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlElementAttribute attribute)
        {
            List.Insert(index, attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlElementAttribute attribute)
        {
            return List.IndexOf(attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlElementAttribute attribute)
        {
            return List.Contains(attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.Remove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlElementAttribute attribute)
        {
            List.Remove(attribute);
        }

        /// <include file='doc\XmlElementAttributes.uex' path='docs/doc[@for="XmlElementAttributes.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlElementAttribute[] array, int index)
        {
            List.CopyTo(array, index);
        }
        private ArrayList InnerList
        {
            get
            {
                if (_list == null)
                    _list = new ArrayList();
                return _list;
            }
        }

        private IList List
        {
            get { return (IList)InnerList; }
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
            InnerList.Clear();
        }

        public void RemoveAt(int index)
        {
            InnerList.RemoveAt(index);
        }

        bool IList.IsReadOnly
        {
            get { return InnerList.IsReadOnly; }
        }

        bool IList.IsFixedSize
        {
            get { return InnerList.IsFixedSize; }
        }

        bool ICollection.IsSynchronized
        {
            get { return InnerList.IsSynchronized; }
        }

        Object ICollection.SyncRoot
        {
            get { throw new NotSupportedException(); }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            List.CopyTo(array, index);
        }

        Object IList.this[int index]
        {
            get
            {
                return InnerList[index];
            }
            set
            {
                InnerList[index] = value;
            }
        }

        bool IList.Contains(Object value)
        {
            return InnerList.Contains(value);
        }

        int IList.Add(Object value)
        {
            return List.Add(value);
        }

        void IList.Remove(Object value)
        {
            int index = InnerList.IndexOf(value);
            InnerList.RemoveAt(index);
        }

        int IList.IndexOf(Object value)
        {
            return InnerList.IndexOf(value);
        }

        void IList.Insert(int index, Object value)
        {
            InnerList.Insert(index, value);
        }

        public IEnumerator GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }
    }
}
