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
using Microsoft.NET.HostModel.MachO.Streams;
using Microsoft.DotNet.CoreSetup;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;
using FluentAssertions;

namespace Microsoft.NET.HostModel.MachO.CodeSign.Tests
{
    public class SigningTests
    {
        internal static MachObjectFile GetMachObjectFileFromResource(string resourceName)
        {
            var aOutStream = typeof(SigningTests).Assembly.GetManifestResourceStream(resourceName)!;
            var objectFile = MachReader.Read(aOutStream).FirstOrDefault();
            Assert.NotNull(objectFile);
            return objectFile;
        }

        internal static bool IsSigned(string filePath)
        {
            // Validate the signature if we can, otherwise, at least ensure there is a signature LoadCommand present
            if (Codesign.IsAvailable())
            {
                return Codesign.Run("--verify", filePath).ExitCode == 0;
            }
            else
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var machObject = MachReader.Read(fileStream).Single();
                    return machObject.LoadCommands.OfType<MachCodeSignature>().Any();
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void RemoveSignatureMatchesCodesignOutput()
        {
            // Ensure that the managed RemoveSignature is byte-for-byute identical to the `codesign --remove-signature` output
            using var testArtifact = TestArtifact.Create(nameof(RemoveSignatureMatchesCodesignOutput));
            var aOutStream = typeof(SigningTests).Assembly.GetManifestResourceStream("Microsoft.NET.HostModel.MachO.Tests.Data.a.out")!;
            Span<byte> aOut = new byte[aOutStream.Length];
            aOutStream.ReadFully(aOut);
            var originalFileTmpName = Path.Combine(testArtifact.Location, "a.out");
            var nextFileName = Path.Combine(testArtifact.Location, "b.out");
            File.WriteAllBytes(originalFileTmpName, aOut);
            File.WriteAllBytes(nextFileName, aOut);

            var (exitCode, output) = Codesign.Run("--verify", originalFileTmpName);
            if (exitCode == 0)
            {
                // Unsign if necessary and ensure identical
                Codesign.Run("--remove-signature", originalFileTmpName);
                Signer.TryRemoveCodesign(nextFileName);

                var originalFileBytes = File.ReadAllBytes(originalFileTmpName);
                var nextFileBytes = File.ReadAllBytes(nextFileName);
                originalFileBytes.SequenceEqual(nextFileBytes).Should().BeTrue();
            }
        }

        [Fact]
        public void CanRemoveSignatureUsingPath()
        {
            using var testPath = TestArtifact.Create(nameof(CanRemoveSignatureUsingPath));
            var filePath = Path.Combine(testPath.Location, "a.out");
            var objectFile = GetMachObjectFileFromResource("Microsoft.NET.HostModel.MachO.Tests.Data.a.out");
            using (var originalFileTmpStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                MachWriter.Write(originalFileTmpStream, objectFile);
            }
            Assert.True(IsSigned(filePath));
            long originalSize = new FileInfo(filePath).Length;

            Assert.True(Signer.TryRemoveCodesign(filePath));

            long strippedSize = new FileInfo(filePath).Length;

            Assert.True(strippedSize < originalSize);
            Assert.True(strippedSize > 0);
            Assert.True(!IsSigned(filePath));
        }

        [Fact]
        public void CanRemoveSignatureUsingStream()
        {
            using var testArtifact = TestArtifact.Create(nameof(CanRemoveSignatureUsingStream));
            var tmpFile = Path.Combine(testArtifact.Location, "a.out");
            var objectFile = GetMachObjectFileFromResource("Microsoft.NET.HostModel.MachO.Tests.Data.a.out");
            using (_ = objectFile.GetOriginalStream())
            using (var streamCopy = new MemoryStream())
            {
                MachWriter.Write(streamCopy, objectFile);
                streamCopy.Position = 0;
                long originalSize = streamCopy.Length;

                Assert.True(Signer.TryRemoveCodesign(streamCopy, out long newSize));

                Assert.True(newSize < originalSize);
                Assert.True(newSize > 0);
                using (var fileStream = new FileStream(tmpFile, FileMode.Create))
                {
                    streamCopy.Position = 0;
                    streamCopy.CopyTo(fileStream);
                }
            }
            Assert.False(IsSigned(tmpFile));
        }

        [Fact]
        public void DoubleRemoveSignatureStream()
        {
            var objectFile = GetMachObjectFileFromResource("Microsoft.NET.HostModel.MachO.Tests.Data.a.out");
            using (_ = objectFile.GetOriginalStream())
            using (var originalFileTmpStream = new MemoryStream())
            {
                MachWriter.Write(originalFileTmpStream, objectFile);
                originalFileTmpStream.Position = 0;
                long originalSize = originalFileTmpStream.Length;

                Assert.True(Signer.TryRemoveCodesign(originalFileTmpStream, out long newSize));
                Assert.True(newSize < originalSize);
                originalFileTmpStream.SetLength(newSize);

                Assert.False(Signer.TryRemoveCodesign(originalFileTmpStream, out long doubleRemovedSize));
                Assert.Equal(newSize, doubleRemovedSize);
            }
        }

        [Fact]
        public void CanRemoveSignatureTwice()
        {
            using var testArtifact = TestArtifact.Create(nameof(CanRemoveSignatureTwice));
            var fileName = Path.Combine(testArtifact.Location, "a.out");
            var objectFile = GetMachObjectFileFromResource("Microsoft.NET.HostModel.MachO.Tests.Data.a.out");
            using (var originalFileTmpStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                MachWriter.Write(originalFileTmpStream, objectFile);
            }

            Assert.True(IsSigned(fileName));
            Assert.True(Signer.TryRemoveCodesign(fileName));
            Assert.False(IsSigned(fileName));
            Assert.False(Signer.TryRemoveCodesign(fileName));
            Assert.False(IsSigned(fileName));
        }

        [Fact]
        public void CanRemoveSignatureAndThenSign()
        {
            using var testArtifact = TestArtifact.Create(nameof(CanRemoveSignatureAndThenSign));

            string tmpFilePath = Path.Combine(testArtifact.Location, "a.out");
            var objectFile = GetMachObjectFileFromResource("Microsoft.NET.HostModel.MachO.Tests.Data.a.out");
            using (var strippedFileTmpStream = new FileStream(tmpFilePath, FileMode.Create))
                MachWriter.Write(strippedFileTmpStream, objectFile);

            Signer.TryRemoveCodesign(tmpFilePath);
            long strippedSize = new FileInfo(tmpFilePath).Length;
            Assert.True(strippedSize > 0);
            Assert.False(IsSigned(tmpFilePath));

            Signer.AdHocSign(tmpFilePath);

            // If we can't validate the signature with codesign, at least make sure the file size has increased
            long signedSize = new FileInfo(tmpFilePath).Length;
            Assert.True(signedSize > strippedSize);
            Assert.True(signedSize > 0);
            Assert.True(IsSigned(tmpFilePath));
        }

        [Fact]
        public void CanRemoveSignatureAndSignTwice()
        {
            using var testArtifact = TestArtifact.Create(nameof(CanRemoveSignatureAndSignTwice));
            string tmpFilePath = Path.Combine(testArtifact.Location, "a.out");
            var objectFile = GetMachObjectFileFromResource("Microsoft.NET.HostModel.MachO.Tests.Data.a.out");
            using (var strippedFileTmpStream = new FileStream(tmpFilePath, FileMode.Create))
                MachWriter.Write(strippedFileTmpStream, objectFile);

            Signer.TryRemoveCodesign(tmpFilePath);
            Assert.False(IsSigned(tmpFilePath));

            Signer.AdHocSign(tmpFilePath);
            Assert.True(IsSigned(tmpFilePath));

            Signer.TryRemoveCodesign(tmpFilePath);
            Assert.False(IsSigned(tmpFilePath));

            Signer.AdHocSign(tmpFilePath);
            Assert.True(IsSigned(tmpFilePath));
        }
    }
}
