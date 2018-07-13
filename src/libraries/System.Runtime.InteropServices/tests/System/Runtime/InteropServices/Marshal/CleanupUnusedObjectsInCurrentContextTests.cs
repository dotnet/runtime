// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class CleanupUnusedObjectsInCurrentContextTests
    {
        [Fact]
        public void CleanupUnusedObjectsInCurrentContext_InvokeSeveralTimes_Success()
        {
            Marshal.CleanupUnusedObjectsInCurrentContext();
            Assert.False(Marshal.AreComObjectsAvailableForCleanup());

            // Invoke twice to make sure things work when unused objects have already been
            // cleaned up and there is nothing to do.
            Marshal.CleanupUnusedObjectsInCurrentContext();
            Assert.False(Marshal.AreComObjectsAvailableForCleanup());
        }
    }
}
