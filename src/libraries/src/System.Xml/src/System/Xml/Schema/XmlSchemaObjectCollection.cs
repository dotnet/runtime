// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaObjectCollection : CollectionBase
    {
        private XmlSchemaObject _parent;

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.XmlSchemaObjectCollection"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaObjectCollection()
        {
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.XmlSchemaObjectCollection1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaObjectCollection(XmlSchemaObject parent)
        {
            _parent = parent;
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.this"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public virtual XmlSchemaObject this[int index]
        {
            get { return (XmlSchemaObject)List[index]; }
            set { List[index] = value; }
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.GetEnumerator"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public new XmlSchemaObjectEnumerator GetEnumerator()
        {
            return new XmlSchemaObjectEnumerator(InnerList.GetEnumerator());
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.Add"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(XmlSchemaObject item)
        {
            return List.Add(item);
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.Insert"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, XmlSchemaObject item)
        {
            List.Insert(index, item);
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.IndexOf"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(XmlSchemaObject item)
        {
            return List.IndexOf(item);
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(XmlSchemaObject item)
        {
            return List.Contains(item);
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.Remove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(XmlSchemaObject item)
        {
            List.Remove(item);
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.CopyTo"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(XmlSchemaObject[] array, int index)
        {
            List.CopyTo(array, index);
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.OnInsert"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnInsert(int index, object item)
        {
            if (_parent != null)
            {
                _parent.OnAdd(this, item);
            }
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.OnSet"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnSet(int index, object oldValue, object newValue)
        {
            if (_parent != null)
            {
                _parent.OnRemove(this, oldValue);
                _parent.OnAdd(this, newValue);
            }
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.OnClear"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnClear()
        {
            if (_parent != null)
            {
                _parent.OnClear(this);
            }
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectCollection.OnRemove"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected override void OnRemove(int index, object item)
        {
            if (_parent != null)
            {
                _parent.OnRemove(this, item);
            }
        }

        internal XmlSchemaObjectCollection Clone()
        {
            XmlSchemaObjectCollection coll = new XmlSchemaObjectCollection();
            coll.Add(this);
            return coll;
        }

        private void Add(XmlSchemaObjectCollection collToAdd)
        {
            this.InnerList.InsertRange(0, collToAdd);
        }
    }

    /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectEnumerator"]/*' />
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class XmlSchemaObjectEnumerator : IEnumerator
    {
        private IEnumerator _enumerator;

        internal XmlSchemaObjectEnumerator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectEnumerator.Reset"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Reset()
        {
            _enumerator.Reset();
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectEnumerator.MoveNext"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectEnumerator.Current"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public XmlSchemaObject Current
        {
            get { return (XmlSchemaObject)_enumerator.Current; }
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectEnumerator.IEnumerator.Reset"]/*' />
        /// <internalonly/>
        void IEnumerator.Reset()
        {
            _enumerator.Reset();
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectEnumerator.IEnumerator.MoveNext"]/*' />
        /// <internalonly/>
        bool IEnumerator.MoveNext()
        {
            return _enumerator.MoveNext();
        }

        /// <include file='doc\XmlSchemaObjectCollection.uex' path='docs/doc[@for="XmlSchemaObjectEnumerator.IEnumerator.Current"]/*' />
        /// <internalonly/>
        object IEnumerator.Current
        {
            get { return _enumerator.Current; }
        }
    }
}
