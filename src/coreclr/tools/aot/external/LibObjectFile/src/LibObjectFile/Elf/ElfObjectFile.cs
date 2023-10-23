// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LibObjectFile.Utils;

namespace LibObjectFile.Elf
{
    using static ElfNative;

    /// <summary>
    /// Defines an ELF object file that can be manipulated in memory.
    /// </summary>
    public sealed class ElfObjectFile : ObjectFileNode
    {
        private readonly List<ElfSection> _sections;
        private ElfSectionHeaderStringTable _sectionHeaderStringTable;
        private readonly List<ElfSegment> _segments;

        public const int IdentSizeInBytes = ElfNative.EI_NIDENT;

        /// <summary>
        /// Creates a new instance with the default sections (null and a shadow program header table).
        /// </summary>
        public ElfObjectFile(ElfArch arch) : this(true)
        {
            Arch = arch;
            switch (arch)
            {
                case ElfArch.I386:
                    FileClass = ElfFileClass.Is32;
                    Encoding = ElfEncoding.Lsb;
                    break;
                case ElfArch.X86_64:
                    FileClass = ElfFileClass.Is64;
                    Encoding = ElfEncoding.Lsb;
                    break;
                case ElfArch.ARM:
                    FileClass = ElfFileClass.Is32;
                    Encoding = ElfEncoding.Lsb; // not 100% valid, but ok for a default
                    break;
                case ElfArch.AARCH64:
                    FileClass = ElfFileClass.Is64;
                    Encoding = ElfEncoding.Lsb; // not 100% valid, but ok for a default
                    break;

                // TODO: Add support for more arch
            }
            Version = ElfNative.EV_CURRENT;
            FileType = ElfFileType.Relocatable;
        }

        internal ElfObjectFile(bool addDefaultSections)
        {
            _segments = new List<ElfSegment>();
            _sections = new List<ElfSection>();
            Layout = new ElfObjectLayout();

            if (addDefaultSections)
            {
                AddSection(new ElfNullSection());
                AddSection(new ElfProgramHeaderTable());
            }
        }

        /// <summary>
        /// Gets or sets the file class (i.e. 32 or 64 bits)
        /// </summary>
        public ElfFileClass FileClass { get; set; }

        /// <summary>
        /// Gets or sets the file encoding (i.e. LSB or MSB)
        /// </summary>
        public ElfEncoding Encoding { get; set; }

        /// <summary>
        /// Gets or sets the version of this file.
        /// </summary>
        public uint Version { get; set; }

        /// <summary>
        /// Gets or sets the OS ABI.
        /// </summary>
        public ElfOSABIEx OSABI { get; set; }

        /// <summary>
        /// Gets or sets the OS ABI version.
        /// </summary>
        public byte AbiVersion { get; set; }

        /// <summary>
        /// Gets or sets the file type (e.g executable, relocatable...)
        /// From Elf Header equivalent of <see cref="ElfNative.Elf32_Ehdr.e_type"/> or <see cref="ElfNative.Elf64_Ehdr.e_type"/>.
        /// </summary>
        public ElfFileType FileType { get; set; }

        /// <summary>
        /// Gets or sets the file flags (not used).
        /// </summary>
        public ElfHeaderFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets the machine architecture (e.g 386, X86_64...)
        /// From Elf Header equivalent of <see cref="ElfNative.Elf32_Ehdr.e_machine"/> or <see cref="ElfNative.Elf64_Ehdr.e_machine"/>.
        /// </summary>
        public ElfArchEx Arch { get; set; }

        /// <summary>
        /// Entry point virtual address.
        /// From Elf Header equivalent of <see cref="ElfNative.Elf32_Ehdr.e_entry"/> or <see cref="ElfNative.Elf64_Ehdr.e_entry"/>.
        /// </summary>
        public ulong EntryPointAddress { get; set; }

        /// <summary>
        /// List of the segments - program headers defined by this instance.
        /// </summary>
        public IReadOnlyList<ElfSegment> Segments => _segments;

        /// <summary>
        /// List of the sections - program headers defined by this instance.
        /// </summary>
        public IReadOnlyList<ElfSection> Sections => _sections;
        
        /// <summary>
        /// Number of visible sections excluding <see cref="ElfShadowSection"/> in the <see cref="Sections"/>.
        /// </summary>
        public uint VisibleSectionCount { get; private set; }

        /// <summary>
        /// Number of <see cref="ElfShadowSection"/> in the <see cref="Sections"/>
        /// </summary>
        public uint ShadowSectionCount { get; private set; }

        /// <summary>
        /// Gets or sets the section header string table used to store the names of the sections.
        /// Must have been added to <see cref="Sections"/>.
        /// </summary>
        public ElfSectionHeaderStringTable SectionHeaderStringTable
        {
            get => _sectionHeaderStringTable;
            set
            {
                if (value != null)
                {
                    if (value.Parent == null)
                    {
                        throw new InvalidOperationException($"The {nameof(ElfSectionHeaderStringTable)} must have been added via `this.{nameof(AddSection)}(section)` before setting {nameof(SectionHeaderStringTable)}");
                    }

                    if (value.Parent != this)
                    {
                        throw new InvalidOperationException($"This {nameof(ElfSectionHeaderStringTable)} belongs already to another {nameof(ElfObjectFile)}. It must be removed from the other instance before adding it to this instance.");
                    }
                }
                _sectionHeaderStringTable = value;
            }
        }

        /// <summary>
        /// Gets the current calculated layout of this instance (e.g offset of the program header table)
        /// </summary>
        public ElfObjectLayout Layout { get; }

        /// <summary>
        /// Verifies the integrity of this ELF object file.
        /// </summary>
        /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
        public override void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            if (FileClass == ElfFileClass.None)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderFileClassNone, $"Cannot compute the layout with an {nameof(ElfObjectFile)} having a {nameof(FileClass)} == {ElfFileClass.None}");
            }

            if (VisibleSectionCount >= ElfNative.SHN_LORESERVE &&
                Sections[0] is not ElfNullSection)
            {
                diagnostics.Error(DiagnosticId.ELF_ERR_MissingNullSection, $"Section count is higher than SHN_LORESERVE ({ElfNative.SHN_LORESERVE}) but the first section is not a NULL section");                
            }

            foreach (var segment in Segments)
            {
                segment.Verify(diagnostics);
            }

            // Verify all sections before doing anything else
            foreach (var section in Sections)
            {
                section.Verify(diagnostics);
            }
        }

        public List<ElfSection> GetSectionsOrderedByStreamIndex()
        {
            var orderedSections = new List<ElfSection>(Sections.Count);
            orderedSections.AddRange(Sections);
            orderedSections.Sort(CompareStreamIndexAndIndexDelegate);
            return orderedSections;
        }

        /// <summary>
        /// Tries to update and calculate the layout of the sections, segments and <see cref="Layout"/>.
        /// </summary>
        /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
        /// <returns><c>true</c> if the calculation of the layout is successful. otherwise <c>false</c></returns>
        public unsafe void UpdateLayout(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            Size = 0;
            
            ulong offset = FileClass == ElfFileClass.Is32 ? (uint)sizeof(ElfNative.Elf32_Ehdr) : (uint)sizeof(ElfNative.Elf64_Ehdr);
            Layout.SizeOfElfHeader = (ushort)offset;
            Layout.OffsetOfProgramHeaderTable = 0;
            Layout.OffsetOfSectionHeaderTable = 0;
            Layout.SizeOfProgramHeaderEntry = FileClass == ElfFileClass.Is32 ? (ushort)sizeof(ElfNative.Elf32_Phdr) : (ushort)sizeof(ElfNative.Elf64_Phdr);
            Layout.SizeOfSectionHeaderEntry = FileClass == ElfFileClass.Is32 ? (ushort)sizeof(ElfNative.Elf32_Shdr) : (ushort)sizeof(ElfNative.Elf64_Shdr);
            Layout.TotalSize = offset;

            bool programHeaderTableFoundAndUpdated = false;

            // If we have any sections, prepare their offsets
            var sections = GetSectionsOrderedByStreamIndex();
            if (sections.Count > 0)
            {
                // Calculate offsets of all sections in the stream
                for (var i = 0; i < sections.Count; i++)
                {
                    var section = sections[i];
                    if (i == 0 && section.Type == ElfSectionType.Null)
                    {
                        continue;
                    }

                    var align = section.Alignment == 0 ? 1 : section.Alignment;
                    offset = AlignHelper.AlignToUpper(offset, align);
                    section.Offset = offset;

                    if (section is ElfProgramHeaderTable programHeaderTable)
                    {
                        if (Segments.Count > 0)
                        {
                            Layout.OffsetOfProgramHeaderTable = section.Offset;
                            Layout.SizeOfProgramHeaderEntry = (ushort) section.TableEntrySize;
                            programHeaderTableFoundAndUpdated = true;
                        }
                    }

                    if (section == SectionHeaderStringTable)
                    {
                        var shstrTable = SectionHeaderStringTable;
                        shstrTable.Reset();

                        // Prepare all section names (to calculate the name indices and the size of the SectionNames)
                        // Do it in two passes to generate optimal string table
                        for (var pass = 0; pass < 2; pass++)
                        {
                            for (var j = 0; j < sections.Count; j++)
                            {
                                var otherSection = sections[j];
                                if ((j == 0 && otherSection.Type == ElfSectionType.Null)) continue;
                                if (otherSection.IsShadow) continue;
                                if (pass == 0)
                                {
                                    shstrTable.ReserveString(otherSection.Name);
                                }
                                else
                                {
                                    otherSection.Name = otherSection.Name.WithIndex(shstrTable.GetOrCreateIndex(otherSection.Name));
                                }
                            }
                        }
                    }

                    section.UpdateLayout(diagnostics);

                    // Console.WriteLine($"{section.ToString(),-50} Offset: {section.Offset:x4} Size: {section.Size:x4}");

                    // A section without content doesn't count with its size
                    if (!section.HasContent)
                    {
                        continue;
                    }

                    offset += section.Size;
                }

                // The Section Header Table will be put just after all the sections
                Layout.OffsetOfSectionHeaderTable = AlignHelper.AlignToUpper(offset, FileClass == ElfFileClass.Is32 ? 4u : 8u);

                Layout.TotalSize = Layout.OffsetOfSectionHeaderTable + (ulong)VisibleSectionCount * Layout.SizeOfSectionHeaderEntry;
            }

            // Update program headers with offsets from auto layout
            if (Segments.Count > 0)
            {
                // Write program headers
                if (!programHeaderTableFoundAndUpdated)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_MissingProgramHeaderTableSection, $"Missing {nameof(ElfProgramHeaderTable)} shadow section for writing program headers / segments from this object file");
                }

                for (int i = 0; i < Segments.Count; i++)
                {
                    var programHeader = Segments[i];
                    programHeader.UpdateLayout(diagnostics);
                }
            }

            Size = offset + (ulong)VisibleSectionCount * Layout.SizeOfSectionHeaderEntry;
        }

        /// <summary>
        /// Adds a segment to <see cref="Segments"/>.
        /// </summary>
        /// <param name="segment">A segment</param>
        public void AddSegment(ElfSegment segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            if (segment.Parent != null)
            {
                if (segment.Parent == this) throw new InvalidOperationException("Cannot add the segment as it is already added");
                if (segment.Parent != this) throw new InvalidOperationException($"Cannot add the segment as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            segment.Parent = this;
            segment.Index = (uint)_segments.Count;
            _segments.Add(segment);
        }

        /// <summary>
        /// Inserts a segment into <see cref="Segments"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Segments"/> to insert the specified segment</param>
        /// <param name="segment">The segment to insert</param>
        public void InsertSegmentAt(int index, ElfSegment segment)
        {
            if (index < 0 || index > _segments.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_segments.Count}");
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            if (segment.Parent != null)
            {
                if (segment.Parent == this) throw new InvalidOperationException("Cannot add the segment as it is already added");
                if (segment.Parent != this) throw new InvalidOperationException($"Cannot add the segment as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            segment.Index = (uint)index;
            _segments.Insert(index, segment);
            segment.Parent = this;

            // Update the index of following segments
            for(int i = index + 1; i < _segments.Count; i++)
            {
                var nextSegment = _segments[i];
                nextSegment.Index++;
            }
        }

        /// <summary>
        /// Removes a segment from <see cref="Segments"/>
        /// </summary>
        /// <param name="segment">The segment to remove</param>
        public void RemoveSegment(ElfSegment segment)
        {
            if (segment == null) throw new ArgumentNullException(nameof(segment));
            if (segment.Parent != this)
            {
                throw new InvalidOperationException($"Cannot remove this segment as it is not part of this {nameof(ElfObjectFile)} instance");
            }

            var i = (int)segment.Index;
            _segments.RemoveAt(i);
            segment.Index = 0;

            // Update indices for other sections
            for (int j = i + 1; j < _segments.Count; j++)
            {
                var nextSegments = _segments[j];
                nextSegments.Index--;
            }

            segment.Parent = null;
        }

        /// <summary>
        /// Removes a segment from <see cref="Segments"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Segments"/> to remove the specified segment</param>
        public ElfSegment RemoveSegmentAt(int index)
        {
            if (index < 0 || index > _segments.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_segments.Count}");
            var segment = _segments[index];
            RemoveSegment(segment);
            return segment;
        }

        /// <summary>
        /// Adds a section to <see cref="Sections"/>.
        /// </summary>
        /// <param name="section">A section</param>
        public TSection AddSection<TSection>(TSection section) where TSection : ElfSection
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != null)
            {
                if (section.Parent == this) throw new InvalidOperationException("Cannot add the section as it is already added");
                if (section.Parent != this) throw new InvalidOperationException($"Cannot add the section as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            section.Parent = this;
            section.Index = (uint)_sections.Count;
            _sections.Add(section);

            if (section.IsShadow)
            {
                section.SectionIndex = 0;
                ShadowSectionCount++;
            }
            else
            {
                section.SectionIndex = VisibleSectionCount;
                VisibleSectionCount++;
            }

            // Setup the ElfSectionHeaderStringTable if not already set
            if (section is ElfSectionHeaderStringTable sectionHeaderStringTable && SectionHeaderStringTable == null)
            {
                SectionHeaderStringTable = sectionHeaderStringTable;
            }

            return section;
        }

        /// <summary>
        /// Inserts a section into <see cref="Sections"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Sections"/> to insert the specified section</param>
        /// <param name="section">The section to insert</param>
        public void InsertSectionAt(int index, ElfSection section)
        {
            if (index < 0 || index > _sections.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_sections.Count}");
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != null)
            {
                if (section.Parent == this) throw new InvalidOperationException("Cannot add the section as it is already added");
                if (section.Parent != this) throw new InvalidOperationException($"Cannot add the section as it is already added to another {nameof(ElfObjectFile)} instance");
            }

            section.Parent = this;
            section.Index = (uint)index;
            _sections.Insert(index, section);

            if (section.IsShadow)
            {
                section.SectionIndex = 0;
                ShadowSectionCount++;

                // Update the index of the following sections
                for (int j = index + 1; j < _sections.Count; j++)
                {
                    var sectionAfter = _sections[j];
                    sectionAfter.Index++;
                }
            }
            else
            {
                ElfSection previousSection = null;
                for (int j = 0; j < index; j++)
                {
                    var sectionBefore = _sections[j];
                    if (!sectionBefore.IsShadow)
                    {
                        previousSection = sectionBefore;
                    }
                }
                section.SectionIndex = previousSection != null ? previousSection.SectionIndex + 1 : 0;

                // Update the index of the following sections
                for (int j = index + 1; j < _sections.Count; j++)
                {
                    var sectionAfter = _sections[j];
                    if (!sectionAfter.IsShadow)
                    {
                        sectionAfter.SectionIndex++;
                    }
                    sectionAfter.Index++;
                }

                VisibleSectionCount++;
            }

            // Setup the ElfSectionHeaderStringTable if not already set
            if (section is ElfSectionHeaderStringTable sectionHeaderStringTable && SectionHeaderStringTable == null)
            {
                SectionHeaderStringTable = sectionHeaderStringTable;
            }
        }

        /// <summary>
        /// Removes a section from <see cref="Sections"/>
        /// </summary>
        /// <param name="section">The section to remove</param>
        public void RemoveSection(ElfSection section)
        {
            if (section == null) throw new ArgumentNullException(nameof(section));
            if (section.Parent != this)
            {
                throw new InvalidOperationException($"Cannot remove the section as it is not part of this {nameof(ElfObjectFile)} instance");
            }

            var i = (int)section.Index;
            _sections.RemoveAt(i);
            section.Index = 0;

            bool wasShadow = section.IsShadow;

            // Update indices for other sections
            for (int j = i + 1; j < _sections.Count; j++)
            {
                var nextSection = _sections[j];
                nextSection.Index--;

                // Update section index as well for following non-shadow sections
                if (!wasShadow && !nextSection.IsShadow)
                {
                    nextSection.SectionIndex--;
                }
            }

            if (wasShadow)
            {
                ShadowSectionCount--;
            }
            else
            {
                VisibleSectionCount--;
            }

            section.Parent = null;

            // Automatically replace the current ElfSectionHeaderStringTable with another existing one if any
            if (section is ElfSectionHeaderStringTable && SectionHeaderStringTable == section)
            {
                SectionHeaderStringTable = null;
                foreach (var nextSection in _sections)
                {
                    if (nextSection is ElfSectionHeaderStringTable nextSectionHeaderStringTable)
                    {
                        SectionHeaderStringTable = nextSectionHeaderStringTable;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a section from <see cref="Sections"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Sections"/> to remove the specified section</param>
        public ElfSection RemoveSectionAt(int index)
        {
            if (index < 0 || index > _sections.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {_sections.Count}");
            var section = _sections[index];
            RemoveSection(section);
            return section;
        }

        /// <summary>
        /// Writes this ELF object file to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public void Write(Stream stream)
        {
            if (!TryWrite(stream, out var diagnostics))
            {
                throw new ObjectFileException($"Invalid {nameof(ElfObjectFile)}", diagnostics);
            }
        }

        /// <summary>
        /// Tries to write this ELF object file to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="diagnostics">The output diagnostics</param>
        /// <returns><c>true</c> if writing was successful. otherwise <c>false</c></returns>
        public bool TryWrite(Stream stream, out DiagnosticBag diagnostics)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            var elfWriter = ElfWriter.Create(this, stream);
            diagnostics = elfWriter.Diagnostics;

            Verify(diagnostics);
            if (diagnostics.HasErrors)
            {
                return false;
            }

            UpdateLayout(diagnostics);
            if (diagnostics.HasErrors)
            {
                return false;
            }

            elfWriter.Write();

            return !diagnostics.HasErrors;
        }

        /// <summary>
        /// Checks if a stream contains an ELF file by checking the magic signature.
        /// </summary>
        /// <param name="stream">The stream containing potentially an ELF file</param>
        /// <returns><c>true</c> if the stream contains an ELF file. otherwise returns <c>false</c></returns>
        public static bool IsElf(Stream stream)
        {
            return IsElf(stream, out _);
        }

        /// <summary>
        /// Checks if a stream contains an ELF file by checking the magic signature.
        /// </summary>
        /// <param name="stream">The stream containing potentially an ELF file</param>
        /// <param name="encoding">Output the encoding if ELF is <c>true</c>.</param>
        /// <returns><c>true</c> if the stream contains an ELF file. otherwise returns <c>false</c></returns>
        public static bool IsElf(Stream stream, out ElfEncoding encoding)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var ident = ArrayPool<byte>.Shared.Rent(EI_NIDENT);
            encoding = ElfEncoding.None;
            try
            {
                var startPosition = stream.Position;
                var length = stream.Read(ident, 0, EI_NIDENT);
                stream.Position = startPosition;

                if (length == EI_NIDENT && (ident[EI_MAG0] == ELFMAG0 && ident[EI_MAG1] == ELFMAG1 && ident[EI_MAG2] == ELFMAG2 && ident[EI_MAG3] == ELFMAG3))
                {
                    encoding = (ElfEncoding)ident[EI_DATA];
                    return true;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(ident);
            }

            return false;
        }

        private static bool TryReadElfObjectFileHeader(Stream stream, out ElfObjectFile file)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var ident = ArrayPool<byte>.Shared.Rent(EI_NIDENT);
            file = null;
            try
            {
                var startPosition = stream.Position;
                var length = stream.Read(ident, 0, EI_NIDENT);
                stream.Position = startPosition;

                if (length == EI_NIDENT && (ident[EI_MAG0] == ELFMAG0 && ident[EI_MAG1] == ELFMAG1 && ident[EI_MAG2] == ELFMAG2 && ident[EI_MAG3] == ELFMAG3))
                {
                    file =new ElfObjectFile(false);
                    file.CopyIndentFrom(ident);
                    return true;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(ident);
            }

            return false;
        }

        /// <summary>
        /// Reads an <see cref="ElfObjectFile"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read ELF object file from</param>
        /// <param name="options">The options for the reader</param>
        /// <returns>An instance of <see cref="ElfObjectFile"/> if the read was successful.</returns>
        public static ElfObjectFile Read(Stream stream, ElfReaderOptions options = null)
        {
            if (!TryRead(stream, out var objectFile, out var diagnostics, options))
            {
                throw new ObjectFileException($"Unexpected error while reading ELF object file", diagnostics);
            }
            return objectFile;
        }

        /// <summary>
        /// Tries to read an <see cref="ElfObjectFile"/> from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read ELF object file from</param>
        /// <param name="objectFile"> instance of <see cref="ElfObjectFile"/> if the read was successful.</param>
        /// <param name="diagnostics">A <see cref="DiagnosticBag"/> instance</param>
        /// <param name="options">The options for the reader</param>
        /// <returns><c>true</c> An instance of <see cref="ElfObjectFile"/> if the read was successful.</returns>
        public static bool TryRead(Stream stream, out ElfObjectFile objectFile, out DiagnosticBag diagnostics, ElfReaderOptions options = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            if (!TryReadElfObjectFileHeader(stream, out objectFile))
            {
                diagnostics = new DiagnosticBag();
                diagnostics.Error(DiagnosticId.ELF_ERR_InvalidHeaderMagic, "ELF magic header not found");
                return false;
            }

            options ??= new ElfReaderOptions();
            var reader = ElfReader.Create(objectFile, stream, options);
            diagnostics = reader.Diagnostics;

            reader.Read();

            return !reader.Diagnostics.HasErrors;
        }

        /// <summary>
        /// Contains the layout of an object available after reading an <see cref="ElfObjectFile"/>
        /// or after calling <see cref="ElfObjectFile.UpdateLayout"/> or <see cref="ElfObjectFile.UpdateLayout"/>
        /// </summary>
        public sealed class ElfObjectLayout
        {
            internal ElfObjectLayout()
            {
            }

            /// <summary>
            /// Size of ELF Header.
            /// </summary>
            public ushort SizeOfElfHeader { get; internal set; }

            /// <summary>
            /// Offset of the program header table.
            /// </summary>
            public ulong OffsetOfProgramHeaderTable { get; internal set; }

            /// <summary>
            /// Size of a program header entry.
            /// </summary>
            public ushort SizeOfProgramHeaderEntry { get; internal set; }

            /// <summary>
            /// Offset of the section header table.
            /// </summary>
            public ulong OffsetOfSectionHeaderTable { get; internal set; }

            /// <summary>
            /// Size of a section header entry.
            /// </summary>
            public ushort SizeOfSectionHeaderEntry { get; internal set; }

            /// <summary>
            /// Size of the entire file
            /// </summary>
            public ulong TotalSize { get; internal set; }

        }

        private static readonly Comparison<ElfSection> CompareStreamIndexAndIndexDelegate = new Comparison<ElfSection>(CompareStreamIndexAndIndex);

        private static int CompareStreamIndexAndIndex(ElfSection left, ElfSection right)
        {
            var delta = left.StreamIndex.CompareTo(right.StreamIndex);
            if (delta != 0) return delta;
            return left.Index.CompareTo(right.Index);
        }
    }
}