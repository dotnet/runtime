// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Tests
{
    public class DirectoryVirtualListViewContextTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var context = new DirectoryVirtualListViewContext();
            DirectoryVirtualListViewContext copy = context.Copy();
            Assert.NotNull(copy);
            Assert.NotSame(copy, context);
        }
    }
}
