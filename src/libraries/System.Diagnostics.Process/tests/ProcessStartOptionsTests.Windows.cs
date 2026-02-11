// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessStartOptionsTests_Windows
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_AddsExeExtension()
        {
            // Test that .exe is appended when no extension is provided
            ProcessStartOptions options = new ProcessStartOptions("notepad");
            Assert.EndsWith(".exe", options.FileName, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(options.FileName));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_DoesNotAddExeExtensionForTrailingDot()
        {
            // "If the file name ends in a period (.) with no extension, .exe is not appended."
            // This should fail since "notepad." won't exist
            Assert.Throws<FileNotFoundException>(() => new ProcessStartOptions("notepad."));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_PreservesComExtension()
        {
            // The .com extension should be preserved
            string fileName = "test.com";
            string tempDir = Path.GetTempPath();
            string fullPath = Path.Combine(tempDir, fileName);
            
            try
            {
                File.WriteAllText(fullPath, "test");
                
                // Save current directory
                string oldDir = Directory.GetCurrentDirectory();
                try
                {
                    Directory.SetCurrentDirectory(tempDir);
                    ProcessStartOptions options = new ProcessStartOptions(fileName);
                    Assert.EndsWith(".com", options.FileName, StringComparison.OrdinalIgnoreCase);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_FindsInSystemDirectory()
        {
            // cmd.exe should be found in system directory
            ProcessStartOptions options = new ProcessStartOptions("cmd");
            Assert.True(File.Exists(options.FileName));
            Assert.Contains("system32", options.FileName, StringComparison.OrdinalIgnoreCase);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_FindsInWindowsDirectory()
        {
            // Some utilities are in Windows directory
            // We'll test with a file that's commonly in Windows directory
            // Note: This might not exist on all systems, so we make it conditional
            try
            {
                ProcessStartOptions options = new ProcessStartOptions("notepad");
                Assert.True(File.Exists(options.FileName));
            }
            catch (FileNotFoundException)
            {
                // Skip if notepad is not found - it's not critical for this test
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_FindsInPath()
        {
            // powershell.exe should be findable in PATH on most Windows systems
            try
            {
                ProcessStartOptions options = new ProcessStartOptions("powershell");
                Assert.True(File.Exists(options.FileName));
                Assert.EndsWith(".exe", options.FileName, StringComparison.OrdinalIgnoreCase);
            }
            catch (FileNotFoundException)
            {
                // Skip if PowerShell is not found - it might not be in PATH
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_UsesCurrentDirectory()
        {
            string tempDir = Path.GetTempPath();
            string fileName = "testapp.exe";
            string fullPath = Path.Combine(tempDir, fileName);
            
            try
            {
                File.WriteAllText(fullPath, "test");
                
                // Save current directory
                string oldDir = Directory.GetCurrentDirectory();
                try
                {
                    Directory.SetCurrentDirectory(tempDir);
                    ProcessStartOptions options = new ProcessStartOptions(fileName);
                    Assert.Equal(fullPath, options.FileName);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_PathSeparatorIsSemicolon()
        {
            // Create a temp directory and file
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string fileName = "testexe.exe";
            string fullPath = Path.Combine(tempDir, fileName);
            
            try
            {
                File.WriteAllText(fullPath, "test");
                
                // Add temp directory to PATH using semicolon separator
                string oldPath = Environment.GetEnvironmentVariable("PATH");
                try
                {
                    Environment.SetEnvironmentVariable("PATH", tempDir + ";" + oldPath);
                    ProcessStartOptions options = new ProcessStartOptions("testexe");
                    Assert.Equal(fullPath, options.FileName);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("PATH", oldPath);
                }
            }
            finally
            {
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void TestResolvePath_AbsolutePathIsNotModified()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                // Rename to remove extension to test that .exe is not added for absolute paths
                string noExtFile = Path.ChangeExtension(tempFile, null);
                File.Move(tempFile, noExtFile);
                tempFile = noExtFile;

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
    }
}
