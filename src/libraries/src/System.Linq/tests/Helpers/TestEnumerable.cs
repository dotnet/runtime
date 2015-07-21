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
    public class TestEnumerable<T> : IEnumerable<T>
    {
        public T[] Items = new T[0];
        public TestEnumerable(T[] items) { Items = items; }

        public IEnumerator<T> GetEnumerator() { return ((IEnumerable<T>)Items).GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return Items.GetEnumerator(); }
    }
}
