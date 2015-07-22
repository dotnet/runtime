// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq.Tests.Helpers
{
    public class TestCollection<T> : ICollection<T>
    {
        public T[] Items = new T[0];
        public int CountTouched = 0;
        public int CopyToTouched = 0;
        public TestCollection(T[] items) { Items = items; }

        public virtual int Count { get { CountTouched++; return Items.Length; } }
        public bool IsReadOnly { get { return false; } }
        public void Add(T item) { throw new NotImplementedException(); }
        public void Clear() { throw new NotImplementedException(); }
        public bool Contains(T item) { return Items.Contains(item); }
        public bool Remove(T item) { throw new NotImplementedException(); }
        public void CopyTo(T[] array, int arrayIndex) { CopyToTouched++; Items.CopyTo(array, arrayIndex); }
        public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)Items).GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return Items.GetEnumerator(); }
    }
}
