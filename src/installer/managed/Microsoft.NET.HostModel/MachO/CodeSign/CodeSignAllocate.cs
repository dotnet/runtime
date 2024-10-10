// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.NET.HostModel.MachO.CodeSign
{
    /// <summary>
    /// Rewriter for the Mach-O or universal binaries that resizes the code signature section
    /// and all related linker commands to make space for new signature.
    /// </summary>
    internal sealed class CodeSignAllocate
    {
        public MachObjectFile objectFile;

        public CodeSignAllocate(MachObjectFile objectFiles)
        {
            this.objectFile = objectFiles;
        }

        public MachCodeSignature SetCodeSignatureSize(uint codeSignatureSize)
        {
            return UpdateCodeSignatureLayout(codeSignatureSize);
        }

        public void AllocateSpace()
        {
            var codeSignature = objectFile.LoadCommands.OfType<MachCodeSignature>().Single();
            var requiredSize = codeSignature.FileOffset + codeSignature.FileSize;
            if (requiredSize <= objectFile.GetOriginalStream().Length)
            {
                return;
            }
            objectFile.SetStreamLength(requiredSize);
        }

        private MachCodeSignature UpdateCodeSignatureLayout(uint codeSignatureSize)
        {
            MachObjectFile machO = objectFile;
            var codeSignatureCommand = machO.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();

            if (codeSignatureCommand == null)
            {
                codeSignatureCommand = new MachCodeSignature(machO);
                codeSignatureCommand.Data.FileOffset = (uint)machO.GetSigningLimit();
                machO.LoadCommands.Add(codeSignatureCommand);
            }

            codeSignatureCommand.Data.Size = codeSignatureSize;
            return codeSignatureCommand;
        }
    }
}
