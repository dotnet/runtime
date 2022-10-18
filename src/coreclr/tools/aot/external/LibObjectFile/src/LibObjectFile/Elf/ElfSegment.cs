// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines a segment or program header.
    /// </summary>
    public sealed class ElfSegment : ElfObject
    {
        public ValueKind OffsetKind { get; set; }
        
        /// <summary>
        /// Gets or sets the type of this segment.
        /// </summary>
        public ElfSegmentType Type { get; set; }

        /// <summary>
        /// Gets or sets the range of section this segment applies to.
        /// It can applies to <see cref="ElfShadowSection"/>.
        /// </summary>
        public ElfSegmentRange Range { get; set; }

        /// <summary>
        /// Gets or sets the virtual address.
        /// </summary>
        public ulong VirtualAddress { get; set; }

        /// <summary>
        /// Gets or sets the physical address.
        /// </summary>
        public ulong PhysicalAddress { get; set; }
        
        /// <summary>
        /// Gets or sets the size in bytes occupied in memory by this segment.
        /// </summary>
        public ulong SizeInMemory { get; set; }

        /// <summary>
        /// Gets or sets the flags of this segment.
        /// </summary>
        public ElfSegmentFlags Flags { get; set; }

        /// <summary>
        /// Gets the alignment requirement of this section.
        /// </summary>
        public ulong Alignment { get; set; }

        public override void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            if (OffsetKind == ValueKind.Auto)
            {
                Offset = Range.Offset;
            }
            
            if (Range.IsEmpty)
            {
                //diagnostics.Error($"Invalid empty {nameof(Range)} in {this}. An {nameof(ElfSegment)} requires to be attached to a section or a range of section or a {nameof(ElfShadowSection)}");
            }
            else
            {
                Size = Range.Size;

                // TODO: Add checks that Alignment is Power Of 2
                var alignment = Alignment == 0 ? Alignment = 1 : Alignment;
                if (!AlignHelper.IsPowerOfTwo(alignment))
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentAlignmentForLoad, $"Invalid segment alignment requirements: Alignment = {alignment} must be a power of 2");
                }

                if (Range.BeginSection.Parent == null)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeBeginSectionParent, $"Invalid null parent {nameof(Range)}.{nameof(Range.BeginSection)} in {this}. The section must be attached to the same {nameof(ElfObjectFile)} than this instance");
                }

                if (Range.EndSection.Parent == null)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndSectionParent, $"Invalid null parent {nameof(Range)}.{nameof(Range.EndSection)} in {this}. The section must be attached to the same {nameof(ElfObjectFile)} than this instance");
                }

                if (Type == ElfSegmentTypeCore.Load)
                {
                    // Specs:
                    // As ‘‘Program Loading’’ later in this part describes, loadable process segments must have congruent values for p_vaddr and p_offset, modulo the page size.
                    // TODO: how to make this configurable?
                    if ((alignment % 4096) != 0)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentAlignmentForLoad, $"Invalid {nameof(ElfNative.PT_LOAD)} segment alignment requirements: {alignment} must be multiple of the Page Size {4096}");
                    }

                    var mod = (VirtualAddress - Range.Offset) & (alignment - 1);
                    if (mod != 0)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentVirtualAddressOrOffset, $"Invalid {nameof(ElfNative.PT_LOAD)} segment alignment requirements: (VirtualAddress - Range.Offset) & (Alignment - 1) == {mod}  while it must be == 0");
                    }
                }

                if (Size > 0)
                {
                    if (Range.BeginOffset >= Range.BeginSection.Size)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeBeginOffset, $"Invalid {nameof(Range)}.{nameof(Range.BeginOffset)}: {Range.BeginOffset} cannot be >= {nameof(Range.BeginSection)}.{nameof(ElfSection.Size)}: {Range.BeginSection.Size} in {this}. The offset must be within the section");
                    }
                    if ((Range.EndOffset >= 0 && (ulong)Range.EndOffset >= Range.EndSection.Size))
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndOffset, $"Invalid {nameof(Range)}.{nameof(Range.EndOffset)}: {Range.EndOffset} cannot be >= {nameof(Range)}.{nameof(ElfSegmentRange.EndSection)}.{nameof(ElfSection.Size)}: {Range.EndSection.Size} in {this}. The offset must be within the section");
                    }
                    else if (Range.EndOffset < 0)
                    {
                        var endOffset = (long)Range.EndSection.Size + Range.EndOffset;
                        if (endOffset < 0)
                        {
                            diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeEndOffset, $"Invalid relative {nameof(Range)}.{nameof(Range.EndOffset)}: {Range.EndOffset}. The resulting end offset {endOffset} with {nameof(Range)}.{nameof(ElfSegmentRange.EndSection)}.{nameof(ElfSection.Size)}: {Range.EndSection.Size} cannot be < 0 in {this}. The offset must be within the section");
                        }
                    }
                }

                if (Range.BeginSection.Parent != null && Range.EndSection.Parent != null)
                {
                    if (Range.BeginSection.Index > Range.EndSection.Index)
                    {
                        diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRangeIndices, $"Invalid index order between {nameof(Range)}.{nameof(ElfSegmentRange.BeginSection)}.{nameof(ElfSegment.Index)}: {Range.BeginSection.Index} and {nameof(Range)}.{nameof(ElfSegmentRange.EndSection)}.{nameof(ElfSegment.Index)}: {Range.EndSection.Index} in {this}. The from index must be <= to the end index.");
                    }
                }
            }
        }


        public override string ToString()
        {
            return $"Segment [{Index}]";
        }
    }
}