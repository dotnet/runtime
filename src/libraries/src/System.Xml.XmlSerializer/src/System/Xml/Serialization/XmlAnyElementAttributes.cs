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
    /// <include file='doc\XmlAnyElementAttributes.uex' path='docs/doc[@for="XmlAnyElementAttributes"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlAnyElementAttributes : IList
    {
        private ArrayList _list;

        /// <include file='doc\XmlAnyElementAttributes.uex' path='docs/doc[@for="XmlAnyElementAttributes.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlAnyElementAttribute this[int index]
        {
            get { return (XmlAnyElementAttribute)List[index]; }
            set { List[index] = value; }
        }

        /// <include file='doc\XmlAnyElementAttributes.uex' path='docs/doc[@for="XmlAnyElementAttributes.Add"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlAnyElementAttribute attribute)
        {
            return List.Add(attribute);
        }

        /// <include file='doc\XmlAnyElementAttributes.uex' path='docs/doc[@for="XmlAnyElementAttributes.Insert"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlAnyElementAttribute attribute)
        {
            List.Insert(index, attribute);
        }

        /// <include file='doc\XmlAnyElementAttributes.uex' path='docs/doc[@for="XmlAnyElementAttributes.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlAnyElementAttribute attribute)
        {
            return List.IndexOf(attribute);
        }

        /// <include file='doc\XmlAnyElementAttributes.uex' path='docs/doc[@for="XmlAnyElementAttributes.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlAnyElementAttribute attribute)
        {
            return List.Contains(attribute);
        }

        /// <include file='doc\XmlAnyElementAttributes.uex' path='docs/doc[@for="XmlAnyElementAttributes.Remove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlAnyElementAttribute attribute)
        {
            List.Remove(attribute);
        }

        /// <include file='doc\XmlAnyElementAttributes.uex' path='docs/doc[@for="XmlAnyElementAttributes.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlAnyElementAttribute[] array, int index)
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
