// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.TypeExtensions.Tests
{
    public class ModuleVersionIdTests
    {
        // This applies on all platforms. See S.R.TE.CoreCLR.Tests for more test cases that rely on
        // that rely platform-specific capabilities.
        [Fact]
        public void HasMvidAndGetMvidBehaveConsistently()
        {
            Module module = typeof(ModuleVersionIdTests).GetTypeInfo().Assembly.ManifestModule;

            if (module.HasModuleVersionId())
            {
                Assert.True(module.GetModuleVersionId() != default(Guid));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => module.GetModuleVersionId());
            }
        }
    }
}
