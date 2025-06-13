// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;

using FluentAssertions;
using Microsoft.NET.HostModel.MachO.CodeSign;
using Microsoft.NET.HostModel.MachO;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;
using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;
using Microsoft.NET.HostModel.MachO.CodeSign.Tests;
using System.ComponentModel;

namespace Microsoft.NET.HostModel.AppHost.Tests
{
    public class CreateAppHost
    {
        /// <summary>
        /// hash value embedded in default apphost executable in a place where the path to the app binary should be stored.
        /// </summary>
        private const string AppBinaryPathPlaceholder = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2";
        private readonly static byte[] AppBinaryPathPlaceholderSearchValue = Encoding.UTF8.GetBytes(AppBinaryPathPlaceholder);

        /// <summary>
        /// Value embedded in default apphost executable for configuration of how it will search for the .NET install
        /// </summary>
        private const string DotNetSearchPlaceholder = "\0\019ff3e9c3602ae8e841925bb461a0adb064a1f1903667a5e0d87e8f608f425ac";
        private static readonly byte[] DotNetSearchPlaceholderValue = Encoding.UTF8.GetBytes(DotNetSearchPlaceholder);

        [Fact]
        public void EmbedAppBinaryPath()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location);
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                HostWriter.CreateAppHost(
                    sourceAppHostMock,
                    destinationFilePath,
                    appBinaryFilePath);

                byte[] binaryPathBlob = Encoding.UTF8.GetBytes(appBinaryFilePath);
                byte[] result = File.ReadAllBytes(destinationFilePath);
                result
                    .Skip(WindowsFileHeader.Length)
                    .Take(binaryPathBlob.Length)
                    .Should()
                    .BeEquivalentTo(binaryPathBlob);

                BitConverter
                    .ToUInt16(result, SubsystemOffset)
                    .Should()
                    .Be((ushort)Subsystem.WindowsCui);
            }
        }

        [Fact]
        public void PlaceholderHashNotFound_Fails()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location, content =>
                {
                    // Corrupt the hash value
                    content[WindowsFileHeader.Length + 1]++;
                });
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                Assert.Throws<PlaceHolderNotFoundInAppHostException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        appBinaryFilePath));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        public void AppBinaryPathTooLong_Fails()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location);
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = new string('a', 1024 + 5);

                Assert.Throws<AppNameTooLongException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        appBinaryFilePath));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        public void AppRelativePathRooted_Fails()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location);
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                HostWriter.DotNetSearchOptions options = new()
                {
                    Location = HostWriter.DotNetSearchOptions.SearchLocation.AppRelative,
                    AppRelativeDotNet = artifact.Location
                };

                Assert.Throws<AppRelativePathRootedException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        "app.dll",
                        dotNetSearchOptions: options));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        public void AppRelativePathTooLong_Fails()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location);
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                HostWriter.DotNetSearchOptions options = new()
                {
                    Location = HostWriter.DotNetSearchOptions.SearchLocation.AppRelative,
                    AppRelativeDotNet = new string('p', 1024)
                };

                Assert.Throws<AppRelativePathTooLongException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        "app.dll",
                        dotNetSearchOptions: options));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        public void GUISubsystem_WindowsPEFile()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location);
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                HostWriter.CreateAppHost(
                    sourceAppHostMock,
                    destinationFilePath,
                    appBinaryFilePath,
                    windowsGraphicalUserInterface: true);

                BitConverter
                   .ToUInt16(File.ReadAllBytes(destinationFilePath), SubsystemOffset)
                   .Should()
                   .Be((ushort)Subsystem.WindowsGui);

                Assert.Equal((ushort)Subsystem.WindowsGui, PEUtils.GetWindowsGraphicalUserInterfaceBit(destinationFilePath));
            }
        }

        [Fact]
        public void GUISubsystem_NonWindowsPEFile_Fails()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location, content =>
                {
                    // Windows PE files must start with 0x5A4D, so write some other value here.
                    content[0] = 1;
                    content[1] = 2;
                });
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                Assert.Throws<AppHostNotPEFileException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        appBinaryFilePath,
                        windowsGraphicalUserInterface: true));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        public void GUISubsystem_WrongDefault_Fails()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location, content =>
                {
                    // Corrupt the value of the subsystem (the default should be 3)
                    content[SubsystemOffset] = 42;
                });
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";

                Assert.Equal(42, PEUtils.GetWindowsGraphicalUserInterfaceBit(sourceAppHostMock));
                Assert.Throws<AppHostNotCUIException>(() =>
                    HostWriter.CreateAppHost(
                        sourceAppHostMock,
                        destinationFilePath,
                        appBinaryFilePath,
                        windowsGraphicalUserInterface: true));

                File.Exists(destinationFilePath).Should().BeFalse();
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void ExecutableImage()
        {
            using TestArtifact artifact = CreateTestDirectory();
            string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location);
            string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
            string appBinaryFilePath = "Test/App/Binary/Path.dll";

            // strip executable permissions from this AppHost template binary
            File.SetUnixFileMode(sourceAppHostMock, UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead);

            // -rwxr-xr-x
            const UnixFileMode expectedPermissions = UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

            HostWriter.CreateAppHost(
                sourceAppHostMock,
                destinationFilePath,
                appBinaryFilePath,
                windowsGraphicalUserInterface: true);

            // assert that the generated app has executable permissions
            // despite different permissions on the template binary.
            File.GetUnixFileMode(destinationFilePath)
                .Should()
                .Be(expectedPermissions);
        }

        [Theory]
        [InlineData("")]
        [InlineData("dir with spaces")]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void CodeSignMachOAppHost(string subdir)
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string testDirectory = Path.Combine(artifact.Location, subdir);
                Directory.CreateDirectory(testDirectory);
                string sourceAppHostMock = Binaries.AppHost.FilePath;
                string destinationFilePath = Path.Combine(testDirectory, Binaries.AppHost.FileName);
                string appBinaryFilePath = "Test/App/Binary/Path.dll";
                HostWriter.CreateAppHost(
                   sourceAppHostMock,
                   destinationFilePath,
                   appBinaryFilePath,
                   windowsGraphicalUserInterface: false,
                   enableMacOSCodeSign: true);

                // Validate that there is a signature present in the apphost Mach file
                SigningTests.IsSigned(destinationFilePath).Should().BeTrue();
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("dir with spaces")]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void SigningExistingAppHostCreatesNewInode(string subdir)
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string testDirectory = Path.Combine(artifact.Location, subdir);
                Directory.CreateDirectory(testDirectory);
                string sourceAppHostMock = Binaries.AppHost.FilePath;
                string destinationFilePath = Path.Combine(testDirectory, Binaries.AppHost.FileName);
                string appBinaryFilePath = "Test/App/Binary/Path.dll";
                HostWriter.CreateAppHost(
                   sourceAppHostMock,
                   destinationFilePath,
                   appBinaryFilePath,
                   windowsGraphicalUserInterface: false,
                   enableMacOSCodeSign: true);
                var firstls = Command.Create("/bin/ls", "-li", destinationFilePath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute();
                firstls.Should().Pass();
                var firstInode = firstls.StdOut.Split(' ')[0];

                // Validate that there is a signature present in the apphost Mach file
                SigningTests.IsSigned(destinationFilePath).Should().BeTrue();

                HostWriter.CreateAppHost(
                   sourceAppHostMock,
                   destinationFilePath,
                   appBinaryFilePath,
                   windowsGraphicalUserInterface: false,
                   enableMacOSCodeSign: true);

                var secondls = Command.Create("/bin/ls", "-li", destinationFilePath)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute();
                secondls.Should().Pass();
                var secondInode = secondls.StdOut.Split(' ')[0];
                // Ensure the MacOS signature cache is cleared
                Assert.False(firstInode == secondInode, "not a different inode after rebundle");

                SigningTests.IsSigned(destinationFilePath).Should().BeTrue();
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("dir with spaces")]
        public void CodeSignMockMachOAppHost(string subdir)
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string testDirectory = Path.Combine(artifact.Location, subdir);
                Directory.CreateDirectory(testDirectory);
                string sourceAppHostMock = PrepareMockMachAppHostFile(testDirectory);
                string destinationFilePath = Path.Combine(testDirectory, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";
                HostWriter.CreateAppHost(
                   sourceAppHostMock,
                   destinationFilePath,
                   appBinaryFilePath,
                   windowsGraphicalUserInterface: false,
                   enableMacOSCodeSign: true);

                // Validate that there is a signature present in the apphost Mach file
                SigningTests.IsSigned(destinationFilePath).Should().BeTrue();
            }
        }

        [Fact]
        public void DoesNotCodeSignAppHostByDefault()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareMockMachAppHostFile(artifact.Location);
                File.SetAttributes(sourceAppHostMock, FileAttributes.ReadOnly);
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";
                HostWriter.CreateAppHost(
                   sourceAppHostMock,
                   destinationFilePath,
                   appBinaryFilePath,
                   windowsGraphicalUserInterface: false);

                if (!Codesign.IsAvailable)
                {
                    return;
                }

                var (exitCode, stdErr) = Codesign.Run("-d", destinationFilePath);
                stdErr.Should().Contain($"{Path.GetFullPath(destinationFilePath)}: code object is not signed at all");
            }
        }

        [Fact]
        public void CodeSignNotMachOThrows()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string sourceAppHostMock = PrepareAppHostMockFile(artifact.Location);
                File.SetAttributes(sourceAppHostMock, FileAttributes.ReadOnly);
                string destinationFilePath = Path.Combine(artifact.Location, "DestinationAppHost.exe.mock");
                string appBinaryFilePath = "Test/App/Binary/Path.dll";
                // The apphost is not a Mach file, so an exception should be thrown.
                var exception = Assert.Throws<InvalidDataException>(() =>
                    HostWriter.CreateAppHost(
                    sourceAppHostMock,
                    destinationFilePath,
                    appBinaryFilePath,
                    windowsGraphicalUserInterface: false,
                    enableMacOSCodeSign: true));
            }
        }

        [Theory]
        [InlineData(true)]  // Bit is set in extended DLL characteristics
        [InlineData(false)] // Bit is not set in extended DLL characteristics
        [InlineData(null)]  // No extended DLL characteristics
        public void CetCompat(bool? cetCompatSet)
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                // Create a PE image with with CET compatability enabled/disabled
                BlobBuilder peBlob = Binaries.CetCompat.CreatePEImage(cetCompatSet);

                // Add the placeholder - it just needs to exist somewhere in the image, as HostWriter.CreateAppHost requires it
                peBlob.WriteBytes(AppBinaryPathPlaceholderSearchValue);

                string source = Path.Combine(artifact.Location, "source.exe");
                using (FileStream stream = new FileStream(source, FileMode.Create))
                {
                    peBlob.WriteContentTo(stream);
                }

                bool originallyEnabled = cetCompatSet.HasValue ? cetCompatSet.Value : false;
                Assert.Equal(originallyEnabled, Binaries.CetCompat.IsMarkedCompatible(source));

                // Validate compatibility is disabled
                string cetDisabled = Path.Combine(artifact.Location, "cetDisabled.exe");
                HostWriter.CreateAppHost(
                   source,
                   cetDisabled,
                   "app",
                   disableCetCompat: true);
                Assert.False(Binaries.CetCompat.IsMarkedCompatible(cetDisabled));

                // Validate compatibility is not changed
                string cetEnabled = Path.Combine(artifact.Location, "cetUnchanged.exe");
                HostWriter.CreateAppHost(
                   source,
                   cetEnabled,
                   "app",
                   disableCetCompat: false);
                Assert.Equal(originallyEnabled, Binaries.CetCompat.IsMarkedCompatible(cetEnabled));
            }
        }

        [ConditionalFact(typeof(Binaries.CetCompat), nameof(Binaries.CetCompat.IsSupported))]
        public void CetCompat_ProductHosts()
        {
            using (TestArtifact artifact = CreateTestDirectory())
            {
                string[] hosts = [Binaries.AppHost.FilePath, Binaries.SingleFileHost.FilePath];
                foreach (string host in hosts)
                {
                    // Hosts should be compatible with CET shadow stack by default
                    Assert.True(Binaries.CetCompat.IsMarkedCompatible(host));
                    string source = Path.Combine(artifact.Location, Path.GetFileName(host));
                    File.Copy(host, source);

                    // Validate compatibility is disabled
                    string cetDisabled = Path.Combine(artifact.Location, $"{Path.GetFileName(host)}_cetDisabled.exe");
                    HostWriter.CreateAppHost(
                       source,
                       cetDisabled,
                       "app",
                       disableCetCompat: true);
                    Assert.False(Binaries.CetCompat.IsMarkedCompatible(cetDisabled));

                    // Validate compatibility is not changed (remains enabled)
                    string cetEnabled = Path.Combine(artifact.Location, $"{Path.GetFileName(host)}_cetEnabled.exe");
                    HostWriter.CreateAppHost(
                       source,
                       cetEnabled,
                       "app",
                       disableCetCompat: false);
                    Assert.True(Binaries.CetCompat.IsMarkedCompatible(cetEnabled));
                }
            }
        }

        [Fact]
        private void ResourceWithUnknownLanguage()
        {
            // https://github.com/dotnet/runtime/issues/88465
            using (TestApp app = TestApp.CreateFromBuiltAssets("AppWithUnknownLanguageResource"))
            {
                app.CreateAppHost();
            }
        }

        private static readonly byte[] s_apphostPlaceholderData = AppBinaryPathPlaceholderSearchValue.Concat(DotNetSearchPlaceholderValue).ToArray();
        private static readonly byte[] s_singleFileApphostPlaceholderData = {
            // 8 bytes represent the bundle header-offset
            // Zero for non-bundle apphosts (default).
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // 32 bytes represent the bundle signature: SHA-256 for ".net core bundle"
            0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38,
            0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
            0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18,
            0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae
        };

        /// <summary>
        /// Prepares a mock executable file with the AppHost placeholder embedded in it.
        /// This file will not run, but can be used to test HostWriter and signing process.
        /// </summary>
        public static string PrepareMockMachAppHostFile(string directory, bool singleFile = false)
        {
            string fileName = "MockAppHost.mach.o";
            string outputFilePath = Path.Combine(directory, fileName);
            using (var aOutStream = TestData.MachObjects.GetAll().First().File.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var managedSignFile = File.OpenWrite(outputFilePath))
            {
                aOutStream!.CopyTo(managedSignFile);
                // Add the placeholder - it just needs to exist somewhere in the image
                // We'll put it at 4096 bytes into the file - this should be in the middle of the __TEXT segment
                managedSignFile.Position = 4096;
                managedSignFile.Write(singleFile ? s_singleFileApphostPlaceholderData : s_apphostPlaceholderData);
            }
            return outputFilePath;
        }

        private string PrepareAppHostMockFile(string directory, Action<byte[]> customize = null)
        {
            // For now we're testing the AppHost on Windows PE files only.
            // The only customization which we do on non-Windows files is the embedding
            // of the binary path, which works the same regardless of the file format.

            int size = WindowsFileHeader.Length + AppBinaryPathPlaceholderSearchValue.Length + DotNetSearchPlaceholderValue.Length;
            byte[] content = new byte[size];
            Array.Copy(WindowsFileHeader, 0, content, 0, WindowsFileHeader.Length);
            Array.Copy(AppBinaryPathPlaceholderSearchValue, 0, content, WindowsFileHeader.Length, AppBinaryPathPlaceholderSearchValue.Length);
            Array.Copy(DotNetSearchPlaceholderValue, 0, content, WindowsFileHeader.Length + AppBinaryPathPlaceholderSearchValue.Length, DotNetSearchPlaceholderValue.Length);
            customize?.Invoke(content);

            string filePath = Path.Combine(directory, "SourceAppHost.exe.mock");
            File.WriteAllBytes(filePath, content);
            return filePath;
        }

        private const int SubsystemOffset = 0xF0 + 0x5C;

        // This is a dump of first 350 bytes of a windows apphost.exe
        // This includes the PE header and part of the Optional header
        private static readonly byte[] WindowsFileHeader = new byte[] {
            77, 90, 144, 0, 3, 0, 0, 0, 4, 0, 0, 0, 255, 255, 0, 0, 184,
            0, 0, 0, 0, 0, 0, 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            240, 0, 0, 0, 14, 31, 186, 14, 0, 180, 9, 205,
            33, 184, 1, 76, 205, 33, 84, 104, 105, 115, 32, 112, 114, 111,
            103, 114, 97, 109, 32, 99, 97, 110, 110, 111, 116, 32, 98, 101,
            32, 114, 117, 110, 32, 105, 110, 32, 68, 79, 83, 32, 109, 111,
            100, 101, 46, 13, 13, 10, 36, 0, 0, 0, 0, 0, 0, 0, 30, 91, 134,
            254, 90, 58, 232, 173, 90, 58, 232, 173, 90, 58, 232, 173, 97,
            100, 235, 172, 93, 58, 232, 173, 97, 100, 237, 172, 99, 58,
            232, 173, 97, 100, 236, 172, 123, 58, 232, 173, 83, 66, 123,
            173, 72, 58, 232, 173, 135, 197, 35, 173, 89, 58, 232, 173,
            90, 58, 233, 173, 204, 58, 232, 173, 205, 100, 237, 172, 92,
            58, 232, 173, 200, 100, 23, 173, 91, 58, 232, 173, 205, 100, 234,
            172, 91, 58, 232, 173, 82, 105, 99, 104, 90, 58, 232, 173, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            80, 69, 0, 0, 100, 134, 7, 0, 29, 151, 54, 91, 0, 0, 0, 0, 0, 0,
            0, 0, 240, 0, 34, 0, 11, 2, 14, 0, 0, 28, 1, 0, 0, 8, 1, 0, 0, 0,
            0, 0, 80, 231, 0, 0, 0, 16, 0, 0, 0, 0, 0, 64, 1, 0, 0, 0, 0, 16,
            0, 0, 0, 2, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0,
            0, 112, 2, 0, 0, 4, 0, 0, 0, 0, 0, 0, 3, 0, 96, 193, 0, 0, 24,
            0, 0, 0, 0, 0, 0, 16, 0, 0, 0, 0 };

        private TestArtifact CreateTestDirectory([CallerMemberName] string callingMethod = "")
            => TestArtifact.Create(callingMethod);
    }
}
