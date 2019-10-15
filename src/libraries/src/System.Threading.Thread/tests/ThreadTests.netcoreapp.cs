// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Threads.Tests
{
    public static partial class ThreadTests
    {
        [Fact]
        public static void GetCurrentProcessorId()
        {
            Assert.True(Thread.GetCurrentProcessorId() >= 0);
        }
    }
}
