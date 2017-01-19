// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Context
{
    public class CustomReflectionContextTests
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void InstantiateContext_Throws()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new DerivedContext());
        }

        private class DerivedContext : CustomReflectionContext { }
    }
}
