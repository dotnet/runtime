using Xunit;
using System.IO;
using System.Linq;
using Melanzana.CodeSign.Requirements;
using Melanzana.MachO;
using Melanzana.Streams;

namespace Melanzana.CodeSign.Tests
{
    public class ResignTest
    {
        [Fact]
        public void Resign()
        {
            // Read the test executable
            var aOutStream = typeof(ResignTest).Assembly.GetManifestResourceStream("Melanzana.CodeSign.Tests.Data.a.out")!;
            var objectFile = MachReader.Read(aOutStream).FirstOrDefault();
            Assert.NotNull(objectFile);

            // Strip the signature
            var codeSignature = objectFile!.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
            Assert.NotNull(codeSignature);
            var originalSignatureSize = codeSignature!.FileSize;
            objectFile!.LoadCommands.Remove(codeSignature);

            // Write the stripped file to disk
            var tempFileName = Path.GetTempFileName();
            using (var tempFile = new FileStream(tempFileName, FileMode.Create))
            {
                MachWriter.Write(tempFile, objectFile);
                Assert.Equal(aOutStream.Length - originalSignatureSize, tempFile.Length);
            }

            // Ad-hoc sign the file
            var codeSignOptions = new CodeSignOptions { };
            var signer = new Signer(codeSignOptions);
            signer.Sign(tempFileName);

            // TODO: Check signature

            File.Delete(tempFileName);
        }
    }
}
