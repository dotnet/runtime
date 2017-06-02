// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class ModuleTests
    {
        [Fact]
        public void GetModuleVersionId_KnownAssembly_ReturnsExpected()
        {
            Module module = Assembly.Load(new AssemblyName("TinyAssembly")).ManifestModule;
            Assert.True(module.HasModuleVersionId());
            Assert.Equal(Guid.Parse("{06BB2468-908C-48CF-ADE9-DB6DE4614004}"), module.GetModuleVersionId());
        }
    }
}
