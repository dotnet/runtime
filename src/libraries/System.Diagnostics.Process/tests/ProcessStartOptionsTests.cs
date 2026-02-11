// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessStartOptionsTests : ProcessTestBase
    {
        [Fact]
        public void TestConstructor_NullFileName_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ProcessStartOptions(null));
        }

        [Fact]
        public void TestConstructor_EmptyFileName_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ProcessStartOptions(string.Empty));
        }

        [Fact]
        public void TestConstructor_NonExistentFile_Throws()
        {
            string nonExistentFile = "ThisFileDoesNotExist_" + Guid.NewGuid().ToString();
            Assert.Throws<FileNotFoundException>(() => new ProcessStartOptions(nonExistentFile));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestConstructor_ResolvesCmdOnWindows()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            ProcessStartOptions options = new ProcessStartOptions("cmd");
            Assert.Contains("cmd.exe", options.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(options.FileName));
        }

        [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.FreeBSD | TestPlatforms.OSX)]
        [Fact]
        public void TestConstructor_ResolvesShOnUnix()
        {
            ProcessStartOptions options = new ProcessStartOptions("sh");
            Assert.Contains("sh", options.FileName);
            Assert.True(File.Exists(options.FileName));
        }

        [Fact]
        public void TestConstructor_WithAbsolutePath()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                ProcessStartOptions options = new ProcessStartOptions(tempFile);
                Assert.Equal(tempFile, options.FileName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void TestArguments_LazyInitialization()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            IList<string> args = options.Arguments;
            Assert.NotNull(args);
            Assert.Empty(args);
        }

        [Fact]
        public void TestArguments_CanAddAndModify()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            options.Arguments.Add("arg1");
            options.Arguments.Add("arg2");
            Assert.Equal(2, options.Arguments.Count);
            Assert.Equal("arg1", options.Arguments[0]);
            Assert.Equal("arg2", options.Arguments[1]);

            options.Arguments = new List<string> { "newArg" };
            Assert.Single(options.Arguments);
            Assert.Equal("newArg", options.Arguments[0]);
        }

        [Fact]
        public void TestEnvironment_LazyInitialization()
        {
            if (PlatformDetection.IsiOS || PlatformDetection.IstvOS || PlatformDetection.IsMacCatalyst)
            {
                // Whole list of environment variables can no longer be accessed on non-OSX apple platforms
                return;
            }

            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            IDictionary<string, string?> env = options.Environment;
            Assert.NotNull(env);
            Assert.NotEmpty(env);
        }

        [Fact]
        public void TestEnvironment_CanAddAndModify()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            IDictionary<string, string?> env = options.Environment;
            
            int originalCount = env.Count;
            env["TestKey1"] = "TestValue1";
            env["TestKey2"] = "TestValue2";
            Assert.Equal(originalCount + 2, env.Count);
            Assert.Equal("TestValue1", env["TestKey1"]);
            Assert.Equal("TestValue2", env["TestKey2"]);

            env.Remove("TestKey1");
            Assert.Equal(originalCount + 1, env.Count);
            Assert.False(env.ContainsKey("TestKey1"));
        }

        [Fact]
        public void TestEnvironment_CaseInsensitivityOnWindows()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            IDictionary<string, string?> env = options.Environment;
            
            env["TestKey"] = "TestValue";
            
            if (OperatingSystem.IsWindows())
            {
                Assert.True(env.ContainsKey("testkey"));
                Assert.Equal("TestValue", env["TESTKEY"]);
            }
            else
            {
                Assert.False(env.ContainsKey("testkey"));
            }
        }

        [Fact]
        public void TestInheritedHandles_LazyInitialization()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            IList<SafeHandle> handles = options.InheritedHandles;
            Assert.NotNull(handles);
            Assert.Empty(handles);
        }

        [Fact]
        public void TestInheritedHandles_CanSet()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            List<SafeHandle> newHandles = new List<SafeHandle>();
            options.InheritedHandles = newHandles;
            Assert.Same(newHandles, options.InheritedHandles);
        }

        [Fact]
        public void TestWorkingDirectory_DefaultIsNull()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            Assert.Null(options.WorkingDirectory);
        }

        [Fact]
        public void TestWorkingDirectory_CanSet()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            string tempDir = Path.GetTempPath();
            options.WorkingDirectory = tempDir;
            Assert.Equal(tempDir, options.WorkingDirectory);
        }

        [Fact]
        public void TestKillOnParentExit_DefaultIsFalse()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            Assert.False(options.KillOnParentExit);
        }

        [Fact]
        public void TestKillOnParentExit_CanSet()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            options.KillOnParentExit = true;
            Assert.True(options.KillOnParentExit);
        }

        [Fact]
        public void TestCreateNewProcessGroup_DefaultIsFalse()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            Assert.False(options.CreateNewProcessGroup);
        }

        [Fact]
        public void TestCreateNewProcessGroup_CanSet()
        {
            ProcessStartOptions options = new ProcessStartOptions(GetCurrentProcessName());
            options.CreateNewProcessGroup = true;
            Assert.True(options.CreateNewProcessGroup);
        }

        private string GetCurrentProcessName()
        {
            // Get a valid executable path for testing
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(Environment.SystemDirectory, "cmd.exe");
            }
            else
            {
                return "/bin/sh";
            }
        }
    }
}
