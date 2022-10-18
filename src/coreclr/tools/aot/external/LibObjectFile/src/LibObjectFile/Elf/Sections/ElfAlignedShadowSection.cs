// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A shadow section allowing to align the following section from <see cref="ElfObjectFile"/>
    /// to respect the <see cref="UpperAlignment"/> of this section.
    /// This section is used to make sure the offset of the following section will be respect
    /// a specific alignment.
    /// </summary>
    public sealed class ElfAlignedShadowSection : ElfShadowSection
    {
        public ElfAlignedShadowSection() : this(0x1000)
        {
        }

        public ElfAlignedShadowSection(uint upperAlignment)
        {
            UpperAlignment = upperAlignment;
        }

        /// <summary>
        /// Gets or sets teh alignment requirement that this section will ensure for the
        /// following sections placed after this section, so that the offset of the following
        /// section is respecting the alignment.
        /// </summary>
        public uint UpperAlignment { get; set; }
        
        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            var nextSectionOffset = AlignHelper.AlignToUpper(Offset, UpperAlignment);
            Size = nextSectionOffset - Offset;
            if (Size >= int.MaxValue)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidAlignmentOutOfRange, $"Invalid alignment 0x{UpperAlignment:x} resulting in an offset beyond int.MaxValue");
            }
        }

        protected override void Read(ElfReader reader)
        {
            throw new NotSupportedException($"An {nameof(ElfAlignedShadowSection)} does not support read and is only used for writing");
        }

        protected override void Write(ElfWriter writer)
        {
            if (Size == 0) return;

            var sharedBuffer = ArrayPool<byte>.Shared.Rent((int)Size);
            Array.Clear(sharedBuffer, 0, sharedBuffer.Length);
            try
            {
                writer.Stream.Write(sharedBuffer, 0, (int) Size);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }
    }
}