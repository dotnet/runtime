// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessModuleTests : ProcessTestBase
    {
        [Fact]
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
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
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "libproc is not supported on iOS/tvOS")]
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

            // Very rarely, this call will fail with ERROR_PARTIAL_COPY; it only happened
            // so far on this particular test, the only one that creates a new process.
            // Assumption is that we need to give a little extra time.
            ProcessModuleCollection modulesCollection = null;
            RetryHelper.Execute(() =>
            {
                modulesCollection = process.Modules;
            }, maxAttempts: 5, backoffFunc: null, retryWhen: e => e.GetType() == typeof(Win32Exception));

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
    }
}
