// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Diagnostics;
    using System;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Globalization;

    /// <devdoc>
    ///    <para>
    ///       Manages a collection of <see cref='System.CodeDom.CodeNamespaceImport'/> objects.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeNamespaceImportCollection : IList
    {
        private ArrayList _data = new ArrayList();
        private Hashtable _keys = new Hashtable(StringComparer.OrdinalIgnoreCase);

        /// <devdoc>
        ///    <para>
        ///       Indexer method that provides collection access.
        ///    </para>
        /// </devdoc>
        public CodeNamespaceImport this[int index]
        {
            get
            {
                return ((CodeNamespaceImport)_data[index]);
            }
            set
            {
                _data[index] = value;
                SyncKeys();
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the number of namespaces in the collection.
        ///    </para>
        /// </devdoc>
        public int Count
        {
            get
            {
                return _data.Count;
            }
        }

        /// <internalonly/>
        bool IList.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <internalonly/>
        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }


        /// <devdoc>
        ///    <para>
        ///       Adds a namespace import to the collection.
        ///    </para>
        /// </devdoc>
        public void Add(CodeNamespaceImport value)
        {
            if (!_keys.ContainsKey(value.Namespace))
            {
                _keys[value.Namespace] = value;
                _data.Add(value);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Adds a set of <see cref='System.CodeDom.CodeNamespaceImport'/> objects to the collection.
        ///    </para>
        /// </devdoc>
        public void AddRange(CodeNamespaceImport[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            foreach (CodeNamespaceImport c in value)
            {
                Add(c);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Clears the collection of members.
        ///    </para>
        /// </devdoc>
        public void Clear()
        {
            _data.Clear();
            _keys.Clear();
        }

        /// <devdoc>
        ///    <para>
        ///    Makes the collection of keys synchronised with the data.
        ///    </para>
        /// </devdoc>
        private void SyncKeys()
        {
            _keys = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (CodeNamespaceImport c in this)
            {
                _keys[c.Namespace] = c;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets an enumerator that enumerates the collection members.
        ///    </para>
        /// </devdoc>
        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        /// <internalonly/>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = (CodeNamespaceImport)value;
                SyncKeys();
            }
        }

        /// <internalonly/>
        int ICollection.Count
        {
            get
            {
                return Count;
            }
        }

        /// <internalonly/>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <internalonly/>
        object ICollection.SyncRoot
        {
            get
            {
                return null;
            }
        }

        /// <internalonly/>
        void ICollection.CopyTo(Array array, int index)
        {
            _data.CopyTo(array, index);
        }

        /// <internalonly/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <internalonly/>
        int IList.Add(object value)
        {
            return _data.Add((CodeNamespaceImport)value);
        }

        /// <internalonly/>
        void IList.Clear()
        {
            Clear();
        }

        /// <internalonly/>
        bool IList.Contains(object value)
        {
            return _data.Contains(value);
        }

        /// <internalonly/>
        int IList.IndexOf(object value)
        {
            return _data.IndexOf((CodeNamespaceImport)value);
        }

        /// <internalonly/>
        void IList.Insert(int index, object value)
        {
            _data.Insert(index, (CodeNamespaceImport)value);
            SyncKeys();
        }

        /// <internalonly/>
        void IList.Remove(object value)
        {
            _data.Remove((CodeNamespaceImport)value);
            SyncKeys();
        }

        /// <internalonly/>
        void IList.RemoveAt(int index)
        {
            _data.RemoveAt(index);
            SyncKeys();
        }
    }
}


