// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
    public class RegexCompileToAssemblyTests : FileCleanupTestBase
    {
        [Fact]
        public void CompileToAssembly_PNSE()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(null, null));
            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(null, null, null));
            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(null, null, null, null));

            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(
                new[] { new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "SomeNamespace", true) },
                new AssemblyName("abcd")));

            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(
                new[] { new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "SomeNamespace", true) },
                new AssemblyName("abcd"),
                new[] { new CustomAttributeBuilder(typeof(AssemblyCompanyAttribute).GetConstructor(new[] { typeof(string) }), new[] { "TestCompany" }) }));

            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(
                new[] { new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "SomeNamespace", true) },
                new AssemblyName("abcd"),
                new[] { new CustomAttributeBuilder(typeof(AssemblyCompanyAttribute).GetConstructor(new[] { typeof(string) }), new[] { "TestCompany" }) },
                "resourceFile"));
        }
    }
}
