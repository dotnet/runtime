// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class ModuleTests
    {
        // This applies on all platforms. See System.Reflection.TypeExtensions.CoreCLR.Tests for more
        // test cases that rely on platform-specific capabilities.
        [Fact]
        public void GetModuleVersionId_HasModuleVersionId_BehaveConsistently()
        {
            Module module = typeof(ModuleTests).GetTypeInfo().Assembly.ManifestModule;

            if (module.HasModuleVersionId())
            {
                Assert.NotEqual(Guid.Empty, module.GetModuleVersionId());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => module.GetModuleVersionId());
            }
        }
    }
}
