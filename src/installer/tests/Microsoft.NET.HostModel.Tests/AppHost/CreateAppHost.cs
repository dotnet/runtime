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
using Microsoft.NET.HostModel.MachO.CodeSign.Blobs;
using System.Buffers.Binary;

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
        public void CodeSignMachOAppHost(string subdir)
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
                var objectFile = MachReader.Read(File.OpenRead(destinationFilePath)).FirstOrDefault();
                Assert.NotNull(objectFile);
                var codeSignature = objectFile!.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
                Assert.NotNull(codeSignature);

                // Verify with codesign as well
                if (!Codesign.IsAvailable())
                {
                    return;
                }
                const string codesign = @"/usr/bin/codesign";
                var psi = new ProcessStartInfo()
                {
                    Arguments = $"-d \"{destinationFilePath}\"",
                    FileName = codesign,
                    RedirectStandardError = true,
                };

                using (var p = Process.Start(psi))
                {
                    p.Start();
                    p.StandardError.ReadToEnd()
                        .Should().Contain($"Executable={Path.GetFullPath(destinationFilePath)}");
                    p.WaitForExit();
                    // Successfully signed the apphost.
                    Assert.True(p.ExitCode == 0, $"Expected exit code was '0' but '{codesign}' returned '{p.ExitCode}' instead.");
                }
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

                if (!Codesign.IsAvailable())
                {
                    return;
                }

                var (exitCode, stdErr) = Codesign.Run("-d", destinationFilePath);
                stdErr.Should().Contain($"{Path.GetFullPath(destinationFilePath)}: code object is not signed at all");
            }
        }

        [Fact]
        public void CodeSigningFailuresThrow()
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

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void SignedAppHostRuns()
        {
            using var testDirectory = TestArtifact.Create(nameof(SignedAppHostRuns));
            var testAppHostPath = Path.Combine(testDirectory.Location, Path.GetFileName(Binaries.AppHost.FilePath));
            File.Copy(Binaries.SingleFileHost.FilePath, testAppHostPath);
            long preRemovalSize = new FileInfo(testAppHostPath).Length;
            if (Signer.TryRemoveCodesign(testAppHostPath))
            {
                Assert.True(preRemovalSize > new FileInfo(testAppHostPath).Length);
            }
            else
            {
                Assert.Equal(preRemovalSize, new FileInfo(testAppHostPath).Length);
            }
            Signer.AdHocSign(testAppHostPath);
            Codesign.Run("-v", testAppHostPath).ExitCode.Should().Be(0);

            File.SetUnixFileMode(testAppHostPath, UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            var executedCommand = Command.Create(testAppHostPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();
            // AppHost exit code should be 149 when the apphost runs properly but cannot find the appliation/runtime
            executedCommand.ExitCode.Should().Be(149);
            Signer.TryRemoveCodesign(testAppHostPath);
            Signer.AdHocSign(testAppHostPath);
            File.SetUnixFileMode(testAppHostPath, UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
            executedCommand = Command.Create(testAppHostPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();
            // AppHost exit code should be 149 when the apphost runs properly but cannot find the appliation/runtime
            executedCommand.ExitCode.Should().Be(149);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void ManagedSignerMatchesCodesignOutput()
        {
            /*
             * This test ensures that the output of the managed signer is nearly identical to the output of the `codesign` command.
             * The outputs should be byte-for-byte identical, except for the padding after the code signature.
             * This affects the size of the LinkEdit segment header, which then impacts the hash of the first page in the code signature (hash index 0).
             * We'll check that the hashes are identical, except for the first page hash.
             * Since these are hashes of the file contents, we can be confident the rest of the file is identical.
             */
            using var testDirectory = TestArtifact.Create(nameof(SignedAppHostRuns));
            var managedSignedAppHostPath = Path.Combine(testDirectory.Location, Path.GetFileName(Binaries.AppHost.FilePath) + ".managedsigned");
            var codesignedAppHostPath = Path.Combine(testDirectory.Location, Path.GetFileName(Binaries.AppHost.FilePath) + ".codesigned");
            string appHostName = Path.GetFileName(Binaries.AppHost.FilePath);
            File.Copy(Binaries.AppHost.FilePath, managedSignedAppHostPath);
            File.Copy(Binaries.AppHost.FilePath, codesignedAppHostPath);
            long preRemovalSize = new FileInfo(managedSignedAppHostPath).Length;
            using (var managedFile = File.Open(managedSignedAppHostPath, FileMode.Open, FileAccess.ReadWrite))
            {
                Signer.TryRemoveCodesign(managedFile, out _);
                long newSize = Signer.AdHocSign(managedFile, appHostName);
                managedFile.SetLength(newSize);
            }
            Codesign.Run("--remove-signature", codesignedAppHostPath).ExitCode.Should().Be(0);
            Codesign.Run("--sign - -i " + appHostName, codesignedAppHostPath).ExitCode.Should().Be(0);

            Codesign.Run("-v", managedSignedAppHostPath).ExitCode.Should().Be(0);
            Codesign.Run("-v", codesignedAppHostPath).ExitCode.Should().Be(0);

            var managedObject = MachReader.Read(File.OpenRead(managedSignedAppHostPath)).FirstOrDefault();
            var codesignObject = MachReader.Read(File.OpenRead(codesignedAppHostPath)).FirstOrDefault();
            var zippedLoadCommands = managedObject.LoadCommands.Zip(codesignObject.LoadCommands);
            foreach(var lc in zippedLoadCommands)
            {
                Assert.Equal(lc.First.GetType(), lc.Second.GetType());
            }
            var managedCodeSignature = managedObject.LoadCommands.OfType<MachCodeSignature>().Single();
            var codesignCodeSignature = codesignObject.LoadCommands.OfType<MachCodeSignature>().Single();

            Assert.True(codesignCodeSignature.FileOffset == managedCodeSignature.FileOffset);
            byte[] managedCSData = new byte[managedCodeSignature.Data.Size];
            byte[] codesignCSData = new byte[codesignCodeSignature.Data.Size];
            {
                var managedReader = managedCodeSignature.Data.GetReadStream();
                managedReader.Position = 0;
                managedReader.ReadExactly(managedCSData);
                var codesignReader = codesignCodeSignature.Data.GetReadStream();
                codesignReader.Position = 0;
                codesignReader.ReadExactly(codesignCSData);
            }

            // Embedded signature header
            BinaryPrimitives.ReadUInt32BigEndian(managedCSData.AsSpan(0, 4)).Should().Be((uint)BlobMagic.EmbeddedSignature);
            BinaryPrimitives.ReadUInt32BigEndian(codesignCSData.AsSpan(0, 4)).Should().Be((uint)BlobMagic.EmbeddedSignature);
            var signatureHeaderSize = BinaryPrimitives.ReadUInt32BigEndian(managedCSData.AsSpan(4, 4));
            BinaryPrimitives.ReadUInt32BigEndian(codesignCSData.AsSpan(4, 4)).Should().Be(signatureHeaderSize);
            var blobsCount = BinaryPrimitives.ReadUInt32BigEndian(managedCSData.AsSpan(8, 4));
            BinaryPrimitives.ReadUInt32BigEndian(codesignCSData.AsSpan(8, 4)).Should().Be(blobsCount);

            // Blob indices
            int codeDirectoryOffset = 0;
            for (int i = 0; i < blobsCount; i++)
            {
                var specialSlot = (CodeDirectorySpecialSlot)BinaryPrimitives.ReadUInt32BigEndian(managedCSData.AsSpan(12 + i * 4, 4));
                BinaryPrimitives.ReadUInt32BigEndian(codesignCSData.AsSpan(12 + i * 4, 4)).Should().Be((uint)specialSlot);
                var offset = BinaryPrimitives.ReadUInt32BigEndian(managedCSData.AsSpan(12 + i * 4 + 4, 4));
                BinaryPrimitives.ReadUInt32BigEndian(codesignCSData.AsSpan(12 + i * 4 + 4, 4)).Should().Be(offset);
                if (specialSlot == CodeDirectorySpecialSlot.CodeDirectory)
                    codeDirectoryOffset = (int)offset;
            }
            Assert.NotEqual(0, codeDirectoryOffset);

            // CodeDirectory blob
            var managedCDHeader = CodeDirectoryBaselineHeader.Read(managedCSData.AsSpan(codeDirectoryOffset), out int bytesRead);
            var codesignCDHeader = CodeDirectoryBaselineHeader.Read(codesignCSData.AsSpan(codeDirectoryOffset), out int _);
            Assert.Equal(managedCDHeader.Magic, codesignCDHeader.Magic);
            Assert.Equal(managedCDHeader.Magic, BlobMagic.CodeDirectory);
            Assert.Equal(managedCDHeader.Size, codesignCDHeader.Size);
            Assert.Equal(managedCDHeader.Version, codesignCDHeader.Version);
            Assert.Equal(managedCDHeader.Flags, codesignCDHeader.Flags);
            Assert.Equal(managedCDHeader.HashesOffset, codesignCDHeader.HashesOffset);
            Assert.Equal(managedCDHeader.IdentifierOffset, codesignCDHeader.IdentifierOffset);
            Assert.Equal(managedCDHeader.SpecialSlotCount, codesignCDHeader.SpecialSlotCount);
            Assert.Equal(managedCDHeader.CodeSlotCount, codesignCDHeader.CodeSlotCount);
            Assert.Equal(managedCDHeader.ExecutableLength, codesignCDHeader.ExecutableLength);
            Assert.Equal(managedCDHeader.HashSize, codesignCDHeader.HashSize);
            Assert.Equal(managedCDHeader.HashType, codesignCDHeader.HashType);
            Assert.Equal(managedCDHeader.Platform, codesignCDHeader.Platform);
            Assert.Equal(managedCDHeader.Log2PageSize, codesignCDHeader.Log2PageSize);
            Assert.Equal(managedCDHeader.Reserved, codesignCDHeader.Reserved);
            Assert.Equal(managedCDHeader._UnknownPadding, codesignCDHeader._UnknownPadding);

            // CodeDirectory hashes
            var managedCDBlob = managedCSData.AsSpan().Slice(codeDirectoryOffset, (int)managedCDHeader.Size);
            var codesignCDBlob = codesignCSData.AsSpan().Slice(codeDirectoryOffset, (int)codesignCDHeader.Size);
            for (int i = (int)(-managedCDHeader.SpecialSlotCount); i < 0; i++)
            {
                var managedHash = managedCDBlob.Slice((int)managedCDHeader.HashesOffset + i * managedCDHeader.HashSize, HashTypeExtensions.GetSize(managedCDHeader.HashType));
                var codesignHash = managedCDBlob.Slice((int)codesignCDHeader.HashesOffset + i * codesignCDHeader.HashSize, HashTypeExtensions.GetSize(codesignCDHeader.HashType));
                Assert.Equal(managedHash, codesignHash);
            }

            // Start at 1 because the first hash is will be different because of size headers
            for (int i = 1; i < managedCDHeader.CodeSlotCount; i++)
            {
                var managedHash = managedCDBlob.Slice((int)managedCDHeader.HashesOffset + i * managedCDHeader.HashType.GetSize(), managedCDHeader.HashType.GetSize());
                var codesignHash = codesignCDBlob.Slice((int)codesignCDHeader.HashesOffset + i * codesignCDHeader.HashType.GetSize(), codesignCDHeader.HashType.GetSize());
                Assert.Equal(managedHash, codesignHash);
            }
        }

        private static readonly byte[] s_placeholderData = AppBinaryPathPlaceholderSearchValue.Concat(DotNetSearchPlaceholderValue).ToArray();
        public static string PrepareMockMachAppHostFile(string directory)
        {
            var objectFile = Microsoft.NET.HostModel.MachO.Tests.ReadTests.GetMachExecutable();
            var segments = objectFile.LoadCommands.OfType<MachSegment>().ToArray();

            var textSegment = segments.Single(s => s.Name == "__TEXT");
            var textSection = textSegment.Sections.First();
            using (var textStream = textSection.GetWriteStream())
            {
                textStream.Write(s_placeholderData);
            }
            // The __TEXT segment has its sections at the end of the segment, with padding at the beginning
            // We can safely move the file offset back to make room for the placeholder data
            textSection.FileOffset -= (uint)(AppBinaryPathPlaceholderSearchValue.Length + DotNetSearchPlaceholderValue.Length);
            string outputFilePath = Path.Combine(directory, "SourceAppHost.mach.o.mock");
            using var outputFileStream = File.OpenWrite(outputFilePath);
            MachWriter.Write(outputFileStream, objectFile);
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
