// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
