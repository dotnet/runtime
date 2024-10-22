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
        internal static bool IsSigned(string filePath)
        {
            // Validate the signature if we can, otherwise, at least ensure there is a signature LoadCommand present
            if (Codesign.IsAvailable)
                return Codesign.Run("--verify", filePath).ExitCode == 0;

            using var appHostSourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostSourceStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            using var managedSignedAccessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite);
            return new MachObjectFile(managedSignedAccessor, Path.GetFileName(filePath)).HasSignature;
        }

        static readonly string[] liveBuiltHosts = new string[] { Binaries.AppHost.FilePath, Binaries.SingleFileHost.FilePath };
        static List<string> GetTestFiles(TestArtifact testArtifact)
        {
            List<(string Name, Stream Data)> testData = TestData.MachObjects.GetAll();
            List<string> testFilePaths = new();
            foreach ((string Name, Stream Data) in testData)
            {
                string originalFilePath = Path.Combine(testArtifact.Location, Name);
                using (var aOutStream = Data)
                using (var managedSignFile = File.OpenWrite(originalFilePath))
                {
                    aOutStream!.CopyTo(managedSignFile);
                }
                testFilePaths.Add(originalFilePath);
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
            foreach (var filePath in GetTestFiles(testArtifact))
            {
                string fileName = Path.GetFileName(filePath);
                string originalFilePath = filePath;
                string managedSignedPath = filePath + ".signed";

                // Managed signed file
                AdHocSignFile(originalFilePath, managedSignedPath, fileName);
                Assert.True(IsSigned(managedSignedPath));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        void MatchesCodesignOutput()
        {
            using var testArtifact = TestArtifact.Create(nameof(MatchesCodesignOutput));
            foreach (var filePath in GetTestFiles(testArtifact))
            {
                string fileName = Path.GetFileName(filePath);
                string originalFilePath = filePath;
                string codesignFilePath = filePath + ".codesigned";
                string managedSignedPath = filePath + ".signed";

                // Codesigned file
                File.Copy(filePath, codesignFilePath);
                Assert.True(Codesign.IsAvailable);
                Codesign.Run("--remove-signature", codesignFilePath).ExitCode.Should().Be(0);
                Codesign.Run("-s -", codesignFilePath).ExitCode.Should().Be(0);

                // Managed signed file
                AdHocSignFile(originalFilePath, managedSignedPath, fileName);

                var check = Codesign.Run("-v", managedSignedPath);
                check.ExitCode.Should().Be(0, check.StdErr);
                Assert.True(MachFilesAreEquivalent(codesignFilePath, managedSignedPath, fileName));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        void SignedHelloWorldRuns()
        {
            using var testArtifact = TestArtifact.Create(nameof(SignedHelloWorldRuns));
            foreach (var filePath in GetTestFiles(testArtifact))
            {
                string fileName = Path.GetFileName(filePath);
                string originalFilePath = filePath;
                string managedSignedPath = filePath + ".signed";

                // Codesigned file
                Assert.True(Codesign.IsAvailable);

                // Managed signed file
                AdHocSignFile(originalFilePath, managedSignedPath, fileName);

                // Set the file to be executable
                File.SetUnixFileMode(managedSignedPath, UnixFileMode.UserRead | UnixFileMode.UserExecute);

                var result = Command.Create(managedSignedPath).CaptureStdErr().CaptureStdOut().Execute();
                switch(result.ExitCode)
                {
                    case 0:
                        break;
                    case 149:
                        result.StdErr.Should().Contain("This executable is not bound to a managed DLL to execute");
                        break;
                    default:
                        Assert.Fail($"'{managedSignedPath}' returned an unexpected exit code: {result.ExitCode}\n\tStdOut:'{result.StdOut}'\n\tStdErr: '{result.StdErr}'");
                        break;
                }
            }
        }

        static bool MachFilesAreEquivalent(string codesignedPath, string managedSignedPath, string fileName)
        {
            using var managedFileStream = new FileStream(managedSignedPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
            using var managedMMapFile = MemoryMappedFile.CreateFromFile(managedFileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            using var managedSignedAccessor = managedMMapFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite);

            using var codesignedFileStream = new FileStream(managedSignedPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
            using var codesignedMMapFile = MemoryMappedFile.CreateFromFile(codesignedFileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            using var codesignedAccessor = codesignedMMapFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite);

            var codesignedObject = new MachObjectFile(codesignedAccessor, fileName);
            var managedSignedObject = new MachObjectFile(managedSignedAccessor, fileName);
            return MachObjectFile.AreEquivalent(codesignedObject, managedSignedObject);
        }

        internal static void AdHocSignFile(string originalFilePath, string managedSignedPath, string fileName)
        {
            Assert.NotEqual(originalFilePath, managedSignedPath);
            using var appHostSourceStream = new FileStream(originalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
            using var memoryMappedFile = MemoryMappedFile.CreateFromFile(appHostSourceStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            using var managedSignedAccessor = memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.CopyOnWrite);

            MachObjectFile machObjectFile = new MachObjectFile(managedSignedAccessor, fileName);
            long newSize = machObjectFile.CreateAdHocSignature(managedSignedAccessor, fileName);

            using (FileStream fileStream = new FileStream(managedSignedPath, FileMode.Create, FileAccess.ReadWrite))
            {
                BinaryUtils.WriteToStream(managedSignedAccessor, fileStream, newSize);
                machObjectFile.WriteCodeSignature(fileStream);
            }
        }
    }
}
