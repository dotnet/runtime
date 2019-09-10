// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// PE reader representing the input MSIL PE file we're copying to the output composite PE file.
        /// </summary>
        private PEReader _peReader;
        
        /// <summary>
        /// Custom sections explicitly injected by the caller.
        /// </summary>
        private HashSet<string> _customSections;
        
        /// <summary>
        /// Complete list of section names includes the sections present in the input MSIL file
        /// (.text, optionally .rsrc and .reloc) and extra questions injected during the R2R PE
        /// creation.
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
        /// Constructor initializes the various control structures and combines the section list.
        /// </summary>
        /// <param name="target">Target environment specifier</param>
        /// <param name="peReader">Input MSIL PE file reader</param>
        /// <param name="getRuntimeFunctionsTable">Callback to retrieve the runtime functions table</param>
        public R2RPEBuilder(
            TargetDetails target,
            PEReader peReader,
            Func<RuntimeFunctionsTableNode> getRuntimeFunctionsTable)
            : base(PEHeaderCopier.Copy(peReader.PEHeaders, target), deterministicIdProvider: null)
        {
            _target = target;
            _peReader = peReader;
            _getRuntimeFunctionsTable = getRuntimeFunctionsTable;
            _sectionRvaDeltas = new List<SectionRVADelta>();

            _sectionBuilder = new SectionBuilder(target);

            _textSectionIndex = _sectionBuilder.AddSection(TextSectionName, SectionCharacteristics.ContainsCode | SectionCharacteristics.MemExecute | SectionCharacteristics.MemRead, 512);
            _dataSectionIndex = _sectionBuilder.AddSection(DataSectionName, SectionCharacteristics.ContainsInitializedData | SectionCharacteristics.MemWrite | SectionCharacteristics.MemRead, 512);

            _customSections = new HashSet<string>();
            foreach (SectionInfo section in _sectionBuilder.GetSections())
            {
                _customSections.Add(section.SectionName);
            }

            if (_sectionBuilder.FindSection(R2RPEBuilder.RelocSectionName) == null)
            {
                // Always inject the relocation section to the end of section list
                _sectionBuilder.AddSection(
                    R2RPEBuilder.RelocSectionName,
                    SectionCharacteristics.ContainsInitializedData |
                    SectionCharacteristics.MemRead |
                    SectionCharacteristics.MemDiscardable,
                    peReader.PEHeaders.PEHeader.SectionAlignment);
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
            _sectionRawSizes = new int[_sections.Length];
        }

        public void SetCorHeader(ISymbolNode symbol, int headerSize)
        {
            _sectionBuilder.SetCorHeader(symbol, headerSize);
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
        /// <param name="mapFile">Optional map file to output the data item to</param>
        public void AddObjectData(ObjectNode.ObjectData objectData, ObjectNodeSection section, string name, TextWriter mapFile)
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

            _sectionBuilder.AddObjectData(objectData, targetSectionIndex, name, mapFile);
        }

        /// <summary>
        /// Emit built sections into the R2R PE file.
        /// </summary>
        /// <param name="outputStream">Output stream for the final R2R PE file</param>
        public void Write(Stream outputStream)
        {
            BlobBuilder outputPeFile = new BlobBuilder();
            Serialize(outputPeFile);

            _sectionBuilder.RelocateOutputFile(
                outputPeFile,
                _peReader.PEHeaders.PEHeader.ImageBase,
                outputStream);

            UpdateSectionRVAs(outputStream);

            ApplyMachineOSOverride(outputStream);

            _written = true;
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

        const int OffsetOfChecksum =
            sizeof(short) + // Magic
            sizeof(byte) +  // MajorLinkerVersion
            sizeof(byte) +  // MinorLinkerVersion
            sizeof(int) +   // SizeOfCode
            sizeof(int) +   // SizeOfInitializedData
            sizeof(int) +   // SizeOfUninitializedData
            sizeof(int) +   // AddressOfEntryPoint
            sizeof(int) +   // BaseOfCode
            sizeof(long) +  // PE32:  BaseOfData (int), ImageBase (int) 
                            // PE32+: ImageBase (long)
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
        const int SectionHeaderRVAOffset = SectionHeaderNameSize + sizeof(int); // skip 8 bytes Name + 4 bytes VirtualSize

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
                outputStream.Seek(sectionHeaderOffset + SectionHeaderSize * sectionIndex + SectionHeaderRVAOffset, SeekOrigin.Begin);
                byte[] rvaBytes = BitConverter.GetBytes(_sectionRVAs[sectionIndex]);
                Debug.Assert(rvaBytes.Length == sizeof(int));
                outputStream.Write(rvaBytes, 0, rvaBytes.Length);
            }

            // Patch SizeOfImage to point past the end of the last section
            outputStream.Seek(DosHeaderSize + PESignatureSize + COFFHeaderSize + OffsetOfSizeOfImage, SeekOrigin.Begin);
            int sizeOfImage = AlignmentHelper.AlignUp(_sectionRVAs[sectionCount - 1] + _sectionRawSizes[sectionCount - 1], Header.SectionAlignment);
            byte[] sizeOfImageBytes = BitConverter.GetBytes(sizeOfImage);
            Debug.Assert(sizeOfImageBytes.Length == sizeof(int));
            outputStream.Write(sizeOfImageBytes, 0, sizeOfImageBytes.Length);
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
        /// Copy all directory entries and the address of entry point, relocating them along the way.
        /// </summary>
        protected override PEDirectoriesBuilder GetDirectories()
        {
            PEDirectoriesBuilder builder = new PEDirectoriesBuilder();

            _sectionBuilder.UpdateDirectories(builder);

            RuntimeFunctionsTableNode runtimeFunctionsTable = _getRuntimeFunctionsTable();
            builder.ExceptionTable = new DirectoryEntry(
                relativeVirtualAddress: _sectionBuilder.GetSymbolRVA(runtimeFunctionsTable),
                size: runtimeFunctionsTable.TableSize);
    
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

            if (outputSectionIndex >= 0)
            {
                _sectionRawSizes[outputSectionIndex] = sectionDataBuilder.Count;
            }

            return sectionDataBuilder;
        }
    }
    
    /// <summary>
    /// Simple helper for copying the various global values in the PE header.
    /// </summary>
    static class PEHeaderCopier
    {
        /// <summary>
        /// Copy PE headers into a PEHeaderBuilder used by PEBuilder.
        /// </summary>
        /// <param name="peHeaders">Headers to copy</param>
        /// <param name="target">Target architecture to set in the header</param>
        public static PEHeaderBuilder Copy(PEHeaders peHeaders, TargetDetails target)
        {
            bool is64BitTarget = target.PointerSize == sizeof(long);

            Characteristics imageCharacteristics = peHeaders.CoffHeader.Characteristics;
            if (is64BitTarget)
            {
                imageCharacteristics &= ~Characteristics.Bit32Machine;
                imageCharacteristics |= Characteristics.LargeAddressAware;
            }

            int fileAlignment = 0x200;
            if (!target.IsWindows && !is64BitTarget)
            {
                // To minimize wasted VA space on 32 bit systems align file to page bounaries (presumed to be 4K).
                fileAlignment = 0x1000;
            }

            int sectionAlignment = 0x1000;
            if (!target.IsWindows && is64BitTarget)
            {
                // On Linux, we must match the bottom 12 bits of section RVA's to their file offsets. For this reason
                // we need the same alignment for both.
                sectionAlignment = fileAlignment;
            }

            DllCharacteristics dllCharacteristics = DllCharacteristics.DynamicBase | DllCharacteristics.NxCompatible;

            if (!is64BitTarget)
            {
                dllCharacteristics |= DllCharacteristics.NoSeh;
            }

            // Copy over selected DLL characteristics bits from IL image
            dllCharacteristics |= peHeaders.PEHeader.DllCharacteristics &
                (DllCharacteristics.TerminalServerAware | DllCharacteristics.AppContainer);

            if (is64BitTarget)
            {
                dllCharacteristics |= DllCharacteristics.HighEntropyVirtualAddressSpace;
            }

            return new PEHeaderBuilder(
                machine: target.MachineFromTarget(),
                sectionAlignment: sectionAlignment,
                fileAlignment: fileAlignment,
                imageBase: peHeaders.PEHeader.ImageBase,
                majorLinkerVersion: 11,
                minorLinkerVersion: 0,
                majorOperatingSystemVersion: 5,
                // Win2k = 5.0 for 32-bit images, Win2003 = 5.2 for 64-bit images
                minorOperatingSystemVersion: is64BitTarget ? (ushort)2 : (ushort)0,
                majorImageVersion: peHeaders.PEHeader.MajorImageVersion,
                minorImageVersion: peHeaders.PEHeader.MinorImageVersion,
                majorSubsystemVersion: peHeaders.PEHeader.MajorSubsystemVersion,
                minorSubsystemVersion: peHeaders.PEHeader.MinorSubsystemVersion,
                subsystem: peHeaders.PEHeader.Subsystem,
                dllCharacteristics: dllCharacteristics,
                imageCharacteristics: imageCharacteristics,
                sizeOfStackReserve: peHeaders.PEHeader.SizeOfStackReserve,
                sizeOfStackCommit: peHeaders.PEHeader.SizeOfStackCommit,
                sizeOfHeapReserve: 0,
                sizeOfHeapCommit: 0);
        }
    }
}
