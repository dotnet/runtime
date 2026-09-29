// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using Xunit;

namespace GCLatencyTest
{
    public class InducedGen0GC
    {
        [Fact]
        public static void Test()
        {
            int _numCollections = GC.CollectionCount(0);
            GC.Collect(0);
            _numCollections = GC.CollectionCount(0) - _numCollections;
            Assert.True(_numCollections > 0);
        }
    }
}
