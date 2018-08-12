// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class AreComObjectsAvailableForCleanupTests
    {
        [Fact]
        public void AreComObjectsAvailableForCleanup_NothingToCleanup_ReturnsFalse()
        {
            Assert.False(Marshal.AreComObjectsAvailableForCleanup());
        }
    }
}
