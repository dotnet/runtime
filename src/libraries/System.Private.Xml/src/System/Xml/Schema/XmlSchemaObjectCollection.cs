// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.Collections;
    using System.ComponentModel;
    using System.Xml.Serialization;

    public class XmlSchemaObjectCollection : CollectionBase
    {
        private XmlSchemaObject _parent;

        public XmlSchemaObjectCollection()
        {
        }

        public XmlSchemaObjectCollection(XmlSchemaObject parent)
        {
            _parent = parent;
        }

        public virtual XmlSchemaObject this[int index]
        {
            get { return (XmlSchemaObject)List[index]; }
            set { List[index] = value; }
        }

        public new XmlSchemaObjectEnumerator GetEnumerator()
        {
            return new XmlSchemaObjectEnumerator(InnerList.GetEnumerator());
        }

        public int Add(XmlSchemaObject item)
        {
            return List.Add(item);
        }

        public void Insert(int index, XmlSchemaObject item)
        {
            List.Insert(index, item);
        }

        public int IndexOf(XmlSchemaObject item)
        {
            return List.IndexOf(item);
        }

        public bool Contains(XmlSchemaObject item)
        {
            return List.Contains(item);
        }

        public void Remove(XmlSchemaObject item)
        {
            List.Remove(item);
        }

        public void CopyTo(XmlSchemaObject[] array, int index)
        {
            List.CopyTo(array, index);
        }

        protected override void OnInsert(int index, object item)
        {
            if (_parent != null)
            {
                _parent.OnAdd(this, item);
            }
        }

        protected override void OnSet(int index, object oldValue, object newValue)
        {
            if (_parent != null)
            {
                _parent.OnRemove(this, oldValue);
                _parent.OnAdd(this, newValue);
            }
        }

        protected override void OnClear()
        {
            if (_parent != null)
            {
                _parent.OnClear(this);
            }
        }

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

    public class XmlSchemaObjectEnumerator : IEnumerator
    {
        private IEnumerator _enumerator;

        internal XmlSchemaObjectEnumerator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public XmlSchemaObject Current
        {
            get { return (XmlSchemaObject)_enumerator.Current; }
        }

        void IEnumerator.Reset()
        {
            _enumerator.Reset();
        }

        bool IEnumerator.MoveNext()
        {
            return _enumerator.MoveNext();
        }

        object IEnumerator.Current
        {
            get { return _enumerator.Current; }
        }
    }
}
