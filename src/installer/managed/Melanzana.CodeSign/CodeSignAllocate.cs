using Melanzana.MachO;
using Melanzana.Streams;

namespace Melanzana.CodeSign
{
    /// <summary>
    /// Rewriter for the Mach-O or universal binaries that resizes the code signature section
    /// and all related linker commands to make space for new signature.
    /// </summary>
    public class CodeSignAllocate
    {
        public IList<MachObjectFile> objectFiles;

        public CodeSignAllocate(IList<MachObjectFile> objectFiles)
        {
            this.objectFiles = objectFiles;
        }

        public void SetArchSize(MachObjectFile machO, uint codeSignatureSize)
        {
            // Page alignment
            codeSignatureSize = (codeSignatureSize + 0x3fffu) & ~0x3fffu;

            UpdateCodeSignatureLayout(machO, codeSignatureSize);
        }

        public string Allocate()
        {
            var tempFileName = Path.GetTempFileName();
            using var output = File.OpenWrite(tempFileName);
            MachWriter.Write(output, objectFiles);
            return tempFileName;
        }

        private static void UpdateCodeSignatureLayout(MachObjectFile machO, uint codeSignatureSize)
        {
            var linkEditSegment = machO.LoadCommands.OfType<MachSegment>().First(s => s.Name == "__LINKEDIT");
            var codeSignatureCommand = machO.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();

            if (codeSignatureCommand == null)
            {
                codeSignatureCommand = new MachCodeSignature(machO);
                codeSignatureCommand.Data.FileOffset = (uint)machO.GetSigningLimit();
                machO.LoadCommands.Add(codeSignatureCommand);
            }

            codeSignatureCommand.Data.Size = codeSignatureSize;
        }
    }
}