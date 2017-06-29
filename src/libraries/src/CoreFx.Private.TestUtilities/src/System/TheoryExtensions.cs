// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System
{
    public static class TheoryExtensions
    {
        /// <summary>
        /// Converts an IEnumerable<typeparamref name="T"/> into an Xunit theory compatible enumerable.
        /// </summary>
        public static TheoryData ToTheoryData<T>(this IEnumerable<T> data)
        {
            // Returning TheoryData rather than IEnumerable<object> directly should
            // encourage discover and usage of TheoryData<T1, ..> classes for more
            // complicated theories. Slightly easier to type as well.
            return new TheoryDataAdapter(data.Select(d => new object[] { d }));
        }

        private class TheoryDataAdapter : TheoryData, IEnumerable<object[]>
        {
            private IEnumerable<object[]> _data;

            public TheoryDataAdapter(IEnumerable<object[]> data)
            {
                _data = data;
            }

            public new IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
