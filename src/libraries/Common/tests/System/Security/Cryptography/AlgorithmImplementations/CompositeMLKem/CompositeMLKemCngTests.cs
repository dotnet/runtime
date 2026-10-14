// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLKemCngTests_AllPlatforms
    {
        [Fact]
        public static void Constructor_NullKey()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AssertExtensions.Throws<ArgumentNullException>("key", static () => new CompositeMLKemCng(null));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => new CompositeMLKemCng(null));
            }
        }
    }
}
