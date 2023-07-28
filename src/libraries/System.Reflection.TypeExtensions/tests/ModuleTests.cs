// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class ModuleTests
    {
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

        // This calls Assembly.Load, but xUnit turn is into a LoadFrom because TinyAssembly is just a Content item in the project.
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsAssemblyLoadingSupported))]
        public void GetModuleVersionId_KnownAssembly_ReturnsExpected()
        {
            Module module = Assembly.Load(new AssemblyName("TinyAssembly")).ManifestModule;
            Assert.True(module.HasModuleVersionId());
            Assert.Equal(Guid.Parse("{06BB2468-908C-48CF-ADE9-DB6DE4614004}"), module.GetModuleVersionId());
        }
    }
}
