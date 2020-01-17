// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Reflection;
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

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void CompileToAssembly_CanRunGeneratedRegex()
        {
            (RegexCompilationInfo rci, string validInput, string invalidInput)[] regexes = new[]
            {
                (new RegexCompilationInfo("abcd", RegexOptions.None, "Namespace1", "Type1", ispublic: true), "123abcd123", "123abed123"),
                (new RegexCompilationInfo("(a|b|cde)+", RegexOptions.None, "Namespace2.Sub", "Type2", ispublic: true), "abcde", "cd"),
            };

            string assemblyName = Path.GetRandomFileName();

            string cwd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = TestDirectory;
            try
            {
                Regex.CompileToAssembly(
                    regexes.Select(r => r.rci).ToArray(),
                    new AssemblyName(assemblyName));
            }
            finally
            {
                Environment.CurrentDirectory = cwd;
            }

            Assembly a = Assembly.LoadFrom(Path.Combine(TestDirectory, assemblyName + ".dll"));
            foreach ((RegexCompilationInfo rci, string validInput, string invalidInput) in regexes)
            {
                Regex r = (Regex)Activator.CreateInstance(a.GetType($"{rci.Namespace}.{rci.Name}"));
                Assert.True(r.IsMatch(validInput));
                Assert.False(r.IsMatch(invalidInput));
            }
        }
    }
}
