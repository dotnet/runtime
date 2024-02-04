// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
    public class RegexCompileToAssemblyTests : FileCleanupTestBase
    {
        public static bool IsDebug => typeof(Regex).Assembly.GetCustomAttributes(false).OfType<DebuggableAttribute>().Any(da => da.IsJITTrackingEnabled);
        public static bool IsRelease => !IsDebug;
        public static bool IsDebugAndRemoteExecutorSupported => IsDebug && RemoteExecutor.IsSupported;

        [ConditionalFact(nameof(IsRelease))]
        public void CompileToAssembly_PNSE()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(null, null));
            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(null, null, null));
            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(null, null, null, null));

            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(
                [new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "SomeNamespace", true)],
                new AssemblyName("abcd")));

            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(
                [new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "SomeNamespace", true)],
                new AssemblyName("abcd"),
                [new CustomAttributeBuilder(typeof(AssemblyCompanyAttribute).GetConstructor([typeof(string)]), new[] { "TestCompany" })]));

            Assert.Throws<PlatformNotSupportedException>(() => Regex.CompileToAssembly(
                [new RegexCompilationInfo("abcd", RegexOptions.None, "abcd", "SomeNamespace", true)],
                new AssemblyName("abcd"),
                [new CustomAttributeBuilder(typeof(AssemblyCompanyAttribute).GetConstructor([typeof(string)]), new[] { "TestCompany" })],
                "resourceFile"));
        }

        [ConditionalFact(nameof(IsDebugAndRemoteExecutorSupported))]
        public void CompileToAssembly_SimpleUseInDebug()
        {
            RemoteExecutor.Invoke(() =>
            {
                (RegexCompilationInfo rci, string validInput, string invalidInput)[] regexes =
                [
                    (new RegexCompilationInfo("abcd", RegexOptions.None, "Type1", "Namespace1", ispublic: true), "123abcd123", "123abed123"),
                    (new RegexCompilationInfo("(a|b|cde)+", RegexOptions.None, "Type2", "Namespace2.Sub", ispublic: true), "abcde", "cd"),
                ];

                string assemblyName = Path.GetRandomFileName();

                string cwd = Environment.CurrentDirectory;
                Environment.CurrentDirectory = TestDirectory;
                try
                {
                    Regex.CompileToAssembly(regexes.Select(r => r.rci).ToArray(), new AssemblyName(assemblyName));
                }
                finally
                {
                    Environment.CurrentDirectory = cwd;
                }

                string assemblyPath = Path.Combine(TestDirectory, assemblyName + ".dll");
                Assert.True(File.Exists(assemblyPath));

                // Uncomment to save the assembly to the desktop for inspection:
                // File.Copy(assemblyPath, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Path.GetFileName(assemblyPath)));
            }).Dispose();
        }
    }
}
