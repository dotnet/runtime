// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Context used when applying relocation via <see cref="ElfRelocationTableExtensions.Relocate"/>.
    /// </summary>
    public struct ElfRelocationContext
    {
        public ulong BaseAddress { get; set; }

        public ulong GlobalObjectTableAddress { get; set; }

        public ulong GlobalObjectTableOffset { get; set; }

        public ulong ProcedureLinkageTableAddress { get; set; }
    }
}