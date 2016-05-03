// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameObjectCollectionBaseSyncRootTests
    {
        [Fact]
        public void SyncRoot()
        {
            ICollection nameObjectCollection1 = new MyNameObjectCollection();
            ICollection nameObjectCollection2 = new MyNameObjectCollection();

            Assert.False(nameObjectCollection1.IsSynchronized);

            Assert.Same(nameObjectCollection1.SyncRoot, nameObjectCollection1.SyncRoot);
            Assert.NotEqual(nameObjectCollection1.SyncRoot, nameObjectCollection2.SyncRoot);
        }
    }
}
