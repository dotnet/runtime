// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.MachO;
using Microsoft.DotNet.CoreSetup;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;
using FluentAssertions;
using System.IO.MemoryMappedFiles;
using System.Collections;
using System.Collections.Generic;
using Microsoft.DotNet.Cli.Build.Framework;
using System.Security.AccessControl;

namespace Microsoft.NET.HostModel.MachO.CodeSign.Tests
{
    public class SigningTests
    {
        public static bool IsSigned(string filePath)
        {
            // Validate the signature if we can, otherwise, at least ensure there is a signature LoadCommand present
            using (var appHostSourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
            using (var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostSourceStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true))
            using (var managedSignedAccessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite))
            {
                if (!MachObjectFile.Create(new MemoryMappedMachOViewAccessor(managedSignedAccessor)).HasSignature)
                {
                    return false;
                }
            }
            if (Codesign.IsAvailable && Codesign.Run("--verify", filePath).ExitCode != 0)
            {
                return false;
            }
            return true;
        }

        public static bool IsMachOImage(string filePath) => MachObjectFile.IsMachOImage(filePath);

        static readonly string[] liveBuiltHosts = new string[] { Binaries.AppHost.FilePath, Binaries.SingleFileHost.FilePath };
        static List<string> GetTestFilePaths(TestArtifact testArtifact)
        {
            List<(string Name, FileInfo File)> testData = TestData.MachObjects.GetAll().ToList();
            List<string> testFilePaths = new();
            foreach ((string name, FileInfo file) in testData)
            {
                string newFilePath = Path.Combine(testArtifact.Location, name);
                File.Copy(file.FullName, newFilePath, true);
                testFilePaths.Add(newFilePath);
            }

            // If we're on mac, we can use the live built binaries to test against
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                foreach (var filePath in liveBuiltHosts)
                {
                    string fileName = Path.GetFileName(filePath);
                    string testFilePath = Path.Combine(testArtifact.Location, fileName);
                    File.Copy(filePath, testFilePath);
                    testFilePaths.Add(testFilePath);
                }
            }

            return testFilePaths;
        }

        [Fact]
        public void CanSignMachObject()
        {
            using var testArtifact = TestArtifact.Create(nameof(CanSignMachObject));
            foreach (var filePath in GetTestFilePaths(testArtifact))
            {
                string fileName = Path.GetFileName(filePath);
                string originalFilePath = filePath;
                string managedSignedPath = filePath + ".signed";

                // Managed signed file
                AdHocSignFile(originalFilePath, managedSignedPath, fileName);
                Assert.True(IsSigned(managedSignedPath), $"Failed to sign a copy of {filePath}");
            }
        }

        [Fact]
        public void CanRemoveSignature()
        {
            using var testArtifact = TestArtifact.Create(nameof(CanRemoveSignature));
            foreach (var filePath in GetTestFilePaths(testArtifact))
            {
                string fileName = Path.GetFileName(filePath);
                string originalFilePath = filePath;
                string managedSignedPath = filePath + ".signed";
                RemoveSignature(originalFilePath, managedSignedPath);
                Assert.False(IsSigned(managedSignedPath), $"Failed to remove signature from {filePath}");
            }
        }

        [Fact]
        public void CanUnsignAndResign()
        {
            using var testArtifact = TestArtifact.Create(nameof(CanUnsignAndResign));
            foreach (var filePath in GetTestFilePaths(testArtifact))
            {
                string fileName = Path.GetFileName(filePath);
                string originalFilePath = filePath;
                string managedSignedPath = filePath + ".signed";

                // Managed signed file
                AdHocSignFile(originalFilePath, managedSignedPath, fileName);
                Assert.True(IsSigned(managedSignedPath), $"Failed to sign a copy of {filePath}");

                // Remove signature
                RemoveSignature(managedSignedPath, managedSignedPath + ".unsigned");
                Assert.False(IsSigned(managedSignedPath + ".unsigned"), $"Failed to remove signature from {filePath}");

                // Resign
                AdHocSignFile(managedSignedPath + ".unsigned", managedSignedPath + ".resigned", fileName);
                Assert.True(IsSigned(managedSignedPath + ".resigned"), $"Failed to resign {filePath}");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        void MatchesCodesignOutput()
        {
            using var testArtifact = TestArtifact.Create(nameof(MatchesCodesignOutput));
            foreach (var filePath in GetTestFilePaths(testArtifact))
            {
                string fileName = Path.GetFileName(filePath);
                string originalFilePath = filePath;
                string codesignFilePath = filePath + ".codesigned";
                string managedSignedPath = filePath + ".signed";

                // Codesigned file
                File.Copy(filePath, codesignFilePath);
                Assert.True(Codesign.IsAvailable, "Could not find codesign tool");
                Codesign.Run("--remove-signature", codesignFilePath).ExitCode.Should().Be(0, $"'codesign --remove-signature {codesignFilePath}' failed!");
                Codesign.Run("-s - -i " + fileName, codesignFilePath).ExitCode.Should().Be(0, $"'codesign -s - {codesignFilePath}' failed!");

                // Managed signed file
                AdHocSignFile(originalFilePath, managedSignedPath, fileName);

                var check = Codesign.Run("-v", managedSignedPath);
                check.ExitCode.Should().Be(0, check.StdErr, $"Failed to sign a copy of '{filePath}'");
                AssertMachFilesAreEquivalent(codesignFilePath, managedSignedPath, fileName);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        void SignedMachOExecutableRuns()
        {
            using var testArtifact = TestArtifact.Create(nameof(SignedMachOExecutableRuns));
            foreach (var (fileName, fileInfo) in TestData.MachObjects.GetRunnable())
            {
                string unsignedFilePath = Path.Combine(testArtifact.Location, fileName);
                string signedPath = unsignedFilePath + ".signed";
                fileInfo.CopyTo(unsignedFilePath);

                AdHocSignFile(unsignedFilePath, signedPath, fileName);

                // Set the file to be executable
                File.SetUnixFileMode(signedPath, UnixFileMode.UserRead | UnixFileMode.UserExecute);

                var result = Command.Create(signedPath).CaptureStdErr().CaptureStdOut().Execute();
                result.ExitCode.Should().Be(0, result.StdErr);
            }
        }

        [Fact]
        void ReadSignedMachIsTheSameAsReadAndResigned()
        {
            using var testArtifact = TestArtifact.Create(nameof(ReadSignedMachIsTheSameAsReadAndResigned));
            foreach (var fileName in GetTestFilePaths(testArtifact))
            {
                string signedPath = fileName + ".signed";

                AdHocSignFile(fileName, signedPath, fileName);
                using (var mmap = MemoryMappedFile.CreateFromFile(signedPath))
                using (var accessor = mmap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite))
                {
                    var signedMachFile = new MemoryMappedMachOViewAccessor(accessor);
                    var signedObject = MachObjectFile.Create(signedMachFile);
                    var resignedObject = MachObjectFile.Create(signedMachFile);
                    resignedObject.AdHocSignFile(signedMachFile, fileName);
                    MachObjectFile.AssertEquivalent(signedObject, resignedObject);
                }
            }
        }

        [Fact]
        void RoundTripMachObjectFileIsTheSame()
        {
            using var testArtifact = TestArtifact.Create(nameof(RoundTripMachObjectFileIsTheSame));
            foreach (var fileName in GetTestFilePaths(testArtifact))
            {
                using (var mmap = MemoryMappedFile.CreateFromFile(fileName))
                using (var accessor = mmap.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite))
                {
                    var machFile = new MemoryMappedMachOViewAccessor(accessor);
                    var machObjectFile = MachObjectFile.Create(machFile);
                    machObjectFile.Write(machFile);
                    var rewrittenMachFile = MachObjectFile.Create(machFile);
                    MachObjectFile.AssertEquivalent(machObjectFile, rewrittenMachFile);
                }
            }
        }

        static void AssertMachFilesAreEquivalent(string codesignedPath, string managedSignedPath, string fileName)
        {
            using var managedFileStream = new FileStream(managedSignedPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
            using var managedMMapFile = MemoryMappedFile.CreateFromFile(managedFileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            using var managedSignedAccessor = managedMMapFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite);
            var managedMachFile = new MemoryMappedMachOViewAccessor(managedSignedAccessor);

            using var codesignedFileStream = new FileStream(codesignedPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
            using var codesignedMMapFile = MemoryMappedFile.CreateFromFile(codesignedFileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            using var codesignedAccessor = codesignedMMapFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite);
            var codesignedMachFile = new MemoryMappedMachOViewAccessor(codesignedAccessor);

            var codesignedObject = MachObjectFile.Create(codesignedMachFile);
            var managedSignedObject = MachObjectFile.Create(managedMachFile);
            MachObjectFile.AssertEquivalent(codesignedObject, managedSignedObject);
        }

        /// <summary>
        /// AdHoc sign a test file. This should look similar to HostWriter.CreateAppHost.
        /// </summary>
        public static void AdHocSignFile(string originalFilePath, string managedSignedPath, string fileName)
        {
            Assert.NotEqual(originalFilePath, managedSignedPath);
            // Open the source host file.
            using (FileStream appHostDestinationStream = new FileStream(managedSignedPath, FileMode.Create, FileAccess.ReadWrite))
            {
                using (FileStream appHostSourceStream = new(originalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
                {
                    appHostSourceStream.CopyTo(appHostDestinationStream);
                }
                var appHostLength = appHostDestinationStream.Length;
                var appHostSignedLength = appHostLength + MachObjectFile.GetSignatureSizeEstimate((uint)appHostLength, fileName);

                using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostDestinationStream, null, appHostSignedLength, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                using (MemoryMappedViewAccessor memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, appHostSignedLength, MemoryMappedFileAccess.ReadWrite))
                {
                    var file = new MemoryMappedMachOViewAccessor(memoryMappedViewAccessor);
                    var machObjectFile = MachObjectFile.Create(file);
                    appHostLength = machObjectFile.AdHocSignFile(file, fileName);
                }
                appHostDestinationStream.SetLength(appHostLength);
            }
        }

        public static void AdHocSignFileInPlace(string managedSignedPath)
        {
            var tmpFile = Path.GetTempFileName();
            var mode = File.GetUnixFileMode(managedSignedPath);
            using (FileStream appHostDestinationStream = new FileStream(tmpFile, FileMode.Create, FileAccess.ReadWrite))
            {
                using (FileStream appHostSourceStream = new(managedSignedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    appHostSourceStream.CopyTo(appHostDestinationStream);
                }
                var appHostLength = appHostDestinationStream.Length;
                var appHostSignedLength = appHostLength + MachObjectFile.GetSignatureSizeEstimate((uint)appHostLength, tmpFile);

                using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostDestinationStream, null, appHostSignedLength, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                using (MemoryMappedViewAccessor memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0, appHostSignedLength, MemoryMappedFileAccess.ReadWrite))
                {
                    var file = new MemoryMappedMachOViewAccessor(memoryMappedViewAccessor);
                    var machObjectFile = MachObjectFile.Create(file);
                    appHostLength = machObjectFile.AdHocSignFile(file, tmpFile);
                }
                appHostDestinationStream.SetLength(appHostLength);
            }
            File.Move(tmpFile, managedSignedPath, true);
            File.SetUnixFileMode(managedSignedPath, mode);
        }

        /// <summary>
        /// AdHoc sign a test file. This should look similar to HostWriter.CreateAppHost.
        /// </summary>
        internal static void RemoveSignature(string originalFilePath, string removedSignaturePath)
        {
            Assert.NotEqual(originalFilePath, removedSignaturePath);
            // Open the source host file.
            using (FileStream appHostDestinationStream = new FileStream(removedSignaturePath, FileMode.Create, FileAccess.ReadWrite))
            {
                using (FileStream appHostSourceStream = new(originalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1))
                {
                    appHostSourceStream.CopyTo(appHostDestinationStream);
                }
                var appHostLength = appHostDestinationStream.Length;
                var destinationFileName = Path.GetFileName(removedSignaturePath);
                var appHostSignedLength = appHostLength + MachObjectFile.GetSignatureSizeEstimate((uint)appHostLength, destinationFileName);

                MachObjectFile.RemoveCodeSignatureIfPresent(appHostDestinationStream);
            }
        }
    }
}
