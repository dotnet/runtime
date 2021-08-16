// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessModuleTests : ProcessTestBase
    {
        [Fact]
        public void TestModuleProperties()
        {
            ProcessModuleCollection modules = Process.GetCurrentProcess().Modules;
            Assert.InRange(modules.Count, 1, int.MaxValue);

            foreach (ProcessModule module in modules)
            {
                Assert.NotNull(module);

                Assert.NotNull(module.FileName);
                Assert.NotEmpty(module.FileName);

                Assert.InRange(module.BaseAddress.ToInt64(), long.MinValue, long.MaxValue);
                Assert.InRange(module.EntryPointAddress.ToInt64(), long.MinValue, long.MaxValue);
                Assert.InRange(module.ModuleMemorySize, 0, long.MaxValue);
            }
        }

        [Fact]
        public void Modules_Get_ContainsHostFileName()
        {
            ProcessModuleCollection modules = Process.GetCurrentProcess().Modules;
            Assert.Contains(modules.Cast<ProcessModule>(), m => m.FileName.Contains(RemoteExecutor.HostRunnerName));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)] // OSX only includes the main module
        public void TestModulesContainsUnixNativeLibs()
        {
            ProcessModuleCollection modules = Process.GetCurrentProcess().Modules;
            Assert.Contains(modules.Cast<ProcessModule>(), m => m.FileName.Contains("libcoreclr"));
            Assert.Contains(modules.Cast<ProcessModule>(), m => m.FileName.Contains("System.Native"));
        }

        [Fact]
        public void Modules_GetMultipleTimes_ReturnsSameInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Assert.Same(currentProcess.Modules, currentProcess.Modules);
        }

        [Fact]
        public void Modules_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.Modules);
        }

        [Fact]
        public void ModuleCollectionSubClass_DefaultConstructor_Success()
        {
            Assert.Empty(new ModuleCollectionSubClass());
        }

        public class ModuleCollectionSubClass : ProcessModuleCollection
        {
            public ModuleCollectionSubClass() : base() { }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ModulesAreDisposedWhenProcessIsDisposed()
        {
            Process process = CreateDefaultProcess();

            ProcessModuleCollection modulesCollection = process.Modules;
            int expectedCount = 0;
            int disposedCount = 0;
            foreach (ProcessModule processModule in modulesCollection)
            {
                expectedCount += 1;
                processModule.Disposed += (_, __) => disposedCount += 1;
            }

            KillWait(process);
            Assert.Equal(0, disposedCount);

            process.Dispose();
            Assert.Equal(expectedCount, disposedCount);
        }

        public static bool Is_LongModuleFileNamesAreSupported_TestEnabled
            => OperatingSystem.IsWindows() // it's specific to Windows
            && PathFeatures.AreAllLongPathsAvailable() // we want to test long paths
            && !PlatformDetection.IsMonoRuntime // Assembly.LoadFile used the way this test is implemented fails on Mono
            && RuntimeInformation.ProcessArchitecture != Architecture.X86; // for some reason it's flaky on x86

        [ConditionalFact(typeof(ProcessModuleTests), nameof(Is_LongModuleFileNamesAreSupported_TestEnabled))]
        public void LongModuleFileNamesAreSupported()
        {
            // To be able to test Long Path support for ProcessModule.FileName we need a .dll that has a path > 260 chars.
            // Since Long Paths support can be disabled (see the ConditionalFact attribute usage above),
            // we just copy "LongName.dll" from bin to a temp directory with a long name and load it from there.
            // Loading from new path is possible because the type exposed by the assembly is not referenced in any explicit way.
            const string libraryName = "LongPath.dll";
            const int minPathLength = 261;

            string testBinPath = Path.GetDirectoryName(typeof(ProcessModuleTests).Assembly.Location);
            string libraryToCopy = Path.Combine(testBinPath, libraryName);
            Assert.True(File.Exists(libraryToCopy), $"{libraryName} was not present in bin folder '{testBinPath}'");

            string directoryWithLongName = Path.Combine(TestDirectory, new string('a', Math.Max(1, minPathLength - TestDirectory.Length)));
            Directory.CreateDirectory(directoryWithLongName);

            string longNamePath = Path.Combine(directoryWithLongName, libraryName);
            Assert.True(longNamePath.Length > minPathLength);

            File.Copy(libraryToCopy, longNamePath);
            Assert.True(File.Exists(longNamePath));

            Assembly loaded = Assembly.LoadFile(longNamePath);
            Assert.Equal(longNamePath, loaded.Location);

            string[] modulePaths = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Select(module => module.FileName).ToArray();
            Assert.Contains(longNamePath, modulePaths);
        }
    }
}
