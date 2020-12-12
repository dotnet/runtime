// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class PriorityQueue_NonGeneric_Tests : TestBase
    {
        [Fact]
        public void ConstructorThrows()
        {
            Assert.Throws<NotImplementedException>(() => new PriorityQueue<string, string>());
        }
    }
}
