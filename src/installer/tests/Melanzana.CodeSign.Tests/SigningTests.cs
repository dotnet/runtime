using Xunit;
using System.IO;
using System.Linq;
using Melanzana.MachO;
using Melanzana.Streams;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.DotNet.CoreSetup.Test;
using System;
using FluentAssertions;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.DotNet.CoreSetup;

namespace Melanzana.CodeSign.Tests
{
    public class SigningTests
    {
        public static MachObjectFile GetMachObjectFileFromResource(string resourceName)
        {
            var aOutStream = typeof(SigningTests).Assembly.GetManifestResourceStream(resourceName)!;
            var objectFile = MachReader.Read(aOutStream).FirstOrDefault();
            Assert.NotNull(objectFile);
            return objectFile;
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void MatchesCodesignOutput()
        {
            var testArtifact = TestArtifact.Create(nameof(MatchesCodesignOutput));
            var aOutStream = typeof(SigningTests).Assembly.GetManifestResourceStream("Melanzana.CodeSign.Tests.Data.a.out")!;
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

            // Re-sign and compare
            Codesign.Run("-s -", originalFileTmpName);
            Signer.AdHocSign(nextFileName);
            var zippedData = File.ReadAllBytes(originalFileTmpName).Zip(File.ReadAllBytes(nextFileName));
            // The managed implementation sometimes adds extra padding to the signature
            Codesign.Run("--verify", originalFileTmpName).ExitCode.Should().Be(0);
            Codesign.Run("--verify", nextFileName).ExitCode.Should().Be(0);

            // Unsign twice and ensure identical
            Codesign.Run("--remove-signature", originalFileTmpName);
            Signer.TryRemoveCodesign(nextFileName).Should().BeTrue();
            zippedData = File.ReadAllBytes(originalFileTmpName).Zip(File.ReadAllBytes(nextFileName));
            Assert.All(zippedData.Select(t => t.First == t.Second), Assert.True);

            Codesign.Run("--remove-signature", originalFileTmpName);
            Signer.TryRemoveCodesign(nextFileName).Should().BeFalse();
            zippedData = File.ReadAllBytes(originalFileTmpName).Zip(File.ReadAllBytes(nextFileName));
            Assert.All(zippedData.Select(t => t.First == t.Second), Assert.True);
        }

        [Fact]
        public void RemoveSignature()
        {
            var originalFileTmpName = Path.GetTempFileName();
            var unsignedFileTmpName = Path.GetTempFileName();
            try
            {
                var objectFile = GetMachObjectFileFromResource("Melanzana.CodeSign.Tests.Data.a.out");
                using (var originalFileTmpStream = new FileStream(originalFileTmpName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    MachWriter.Write(originalFileTmpStream, objectFile);
                }
                long originalSize = new FileInfo(originalFileTmpName).Length;

                Assert.True(Signer.TryRemoveCodesign(originalFileTmpName, unsignedFileTmpName));

                long strippedSize = new FileInfo(unsignedFileTmpName).Length;

                Assert.True(strippedSize < originalSize);
                Assert.True(strippedSize > 0);

                if (Codesign.IsAvailable())
                {
                    var (exitCode, _) = Codesign.Run("--verify", originalFileTmpName);
                    Assert.Equal(0, exitCode);

                    (exitCode, _) = Codesign.Run("--verify", unsignedFileTmpName);
                    Assert.NotEqual(0, exitCode);
                }
            }
            finally
            {
                if (File.Exists(originalFileTmpName))
                    File.Delete(originalFileTmpName);
                if (File.Exists(unsignedFileTmpName))
                    File.Delete(unsignedFileTmpName);
            }
        }

        [Fact]
        public void RemoveSignatureStream()
        {
            var objectFile = GetMachObjectFileFromResource("Melanzana.CodeSign.Tests.Data.a.out");
            using (_ = objectFile.GetOriginalStream())
            using (var originalFileTmpStream = new MemoryStream())
            {
                MachWriter.Write(originalFileTmpStream, objectFile);
                originalFileTmpStream.Position = 0;
                long originalSize = originalFileTmpStream.Length;

                Assert.True(Signer.TryRemoveCodesign(originalFileTmpStream, originalFileTmpStream));

                long newSize = originalFileTmpStream.Length;
                Assert.True(newSize < originalSize);
                Assert.True(newSize > 0);
            }
        }

        [Fact]
        public void DoubleRemoveSignatureStream()
        {
            var objectFile = GetMachObjectFileFromResource("Melanzana.CodeSign.Tests.Data.a.out");
            using (_ = objectFile.GetOriginalStream())
            using (var originalFileTmpStream = new MemoryStream())
            {
                MachWriter.Write(originalFileTmpStream, objectFile);
                originalFileTmpStream.Position = 0;
                long originalSize = originalFileTmpStream.Length;

                Assert.True(Signer.TryRemoveCodesign(originalFileTmpStream, originalFileTmpStream));

                long newSize = originalFileTmpStream.Length;
                Assert.True(newSize < originalSize);

                Assert.False(Signer.TryRemoveCodesign(originalFileTmpStream, originalFileTmpStream));

                long doubleRemovedSize = originalFileTmpStream.Length;
                Assert.Equal(newSize, doubleRemovedSize);
            }
        }

        [Fact]
        public void DoubleRemoveSignature()
        {
            var originalFileTmpName = Path.GetTempFileName();
            var unsignedFileTmpName = Path.GetTempFileName();
            try
            {
                var objectFile = GetMachObjectFileFromResource("Melanzana.CodeSign.Tests.Data.a.out");
                using (var originalFileTmpStream = new FileStream(originalFileTmpName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    MachWriter.Write(originalFileTmpStream, objectFile);
                }

                Assert.True(Signer.TryRemoveCodesign(originalFileTmpName, unsignedFileTmpName));
                Assert.False(Signer.TryRemoveCodesign(unsignedFileTmpName));

                if (Codesign.IsAvailable())
                {
                    var (exitCode, _) = Codesign.Run("--verify", originalFileTmpName);
                    Assert.Equal(0, exitCode);

                    (exitCode, _) = Codesign.Run("--verify", unsignedFileTmpName);
                    Assert.NotEqual(0, exitCode);
                }
            }
            finally
            {
                if (File.Exists(originalFileTmpName))
                    File.Delete(originalFileTmpName);
                if (File.Exists(unsignedFileTmpName))
                    File.Delete(unsignedFileTmpName);
            }
        }

        [Fact]
        public void RemoveSignatureAndSign()
        {
            string tmpFilePath = Path.GetTempFileName();
            try
            {
                var objectFile = GetMachObjectFileFromResource("Melanzana.CodeSign.Tests.Data.a.out");
                using (var strippedFileTmpStream = new FileStream(tmpFilePath, FileMode.Create))
                    MachWriter.Write(strippedFileTmpStream, objectFile);

                Signer.TryRemoveCodesign(tmpFilePath);

                if (Codesign.IsAvailable())
                {
                    var (exitCode, _) = Codesign.Run("--verify", tmpFilePath);
                    Assert.NotEqual(0, exitCode);
                }
                long strippedSize = new FileInfo(tmpFilePath).Length;
                Assert.True(strippedSize > 0);

                Signer.AdHocSign(tmpFilePath);

                long signedSize = new FileInfo(tmpFilePath).Length;

                // If we can't validate the signature with codesign, at least make sure the file size has increased
                Assert.True(signedSize > strippedSize);
                Assert.True(signedSize > 0);

                if (Codesign.IsAvailable())
                {
                    var (exitCode, _) = Codesign.Run("--verify", tmpFilePath);
                    Assert.Equal(0, exitCode);
                }

            }
            finally
            {
                if (File.Exists(tmpFilePath))
                    File.Delete(tmpFilePath);
            }
        }

        [Fact]
        public void RemoveSignatureAndSignTwice()
        {
            string tmpFilePath = Path.GetTempFileName();
            try
            {
                var objectFile = GetMachObjectFileFromResource("Melanzana.CodeSign.Tests.Data.a.out");
                using (var strippedFileTmpStream = new FileStream(tmpFilePath, FileMode.Create))
                    MachWriter.Write(strippedFileTmpStream, objectFile);

                Signer.TryRemoveCodesign(tmpFilePath);
                Signer.AdHocSign(tmpFilePath);

                Signer.TryRemoveCodesign(tmpFilePath);
                Signer.AdHocSign(tmpFilePath);
            }
            finally
            {
                if (File.Exists(tmpFilePath))
                    File.Delete(tmpFilePath);
            }
        }
    }
}
