// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Diagnostics.Tests
{
    [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.FreeBSD | TestPlatforms.OSX)]
    public class ProcessStartOptionsTests_Unix
    {
        [Fact]
        public void TestResolvePath_FindsInPath()
        {
            // sh should be findable in PATH on all Unix systems
            ProcessStartOptions options = new ProcessStartOptions("sh");
            Assert.True(File.Exists(options.FileName));
            Assert.EndsWith("sh", options.FileName);
        }

        [Fact]
        public void TestResolvePath_DoesNotAddExeExtension()
        {
            // On Unix, no .exe extension should be added
            ProcessStartOptions options = new ProcessStartOptions("sh");
            Assert.False(options.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void TestResolvePath_UsesCurrentDirectory()
        {
            string tempDir = Path.GetTempPath();
            string fileName = "testscript.sh";
            string fullPath = Path.Combine(tempDir, fileName);
            
            string oldDir = Directory.GetCurrentDirectory();
            try
            {
                File.WriteAllText(fullPath, "#!/bin/sh\necho test");
                // Make it executable
                File.SetUnixFileMode(fullPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                
                Directory.SetCurrentDirectory(tempDir);
                ProcessStartOptions options = new ProcessStartOptions(fileName);
                Assert.Equal(fullPath, options.FileName);
            }
            finally
            {
                Directory.SetCurrentDirectory(oldDir);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        [Fact]
        public void TestResolvePath_PathSeparatorIsColon()
        {
            // Create a temp directory and file
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string fileName = "testscript";
            string fullPath = Path.Combine(tempDir, fileName);
            
            string oldPath = Environment.GetEnvironmentVariable("PATH");
            try
            {
                File.WriteAllText(fullPath, "#!/bin/sh\necho test");
                // Make it executable
                File.SetUnixFileMode(fullPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                
                // Add temp directory to PATH using colon separator
                Environment.SetEnvironmentVariable("PATH", tempDir + ":" + oldPath);
                ProcessStartOptions options = new ProcessStartOptions(fileName);
                Assert.Equal(fullPath, options.FileName);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", oldPath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        [Fact]
        public void TestResolvePath_ChecksExecutablePermissions()
        {
            // Create a file without execute permissions
            string tempDir = Path.GetTempPath();
            string fileName = "nonexecutable.sh";
            string fullPath = Path.Combine(tempDir, fileName);
            
            try
            {
                File.WriteAllText(fullPath, "#!/bin/sh\necho test");
                // Explicitly make it non-executable
                File.SetUnixFileMode(fullPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                
                // Save current directory
                string oldDir = Directory.GetCurrentDirectory();
                try
                {
                    Directory.SetCurrentDirectory(tempDir);
                    // Should throw because file is not executable
                    Assert.Throws<FileNotFoundException>(() => new ProcessStartOptions(fileName));
                }
                finally
                {
                    Directory.SetCurrentDirectory(oldDir);
                }
            }
            finally
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
        }

        [Fact]
        public void TestResolvePath_AbsolutePathIsNotModified()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                ProcessStartOptions options = new ProcessStartOptions(tempFile);
                Assert.Equal(tempFile, options.FileName);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void TestResolvePath_FindsCommonUtilities()
        {
            // Test common Unix utilities
            string[] commonUtils = { "ls", "cat", "echo", "sh" };
            
            foreach (string util in commonUtils)
            {
                ProcessStartOptions options = new ProcessStartOptions(util);
                Assert.True(File.Exists(options.FileName), $"{util} should be found and exist");
                Assert.Contains(util, options.FileName);
            }
        }

        [Fact]
        public void TestResolvePath_RejectsDirectories()
        {
            // Create a directory with executable permissions
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Try to use the directory name as a command
                string oldDir = Directory.GetCurrentDirectory();
                try
                {
                    Directory.SetCurrentDirectory(Path.GetTempPath());
                    Assert.Throws<FileNotFoundException>(() => new ProcessStartOptions(Path.GetFileName(tempDir)));
                }
                finally
                {
                    Directory.SetCurrentDirectory(oldDir);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir);
                }
            }
        }
    }
}
