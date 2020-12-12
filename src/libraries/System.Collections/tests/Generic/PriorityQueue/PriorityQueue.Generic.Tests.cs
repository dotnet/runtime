// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public abstract class PriorityQueue_Generic_Tests<TElement, TPriority> : TestBase<(TElement, TPriority)>
    {
        [Fact]
        public void ConstructorThrows()
        {
            Assert.Throws<NotImplementedException>(() => new PriorityQueue<TElement, TPriority>());
        }
    }
}
