using Xunit;
using System.IO;
using System.Linq;
using Melanzana.MachO;
using Melanzana.Streams;
using System.Runtime.InteropServices;
using System.Diagnostics;

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

            if (IsCodesignAvailable())
            {
                var (exitCode, _) = RunCodesign("--verify", tempFileName);
                Assert.NotEqual(0, exitCode);
            }

            // Ad-hoc sign the file
            Signer.AdHocSign(tempFileName);

            // TODO: Check signature
            if (IsCodesignAvailable())
            {
                var (exitCode, _) = RunCodesign("--verify", tempFileName);
                Assert.Equal(0, exitCode);
            }
            File.Delete(tempFileName);
        }

        private const string CodesignPath = @"/usr/bin/codesign";

        public static bool IsCodesignAvailable() => File.Exists(CodesignPath);

        public static (int ExitCode, string StdErr) RunCodesign(string args, string appHostPath)
        {
            Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            Debug.Assert(IsCodesignAvailable());

            var psi = new ProcessStartInfo()
            {
                Arguments = $"{args} \"{appHostPath}\"",
                FileName = CodesignPath,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var p = Process.Start(psi))
            {
                if (p == null)
                    return (-1, "Failed to start process");
                p.WaitForExit();
                return (p.ExitCode, p.StandardError.ReadToEnd());
            }
        }
    }
}
