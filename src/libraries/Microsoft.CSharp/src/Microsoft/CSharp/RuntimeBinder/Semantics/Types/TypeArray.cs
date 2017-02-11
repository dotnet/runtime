// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////
    // Encapsulates a type list, including its size and metadata token.

    internal sealed class TypeArray
    {
        private readonly CType[] _items;

        public TypeArray(CType[] types)
        {
            _items = types;
            if (_items == null)
            {
                _items = Array.Empty<CType>();
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
