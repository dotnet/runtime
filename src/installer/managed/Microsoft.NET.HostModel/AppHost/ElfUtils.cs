// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.NET.HostModel.AppHost
{
    internal static class ElfUtils
    {
        // The Linux Headers are copied from elf.h

#pragma warning disable 0649
        private struct ElfHeader
        {
            private byte EI_MAG0;
            private byte EI_MAG1;
            private byte EI_MAG2;
            private byte EI_MAG3;

            public bool IsValid()
            {
                return EI_MAG0 == 0x7f &&
                       EI_MAG1 == 0x45 &&
                       EI_MAG2 == 0x4C &&
                       EI_MAG3 == 0x46;
            }


        }
#pragma warning restore 0649

        public static bool IsElfImage(string filePath)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filePath)))
            {
                if (reader.BaseStream.Length < 16) // EI_NIDENT = 16
                {
                    return false;
                }

                byte[] eIdent = reader.ReadBytes(4);

                // Check that the first four bytes are 0x7f, 'E', 'L', 'F'
                return eIdent[0] == 0x7f &&
                       eIdent[1] == 0x45 &&
                       eIdent[2] == 0x4C &&
                       eIdent[3] == 0x46;
            }
        }
    }
}
