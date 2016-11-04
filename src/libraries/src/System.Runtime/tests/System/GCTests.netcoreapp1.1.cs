// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static partial class GCTests
    {
        [Theory]
        [InlineData(1000)]
        [InlineData(100000)]
        public static void GetAllocatedBytesForCurrentThread(int size)
        {
            long start = GC.GetAllocatedBytesForCurrentThread();

            GC.KeepAlive(new String('a', size));

            long end = GC.GetAllocatedBytesForCurrentThread();

            Assert.True((end - start) > size, $"Allocated too little: start: {start} end: {end} size: {size}");
            Assert.True((end - start) < 5 * size, $"Allocated too much: start: {start} end: {end} size: {size}");
        }
    }
}
