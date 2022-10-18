// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;

namespace LibObjectFile.Elf
{
    using static ElfNative;

    /// <summary>
    /// Extensions for <see cref="ElfObjectFile"/>
    /// </summary>
    public static class ElfObjectFileExtensions
    {
        /// <summary>
        /// Copy to an array buffer the ident array as found in ELF header <see cref="Elf32_Ehdr.e_ident"/>
        /// or <see cref="Elf64_Ehdr.e_ident"/>.
        /// </summary>
        /// <param name="objectFile">The object file to copy the ident from.</param>
        /// <param name="ident">A span receiving the ident. Must be >= 16 bytes length</param>
        public static void CopyIdentTo(this ElfObjectFile objectFile, Span<byte> ident)
        {
            if (objectFile == null) throw new ArgumentNullException(nameof(objectFile));
            if (ident.Length < EI_NIDENT)
            {
                throw new ArgumentException($"Expecting span length to be >= {EI_NIDENT}");
            }
            
            // Clear ident
            for (int i = 0; i < EI_NIDENT; i++)
            {
                ident[i] = 0;
            }

            ident[EI_MAG0] = ELFMAG0;
            ident[EI_MAG1] = ELFMAG1;
            ident[EI_MAG2] = ELFMAG2;
            ident[EI_MAG3] = ELFMAG3;
            ident[EI_CLASS] = (byte) objectFile.FileClass;
            ident[EI_DATA] = (byte) objectFile.Encoding;
            ident[EI_VERSION] = (byte)objectFile.Version;
            ident[EI_OSABI] = (byte)objectFile.OSABI.Value;
            ident[EI_ABIVERSION] = objectFile.AbiVersion;
        }

        /// <summary>
        /// Tries to copy from an ident array as found in ELF header <see cref="Elf32_Ehdr.e_ident"/> to this ELF object file instance.
        /// or <see cref="Elf64_Ehdr.e_ident"/>.
        /// </summary>
        /// <param name="objectFile">The object file to receive the ident from.</param>
        /// <param name="ident">A span to read from. Must be >= 16 bytes length</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <returns><c>true</c> if copying the ident was successful. <c>false</c> otherwise</returns>
        public static bool TryCopyIdentFrom(this ElfObjectFile objectFile, ReadOnlySpan<byte> ident, DiagnosticBag diagnostics)
        {
            if (objectFile == null) throw new ArgumentNullException(nameof(objectFile));
            if (ident.Length < EI_NIDENT)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderIdentLength, $"Invalid ELF Ident length found. Must be >= {EI_NIDENT}");
                return false;
            }

            if (ident[EI_MAG0] != ELFMAG0 || ident[EI_MAG1] != ELFMAG1 || ident[EI_MAG2] != ELFMAG2 || ident[EI_MAG3] != ELFMAG3)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderMagic, "Invalid ELF Magic found");
                return false;
            }

            CopyIndentFrom(objectFile, ident);
            return true;
        }

        internal static void CopyIndentFrom(this ElfObjectFile objectFile, ReadOnlySpan<byte> ident)
        {
            objectFile.FileClass = (ElfFileClass)ident[EI_CLASS];
            objectFile.Encoding = (ElfEncoding)ident[EI_DATA];
            objectFile.Version = ident[EI_VERSION];
            objectFile.OSABI = new ElfOSABIEx(ident[EI_OSABI]);
            objectFile.AbiVersion = ident[EI_ABIVERSION];
        }
    }
}