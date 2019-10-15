// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Threads.Tests
{
    public static class ThreadExceptionEventArgsTests
    {
        [Fact]
        public static void ConstructorTest()
        {
            var e = new ThreadExceptionEventArgs(null);
            Assert.Null(e.Exception);

            var ex = new Exception();
            e = new ThreadExceptionEventArgs(ex);
            Assert.Same(ex, e.Exception);
        }
    }
}
