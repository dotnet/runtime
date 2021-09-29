// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCompileToAssemblyTests : FileCleanupTestBase
    {
        [Fact]
        public void CompileToAssembly_InvalidArgs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("assemblyname", () => Regex.CompileToAssembly(null, null));
            AssertExtensions.Throws<ArgumentNullException>("assemblyname", () => Regex.CompileToAssembly(null, null, null));
            AssertExtensions.Throws<ArgumentNullException>("assemblyname", () => Regex.CompileToAssembly(null, null, null, null));

            AssertExtensions.Throws<ArgumentNullException>("regexinfos", () => Regex.CompileToAssembly(null, new AssemblyName("abcd")));
            AssertExtensions.Throws<ArgumentNullException>("regexinfos", () => Regex.CompileToAssembly(null, new AssemblyName("abcd"), null));
            AssertExtensions.Throws<ArgumentNullException>("regexinfos", () => Regex.CompileToAssembly(null, new AssemblyName("abcd"), null, null));

            AssertExtensions.Throws<ArgumentNullException>("regexinfos", "regexes", () => Regex.CompileToAssembly(new RegexCompilationInfo[] { null }, new AssemblyName("abcd")));
            AssertExtensions.Throws<ArgumentNullException>("regexinfos", "regexes", () => Regex.CompileToAssembly(new RegexCompilationInfo[] { new RegexCompilationInfo("abc", RegexOptions.None, "abc", "", true), null }, new AssemblyName("abcd")));
            AssertExtensions.Throws<ArgumentNullException>("regexinfos", "regexes", () => Regex.CompileToAssembly(new RegexCompilationInfo[] { null }, new AssemblyName("abcd"), new CustomAttributeBuilder[0]));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileToAssembly_PNSE()
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                Regex.CompileToAssembly(
                    new[] { new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "SomeNamespace", true) },
                    new AssemblyName("abcd")));

            Assert.Throws<PlatformNotSupportedException>(() =>
                Regex.CompileToAssembly(
                    new[] { new RegexCompilationInfo("abcd", RegexOptions.CultureInvariant, "abcd", "", true, TimeSpan.FromMinutes(1)) },
                    new AssemblyName("abcdWithTimeout")));

            Assert.Throws<PlatformNotSupportedException>(() =>
                Regex.CompileToAssembly(
                    new[] { new RegexCompilationInfo("(?<FirstTwoLetters>ab)cd", RegexOptions.None, "abcd", "", true, TimeSpan.FromMinutes(1)) },
                    new AssemblyName("abcdWithNamedCapture")));

            Assert.Throws<PlatformNotSupportedException>(() =>
                Regex.CompileToAssembly(
                    new[] { new RegexCompilationInfo(".*\\B(\\d+)(?<output>SUCCESS)\\B.*", RegexOptions.None, "withCaptures", "", true) },
                    new AssemblyName("withCaptures")));

            Assert.Throws<PlatformNotSupportedException>(() =>
                Regex.CompileToAssembly(
                    new[] { new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "", true) },
                    new AssemblyName("abcdWithCustomAttribute"),
                    new[] { new CustomAttributeBuilder(typeof(AssemblyCompanyAttribute).GetConstructor(new[] { typeof(string) }), new[] { "TestCompany" }) }));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileToAssembly_ResourceFile_PNSE()
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                Regex.CompileToAssembly(
                    new[] { new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "", true) },
                    new AssemblyName("abcdWithUnsupportedResourceFile"),
                    attributes: null,
                    "unsupportedResourceFile"));
        }
    }
}
