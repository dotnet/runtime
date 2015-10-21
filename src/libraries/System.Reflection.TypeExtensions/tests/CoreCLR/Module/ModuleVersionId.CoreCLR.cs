// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Reflection.TypeExtensions.Tests
{
    public class ModuleVersionIdTests
    {
        [Fact]
        public void KnownMvid()
        {
            Module module = Assembly.Load(new AssemblyName("TinyAssembly")).ManifestModule;
            Assert.True(module.HasModuleVersionId());
            Assert.Equal(Guid.Parse("{06BB2468-908C-48CF-ADE9-DB6DE4614004}"), module.GetModuleVersionId());
        }
    }
}
