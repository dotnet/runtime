// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
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

        [ConditionalFact(nameof(IsProcessElevated))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TestModuleLongPath()
        {
            // // Metadata version: v4.0.30319
            // .assembly Test
            // {
            //   .ver 0:0:0:0
            // }
            // .module Test.dll
            // // MVID: {48E60D10-353B-45DC-AA09-3A1E6B0FD382}
            // .imagebase 0x00400000
            // .file alignment 0x00000200
            // .stackreserve 0x00100000
            // .subsystem 0x0003       // WINDOWS_CUI
            // .corflags 0x00000001    //  ILONLY
            byte[] assemblyImage = Convert.FromBase64String(
                "TVqQAAMAAAAEAAAA//8AALgAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAAA4fug4AtAnNIbgBTM0hVGhpcyBwcm9ncmFt" +
                "IGNhbm5vdCBiZSBydW4gaW4gRE9TIG1vZGUuDQ0KJAAAAAAAAABQRQAATAECAJyj7l8AAAAAAAAAAOAAAiELAQsAAAIAAAACAAAAAAAAfiEAAAAgAAAAQAAA" +
                "AABAAAAgAAAAAgAABAAAAAAAAAAEAAAAAAAAAABgAAAAAgAAAAAAAAMAQIUAABAAABAAAAAAEAAAEAAAAAAAABAAAAAAAAAAAAAAADAhAABLAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAEAAAAwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAIAAACAAAAAAAAAAAAAAA" +
                "CCAAAEgAAAAAAAAAAAAAAC50ZXh0AAAAhAEAAAAgAAAAAgAAAAIAAAAAAAAAAAAAAAAAACAAAGAucmVsb2MAAAwAAAAAQAAAAAIAAAAEAAAAAAAAAAAAAAAA" +
                "AABAAABCAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABgIQAAAAAAAEgAAAACAAUAUCAAAOAAAAABAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEJTSkIBAAEAAAAAAAwAAAB2NC4wLjMwMzE5AAAAAAQAXAAAAFQA" +
                "AAAjfgAAsAAAABgAAAAjU3RyaW5ncwAAAADIAAAACAAAACNVUwDQAAAAEAAAACNHVUlEAAAAAAAAAAIAAAEFAAAAAQAAAAD6JTMAFgAAAQAAAAEAAAABAAAA" +
                "AAAKAAEAAAAAAAAAAAABAAAAAAABAAEAAAAAAAAAAAAAAAAAAAAAAAAAEwAAAAAAADxNb2R1bGU+AFRlc3QuZGxsAFRlc3QAAAMgAAAAAAAQDeZIOzXcRaoJ" +
                "Oh5rD9OCWCEAAAAAAAAAAAAAbiEAAAAgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAGAhAAAAAAAAAABfQ29yRGxsTWFpbgBtc2NvcmVlLmRsbAAAAAAA/yUAIEAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAMAAAAgDEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                "AAAAAAAA"
            );

            string libraryDirectory = GetTestFilePath();
            Directory.CreateDirectory(libraryDirectory);
            string libraryPath = Path.Combine(libraryDirectory, new string('_', 250) + ".dll");
            Assert.True(libraryPath.Length > 260);
            File.WriteAllBytes(libraryPath, assemblyImage);

            IntPtr library = NativeLibrary.Load(libraryPath);
            Assert.True(library != IntPtr.Zero);
            try
            {
                Assert.Contains(Process.GetCurrentProcess().Modules.Cast<ProcessModule>(), module => module.FileName == libraryPath);
            }
            finally
            {
                NativeLibrary.Free(library);
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
    }
}
