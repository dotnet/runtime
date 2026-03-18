// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessStartOptionsTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Constructor_ResolvesCmdOnWindows()
        {
            ProcessStartOptions options = new("cmd");
            Assert.EndsWith("cmd.exe", options.FileName);
            Assert.True(File.Exists(options.FileName));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer), nameof(PlatformDetection.IsNotWindowsServerCore))]
        public void ResolvePath_AddsExeExtension()
        {
            // Test that .exe is appended when no extension is provided
            ProcessStartOptions options = new("notepad");
            Assert.EndsWith(".exe", options.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(options.FileName));
        }

        [Fact]
        public void ResolvePath_DoesNotAddExeExtensionForTrailingDot()
        {
            // "If the file name ends in a period (.) with no extension, .exe is not appended."
            // This should fail since "notepad." won't exist
            Assert.Throws<FileNotFoundException>(() => new ProcessStartOptions("notepad."));
        }

        [Fact]
        public void ResolvePath_PreservesComExtension()
        {
            // The .com extension should be preserved
            string fileName = "test.com";
            string tempDir = Path.GetTempPath();
            string fullPath = Path.Combine(tempDir, fileName);
            
            string oldDir = Directory.GetCurrentDirectory();
            try
            {
                File.WriteAllText(fullPath, "test");
                Directory.SetCurrentDirectory(tempDir);
                ProcessStartOptions options = new($".\\{fileName}");
                Assert.EndsWith(".com", options.FileName, StringComparison.Ordinal);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ResolvePath_FindsInSystemDirectory()
        {
            // cmd.exe should be found in system directory
            ProcessStartOptions options = new("cmd");
            Assert.True(File.Exists(options.FileName));
            string expectedPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            Assert.Equal(expectedPath, options.FileName);
        }

        [Theory]
        [InlineData(".\\testapp.exe", true)]
        [InlineData("testapp.exe", false)]
        public void ResolvePath_UsesCurrentDirectory(string fileNameFormat, bool shouldSucceed)
        {
            string tempDir = Path.GetTempPath();
            string fileName = "testapp.exe";
            string fullPath = Path.Combine(tempDir, fileName);
            
            string oldDir = Directory.GetCurrentDirectory();
            try
            {
                File.WriteAllText(fullPath, "test");
                Directory.SetCurrentDirectory(tempDir);

                if (shouldSucceed)
                {
                    ProcessStartOptions options = new(fileNameFormat);
                    Assert.Equal(fullPath, options.FileName);
                }
                else
                {
                    // Without .\ prefix, should not find file in CWD and should throw
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
        public void ResolvePath_PathSeparatorIsSemicolon()
        {
            // Create a temp directory and file
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string fileName = "testexe.exe";
            string fullPath = Path.Combine(tempDir, fileName);
            
            string oldPath = Environment.GetEnvironmentVariable("PATH");
            try
            {
                File.WriteAllText(fullPath, "test");
                Environment.SetEnvironmentVariable("PATH", tempDir + ";" + oldPath);
                ProcessStartOptions options = new("testexe");
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
        public void ResolvePath_AbsolutePathIsNotModified()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                // Rename to remove extension to test that .exe is not added for absolute paths
                string noExtFile = Path.ChangeExtension(tempFile, null);
                File.Move(tempFile, noExtFile);
                tempFile = noExtFile;

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

        [Fact]
        public void ResolvePath_RootedButNotFullyQualifiedPath()
        {
            // Test paths like "C:foo.exe" (without backslash after colon) which are rooted but not fully qualified
            // These resolve relative to the current directory on that drive
            string tempDir = Path.GetTempPath();
            string fileName = "test_rooted.tmp";
            string fullPath = Path.Combine(tempDir, fileName);

            string oldDir = Directory.GetCurrentDirectory();
            try
            {
                File.WriteAllText(fullPath, "test");
                Directory.SetCurrentDirectory(tempDir);

                // Create a rooted but not fully qualified path: "C:filename" (no backslash after drive)
                string drive = Path.GetPathRoot(tempDir)!.TrimEnd('\\', '/'); // e.g., "C:"
                string rootedPath = $"{drive}{fileName}"; // e.g., "C:test_rooted.tmp"

                Assert.True(Path.IsPathRooted(rootedPath));
                Assert.False(Path.IsPathFullyQualified(rootedPath));

                ProcessStartOptions options = new(rootedPath);

                Assert.True(Path.IsPathFullyQualified(options.FileName));
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
    }
}
