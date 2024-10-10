using Xunit;
using System.IO;
using System.Linq;
using Microsoft.NET.HostModel.MachO;
using Microsoft.NET.HostModel.MachO.Streams;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using FluentAssertions;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.DotNet.CoreSetup;
using System.IO.MemoryMappedFiles;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using System.Runtime.CompilerServices;

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
        public void UseMemoryMappedFile()
        {
            var objectFile = GetMachObjectFileFromResource("Microsoft.NET.HostModel.MachO.Tests.Data.a.out");
            string tmpFilePath = Path.GetTempFileName();
            string tmpFileName = Path.GetFileName(tmpFilePath);
            Console.WriteLine($"tmpFilePath: {tmpFilePath}");
            long originalFileLength = objectFile.GetOriginalStream().Length;
            long tmpFileSize = originalFileLength + Signer.GetCodeSignatureSize(originalFileLength);

            using (var fileStream = new FileStream(tmpFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                objectFile.GetOriginalStream().CopyTo(fileStream);
                fileStream.SetLength(tmpFileSize);
                // Disregard the new size -- we want to reserve extra space for the signature since memorymapped files can't be resized
                Signer.TryRemoveCodesign(fileStream, out _);

                using (var mmf = MemoryMappedFile.CreateFromFile(fileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true))
                {
                    using (MemoryMappedViewStream accessor = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.CopyOnWrite))
                    {
                        long newSize = Signer.AdHocSignMachO(accessor, tmpFileName);
                        accessor.Flush();
                        accessor.Position = 0;
                        fileStream.Position = 0;
                        accessor.CopyTo(fileStream);
                        fileStream.SetLength(newSize);
                    }
                }
            }

            Assert.True(IsSigned(tmpFilePath));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void RemoveSignatureMatchesCodesignOutput()
        {
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
        public void RemoveSignature()
        {
            using var testPath = TestArtifact.Create(nameof(RemoveSignature));
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
        public void RemoveSignatureStream()
        {
            using var testArtifact = TestArtifact.Create(nameof(RemoveSignatureStream));
            var objectFile = GetMachObjectFileFromResource("Microsoft.NET.HostModel.MachO.Tests.Data.a.out");
            using (_ = objectFile.GetOriginalStream())
            using (var originalFileTmpStream = new MemoryStream())
            {
                MachWriter.Write(originalFileTmpStream, objectFile);
                originalFileTmpStream.Position = 0;
                long originalSize = originalFileTmpStream.Length;

                Assert.True(Signer.TryRemoveCodesign(originalFileTmpStream, out long newSize));

                Assert.True(newSize < originalSize);
                Assert.True(newSize > 0);
                var tmpFile = Path.Combine(testArtifact.Location, "a.out");
                using var fileStream = new FileStream(tmpFile, FileMode.Create);
                {
                    originalFileTmpStream.Position = 0;
                    originalFileTmpStream.CopyTo(fileStream);
                }
                Assert.False(IsSigned(tmpFile));
            }
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
        public void DoubleRemoveSignature()
        {
            using var testArtifact = TestArtifact.Create(nameof(DoubleRemoveSignature));
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
        public void RemoveSignatureAndSign()
        {
            using var testArtifact = TestArtifact.Create(nameof(RemoveSignatureAndSign));

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
        public void RemoveSignatureAndSignTwice()
        {
            using var testArtifact = TestArtifact.Create(nameof(RemoveSignatureAndSignTwice));
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
