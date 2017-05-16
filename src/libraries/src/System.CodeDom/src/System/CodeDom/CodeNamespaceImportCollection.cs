// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.CodeDom
{
    public class CodeNamespaceImportCollection : IList
    {
        private readonly ArrayList _data = new ArrayList(); // not List<CodeNamespaceImport> to provide desktop-consistent semantics for CopyTo
        private readonly Dictionary<string, CodeNamespaceImport> _keys = new Dictionary<string, CodeNamespaceImport>(StringComparer.OrdinalIgnoreCase);

        public CodeNamespaceImport this[int index]
        {
            get { return (CodeNamespaceImport)_data[index]; }
            set
            {
                _data[index] = value;
                SyncKeys();
            }
        }

        public int Count => _data.Count;

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        public void Add(CodeNamespaceImport value)
        {
            if (!_keys.ContainsKey(value.Namespace))
            {
                _keys[value.Namespace] = value;
                _data.Add(value);
            }
        }

        public void AddRange(CodeNamespaceImport[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            foreach (CodeNamespaceImport c in value)
            {
                Add(c);
            }
        }

        public void Clear()
        {
            _data.Clear();
            _keys.Clear();
        }

        private void SyncKeys()
        {
            _keys.Clear();
            foreach (CodeNamespaceImport c in _data)
            {
                _keys[c.Namespace] = c;
            }
        }

        public IEnumerator GetEnumerator() => _data.GetEnumerator();

        object IList.this[int index]
        {
            get { return this[index]; }
            set
            {
                this[index] = (CodeNamespaceImport)value;
                SyncKeys();
            }
        }

        int ICollection.Count => Count;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => null;

        void ICollection.CopyTo(Array array, int index) => _data.CopyTo(array, index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IList.Add(object value) => _data.Add((CodeNamespaceImport)value);

        void IList.Clear() => Clear();

        bool IList.Contains(object value) => _data.Contains(value);

        int IList.IndexOf(object value) => _data.IndexOf((CodeNamespaceImport)value);

        void IList.Insert(int index, object value)
        {
            _data.Insert(index, (CodeNamespaceImport)value);
            SyncKeys();
        }

        void IList.Remove(object value)
        {
            _data.Remove((CodeNamespaceImport)value);
            SyncKeys();
        }

        void IList.RemoveAt(int index)
        {
            _data.RemoveAt(index);
            SyncKeys();
        }
    }
}
