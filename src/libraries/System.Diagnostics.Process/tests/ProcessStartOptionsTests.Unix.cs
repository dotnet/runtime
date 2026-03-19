// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Diagnostics.Tests
{
    [PlatformSpecific(TestPlatforms.Linux | TestPlatforms.FreeBSD | TestPlatforms.OSX)]
    public partial class ProcessStartOptionsTests
    {
        [Fact]
        public void Constructor_ResolvesShOnUnix()
        {
            ProcessStartOptions options = new("sh");
            Assert.True(File.Exists(options.FileName));
            // Verify the resolved path ends with "sh" (could be /bin/sh, /usr/bin/sh, etc.)
            Assert.EndsWith("sh", options.FileName);
        }

        [Fact]
        public void ResolvePath_FindsInPath()
        {
            // sh should be findable in PATH on all Unix systems
            ProcessStartOptions options = new("sh");
            Assert.True(File.Exists(options.FileName));
            // Verify the resolved path ends with "sh" (could be /bin/sh, /usr/bin/sh, etc.)
            Assert.EndsWith("sh", options.FileName);
        }

        [Fact]
        public void ResolvePath_DoesNotAddExeExtension()
        {
            // On Unix, no .exe extension should be added
            ProcessStartOptions options = new("sh");
            Assert.False(options.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("./testscript.sh", true)]
        [InlineData("testscript.sh", false)]
        public void ResolvePath_UsesCurrentDirectory(string fileNameFormat, bool shouldSucceed)
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

                if (shouldSucceed)
                {
                    ProcessStartOptions options = new(fileNameFormat);
                    Assert.True(File.Exists(options.FileName));
                    // on macOS, we need to handle /tmp/testscript.sh -> /private/tmp/testscript.sh
                    Assert.EndsWith(fullPath, options.FileName);
                }
                else
                {
                    // Without ./ prefix, should not find file in CWD and should throw
                    Assert.Throws<FileNotFoundException>(() => new ProcessStartOptions(fileNameFormat));
                }
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
        public void ResolvePath_PathSeparatorIsColon()
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
                ProcessStartOptions options = new(fileName);
                Assert.Equal(Path.GetFullPath(fullPath), options.FileName);
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
        public void ResolvePath_AbsolutePathIsNotModified()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                ProcessStartOptions options = new(tempFile);
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

        [Theory]
        [InlineData("ls")]
        [InlineData("cat")]
        [InlineData("echo")]
        [InlineData("sh")]
        public void ResolvePath_FindsCommonUtilities(string utilName)
        {
            ProcessStartOptions options = new(utilName);
            Assert.True(File.Exists(options.FileName), $"{utilName} should be found and exist");
            Assert.EndsWith(utilName, options.FileName);
        }

        [Fact]
        public void ResolvePath_RejectsDirectories()
        {
            // Create a directory with executable permissions
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            string oldDir = Directory.GetCurrentDirectory();
            try
            {
                // Try to use the directory name as a command
                Directory.SetCurrentDirectory(Path.GetTempPath());
                Assert.Throws<FileNotFoundException>(() => new ProcessStartOptions(Path.GetFileName(tempDir)));
            }
            finally
            {
                Directory.SetCurrentDirectory(oldDir);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir);
                }
            }
        }
    }
}
