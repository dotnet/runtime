// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public partial class EmptyEnumerableTest
    {
        [Fact]
        public void CachedEnumerator()
        {
            var emptyEnumerable1 = Enumerable.Empty<object>();
            var emptyEnumerable2 = (IList<object>)emptyEnumerable1;

            Assert.Same(emptyEnumerable1.GetEnumerator(), emptyEnumerable2.GetEnumerator());
        }
    }
}