// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using static System.Collections.Specialized.BitVector32;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Internal implementation of <see cref="ElfReader"/> to read from a stream to an <see cref="ElfObjectFile"/> instance.
    /// </summary>
    /// <typeparam name="TDecoder">The decoder used for LSB/MSB conversion</typeparam>
    internal abstract class ElfReader<TDecoder> : ElfReader where TDecoder : struct, IElfDecoder
    {
        private TDecoder _decoder;
        private ulong _startOfFile;
        private ushort _programHeaderCount;
        private uint _sectionHeaderCount;
        private uint _sectionStringTableIndex;
        private bool _isFirstSectionValidNull;
        private bool _hasValidSectionStringTable;

        protected ElfReader(ElfObjectFile objectFile, Stream stream, ElfReaderOptions options) : base(objectFile, stream, options)
        {
            _decoder = new TDecoder();
        }

        private ElfObjectFile.ElfObjectLayout Layout => ObjectFile.Layout;

        internal override void Read()
        {
            if (ObjectFile.FileClass == ElfFileClass.None)
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderFileClassNone, "Cannot read an ELF Class = None");
                throw new ObjectFileException($"Invalid {nameof(ElfObjectFile)}", Diagnostics);
            }

            _startOfFile = (ulong)Stream.Position;
            ReadElfHeader();
            ReadProgramHeaders();
            ReadSections();

            VerifyAndFixProgramHeadersAndSections();
        }
        
        private void ReadElfHeader()
        {
            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                ReadElfHeader32();
            }
            else
            {
                ReadElfHeader64();
            }

            if (_sectionHeaderCount >= ElfNative.SHN_LORESERVE)
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionHeaderCount, $"Invalid number `{_sectionHeaderCount}` of section headers found from Elf Header. Must be < {ElfNative.SHN_LORESERVE}");
            }
        }
        
        private unsafe void ReadElfHeader32()
        {
            ElfNative.Elf32_Ehdr hdr;
            ulong streamOffset = (ulong)Stream.Position;
            if (!TryReadData(sizeof(ElfNative.Elf32_Ehdr), out hdr))
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader32Size, $"Unable to read entirely Elf header. Not enough data (size: {sizeof(ElfNative.Elf32_Ehdr)}) read at offset {streamOffset} from the stream");
            }

            ObjectFile.FileType = (ElfFileType)_decoder.Decode(hdr.e_type);
            ObjectFile.Arch = new ElfArchEx(_decoder.Decode(hdr.e_machine));
            ObjectFile.Version = _decoder.Decode(hdr.e_version);

            ObjectFile.EntryPointAddress = _decoder.Decode(hdr.e_entry);
            Layout.SizeOfElfHeader = _decoder.Decode(hdr.e_ehsize);
            ObjectFile.Flags = _decoder.Decode(hdr.e_flags);

            // program headers
            Layout.OffsetOfProgramHeaderTable = _decoder.Decode(hdr.e_phoff);
            Layout.SizeOfProgramHeaderEntry = _decoder.Decode(hdr.e_phentsize);
            _programHeaderCount = _decoder.Decode(hdr.e_phnum);

            // entries for sections
            Layout.OffsetOfSectionHeaderTable = _decoder.Decode(hdr.e_shoff);
            Layout.SizeOfSectionHeaderEntry = _decoder.Decode(hdr.e_shentsize);
            _sectionHeaderCount = _decoder.Decode(hdr.e_shnum);
            _sectionStringTableIndex = _decoder.Decode(hdr.e_shstrndx);
        }

        private unsafe void ReadElfHeader64()
        {
            ElfNative.Elf64_Ehdr hdr;
            ulong streamOffset = (ulong)Stream.Position;
            if (!TryReadData(sizeof(ElfNative.Elf64_Ehdr), out hdr))
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteHeader64Size, $"Unable to read entirely Elf header. Not enough data (size: {sizeof(ElfNative.Elf64_Ehdr)}) read at offset {streamOffset} from the stream");
            }

            ObjectFile.FileType = (ElfFileType)_decoder.Decode(hdr.e_type);
            ObjectFile.Arch = new ElfArchEx(_decoder.Decode(hdr.e_machine));
            ObjectFile.Version = _decoder.Decode(hdr.e_version);

            ObjectFile.EntryPointAddress = _decoder.Decode(hdr.e_entry);
            Layout.SizeOfElfHeader = _decoder.Decode(hdr.e_ehsize);
            ObjectFile.Flags = _decoder.Decode(hdr.e_flags);

            // program headers
            Layout.OffsetOfProgramHeaderTable = _decoder.Decode(hdr.e_phoff);
            Layout.SizeOfProgramHeaderEntry = _decoder.Decode(hdr.e_phentsize);
            _programHeaderCount = _decoder.Decode(hdr.e_phnum);

            // entries for sections
            Layout.OffsetOfSectionHeaderTable = _decoder.Decode(hdr.e_shoff);
            Layout.SizeOfSectionHeaderEntry = _decoder.Decode(hdr.e_shentsize);
            _sectionHeaderCount = _decoder.Decode(hdr.e_shnum);
            _sectionStringTableIndex = _decoder.Decode(hdr.e_shstrndx);
        }

        private void ReadProgramHeaders()
        {
            if (Layout.SizeOfProgramHeaderEntry == 0)
            {
                if (_programHeaderCount > 0)
                {
                    Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidZeroProgramHeaderTableEntrySize, $"Unable to read program header table as the size of program header entry ({nameof(ElfNative.Elf32_Ehdr.e_phentsize)}) == 0 in the Elf Header");
                }
                return;
            }

            for (int i = 0; i < _programHeaderCount; i++)
            {
                var offset = Layout.OffsetOfProgramHeaderTable + (ulong)i * Layout.SizeOfProgramHeaderEntry;

                if (offset >= (ulong)Stream.Length)
                {
                    Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidProgramHeaderStreamOffset, $"Unable to read program header [{i}] as its offset {offset} is out of bounds");
                    break;
                }

                // Seek to the header position
                Stream.Position = (long)offset;

                var segment = (ObjectFile.FileClass == ElfFileClass.Is32) ? ReadProgramHeader32(i) : ReadProgramHeader64(i);
                ObjectFile.AddSegment(segment);
            }
        }
        
        private ElfSegment ReadProgramHeader32(int phdrIndex)
        {
            var streamOffset = Stream.Position;
            if (!TryReadData(Layout.SizeOfSectionHeaderEntry, out ElfNative.Elf32_Phdr hdr))
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteProgramHeader32Size, $"Unable to read entirely program header [{phdrIndex}]. Not enough data (size: {Layout.SizeOfProgramHeaderEntry}) read at offset {streamOffset} from the stream");
            }

            return new ElfSegment
            {
                Type = new ElfSegmentType(_decoder.Decode(hdr.p_type)),
                Offset =_decoder.Decode(hdr.p_offset),
                VirtualAddress = _decoder.Decode(hdr.p_vaddr),
                PhysicalAddress = _decoder.Decode(hdr.p_paddr),
                Size = _decoder.Decode(hdr.p_filesz),
                SizeInMemory = _decoder.Decode(hdr.p_memsz),
                Flags = new ElfSegmentFlags(_decoder.Decode(hdr.p_flags)),
                Alignment = _decoder.Decode(hdr.p_align)
            };
        }

        private ElfSegment ReadProgramHeader64(int phdrIndex)
        {
            var streamOffset = Stream.Position;
            if (!TryReadData(Layout.SizeOfSectionHeaderEntry, out ElfNative.Elf64_Phdr hdr))
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteProgramHeader64Size, $"Unable to read entirely program header [{phdrIndex}]. Not enough data (size: {Layout.SizeOfProgramHeaderEntry}) read at offset {streamOffset} from the stream");
            }

            return new ElfSegment
            {
                Type = new ElfSegmentType(_decoder.Decode(hdr.p_type)),
                Offset = _decoder.Decode(hdr.p_offset),
                VirtualAddress = _decoder.Decode(hdr.p_vaddr),
                PhysicalAddress = _decoder.Decode(hdr.p_paddr),
                Size = _decoder.Decode(hdr.p_filesz),
                SizeInMemory = _decoder.Decode(hdr.p_memsz),
                Flags = new ElfSegmentFlags(_decoder.Decode(hdr.p_flags)),
                Alignment = _decoder.Decode(hdr.p_align)
            };
        }
        
        private void ReadSections()
        {
            if (Layout.OffsetOfSectionHeaderTable == 0) return;

            // Write section header table
            ReadSectionHeaderTable();
        }

        private void ReadSectionHeaderTable()
        {
            if (Layout.SizeOfSectionHeaderEntry == 0)
            {
                if (_sectionHeaderCount > 0)
                {
                    Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidZeroSectionHeaderTableEntrySize, $"Unable to read section header table as the size of section header entry ({nameof(ElfNative.Elf32_Ehdr.e_ehsize)}) == 0 in the Elf Header");
                }
                return;
            }

            uint i = 0;

            if (_sectionHeaderCount == 0)
            {
                // We are dealing with an object file that has more than SHN_LORESERVE
                // (0xff00) sections. It has to begin with a NULL section header where
                // its Size contains the real number of sections, and Link optionally
                // points to string table section if it's section index is too high.
                if (ReadExtendedNullSectionTableEntry())
                {
                    i = 1;
                    ObjectFile.AddSection(new ElfNullSection());
                    _isFirstSectionValidNull = true;
                }
            }

            for (; i < _sectionHeaderCount; i++)
            {
                var offset = Layout.OffsetOfSectionHeaderTable + i * Layout.SizeOfSectionHeaderEntry;

                if (offset >= (ulong)Stream.Length)
                {
                    Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionHeaderStreamOffset, $"Unable to read section [{i}] as its offset {offset} is out of bounds");
                    break;
                }

                // Seek to the header position
                Stream.Position = (long)offset;

                var section = ReadSectionTableEntry(i);
                ObjectFile.AddSection(section);
            }
        }

        private ElfSection ReadSectionTableEntry(uint sectionIndex)
        {
            return ObjectFile.FileClass == ElfFileClass.Is32 ? ReadSectionTableEntry32(sectionIndex) : ReadSectionTableEntry64(sectionIndex);
        }

        private ElfSection ReadSectionTableEntry32(uint sectionIndex)
        {
            var streamOffset = Stream.Position;
            if (!TryReadData(Layout.SizeOfSectionHeaderEntry, out ElfNative.Elf32_Shdr rawSection))
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteSectionHeader32Size, $"Unable to read entirely section header [{sectionIndex}]. Not enough data (size: {Layout.SizeOfSectionHeaderEntry}) read at offset {streamOffset} from the stream");
            }

            if (sectionIndex == 0)
            {
                _isFirstSectionValidNull = rawSection.IsNull;
            }
            
            var sectionType = (ElfSectionType)_decoder.Decode(rawSection.sh_type);
            bool isValidNullSection = sectionIndex == 0 && rawSection.IsNull;
            var section = CreateElfSection(sectionIndex, sectionType, isValidNullSection);

            if (!isValidNullSection)
            {
                section.Name = new ElfString(_decoder.Decode(rawSection.sh_name));
                section.Type = (ElfSectionType)_decoder.Decode(rawSection.sh_type);
                section.Flags = (ElfSectionFlags)_decoder.Decode(rawSection.sh_flags);
                section.VirtualAddress = _decoder.Decode(rawSection.sh_addr);
                section.Offset = _decoder.Decode(rawSection.sh_offset);
                section.Alignment = _decoder.Decode(rawSection.sh_addralign);
                section.Link = new ElfSectionLink(_decoder.Decode(rawSection.sh_link));
                section.Info = new ElfSectionLink(_decoder.Decode(rawSection.sh_info));
                section.Size = _decoder.Decode(rawSection.sh_size);
                section.OriginalTableEntrySize = _decoder.Decode(rawSection.sh_entsize);
            }

            return section;
        }

        private ElfSection ReadSectionTableEntry64(uint sectionIndex)
        {
            var streamOffset = Stream.Position;
            if (!TryReadData(Layout.SizeOfSectionHeaderEntry, out ElfNative.Elf64_Shdr rawSection))
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteSectionHeader64Size, $"Unable to read entirely section header [{sectionIndex}]. Not enough data (size: {Layout.SizeOfSectionHeaderEntry}) read at offset {streamOffset} from the stream");
            }

            if (sectionIndex == 0)
            {
                _isFirstSectionValidNull = rawSection.IsNull;
            }

            var sectionType = (ElfSectionType)_decoder.Decode(rawSection.sh_type);
            bool isValidNullSection = sectionIndex == 0 && rawSection.IsNull;
            var section = CreateElfSection(sectionIndex, sectionType, sectionIndex == 0 && rawSection.IsNull);

            if (!isValidNullSection)
            {
                section.Name = new ElfString(_decoder.Decode(rawSection.sh_name));
                section.Type = (ElfSectionType)_decoder.Decode(rawSection.sh_type);
                section.Flags = (ElfSectionFlags)_decoder.Decode(rawSection.sh_flags);
                section.VirtualAddress = _decoder.Decode(rawSection.sh_addr);
                section.Offset = _decoder.Decode(rawSection.sh_offset);
                section.Alignment = _decoder.Decode(rawSection.sh_addralign);
                section.Link = new ElfSectionLink(_decoder.Decode(rawSection.sh_link));
                section.Info = new ElfSectionLink(_decoder.Decode(rawSection.sh_info));
                section.Size = _decoder.Decode(rawSection.sh_size);
                section.OriginalTableEntrySize = _decoder.Decode(rawSection.sh_entsize);
            }

            return section;
        }

        private bool ReadExtendedNullSectionTableEntry()
        {
            uint sh_type;
            ulong sh_size;
            uint sh_link;
            bool isNull;

            Stream.Position = (long)Layout.OffsetOfSectionHeaderTable;

            if (ObjectFile.FileClass == ElfFileClass.Is32)
            {
                
                if (!TryReadData(Layout.SizeOfSectionHeaderEntry, out ElfNative.Elf32_Shdr rawSection32))
                {
                    Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteSectionHeader32Size, $"Unable to read entirely NULL section header. Not enough data (size: {Layout.SizeOfSectionHeaderEntry}) read at offset {Layout.OffsetOfSectionHeaderTable} from the stream");
                    return false;
                }

                sh_type = _decoder.Decode(rawSection32.sh_type);
                sh_size = _decoder.Decode(rawSection32.sh_size);
                sh_link = _decoder.Decode(rawSection32.sh_link);
                rawSection32.sh_size = 0;
                rawSection32.sh_link = 0;
                isNull = rawSection32.IsNull;
            }
            else
            {
                if (!TryReadData(Layout.SizeOfSectionHeaderEntry, out ElfNative.Elf64_Shdr rawSection64))
                {
                    Diagnostics.Error(DiagnosticId.ELF_ERR_IncompleteSectionHeader64Size, $"Unable to read entirely NULL section header. Not enough data (size: {Layout.SizeOfSectionHeaderEntry}) read at offset {Layout.OffsetOfSectionHeaderTable} from the stream");
                    return false;
                }

                sh_type = _decoder.Decode(rawSection64.sh_type);
                sh_size = _decoder.Decode(rawSection64.sh_size);
                sh_link = _decoder.Decode(rawSection64.sh_link);
                rawSection64.sh_size = 0;
                rawSection64.sh_link = 0;
                isNull = rawSection64.IsNull;
            }

            if (!isNull)
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidFirstSectionExpectingUndefined, $"Invalid Section [0] {(ElfSectionType)sh_type}. Expecting {ElfNative.SHN_UNDEF}");
                return false;
            }

            if (sh_size >= uint.MaxValue)
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionHeaderCount, $"Extended section count [{sh_size}] exceeds {uint.MaxValue}");
                return false;
            }

            _sectionHeaderCount = (uint)sh_size;
            if (_sectionStringTableIndex == ElfNative.SHN_XINDEX)
            {
                _sectionStringTableIndex = sh_link;
            }

            return true;
        }
        
        public override ElfSectionLink ResolveLink(ElfSectionLink link, string errorMessageFormat)
        {
            if (errorMessageFormat == null) throw new ArgumentNullException(nameof(errorMessageFormat));

            // Connect section Link instance
            if (!link.IsEmpty)
            {
                if (link.SpecialIndex == _sectionStringTableIndex)
                {
                    link = new ElfSectionLink(ObjectFile.SectionHeaderStringTable);
                }
                else
                {
                    var sectionIndex = link.SpecialIndex;

                    bool sectionFound = false;
                    if (sectionIndex < ObjectFile.Sections.Count && ObjectFile.Sections[(int)sectionIndex].SectionIndex == sectionIndex)
                    {
                        link = new ElfSectionLink(ObjectFile.Sections[(int)sectionIndex]);
                        sectionFound = true;
                    }
                    else
                    {
                        foreach (var section in ObjectFile.Sections)
                        {
                            if (section.SectionIndex == sectionIndex)
                            {
                                link = new ElfSectionLink(section);
                                sectionFound = true;
                                break;
                            }
                        }
                    }

                    if (!sectionFound)
                    {
                        Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidResolvedLink, string.Format(errorMessageFormat, link.SpecialIndex));
                    }
                }
            }

            return link;
        }
       
        private void VerifyAndFixProgramHeadersAndSections()
        {
            if (!_isFirstSectionValidNull && ObjectFile.Sections.Count > 0)
            {
                Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidFirstSectionExpectingUndefined, $"Invalid Section [0] {ObjectFile.Sections[0].Type}. Expecting {ElfNative.SHN_UNDEF}");
            }

            if (_hasValidSectionStringTable)
            {
                Stream.Position = (long)ObjectFile.SectionHeaderStringTable.Offset;
                ObjectFile.SectionHeaderStringTable.ReadInternal(this);
            }

            for (var i = 0; i < ObjectFile.Sections.Count; i++)
            {
                var section = ObjectFile.Sections[i];
                if (section is ElfNullSection || section is ElfProgramHeaderTable) continue;

                // Resolve the name of the section
                if (ObjectFile.SectionHeaderStringTable != null && ObjectFile.SectionHeaderStringTable.TryFind(section.Name.Index, out var sectionName))
                {
                    section.Name = section.Name.WithName(sectionName);
                }
                else
                {
                    if (ObjectFile.SectionHeaderStringTable == null)
                    {
                        Diagnostics.Warning(DiagnosticId.ELF_ERR_InvalidStringIndexMissingStringHeaderTable, $"Unable to resolve string index [{section.Name.Index}] for section [{section.Index}] as section header string table does not exist");
                    }
                    else
                    {
                        Diagnostics.Warning(DiagnosticId.ELF_ERR_InvalidStringIndex, $"Unable to resolve string index [{section.Name.Index}] for section [{section.Index}] from section header string table");
                    }
                }

                // Connect section Link instance
                section.Link = ResolveLink(section.Link, $"Invalid section Link [{{0}}] for section [{i}]");

                // Connect section Info instance
                if (section.Type != ElfSectionType.DynamicLinkerSymbolTable && section.Type != ElfSectionType.SymbolTable)
                {
                    section.Info = ResolveLink(section.Info, $"Invalid section Info [{{0}}] for section [{i}]");
                }

                if (i == 0 && _isFirstSectionValidNull)
                {
                    continue;
                }

                if (i == _sectionStringTableIndex && _hasValidSectionStringTable)
                {
                    continue;
                }

                if (section.HasContent)
                {
                    Stream.Position = (long)section.Offset;
                    section.ReadInternal(this);
                }
            }

            foreach (var section in ObjectFile.Sections)
            {
                section.AfterReadInternal(this);
            }

            var fileParts = new ElfFilePartList(ObjectFile.Sections.Count + ObjectFile.Segments.Count);

            if (_isFirstSectionValidNull)
            {
                var programHeaderTable = new ElfProgramHeaderTable()
                {
                    Offset = Layout.OffsetOfProgramHeaderTable,
                };

                // Add the shadow section ElfProgramHeaderTable
                ObjectFile.InsertSectionAt(1, programHeaderTable);
                programHeaderTable.UpdateLayout(Diagnostics);

                if (programHeaderTable.Size > 0)
                {
                    fileParts.Insert(new ElfFilePart(programHeaderTable));
                }
            }

            // Make sure to pre-sort all sections by offset
            var orderedSections = new List<ElfSection>(ObjectFile.Sections.Count);
            orderedSections.AddRange(ObjectFile.Sections);
            orderedSections.Sort(CompareSectionOffsetsAndSizesDelegate);
            // Store the stream index to recover the same order when saving back.
            for(int i = 0; i < orderedSections.Count; i++)
            {
                orderedSections[i].StreamIndex = (uint)i;
            }

            // Lastly verify integrity of all sections
            bool hasShadowSections = false;

            var lastOffset = fileParts.Count > 0 ? fileParts[fileParts.Count - 1].EndOffset : 0;
            for (var i = 0; i < orderedSections.Count; i++)
            {
                var section = orderedSections[i];
                section.Verify(this.Diagnostics);

                if (lastOffset > 0 && section.Offset > lastOffset)
                {
                    if (section.Offset > lastOffset)
                    {
                        // Create parts for the segment
                        fileParts.CreateParts(lastOffset + 1, section.Offset - 1);
                        hasShadowSections = true;
                    }
                }

                if (section.Size == 0 || !section.HasContent)
                {
                    continue;
                }

                // Collect sections parts
                fileParts.Insert(new ElfFilePart(section));
                lastOffset = section.Offset + section.Size - 1;

                // Verify overlapping sections and generate and error
                if (i + 1 < orderedSections.Count)
                {
                    var otherSection = orderedSections[i + 1];
                    if (otherSection.Offset < section.Offset + section.Size)
                    {
                        Diagnostics.Warning(DiagnosticId.ELF_ERR_InvalidOverlappingSections, $"The section {section} [{section.Offset} : {section.Offset + section.Size - 1}] is overlapping with the section {otherSection} [{otherSection.Offset} : {otherSection.Offset + otherSection.Size - 1}]");
                    }
                }
            }
            
            // Link segments to sections if we have an exact match.
            // otherwise record any segments that are not bound to a section.

            foreach (var segment in ObjectFile.Segments)
            {
                if (segment.Size == 0) continue;

                var segmentEndOffset = segment.Offset + segment.Size - 1;
                foreach (var section in orderedSections)
                {
                    if (section.Size == 0 || !section.HasContent) continue;

                    var sectionEndOffset = section.Offset + section.Size - 1;
                    if (segment.Offset == section.Offset && segmentEndOffset == sectionEndOffset)
                    {
                        // Single case: segment == section
                        // If we found a section, we will bind the program header to this section
                        // and switch the offset calculation to auto
                        segment.Range = section;
                        segment.OffsetKind = ValueKind.Auto;
                        break;
                    }
                }

                if (segment.Range.IsEmpty)
                {
                    var offset = segment.Offset;

                    // If a segment offset is set to 0, we need to take into
                    // account the fact that the Elf header is already being handled
                    // so we should not try to create a shadow section for it
                    if (offset < Layout.SizeOfElfHeader)
                    {
                        offset = Layout.SizeOfElfHeader;
                    }
                    
                    // Create parts for the segment
                    fileParts.CreateParts(offset, segmentEndOffset);
                    hasShadowSections = true;
                }
            }

            // If the previous loop has created ElfFilePart, we have to 
            // create ElfCustomShadowSection and update the ElfSegment.Range
            if (hasShadowSections)
            {
                int shadowCount = 0;
                // If we have sections and the first section is NULL valid, we can start inserting
                // shadow sections at index 1 (after null section), otherwise we can insert shadow
                // sections before.
                uint previousSectionIndex = _isFirstSectionValidNull ? 1U : 0U;

                // Create ElfCustomShadowSection for any parts in the file
                // that are referenced by a segment but doesn't have a section
                for (var i = 0; i < fileParts.Count; i++)
                {
                    var part = fileParts[i];
                    if (part.Section == null)
                    {
                        var shadowSection = new ElfBinaryShadowSection()
                        {
                            Name = ".shadow." + shadowCount,
                            Offset = part.StartOffset, 
                            Size = part.EndOffset - part.StartOffset + 1
                        };
                        shadowCount++;

                        Stream.Position = (long)shadowSection.Offset;
                        shadowSection.ReadInternal(this);

                        // Insert the shadow section with this order
                        shadowSection.StreamIndex = previousSectionIndex;
                        for (int j = (int)previousSectionIndex; j < orderedSections.Count; j++)
                        {
                            var otherSection = orderedSections[j];
                            otherSection.StreamIndex++;
                        }
                        // Update ordered sections
                        orderedSections.Insert((int)previousSectionIndex, shadowSection);
                        ObjectFile.AddSection(shadowSection);

                        fileParts[i] = new ElfFilePart(shadowSection);
                    }
                    else
                    {
                        previousSectionIndex = part.Section.StreamIndex + 1;
                    }
                }

                // Update all segment Ranges
                foreach (var segment in ObjectFile.Segments)
                {
                    if (segment.Size == 0) continue;
                    if (!segment.Range.IsEmpty) continue;

                    var segmentEndOffset = segment.Offset + segment.Size - 1;
                    for (var i = 0; i < orderedSections.Count; i++)
                    {
                        var section = orderedSections[i];
                        if (section.Size == 0 || !section.HasContent) continue;

                        var sectionEndOffset = section.Offset + section.Size - 1;
                        if (segment.Offset >= section.Offset && segment.Offset <= sectionEndOffset)
                        {
                            ElfSection beginSection = section;
                            ElfSection endSection = null;
                            for (int j = i; j < orderedSections.Count; j++)
                            {
                                var nextSection = orderedSections[j];
                                if (nextSection.Size == 0 || !nextSection.HasContent) continue;

                                sectionEndOffset = nextSection.Offset + nextSection.Size - 1;

                                if (segmentEndOffset >= nextSection.Offset && segmentEndOffset <= sectionEndOffset)
                                {
                                    endSection = nextSection;
                                    break;
                                }
                            }

                            if (endSection == null)
                            {
                                // TODO: is it a throw/assert or a log?
                                Diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSegmentRange, $"Invalid range for {segment}. The range is set to empty");
                            }
                            else
                            {
                                segment.Range = new ElfSegmentRange(beginSection, segment.Offset - beginSection.Offset, endSection, (long)(segmentEndOffset - endSection.Offset));
                            }

                            segment.OffsetKind = ValueKind.Auto;
                            break;
                        }
                    }
                }
            }
        }
        
        private ElfSection CreateElfSection(uint sectionIndex, ElfSectionType sectionType, bool isNullSection)
        {
            ElfSection section = null;

            switch (sectionType)
            {
                case ElfSectionType.Null:
                    if (isNullSection)
                    {
                        section = new ElfNullSection();
                    }
                    break;
                case ElfSectionType.DynamicLinkerSymbolTable:
                case ElfSectionType.SymbolTable:
                    section = new ElfSymbolTable();
                    break;
                case ElfSectionType.StringTable:

                    if (sectionIndex == _sectionStringTableIndex)
                    {
                        _hasValidSectionStringTable = true;
                        section = new ElfSectionHeaderStringTable();
                    }
                    else
                    {
                        section = new ElfStringTable();
                    }
                    break;
                case ElfSectionType.Relocation:
                case ElfSectionType.RelocationAddends:
                    section = new ElfRelocationTable();
                    break;
                case ElfSectionType.Note:
                    section = new ElfNoteTable();
                    break;
                case ElfSectionType.SymbolTableSectionHeaderIndices:
                    section = new ElfSymbolTableSectionHeaderIndices();
                    break;
            }

            // If the section is not a builtin section, try to offload to a delegate
            // or use the default ElfCustomSection.
            if (section == null)
            {
                if (Options.TryCreateSection != null)
                {
                    section = Options.TryCreateSection(sectionType, Diagnostics);
                }

                if (section == null)
                {
                    section = new ElfBinarySection();
                }
            }

            return section;
        }
        
        public override ushort Decode(ElfNative.Elf32_Half src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(ElfNative.Elf64_Half src)
        {
            return _decoder.Decode(src);
        }

        public override uint Decode(ElfNative.Elf32_Word src)
        {
            return _decoder.Decode(src);
        }

        public override uint Decode(ElfNative.Elf64_Word src)
        {
            return _decoder.Decode(src);
        }

        public override int Decode(ElfNative.Elf32_Sword src)
        {
            return _decoder.Decode(src);
        }

        public override int Decode(ElfNative.Elf64_Sword src)
        {
            return _decoder.Decode(src);
        }

        public override ulong Decode(ElfNative.Elf32_Xword src)
        {
            return _decoder.Decode(src);
        }

        public override long Decode(ElfNative.Elf32_Sxword src)
        {
            return _decoder.Decode(src);
        }

        public override ulong Decode(ElfNative.Elf64_Xword src)
        {
            return _decoder.Decode(src);
        }

        public override long Decode(ElfNative.Elf64_Sxword src)
        {
            return _decoder.Decode(src);
        }

        public override uint Decode(ElfNative.Elf32_Addr src)
        {
            return _decoder.Decode(src);
        }

        public override ulong Decode(ElfNative.Elf64_Addr src)
        {
            return _decoder.Decode(src);
        }

        public override uint Decode(ElfNative.Elf32_Off src)
        {
            return _decoder.Decode(src);
        }

        public override ulong Decode(ElfNative.Elf64_Off src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(ElfNative.Elf32_Section src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(ElfNative.Elf64_Section src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(ElfNative.Elf32_Versym src)
        {
            return _decoder.Decode(src);
        }

        public override ushort Decode(ElfNative.Elf64_Versym src)
        {
            return _decoder.Decode(src);
        }

        private static readonly Comparison<ElfSection> CompareSectionOffsetsAndSizesDelegate = new Comparison<ElfSection>(CompareSectionOffsetsAndSizes);

        private static int CompareSectionOffsetsAndSizes(ElfSection left, ElfSection right)
        {
            int result = left.Offset.CompareTo(right.Offset);
            if (result == 0)
            {
                result = left.Size.CompareTo(right.Size);
            }
            return result;
        }
    }
}