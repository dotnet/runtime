using Xunit;
using System.IO;
using System.Linq;
using Melanzana.MachO;
using Melanzana.Streams;
using System.Runtime.InteropServices;
using System.Diagnostics;

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

                Assert.True(Signer.TryRemoveCodesign(originalFileTmpName), unsignedFileTmpName);

                long strippedSize = new FileInfo(unsignedFileTmpName).Length;

                // RemoveCodesignIfPresent doesn't guarantee the file size will be smaller
                // Assert.True(strippedSize < originalSize);

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
                Assert.True(!Signer.TryRemoveCodesign(unsignedFileTmpName));

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

                Signer.AdHocSign(tmpFilePath);

                long signedSize = new FileInfo(tmpFilePath).Length;

                // If we can't validate the signature with codesign, at least make sure the file size has increased
                Assert.True(signedSize > strippedSize);

                if (Codesign.IsAvailable())
                {
                    var (exitCode, _) = Codesign.Run("--verify", tmpFilePath);
                    Assert.NotEqual(0, exitCode);
                }

            }
            finally
            {
                if (File.Exists(tmpFilePath))
                    File.Delete(tmpFilePath);
            }
        }
    }
}
