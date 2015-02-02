// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////
    // Encapsulates a type list, including its size and metadata token.

    internal class TypeArray
    {
        private CType[] _items;

        public TypeArray(CType[] types)
        {
            _items = types;
            if (_items == null)
            {
                _items = new CType[0];
            }
        }

        public int Size { get { return _items.Length; } }
        public int size { get { return Size; } }

        public bool HasErrors() { return false; }
        public CType Item(int i) { return _items[i]; }
        public TypeParameterType ItemAsTypeParameterType(int i) { return _items[i].AsTypeParameterType(); }

        public CType[] ToArray() { return _items.ToArray(); }

        [System.Runtime.CompilerServices.IndexerName("EyeTim")]
        public CType this[int i]
        {
            get { return _items[i]; }
        }

        public int Count
        {
            get { return _items.Length; }
        }

#if DEBUG
        public void AssertValid()
        {
            Debug.Assert(size >= 0);
            for (int i = 0; i < size; i++)
            {
                Debug.Assert(_items[i] != null);
            }
        }
#endif

        public void CopyItems(int i, int c, CType[] dest)
        {
            for (int j = 0; j < c; ++j)
            {
                dest[j] = _items[i + j];
            }
        }
    }
}
