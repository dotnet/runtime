// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessStartOptionsTests
    {
        [Fact]
        public void Constructor_NullFileName_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ProcessStartOptions(null));
        }

        [Fact]
        public void Constructor_EmptyFileName_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ProcessStartOptions(string.Empty));
        }

        [Fact]
        public void Constructor_NonExistentFile_Throws()
        {
            string nonExistentFile = "ThisFileDoesNotExist_" + Guid.NewGuid().ToString();
            Assert.Throws<FileNotFoundException>(() => new ProcessStartOptions(nonExistentFile));
        }

        [Fact]
        public void Constructor_WithAbsolutePath()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                ProcessStartOptions options = new(tempFile);
                Assert.Equal(tempFile, options.FileName);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Arguments_DefaultIsEmpty()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            IList<string> args = options.Arguments;
            Assert.NotNull(args);
            Assert.Empty(args);
        }

        [Fact]
        public void Arguments_CanAddAndModify()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
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
        public void Environment_CanAddAndModify()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
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
        public void Environment_CaseSensitivityIsPlatformSpecific()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
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
        public void InheritedHandles_DefaultIsEmpty()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            IList<SafeHandle> handles = options.InheritedHandles;
            Assert.NotNull(handles);
            Assert.Empty(handles);
        }

        [Fact]
        public void InheritedHandles_CanSet()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            List<SafeHandle> newHandles = [];
            options.InheritedHandles = newHandles;
            Assert.Same(newHandles, options.InheritedHandles);
        }

        [Fact]
        public void WorkingDirectory_DefaultIsNull()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            Assert.Null(options.WorkingDirectory);
        }

        [Fact]
        public void WorkingDirectory_CanSet()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            string tempDir = Path.GetTempPath();
            options.WorkingDirectory = tempDir;
            Assert.Equal(tempDir, options.WorkingDirectory);
        }

        [Fact]
        public void KillOnParentExit_DefaultIsFalse()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            Assert.False(options.KillOnParentExit);
        }

        [Fact]
        public void KillOnParentExit_CanSet()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            options.KillOnParentExit = true;
            Assert.True(options.KillOnParentExit);
        }

        [Fact]
        public void CreateNewProcessGroup_DefaultIsFalse()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            Assert.False(options.CreateNewProcessGroup);
        }

        [Fact]
        public void CreateNewProcessGroup_CanSet()
        {
            ProcessStartOptions options = new(GetCurrentProcessName());
            options.CreateNewProcessGroup = true;
            Assert.True(options.CreateNewProcessGroup);
        }

        private string GetCurrentProcessName()
        {
            return Environment.ProcessPath ?? (OperatingSystem.IsWindows()
                ? Path.Combine(Environment.SystemDirectory, "cmd.exe")
                : "/bin/sh");
        }
    }
}
