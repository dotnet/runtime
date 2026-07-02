// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public sealed class CompositeMLDsaCngTests_AllPlatforms
    {
        [Fact]
        public void CompositeMLDsaCng_Ctor_ArgValidation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AssertExtensions.Throws<ArgumentNullException>("key", static () => new CompositeMLDsaCng(null));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() => new CompositeMLDsaCng(null));
            }
        }
    }
}
