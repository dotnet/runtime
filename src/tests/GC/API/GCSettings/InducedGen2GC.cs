// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using Xunit;

namespace GCLatencyTest
{
    public class InducedGen2GC
    {
        [Fact]
        public static void Test()
        {
            int _numCollections = GC.CollectionCount(2);
            GC.Collect();
            _numCollections = GC.CollectionCount(2) - _numCollections;
            Assert.True(_numCollections > 0);
        }
    }
}
