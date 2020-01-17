// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexCompileToAssemblyTests
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
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileToAssembly_PNSE()
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                Regex.CompileToAssembly(
                    new[] { new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "", true) },
                    new AssemblyName("abcd")));
        }
    }
}
