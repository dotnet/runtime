// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;

using Internal.TypeSystem;

namespace ILCompiler.PEWriter
{
    /// <summary>
    /// Ready-to-run PE builder combines copying the input MSIL PE executable with managed
    /// metadata and IL and adding new code and data representing the R2R JITted code and
    /// additional runtime structures (R2R header and tables).
    /// </summary>
    public class R2RPEBuilder : PEBuilder
    {
        /// <summary>
        /// Number of low-order RVA bits that must match file position on Linux.
        /// </summary>
        const int RVABitsToMatchFilePos = 16;

        /// <summary>
        /// This structure describes how a particular section moved between the original MSIL
        /// and the output PE file. It holds beginning and end RVA of the input (MSIL) section
        /// and a delta between the input and output starting RVA of the section.
        /// </summary>
        struct SectionRVADelta
        {
            /// <summary>
            /// Starting RVA of the section in the input MSIL PE.
            /// </summary>
            public readonly int StartRVA;

            /// <summary>
            /// End RVA (one plus the last RVA in the section) of the section in the input MSIL PE.
            /// </summary>
            public readonly int EndRVA;

            /// <summary>
            /// Starting RVA of the section in the output PE minus its starting RVA in the input MSIL.
            /// </summary>
            public readonly int DeltaRVA;

            /// <summary>
            /// Initialize the section RVA delta information.
            /// </summary>
            /// <param name="startRVA">Starting RVA of the section in the input MSIL</param>
            /// <param name="endRVA">End RVA of the section in the input MSIL</param>
            /// <param name="deltaRVA">Output RVA of the section minus input RVA of the section</param>
            public SectionRVADelta(int startRVA, int endRVA, int deltaRVA)
            {
                StartRVA = startRVA;
                EndRVA = endRVA;
                DeltaRVA = deltaRVA;
            }
        }

        /// <summary>
        /// Name of the text section.
        /// </summary>
        public const string TextSectionName = ".text";

        /// <summary>
        /// Name of the initialized data section.
        /// </summary>
        public const string SDataSectionName = ".sdata";
        
        /// <summary>
        /// Name of the relocation section.
        /// </summary>
        public const string RelocSectionName = ".reloc";

        /// <summary>
        /// Name of the writeable data section.
        /// </summary>
        public const string DataSectionName = ".data";

        /// <summary>
        /// Name of the export data section.
        /// </summary>
        public const string ExportDataSectionName = ".edata";

        /// <summary>
        /// Compilation target OS and architecture specification.
        /// </summary>
        private TargetDetails _target;

        /// <summary>
        /// Complete list of sections to emit into the output R2R executable.
        /// </summary>
        private ImmutableArray<Section> _sections;

        /// <summary>
        /// Callback to retrieve the runtime function table which needs setting to the
        /// ExceptionTable PE directory entry.
        /// </summary>
        private Func<RuntimeFunctionsTableNode> _getRuntimeFunctionsTable;

        /// <summary>
        /// For each copied section, we store its initial and end RVA in the source PE file
        /// and the RVA difference between the old and new file. We use this table to relocate
        /// directory entries in the PE file header.
        /// </summary>
        private List<SectionRVADelta> _sectionRvaDeltas;

        /// <summary>
        /// Logical section start RVAs. When emitting R2R PE executables for Linux, we must
        /// align RVA's so that their 'RVABitsToMatchFilePos' lowest-order bits match the
        /// file position (otherwise memory mapping of the file fails and CoreCLR silently
        /// switches over to runtime JIT). PEBuilder doesn't support this today so that we
        /// must store the RVA's and post-process the produced PE by patching the section
        /// headers in the PE header.
        /// </summary>
        private int[] _sectionRVAs;

        /// <summary>
        /// Pointers to the location of the raw data. Needed to allow phyical file alignment
        /// beyond 4KB. PEBuilder doesn't support this today so that we
        /// must store the RVA's and post-process the produced PE by patching the section
        /// headers in the PE header.
        /// </summary>
        private int[] _sectionPointerToRawData;

        /// <summary>
        /// Maximum of virtual and physical size for each section.
        /// </summary>
        private int[] _sectionRawSizes;

        /// <summary>
        /// R2R PE section builder &amp; relocator.
        /// </summary>
        private readonly SectionBuilder _sectionBuilder;

        /// <summary>
        /// Zero-based index of the CPAOT-generated text section
        /// </summary>
        private readonly int _textSectionIndex;

        /// <summary>
        /// Zero-based index of the CPAOT-generated read-write data section
        /// </summary>
        private readonly int _dataSectionIndex;

        /// <summary>
        /// True after Write has been called; it's not possible to add further object data items past that point.
        /// </summary>
        private bool _written;

        /// <summary>
        /// If non-null, the PE file will be laid out such that it can naturally be mapped with a higher alignment than 4KB
        /// This is used to support loading via large pages on Linux
        /// </summary>
        private readonly int _customPESectionAlignment;

        /// <summary>
        /// Constructor initializes the various control structures and combines the section list.
        /// </summary>
        /// <param name="target">Target environment specifier</param>
        /// <param name="peHeaderBuilder">PE file header builder</param>
        /// <param name="getRuntimeFunctionsTable">Callback to retrieve the runtime functions table</param>
        public R2RPEBuilder(
            TargetDetails target,
            PEHeaderBuilder peHeaderBuilder,
            ISymbolNode r2rHeaderExportSymbol,
            string outputFileSimpleName,
            Func<RuntimeFunctionsTableNode> getRuntimeFunctionsTable,
            int customPESectionAlignment,
            Func<IEnumerable<Blob>, BlobContentId> deterministicIdProvider)
            : base(peHeaderBuilder, deterministicIdProvider: deterministicIdProvider)
        {
            _target = target;
            _getRuntimeFunctionsTable = getRuntimeFunctionsTable;
            _sectionRvaDeltas = new List<SectionRVADelta>();

            _sectionBuilder = new SectionBuilder(target);

            _textSectionIndex = _sectionBuilder.AddSection(TextSectionName, SectionCharacteristics.ContainsCode | SectionCharacteristics.MemExecute | SectionCharacteristics.MemRead, 512);
            _dataSectionIndex = _sectionBuilder.AddSection(DataSectionName, SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemWrite | SectionCharacteristics.MemRead, 512);

            _customPESectionAlignment = customPESectionAlignment;

            if (r2rHeaderExportSymbol != null)
            {
                _sectionBuilder.AddSection(R2RPEBuilder.ExportDataSectionName, SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemRead, 512);
                _sectionBuilder.AddExportSymbol("RTR_HEADER", 1, r2rHeaderExportSymbol);
                _sectionBuilder.SetDllNameForExportDirectoryTable(outputFileSimpleName);
            }

            if (_sectionBuilder.FindSection(R2RPEBuilder.RelocSectionName) == null)
            {
                // Always inject the relocation section to the end of section list
                _sectionBuilder.AddSection(
                    R2RPEBuilder.RelocSectionName,
                    SectionCharacteristics.ContainsInitializedData |
                    SectionCharacteristics.MemRead |
                    SectionCharacteristics.MemDiscardable,
                    PEHeaderConstants.SectionAlignment);
            }

            ImmutableArray<Section>.Builder sectionListBuilder = ImmutableArray.CreateBuilder<Section>();
            foreach (SectionInfo sectionInfo in _sectionBuilder.GetSections())
            {
                ILCompiler.PEWriter.Section builderSection = _sectionBuilder.FindSection(sectionInfo.SectionName);
                Debug.Assert(builderSection != null);
                sectionListBuilder.Add(new Section(builderSection.Name, builderSection.Characteristics));
            }

            _sections = sectionListBuilder.ToImmutableArray();
            _sectionRVAs = new int[_sections.Length];
            _sectionPointerToRawData = new int[_sections.Length];
            _sectionRawSizes = new int[_sections.Length];
        }

        public void SetCorHeader(ISymbolNode symbol, int headerSize)
        {
            _sectionBuilder.SetCorHeader(symbol, headerSize);
        }

        public void SetDebugDirectory(ISymbolNode symbol, int size)
        {
            _sectionBuilder.SetDebugDirectory(symbol, size);
        }

        public void SetWin32Resources(ISymbolNode symbol, int resourcesSize)
        {
            _sectionBuilder.SetWin32Resources(symbol, resourcesSize);
        }

        /// <summary>
        /// Emit a single object data item into the output R2R PE file using the section builder.
        /// </summary>
        /// <param name="objectData">Object data to emit</param>
        /// <param name="section">Target section</param>
        /// <param name="name">Textual name of the object data for diagnostic purposese</param>
        /// <param name="outputInfoBuilder">Optional output info builder to output the data item to</param>
        public void AddObjectData(DependencyAnalysis.ObjectNode.ObjectData objectData, ObjectNodeSection section, string name, OutputInfoBuilder outputInfoBuilder)
        {
            if (_written)
            {
                throw new InternalCompilerErrorException("Inconsistent upstream behavior - AddObjectData mustn't be called after Write");
            }

            int targetSectionIndex;
            switch (section.Type)
            {
                case SectionType.ReadOnly:
                    // We put ReadOnly data into the text section to limit the number of sections.
                case SectionType.Executable:
                    targetSectionIndex = _textSectionIndex;
                    break;

                case SectionType.Writeable:
                    targetSectionIndex = _dataSectionIndex;
                    break;

                default:
                    throw new NotImplementedException();
            }

            _sectionBuilder.AddObjectData(objectData, targetSectionIndex, name, outputInfoBuilder);
        }

        /// <summary>
        /// Add a symbol to the symbol map which defines the area of the binary between the two emitted symbols.
        /// This allows relocations (both position and size) to regions of the image. Both nodes must be in the
        /// same section and firstNode must be emitted before secondNode.
        /// </summary>
        public void AddSymbolForRange(ISymbolNode symbol, ISymbolNode firstNode, ISymbolNode secondNode)
        {
            _sectionBuilder.AddSymbolForRange(symbol, firstNode, secondNode);
        }

        public int GetSymbolFilePosition(ISymbolNode symbol)
        {
            return _sectionBuilder.GetSymbolFilePosition(symbol);
        }

        /// <summary>
        /// Emit built sections into the R2R PE file.
        /// </summary>
        /// <param name="outputStream">Output stream for the final R2R PE file</param>
        /// <param name="timeDateStamp">Timestamp to set in the PE header of the output R2R executable</param>
        public void Write(Stream outputStream, int? timeDateStamp)
        {
            BlobBuilder outputPeFile = new BlobBuilder();
            Serialize(outputPeFile);

            _sectionBuilder.RelocateOutputFile(outputPeFile, Header.ImageBase, outputStream);

            UpdateSectionRVAs(outputStream);

            if (_customPESectionAlignment != 0)
                SetPEHeaderSectionAlignment(outputStream, _customPESectionAlignment);

            ApplyMachineOSOverride(outputStream);

            if (timeDateStamp.HasValue)
                SetPEHeaderTimeStamp(outputStream, timeDateStamp.Value);

            _written = true;
        }

        /// <summary>
        /// Fill in map builder section table.
        /// </summary>
        /// <param name="outputInfoBuilder">Object info builder to set up</param>
        public void AddSections(OutputInfoBuilder outputInfoBuilder)
        {
            _sectionBuilder.AddSections(outputInfoBuilder);
        }

        /// <summary>
        /// PE header constants copied from System.Reflection.Metadata where they are
        /// sadly mostly internal or private.
        /// </summary>
        const int DosHeaderSize = 0x80;
        const int PESignatureSize = sizeof(uint);

        const int COFFHeaderSize =
            sizeof(short) + // Machine
            sizeof(short) + // NumberOfSections
            sizeof(int) +   // TimeDateStamp:
            sizeof(int) +   // PointerToSymbolTable
            sizeof(int) +   // NumberOfSymbols
            sizeof(short) + // SizeOfOptionalHeader:
            sizeof(ushort); // Characteristics

        const int OffsetOfSectionAlign =
            sizeof(short) + // Magic
            sizeof(byte) +  // MajorLinkerVersion
            sizeof(byte) +  // MinorLinkerVersion
            sizeof(int) +   // SizeOfCode
            sizeof(int) +   // SizeOfInitializedData
            sizeof(int) +   // SizeOfUninitializedData
            sizeof(int) +   // AddressOfEntryPoint
            sizeof(int) +   // BaseOfCode
            sizeof(long);   // PE32:  BaseOfData (int), ImageBase (int) 
                            // PE32+: ImageBase (long)
        const int OffsetOfChecksum = OffsetOfSectionAlign +
            sizeof(int) +   // SectionAlignment
            sizeof(int) +   // FileAlignment
            sizeof(short) + // MajorOperatingSystemVersion
            sizeof(short) + // MinorOperatingSystemVersion
            sizeof(short) + // MajorImageVersion
            sizeof(short) + // MinorImageVersion
            sizeof(short) + // MajorSubsystemVersion
            sizeof(short) + // MinorSubsystemVersion
            sizeof(int) +   // Win32VersionValue
            sizeof(int) +   // SizeOfImage
            sizeof(int);    // SizeOfHeaders

        const int OffsetOfSizeOfImage = OffsetOfChecksum - 2 * sizeof(int); // SizeOfHeaders, SizeOfImage

        const int SectionHeaderNameSize = 8;
        const int SectionHeaderVirtualSize = SectionHeaderNameSize; // VirtualSize follows 
        const int SectionHeaderRVAOffset = SectionHeaderVirtualSize + sizeof(int); // RVA Offset follows VirtualSize + 4 bytes VirtualSize
        const int SectionHeaderSizeOfRawData = SectionHeaderRVAOffset + sizeof(int); // SizeOfRawData follows RVA
        const int SectionHeaderPointerToRawDataOffset = SectionHeaderSizeOfRawData + sizeof(int); // PointerToRawData immediately follows the SizeOfRawData

        const int SectionHeaderSize =
            SectionHeaderNameSize +
            sizeof(int) +   // VirtualSize
            sizeof(int) +   // VirtualAddress
            sizeof(int) +   // SizeOfRawData
            sizeof(int) +   // PointerToRawData
            sizeof(int) +   // PointerToRelocations
            sizeof(int) +   // PointerToLineNumbers
            sizeof(short) + // NumberOfRelocations
            sizeof(short) + // NumberOfLineNumbers 
            sizeof(int);    // SectionCharacteristics

        /// <summary>
        /// On Linux, we must patch the section headers. This is because the CoreCLR runtime on Linux
        /// requires the 12-16 low-order bits of section RVAs (the number of bits corresponds to the page
        /// size) to be identical to the file offset, otherwise memory mapping of the file fails.
        /// Sadly PEBuilder in System.Reflection.Metadata doesn't support this so we must post-process
        /// the EXE by patching section headers with the correct RVA's. To reduce code variations
        /// we're performing the same transformation on Windows where it is a no-op.
        /// </summary>
        /// <param name="outputStream"></param>
        private void UpdateSectionRVAs(Stream outputStream)
        {
            int peHeaderSize =
                OffsetOfChecksum +
                sizeof(int) +             // Checksum
                sizeof(short) +           // Subsystem
                sizeof(short) +           // DllCharacteristics
                4 * _target.PointerSize + // SizeOfStackReserve, SizeOfStackCommit, SizeOfHeapReserve, SizeOfHeapCommit
                sizeof(int) +             // LoaderFlags
                sizeof(int) +             // NumberOfRvaAndSizes
                16 * sizeof(long);        // directory entries

            int sectionHeaderOffset = DosHeaderSize + PESignatureSize + COFFHeaderSize + peHeaderSize;
            int sectionCount = _sectionRVAs.Length;
            for (int sectionIndex = 0; sectionIndex < sectionCount; sectionIndex++)
            {
                if (_customPESectionAlignment != 0)
                {
                    // When _customPESectionAlignment is set, the physical and virtual sizes are the same
                    byte[] sizeBytes = BitConverter.GetBytes(_sectionRawSizes[sectionIndex]);
                    Debug.Assert(sizeBytes.Length == sizeof(int));

                    // Update VirtualSize
                    {
                        outputStream.Seek(sectionHeaderOffset + SectionHeaderSize * sectionIndex + SectionHeaderVirtualSize, SeekOrigin.Begin);
                        outputStream.Write(sizeBytes, 0, sizeBytes.Length);
                    }
                    // Update SizeOfRawData
                    {
                        outputStream.Seek(sectionHeaderOffset + SectionHeaderSize * sectionIndex + SectionHeaderSizeOfRawData, SeekOrigin.Begin);
                        outputStream.Write(sizeBytes, 0, sizeBytes.Length);
                    }
                }

                // Update RVAs
                {
                    outputStream.Seek(sectionHeaderOffset + SectionHeaderSize * sectionIndex + SectionHeaderRVAOffset, SeekOrigin.Begin);
                    byte[] rvaBytes = BitConverter.GetBytes(_sectionRVAs[sectionIndex]);
                    Debug.Assert(rvaBytes.Length == sizeof(int));
                    outputStream.Write(rvaBytes, 0, rvaBytes.Length);
                }

                // Update pointer to raw data
                {
                    outputStream.Seek(sectionHeaderOffset + SectionHeaderSize * sectionIndex + SectionHeaderPointerToRawDataOffset, SeekOrigin.Begin);
                    byte[] rawDataBytesBytes = BitConverter.GetBytes(_sectionPointerToRawData[sectionIndex]);
                    Debug.Assert(rawDataBytesBytes.Length == sizeof(int));
                    outputStream.Write(rawDataBytesBytes, 0, rawDataBytesBytes.Length);
                }
            }

            // Patch SizeOfImage to point past the end of the last section
            outputStream.Seek(DosHeaderSize + PESignatureSize + COFFHeaderSize + OffsetOfSizeOfImage, SeekOrigin.Begin);
            int sizeOfImage = AlignmentHelper.AlignUp(_sectionRVAs[sectionCount - 1] + _sectionRawSizes[sectionCount - 1], Header.SectionAlignment);
            byte[] sizeOfImageBytes = BitConverter.GetBytes(sizeOfImage);
            Debug.Assert(sizeOfImageBytes.Length == sizeof(int));
            outputStream.Write(sizeOfImageBytes, 0, sizeOfImageBytes.Length);
        }

        /// <summary>
        /// Set PE header section alignment, for alignments not supported by the System.Reflection.Metadata
        /// </summary>
        /// <param name="outputStream">Output stream representing the R2R PE executable</param>
        /// <param name="customAlignment">Timestamp to set in the R2R PE header</param>
        private void SetPEHeaderSectionAlignment(Stream outputStream, int customAlignment)
        {
            outputStream.Seek(DosHeaderSize + PESignatureSize + COFFHeaderSize + OffsetOfSectionAlign, SeekOrigin.Begin);
            byte[] alignBytes = BitConverter.GetBytes(customAlignment);
            Debug.Assert(alignBytes.Length == sizeof(int));
            outputStream.Write(alignBytes, 0, alignBytes.Length);
        }

        /// <summary>
        /// TODO: System.Reflection.Metadata doesn't currently support OS machine overrides.
        /// We cannot directly pass the xor-ed target machine to PEHeaderBuilder because it
        /// may incorrectly detect 32-bitness and emit wrong OptionalHeader.Magic. Therefore
        /// we create the executable using the raw Machine ID and apply the override as the
        /// last operation before closing the file.
        /// </summary>
        /// <param name="outputStream">Output stream representing the R2R PE executable</param>
        private void ApplyMachineOSOverride(Stream outputStream)
        {
            byte[] patchedTargetMachine = BitConverter.GetBytes(
                (ushort)unchecked((ushort)Header.Machine ^ (ushort)_target.MachineOSOverrideFromTarget()));
            Debug.Assert(patchedTargetMachine.Length == sizeof(ushort));

            outputStream.Seek(DosHeaderSize + PESignatureSize, SeekOrigin.Begin);
            outputStream.Write(patchedTargetMachine, 0, patchedTargetMachine.Length);
        }

        /// <summary>
        /// Set PE header timestamp in the output R2R image to a given value.
        /// </summary>
        /// <param name="outputStream">Output stream representing the R2R PE executable</param>
        /// <param name="timeDateStamp">Timestamp to set in the R2R PE header</param>
        private void SetPEHeaderTimeStamp(Stream outputStream, int timeDateStamp)
        {
            byte[] patchedTimestamp = BitConverter.GetBytes(timeDateStamp);
            int seekSize =
                DosHeaderSize +
                PESignatureSize +
                sizeof(short) +     // Machine
                sizeof(short);      // NumberOfSections

            outputStream.Seek(seekSize, SeekOrigin.Begin);
            outputStream.Write(patchedTimestamp, 0, patchedTimestamp.Length);
        }

        /// <summary>
        /// Copy all directory entries and the address of entry point, relocating them along the way.
        /// </summary>
        protected override PEDirectoriesBuilder GetDirectories()
        {
            PEDirectoriesBuilder builder = new PEDirectoriesBuilder();

            _sectionBuilder.UpdateDirectories(builder);

            if (_getRuntimeFunctionsTable != null)
            {
                RuntimeFunctionsTableNode runtimeFunctionsTable = _getRuntimeFunctionsTable();
                builder.ExceptionTable = new DirectoryEntry(
                    relativeVirtualAddress: _sectionBuilder.GetSymbolRVA(runtimeFunctionsTable),
                    size: runtimeFunctionsTable.TableSizeExcludingSentinel);
            }
    
            return builder;
        }

        /// <summary>
        /// Relocate a single directory entry.
        /// </summary>
        /// <param name="entry">Directory entry to allocate</param>
        /// <returns>Relocated directory entry</returns>
        public DirectoryEntry RelocateDirectoryEntry(DirectoryEntry entry)
        {
            return new DirectoryEntry(RelocateRVA(entry.RelativeVirtualAddress), entry.Size);
        }
        
        /// <summary>
        /// Relocate a given RVA using the section offset table produced during section serialization.
        /// </summary>
        /// <param name="rva">RVA to relocate</param>
        /// <returns>Relocated RVA</returns>
        private int RelocateRVA(int rva)
        {
            if (rva == 0)
            {
                // Zero RVA is normally used as NULL
                return rva;
            }
            foreach (SectionRVADelta sectionRvaDelta in _sectionRvaDeltas)
            {
                if (rva >= sectionRvaDelta.StartRVA && rva < sectionRvaDelta.EndRVA)
                {
                    // We found the input section holding the RVA, apply its specific delt (output RVA - input RVA).
                    return rva + sectionRvaDelta.DeltaRVA;
                }
            }
            Debug.Assert(false, "RVA is not within any of the input sections - output PE may be inconsistent");
            return rva;
        }

        /// <summary>
        /// Provide an array of sections for the PEBuilder to use.
        /// </summary>
        protected override ImmutableArray<Section> CreateSections()
        {
            return _sections;
        }

        /// <summary>
        /// Output the section with a given name. For sections existent in the source MSIL PE file
        /// (.text, optionally .rsrc and .reloc), we first copy the content of the input MSIL PE file
        /// and then call the section serialization callback to emit the extra content after the input
        /// section content.
        /// </summary>
        /// <param name="name">Section name</param>
        /// <param name="location">RVA and file location where the section will be put</param>
        /// <returns>Blob builder representing the section data</returns>
        protected override BlobBuilder SerializeSection(string name, SectionLocation location)
        {
            BlobBuilder sectionDataBuilder = null;
            int sectionStartRva = location.RelativeVirtualAddress;

            int outputSectionIndex = _sections.Length - 1;
            while (outputSectionIndex >= 0 && _sections[outputSectionIndex].Name != name)
            {
                outputSectionIndex--;
            }

            int injectedPadding = 0;
            if (_customPESectionAlignment != 0)
            {
                if (outputSectionIndex > 0)
                {
                    sectionStartRva = Math.Max(sectionStartRva, _sectionRVAs[outputSectionIndex - 1] + _sectionRawSizes[outputSectionIndex - 1]);
                }

                int newSectionStartRva = AlignmentHelper.AlignUp(sectionStartRva, _customPESectionAlignment);
                int newSectionPointerToRawData = AlignmentHelper.AlignUp(location.PointerToRawData, _customPESectionAlignment);
                if (newSectionPointerToRawData > location.PointerToRawData)
                {
                    sectionDataBuilder = new BlobBuilder();
                    injectedPadding = newSectionPointerToRawData - location.PointerToRawData;
                    sectionDataBuilder.WriteBytes(1, injectedPadding);
                }
                sectionStartRva = newSectionStartRva;
                location = new SectionLocation(sectionStartRva, newSectionPointerToRawData);
            }

            if (!_target.IsWindows)
            {
                if (outputSectionIndex > 0)
                {
                    sectionStartRva = Math.Max(sectionStartRva, _sectionRVAs[outputSectionIndex - 1] + _sectionRawSizes[outputSectionIndex - 1]);
                }

                const int RVAAlign = 1 << RVABitsToMatchFilePos;
                sectionStartRva = AlignmentHelper.AlignUp(sectionStartRva, RVAAlign);

                int rvaAdjust = (location.PointerToRawData - sectionStartRva) & (RVAAlign - 1);
                sectionStartRva += rvaAdjust;
                location = new SectionLocation(sectionStartRva, location.PointerToRawData);
            }

            if (outputSectionIndex >= 0)
            {
                _sectionRVAs[outputSectionIndex] = sectionStartRva;
                _sectionPointerToRawData[outputSectionIndex] = location.PointerToRawData;
            }

            BlobBuilder extraData = _sectionBuilder.SerializeSection(name, location);
            if (extraData != null)
            {
                if (sectionDataBuilder == null)
                {
                    // See above - there's a bug due to which LinkSuffix to an empty BlobBuilder screws up the blob content.
                    sectionDataBuilder = extraData;
                }
                else
                {
                    sectionDataBuilder.LinkSuffix(extraData);
                }
            }

            // Make sure the section has at least 1 byte, otherwise the PE emitter goes mad,
            // messes up the section map and corrups the output executable.
            if (sectionDataBuilder == null)
            {
                sectionDataBuilder = new BlobBuilder();
            }

            if (sectionDataBuilder.Count == 0)
            {
                sectionDataBuilder.WriteByte(0);
            }

            int sectionRawSize = sectionDataBuilder.Count - injectedPadding;

            if (_customPESectionAlignment != 0)
            {
                // Align the end of the section to the padding offset
                int count = AlignmentHelper.AlignUp(sectionRawSize, _customPESectionAlignment);
                sectionDataBuilder.WriteBytes(0, count - sectionRawSize);
                sectionRawSize = count;
            }

            if (outputSectionIndex >= 0)
            {
                _sectionRawSizes[outputSectionIndex] = sectionRawSize;
            }

            return sectionDataBuilder;
        }
    }
    
    /// <summary>
    /// Simple helper for filling in PE header information.
    /// </summary>
    static class PEHeaderProvider
    {
        /// <summary>
        /// Fill in PE header information into a PEHeaderBuilder used by PEBuilder.
        /// </summary>
        /// <param name="subsystem">Targeting subsystem</param>
        /// <param name="target">Target architecture to set in the header</param>
        public static PEHeaderBuilder Create(Subsystem subsystem, TargetDetails target)
        {
            bool is64BitTarget = target.PointerSize == sizeof(long);

            Characteristics imageCharacteristics = Characteristics.ExecutableImage | Characteristics.Dll;
            imageCharacteristics |= is64BitTarget ? Characteristics.LargeAddressAware : Characteristics.Bit32Machine;

            ulong imageBase = is64BitTarget ? PE64HeaderConstants.DllImageBase : PE32HeaderConstants.ImageBase;

            int fileAlignment = 0x200;
            if (!target.IsWindows && !is64BitTarget)
            {
                // To minimize wasted VA space on 32-bit systems, align file to page boundaries (presumed to be 4K)
                fileAlignment = 0x1000;
            }

            int sectionAlignment = 0x1000;
            if (!target.IsWindows && is64BitTarget)
            {
                // On Linux, we must match the bottom 12 bits of section RVA's to their file offsets. For this reason
                // we need the same alignment for both.
                sectionAlignment = fileAlignment;
            }

            // Without NxCompatible the PE executable cannot execute on Windows ARM64
            DllCharacteristics dllCharacteristics =
                DllCharacteristics.DynamicBase |
                DllCharacteristics.NxCompatible |
                DllCharacteristics.TerminalServerAware;

            if (is64BitTarget)
            {
                dllCharacteristics |= DllCharacteristics.HighEntropyVirtualAddressSpace;
            }
            else
            {
                dllCharacteristics |= DllCharacteristics.NoSeh;
            }

            return new PEHeaderBuilder(
                machine: target.MachineFromTarget(),
                sectionAlignment: sectionAlignment,
                fileAlignment: fileAlignment,
                imageBase: imageBase,
                majorLinkerVersion: PEHeaderConstants.MajorLinkerVersion,
                minorLinkerVersion: PEHeaderConstants.MinorLinkerVersion,
                majorOperatingSystemVersion: PEHeaderConstants.MajorOperatingSystemVersion,
                minorOperatingSystemVersion: PEHeaderConstants.MinorOperatingSystemVersion,
                majorImageVersion: PEHeaderConstants.MajorImageVersion,
                minorImageVersion: PEHeaderConstants.MinorImageVersion,
                majorSubsystemVersion: PEHeaderConstants.MajorSubsystemVersion,
                minorSubsystemVersion: PEHeaderConstants.MinorSubsystemVersion,
                subsystem: subsystem,
                dllCharacteristics: dllCharacteristics,
                imageCharacteristics: imageCharacteristics,
                sizeOfStackReserve: (is64BitTarget ? PE64HeaderConstants.SizeOfStackReserve : PE32HeaderConstants.SizeOfStackReserve),
                sizeOfStackCommit: (is64BitTarget ? PE64HeaderConstants.SizeOfStackCommit : PE32HeaderConstants.SizeOfStackCommit),
                sizeOfHeapReserve: (is64BitTarget ? PE64HeaderConstants.SizeOfHeapReserve : PE32HeaderConstants.SizeOfHeapReserve),
                sizeOfHeapCommit: (is64BitTarget ? PE64HeaderConstants.SizeOfHeapCommit : PE32HeaderConstants.SizeOfHeapCommit));
        }
    }
}
