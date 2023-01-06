// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.NET.HostModel.AppHost
{
    internal static class ElfUtils
    {
        // First four bytes of valid ELF, as defined in https://github.com/torvalds/linux/blob/aae703b/include/uapi/linux/elf.h
        //    0x7f (DEL), 'E', 'L', 'F'
        private static ReadOnlySpan<byte> ElfMagic => "\u007f"u8 + "ELF"u8;

        public static bool IsElfImage(string filePath)
        {
            using FileStream fileStream = File.OpenRead(filePath);
            using BinaryReader reader = new(fileStream);

            if (reader.BaseStream.Length < 16) // EI_NIDENT = 16
            {
                return false;
            }

            byte[] eIdent = reader.ReadBytes(4);

            return
                eIdent[0] == ElfMagic[0] &&
                eIdent[1] == ElfMagic[1] &&
                eIdent[2] == ElfMagic[2] &&
                eIdent[3] == ElfMagic[3];
        }
    }
}
