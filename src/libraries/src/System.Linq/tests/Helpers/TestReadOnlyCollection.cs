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
    public class TestReadOnlyCollection<T> : IReadOnlyCollection<T>
    {
        public T[] Items = new T[0];
        public int CountTouched = 0;
        public TestReadOnlyCollection(T[] items) { Items = items; }

        public int Count { get { CountTouched++; return Items.Length; } }
        public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)Items).GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return Items.GetEnumerator(); }
    }
}
