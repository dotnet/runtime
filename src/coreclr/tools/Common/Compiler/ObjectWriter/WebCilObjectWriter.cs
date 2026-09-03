// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Microsoft.NET.WebAssembly.Webcil;
using CodeDataLayout = CodeDataLayoutMode.CodeDataLayout;

namespace ILCompiler.ObjectWriter
{
    internal class PaddingHelper
    {
        private byte[] _padding;
        public PaddingHelper(int n, byte padByte = 0)
        {
            _padding = new byte[n];
            _padding.AsSpan().Fill(padByte);
        }

        public void PadStream(Stream s, int n)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(n, 0);

            while (n > 0)
            {
                int bytesToWrite = Math.Min(n, _padding.Length);
                s.Write(_padding, 0, bytesToWrite);
                n -= bytesToWrite;
            }
        }
    }

    /// <summary>
    /// WebCIL object file format writer.
    /// </summary>
    internal sealed class WebCilObjectWriter : WasmObjectWriter
    {
        public const int WebcilSectionAlignment = 16;

        protected override CodeDataLayout LayoutMode => CodeDataLayout.Separate;

        // We use 2 Wasm data segments for webcil,
        // 1 for the payload size, and the second for the payload itself.
        const int NumDataSegments = 2;

        public WebCilObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
        }

        private Dictionary<SortableDependencyNode.ObjectNodeOrder, Utf8String> _wellKnownSymbols = new();
        private protected override void RecordWellKnownSymbol(Utf8String currentSymbolName, SortableDependencyNode.ObjectNodeOrder classCode)
        {
            if (classCode is SortableDependencyNode.ObjectNodeOrder.CorHeaderNode
                or SortableDependencyNode.ObjectNodeOrder.DebugDirectoryNode)
            {

                bool added = _wellKnownSymbols.TryAdd(classCode, currentSymbolName);
                Debug.Assert(added,
                    $"Well-known symbol for '{classCode}' was already recorded as '{_wellKnownSymbols[classCode]}', ");
            }
        }

        private protected override SectionDataEmitter CreateDataSection(
            ObjectNodeSection section,
            int sectionIndex,
            Stream sectionStream)
        {
            return new WebcilSection(
                new Utf8String(section.Name),
                default(WebcilSectionHeader),
                sectionStream,
                sectionIndex);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            WebcilSection section = _sections[sectionIndex] as WebcilSection;
            // We should only be updating the alignment of Webcil sections; Wasm-native sections should
            // not have alignment constraints.
            Debug.Assert(section != null || alignment == 1, $"Section: {sectionIndex} is not a WebcilSection but alignment {alignment} requested");
            if (section == null)
            {
                return;
            }

            section.MinAlignment = Math.Max(section.MinAlignment, alignment);
        }

#if READYTORUN
        WasmInstructionGroup GetImageFunctionPointerBaseOffset(int offset)
        {
            return new WasmInstructionGroup([
                Global.Get(TableBaseGlobalIndex),
                I32.Const(offset),
                I32.Add,
            ]);
        }

        private class WebcilSegment
        {
            public WebcilHeader Header;
            public WebcilSection[] Sections;

            public WebcilSegment(WebcilHeader header, WebcilSection[] sections)
            {
                Header = header;
                Sections = sections;
            }

            public int GetFlatMappedSize()
            {
                int size = 0;
                size += WebcilEncoder.HeaderEncodeSize(WebcilVersion.Version1); // include header
                size += Sections.Length * WebcilEncoder.SectionHeaderEncodeSize(); // include size of all section headers
                size = AlignmentHelper.AlignUp(size, WebcilSectionAlignment); // account for padding before first section

                foreach (WebcilSection section in Sections)
                {
                    size += (int)section.Header.SizeOfRawData; // include raw data size of each section (same as virtual size since Webcil has a flat mapping)
                }

                return size;
            }

        }

        static WasmFunctionBody GetWebcilSize = new WasmFunctionBody(
            new WasmFuncType(new([WasmValueType.I32]), new([])), // (func (destPtr i32) (result))
                [
                    Local.Get(0), // (local.get $destPtr)
                    I32.Const(0),
                    I32.Const(4),
                    Memory.Init(0)
                ]
        );

        WasmFunctionBody FillWebcilTable(int tableSize) => new WasmFunctionBody(
            new WasmFuncType(new([]), new([])), // (func)
                [
                    Global.Get(WebCilObjectWriter.TableBaseGlobalIndex),
                    I32.Const(0),
                    I32.Const(tableSize),
                    Table.Init(0, 0)
                ]
        );

        WasmFunctionBody GetWebcilPayload => new WasmFunctionBody(
            new WasmFuncType(new([WasmValueType.I32, WasmValueType.I32]), new([])), // (func ($d i32) ($n i32))
                [
                    Local.Get(0), // (local.get $d)
                    I32.Const(0),
                    Local.Get(1), // (local.get $n)
                    Memory.Init(1),
                    Local.Get(1),
                    I32.Const(32),
                    I32.Ge_s,
                    Block.If(WasmBlockType.Empty),
                    Local.Get(0), // (local.get $d)
                    Global.Get(WebCilObjectWriter.TableBaseGlobalIndex), // (global.get $tableBase)
                    I32.Store((ulong)WebcilEncoder.TableBaseOffset), // i32.store offset=TableBaseOffset
                    Block.End
                ]
        );

        private long ResolveSymbolRVA(WebcilSection[] sections, SymbolDefinition definition)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                WebcilSection section = sections[i];
                if (definition.SectionIndex == section.SectionIndex)
                {
                    return section.Header.VirtualAddress + definition.Value;
                }
            }

            return 0;
        }

        /// <summary>
        /// Assigns VirtualAddresses and related header fields to each webcil section based on the
        /// total section count and each section's stream length. This can be called before all
        /// sections have their final content as long as the section count is finalized, though
        /// sections whose size changes later must come last so they don't invalidate earlier VAs.
        /// </summary>
        private static void AssignWebcilSectionVirtualAddresses(WebcilSection[] webcilSections)
        {
            uint sizeOfHeaders = (uint)WebcilEncoder.HeaderEncodeSize(WebcilVersion.Version1) + (uint)(webcilSections.Length * WebcilEncoder.SectionHeaderEncodeSize());
            uint pointerToRawData = (uint)AlignmentHelper.AlignUp((int)sizeOfHeaders, (int)WebcilSectionAlignment);
            uint virtualAddress = pointerToRawData;

            for (int i = 0; i < webcilSections.Length; i++)
            {
                WebcilSection webcilSection = webcilSections[i];
                Debug.Assert(BitOperations.IsPow2(webcilSection.MinAlignment) && BitOperations.IsPow2(WebcilSectionAlignment) &&
                    WebcilSectionAlignment >= webcilSection.MinAlignment);

                uint rawSectionSize = (uint)webcilSection.ContentReadStream.Length;
                uint alignedSectionSize = (uint)AlignmentHelper.AlignUp((int)rawSectionSize, (int)WebcilSectionAlignment);

                // Webcil files are flat-mapped, since (for example) there is no uninitialized data which is expanded on load.
                // As a result, the virtual size is the same as the aligned raw size (including padding), and
                // the pointer to raw data for each section is also the same as the virtual address.
                uint virtualSize = alignedSectionSize;
                WebcilSectionHeader sectionHeader = new WebcilSectionHeader(
                    virtualSize: virtualSize,
                    virtualAddress: virtualAddress,
                    sizeOfRawData: alignedSectionSize,
                    pointerToRawData: pointerToRawData
                );
                webcilSection.Header = sectionHeader;

                pointerToRawData += alignedSectionSize;
                virtualAddress += virtualSize;
            }
        }

        private WebcilSegment BuildWebcilDataSegment()
        {
            WebcilSection[] webcilSections = _sections.Sections.OfType<WebcilSection>().ToArray();

            AssignWebcilSectionVirtualAddresses(webcilSections);

            // Populate the RVAs for the Cor header/size and debug directory/size, which are required for the runtime
            // to be able to load this segment.
            Utf8String corHeaderDefName = _wellKnownSymbols[SortableDependencyNode.ObjectNodeOrder.CorHeaderNode];
            SymbolDefinition corHeaderNode = _definedSymbols[corHeaderDefName];
            uint peCliHeaderRva = (uint)ResolveSymbolRVA(webcilSections, corHeaderNode);
            Debug.Assert(peCliHeaderRva != 0);
            uint peCliHeaderSize = (uint)corHeaderNode.Size;

            Utf8String debugDirectoryDefName = _wellKnownSymbols[SortableDependencyNode.ObjectNodeOrder.DebugDirectoryNode];
            SymbolDefinition debugDirectoryDef = _definedSymbols[debugDirectoryDefName];
            uint peDebugRva = (uint)ResolveSymbolRVA(webcilSections, debugDirectoryDef);
            Debug.Assert(peDebugRva != 0);
            uint peDebugSize = (uint)debugDirectoryDef.Size;

            // The index of the reloc section is either: 0 (if no reloc section) OR
            // the 1-based index of the section, which in our case is assumed to be the last section
            if (_baseRelocMap.Count > 0)
            {
                Debug.Assert(webcilSections.Length > 0);
                Debug.Assert(webcilSections[webcilSections.Length - 1].SectionName.ToString() == "reloc");
            }
            ushort relocSectionIdx = _baseRelocMap.Count > 0 ? checked((ushort)webcilSections.Length) : (ushort)0;

            WebcilHeader header = new WebcilHeader
            {
                Id = WebcilConstants.WEBCIL_MAGIC,
                VersionMajor = WebcilConstants.WC_VERSION_MAJOR,
                VersionMinor = WebcilConstants.WC_VERSION_MINOR,
                CoffSections = (ushort)webcilSections.Length,
                // In Webcil v1.0, Reserved0 is used for the index of the image base reloc section
                Reserved0 = relocSectionIdx,
                PeCliHeaderRva = peCliHeaderRva,
                PeCliHeaderSize = peCliHeaderSize,
                PeDebugRva = peDebugRva,
                PeDebugSize = peDebugSize
            };

            return new WebcilSegment(header, webcilSections.ToArray());
        }
#endif

        private protected override ObjectNodeSection GetEmitSection(ObjectNodeSection section)
        {
            if (section == ObjectNodeSection.TextSection || section == ObjectNodeSection.ManagedCodeUnixContentSection)
            {
                return ObjectNodeSection.WasmCodeSection;
            }

            return section;
        }

        private void WriteDataCountSection()
        {
            SectionDataEmitter section = GetOrCreateSection(WasmObjectNodeSection.DataCountSection, out SectionWriter writer);
            writer.WriteULEB128(NumDataSegments); // number of data segments
        }

        private WebcilSegment _webcilSegment = null;
        private protected override void EmitSectionsAndLayout()
        {
            int totalMethodCount = MethodCount + 3;
            InsertWasmStub(new Utf8String("getWebcilSize"), GetWebcilSize);
            InsertWasmStub(new Utf8String("getWebcilPayload"), GetWebcilPayload);
            InsertWasmStub(new Utf8String("fillWebcilTable"), FillWebcilTable(totalMethodCount));
            Debug.Assert(MethodCount == totalMethodCount);

            WriteDataCountSection();
        }

        private protected override void WriteGlobalSection()
        {
            // webcilVersion: i32 const = 0
            WriteGlobal("webcilVersion", WasmValueType.I32, WasmMutabilityType.Const,
                new WasmInstructionGroup([new WasmConstExpr(WasmExprKind.I32Const, WebcilConstants.WC_VERSION_MAJOR)]));
        }

        private static readonly ObjectNodeSection WebcilRelocSection = new ObjectNodeSection("reloc", SectionType.ReadOnly);
        private void EmitRelocSectionData()
        {
            GetOrCreateSection<WebcilSection>(WebcilRelocSection, out SectionWriter writer);
            Debug.Assert(writer.SectionIndex == _sections.Count - 1, "The .reloc section must be the last section we emit.");

            foreach (var kv in _baseRelocMap)
            {
                uint pageRva = kv.Key;
                List<ushort> entries = kv.Value;
                entries.Sort();

                int entriesSize = entries.Count * 2;
                int sizeOfBlock = 8 + entriesSize;
                sizeOfBlock = AlignmentHelper.AlignUp(sizeOfBlock, 4);

                writer.WriteLittleEndian(pageRva);
                writer.WriteLittleEndian((uint)sizeOfBlock);

                // Emit entries
                foreach (ushort e in entries)
                {
                    writer.WriteLittleEndian(e);
                }

                // Ensure block is 4-byte aligned
                writer.EmitAlignment(4);
            }
        }

        private PaddingHelper _paddingHelper = new PaddingHelper(WebcilSectionAlignment);

        private protected override void EmitObjectFile(Stream outputFileStream)
        {
            Debug.Assert(outputFileStream.CanSeek, $"EmitObjectFile requires seekable output stream");

            if (_pendingBaseRelocs.Count > 0)
            {
                GetOrCreateSection<WebcilSection>(WebcilRelocSection, out _);
            }

            WebcilSection[] webcilSections = _sections.Sections.OfType<WebcilSection>().ToArray();
            // At this point, our count of sections is final since we've determined if we have base relocs.
            // This allows us to do an initial assignment of virtual addresses to our webcil sections,
            // which is required for resolving file-level relocations whose RVA depends on the section VAs.
            AssignWebcilSectionVirtualAddresses(webcilSections);

            // We can now build our base relocs with the correct addresses
            BuildBaseRelocMap();

            if (_baseRelocMap.Count > 0)
            {
                EmitRelocSectionData();
            }

            // Build the final webcil segment (re-assigns VAs with reloc section's real size). This must come last,
            // since we must know if we have a reloc section as well as its final size to determine the segment layout.
            _webcilSegment = BuildWebcilDataSegment();

            // Writing our memory import <- size of the webcil segment (for an accurate minimum size)
            WriteMemoryImport((ulong)_webcilSegment.GetFlatMappedSize());
            FinalizeSectionEntryCounts();

           /*********************************************************************
           * Write Wasm Sections, Excluding Data
           *********************************************************************/

            EmitWasmHeader(outputFileStream);
            int codeSectionIndex = _sections.Contains(ObjectNodeSection.WasmCodeSection.Name)
                ? _sections.GetSectionIndex(ObjectNodeSection.WasmCodeSection.Name)
                : -1;
            long codeContentFileOffset = 0;
            foreach (int index in SectionEmitOrder)
            {
                SectionDataEmitter section = _sections[index];
                if (_resolvableRelocations.TryGetValue(index, out List<SymbolicRelocation> relocations) &&
                    section is WasmSection)
                {
                    using (Stream originalStream = section.ContentReadStream)
                    {
                        MemoryStream destStream = new MemoryStream((int)originalStream.Length);
                        originalStream.Position = 0;
                        ResolveRelocations(index, originalStream, destStream, relocations, sectionStart: 0, shrink: true);
                        section.ContentReadStream = destStream;
                        // originalStream may be disposed, section.Stream now points to resolved stream
                    }
                }

                if (index == codeSectionIndex)
                {
                    // Function bodies begin after the section header and the (externally counted) entry-count prefix.
                    codeContentFileOffset = outputFileStream.Position + (section.EncodeSize() - section.ContentReadStream.Length);
                }

                section.EmitToStream(outputFileStream);
            }

#if READYTORUN
            /*****************************************************************
             * Emit Webcil segment at end of file to support ReadyToRun
             ****************************************************************/


            MemoryStream webcilStream = new(_webcilSegment.GetFlatMappedSize());
            WebcilEncoder.EmitHeader(_webcilSegment.Header, webcilStream);

            foreach (WebcilSection section in _webcilSegment.Sections)
            {
                WebcilEncoder.EncodeSectionHeader(section.Header, webcilStream);
            }

            foreach (WebcilSection section in _webcilSegment.Sections)
            {
                // Move stream position forward to account for inter-section padding (precalculated in BuildWebcilDataSegment())
                webcilStream.Position = section.Header.PointerToRawData;
                section.ContentReadStream.Position = 0;

                if (_resolvableRelocations.TryGetValue(section.SectionIndex, out List<SymbolicRelocation> relocations))
                {
                    // We emit all Webcil sections into one stream, and copy data / resolve relocations directly into this combined stream.
                    // As a result, the real offsets that relocs in our list have need to be calculated based on the section's
                    // position within the Webcil segment
                    ResolveRelocations(section.SectionIndex, section.ContentReadStream, webcilStream, relocations, sectionStart: (long)section.Header.PointerToRawData, shrink: false);
                }
                else
                {
                    section.ContentReadStream.CopyTo(webcilStream);
                }

                long bytesWritten = (long)webcilStream.Position - (long)section.Header.PointerToRawData;
                Debug.Assert(section.Header.SizeOfRawData - bytesWritten == section.Padding, $"Unexpected padding: {section.Header.SizeOfRawData - bytesWritten} != {section.Padding}");
            }

            if (_webcilSegment.Sections.Length > 0)
            {
                // Write final padding after last section
                WebcilSection lastSection = _webcilSegment.Sections[_webcilSegment.Sections.Length - 1];
                webcilStream.Seek(0, SeekOrigin.End);
                _paddingHelper.PadStream(webcilStream, (int)lastSection.Padding);
            }
            Debug.Assert(webcilStream.Position == _webcilSegment.GetFlatMappedSize(), $"Total Size Mismatch: {webcilStream.Position} != {_webcilSegment.GetFlatMappedSize()}");

            // Create passive data segment for encoding the size of the webcil payload (size must fit in 32-bit uint)
            byte[] lengthBuffer = new byte[sizeof(uint) * 2];
            BinaryPrimitives.WriteUInt32LittleEndian(lengthBuffer, (uint)_webcilSegment.GetFlatMappedSize());
            BinaryPrimitives.WriteUInt32LittleEndian(lengthBuffer.AsSpan().Slice(4), (uint)MethodCount);
            MemoryStream webcilSizeSegmentStream = new MemoryStream(lengthBuffer);
            WasmDataSegment webcilSizeSegment = new WasmDataSegment(webcilSizeSegmentStream, new Utf8String("webcilCount"),
                WasmDataSegmentType.Passive, null);

            // Passive data segment for webcil payload contents
            WasmDataSegment webcilContentsSegment = new WasmDataSegment(webcilStream, new Utf8String("webcilPayload"),
                WasmDataSegmentType.Passive, null);

            // Create combined data section and emit
            WasmDataSection dataSection = new WasmDataSection(
                [webcilSizeSegment, webcilContentsSegment],
                new Utf8String("data"),
                contentAlign: WebcilSectionAlignment);
            dataSection.EmitToStream(outputFileStream);
#endif

            // The name section goes last, after the data section, as tooling expects. It is the only
            // record of function names now that they are no longer carried by the export table, and
            // wasm-merge -g synthesizes the merged module's names from it.
            WasmNameSection nameSection = new WasmNameSection(_wasmSymbolManager.GetDefinitions(WasmIndexSpace.Function));
            nameSection.EmitToStream(outputFileStream);

            if (_outputInfoBuilder is not null)
            {
                // Populate the output section layout so OutputInfoBuilder.EnumerateMethods can resolve each
                // method node's section. The list is index-aligned with the wasm section table; only the
                // code section (which holds the method bodies) needs a real file offset for the perfmap.
                for (int i = 0; i < _sections.Count; i++)
                {
                    SectionDataEmitter emittedSection = _sections[i];
                    ulong fileOffset = (i == codeSectionIndex) ? (ulong)codeContentFileOffset : 0;
                    _outputSectionLayout.Add(new OutputSection(
                        emittedSection.SectionName.ToString(), fileOffset, fileOffset, (ulong)emittedSection.ContentReadStream.Length));
                }

                if (codeSectionIndex >= 0)
                {
                    _outputInfoBuilder.RemapMethodNodeOffsets(codeSectionIndex, _codeOffsetMap);
                }
            }
        }

        // Maps each code-section entry boundary's pre-shrink content offset to its final (post-shrink)
        // offset, populated during ResolveCodeRelocations so method node offsets and lengths can be
        // corrected for the R2R perfmap.
        private readonly Dictionary<ulong, ulong> _codeOffsetMap = new();

        Dictionary<int, List<SymbolicRelocation>> _resolvableRelocations = new();
        SortedDictionary<uint, List<ushort>> _baseRelocMap = new();
        // We group webcil relocs into 4kb blocks, similar to PE
        const uint WebcilRelocPageSize = 0x1000;

        // File-level relocations whose RVA computation is deferred until webcil section
        // VirtualAddresses have been assigned.
        private readonly record struct PendingBaseReloc(int SectionIndex, long Offset, RelocType FileRelocType);
        private readonly List<PendingBaseReloc> _pendingBaseRelocs = new();

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            foreach (var reloc in relocationList)
            {
                if (!_resolvableRelocations.TryGetValue(sectionIndex, out List<SymbolicRelocation> resolvable))
                {
                    _resolvableRelocations[sectionIndex] = resolvable = new List<SymbolicRelocation>();
                }
                // Unconditionally add the reloc to our resolvable list; we do some amount of relocation resolution
                // for all relocation types.
                resolvable.Add(reloc);

                // A few relocation types (table indices and IMAGE_REL type relocs in Webcil) need
                // an additional runtime reloc as well to add a base address.
                // We defer the actual RVA computation to EmitObjectFile, where webcil section
                // VirtualAddresses will have been assigned. Here we just record the raw info.
                RelocType fileRelocType = Relocation.GetFileRelocationType(reloc.Type);
                if (fileRelocType is not RelocType.IMAGE_REL_BASED_ABSOLUTE)
                {
                    Debug.Assert(_sections[sectionIndex] is WebcilSection);
                    _pendingBaseRelocs.Add(new PendingBaseReloc(sectionIndex, reloc.Offset, fileRelocType));
                }
            }
        }

        /// <summary>
        /// Processes the deferred file-level relocations after webcil section VirtualAddresses
        /// have been assigned. Populates <see cref="_baseRelocMap"/> with page-grouped base reloc
        /// entries, mirroring the PE base relocation format.
        /// </summary>
        private void BuildBaseRelocMap()
        {
            foreach (PendingBaseReloc pending in _pendingBaseRelocs)
            {
                Debug.Assert(_sections[pending.SectionIndex] is WebcilSection);
                WebcilSection webcilSection = (WebcilSection)_sections[pending.SectionIndex];
                Debug.Assert(pending.Offset >= 0, "Pending base relocation has a negative offset.");
                // Gather file-level relocations that need to go into the webcil .reloc
                // section. We collect entries grouped by 4KB page into a map of
                // (page RVA -> list of (type<<12 | offsetInPage) WORD entries).
                // Note that this handling is logically the same as the implementation in the PE Object Writer.
                uint targetRva = webcilSection.Header.VirtualAddress + (uint)pending.Offset;
                Debug.Assert(targetRva != 0); // this section should have been assigned a non-zero VirtualAddress at this point.
                uint pageRva = targetRva & ~(WebcilRelocPageSize - 1);
                ushort offsetInPage = (ushort)(targetRva & (WebcilRelocPageSize - 1));
                ushort entry = (ushort)(((ushort)pending.FileRelocType << 12) | offsetInPage);

                if (!_baseRelocMap.TryGetValue(pageRva, out List<ushort> list))
                {
                    list = new List<ushort>();
                    _baseRelocMap.Add(pageRva, list);
                }
                list.Add(entry);
            }
        }

        private bool IsWithinSection(long rva, WebcilSection section)
        {
            return rva >= section.Header.VirtualAddress && rva < section.Header.VirtualAddress + section.Header.VirtualSize;
        }

#nullable enable
        static void CopyOnly(MemoryStream src, long srcPos, MemoryStream dest, long destPos, long count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            src.GetBuffer().AsSpan((int)srcPos, (int)count).CopyTo(dest.GetBuffer().AsSpan((int)destPos, (int)count));
        }

        private readonly record struct CodeBlob(long Size, long Start, long End);

        private List<CodeBlob> ParseCodeBlobs(Stream sectionStream)
        {
            List<CodeBlob> blobs = new();
            while (true)
            {
                ulong? decoded = DwarfHelper.ReadULEB128(sectionStream, out _);
                if (decoded is null) break; // end of stream

                Debug.Assert(sectionStream.Position + (long)decoded <= sectionStream.Length);
                blobs.Add(new CodeBlob((long)decoded, sectionStream.Position, sectionStream.Position + (long)decoded));
                sectionStream.Position += (long)decoded;
            }

            return blobs;
        }

        /// <summary>
        /// Resolve relocations in the code section, shrinking the size of all ULEB relocations to their minimal size.
        /// This requires code blobs to be pre-split so that we can shrink the size of relocs in each blob independently, and then re-encode the blob with its new size.
        /// </summary>
        // We use an in-place copying strategy here with a read cursor (sectionStream.Position) and a separate write cursor where (write <= read),
        // since the resolved blobs will always be equal to or smaller in size than the original blobs.
        // Within the blobs, we split on relocations and copy the data between them, resolving each relocation to its minimal size.
        private void ResolveCodeRelocations(int sectionIndex, MemoryStream sectionStream, List<CodeBlob> blobs, List<SymbolicRelocation> relocs, bool shrink = false)
        {
            if (blobs.Count == 0 && relocs.Count > 0)
            {
                throw new InvalidDataException();
            }

            long maxBlobSize = blobs.Max(blob => blob.End - blob.Start);
            MemoryStream tempStream = new MemoryStream((int)maxBlobSize);
            byte[] relocScratchBuffer = new byte[Relocation.MaxSize];
            int[] blobShrink = new int[blobs.Count];
            // Post-shrink content offset of each blob's entry (size prefix), used to correct perfmap offsets.
            long[] postEntryStart = new long[blobs.Count];

            blobs.Sort((a, b) => a.Start.CompareTo(b.Start));
            relocs.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            long writeCursor = 0;
            int relocCursor = 0;

            byte[] countBuffer = new byte[5];

            // Invariant: writeCursor is where we are writing to in the sectionStream. Further, writeCursor is always less than or equal to the start of the current blob we are processing.
            for (int b = 0; b < blobs.Count; b++)
            {
                CodeBlob blob = blobs[b];
                // writeCursor is the post-shrink offset where this entry's (new) size prefix will be written.
                postEntryStart[b] = writeCursor;
                Debug.Assert(writeCursor <= blobs[b].Start, $"Write cursor {writeCursor} is beyond the start of blob {blobs[b].Start}");

                bool hasRelocs = relocCursor < relocs.Count && relocs[relocCursor].Offset >= blob.Start && relocs[relocCursor].Offset < blob.End;
                if (hasRelocs)
                {
                    tempStream.Position = 0;
                    tempStream.SetLength(blob.Size);
                    sectionStream.Position = blob.Start; // sectionStream.Position is now our read cursor
                    SymbolicRelocation firstReloc = relocs[relocCursor];

                    if (firstReloc.Offset > 0)
                    {
                        // Copy the initial data in the blob before the first relocation
                        int initialSize = (int)firstReloc.Offset - (int)blob.Start;
                        CopyOnly(sectionStream, sectionStream.Position, tempStream, tempStream.Position, initialSize);
                        sectionStream.Position += initialSize;
                        tempStream.Position += initialSize;
                    }
                    Debug.Assert(sectionStream.Position == firstReloc.Offset, $"Section stream position sectionStream.Position does not match first reloc offset {firstReloc.Offset}");

                    while (relocCursor < relocs.Count && relocs[relocCursor].Offset < blob.End)
                    {
                        SymbolicRelocation curReloc = relocs[relocCursor];
                        SymbolicRelocation? nextReloc = null;
                        // look ahead to the next relocation, if any, to determine how much data is between this relocation and the next one
                        if (relocCursor + 1 < relocs.Count && relocs[relocCursor + 1].Offset < blob.End)
                        {
                            nextReloc = relocs[relocCursor + 1];
                        }

                        int size = ResolveReloc(sectionIndex, sectionStream, curReloc.Offset, tempStream, tempStream.Position, curReloc, relocScratchBuffer, shrink: shrink);
                        blobShrink[b] += (int)Relocation.GetSize(curReloc.Type) - size;

                        long nextStart = curReloc.Offset + Relocation.GetSize(curReloc.Type);
                        long nextEnd = nextReloc is not null ? nextReloc.Offset : blob.End;
                        long betweenSize = nextEnd - nextStart;

                        Debug.Assert(nextStart == sectionStream.Position);
                        CopyOnly(sectionStream, sectionStream.Position, tempStream, tempStream.Position, (int)betweenSize);
                        sectionStream.Position += betweenSize;
                        tempStream.Position += betweenSize;
                        relocCursor++;
                    }

                    Debug.Assert(tempStream.Position <= blob.Size && blob.Size <= tempStream.Length, $"Temp stream position {tempStream.Position} exceeds blob size {blob.Size}");

                    tempStream.SetLength(tempStream.Position);

                    // Write the temp stream back into the original stream with a NEW length prefix, starting at writeCursor
                    DwarfHelper.WriteULEB128(countBuffer, (ulong)tempStream.Length);
                    sectionStream.Position = writeCursor;
                    sectionStream.Write(countBuffer, 0, (int)DwarfHelper.SizeOfULEB128((ulong)tempStream.Length));
                    writeCursor = sectionStream.Position; // set writeCursor to the position after the length prefix we just wrote

                    tempStream.Position = 0;
                    tempStream.CopyTo(sectionStream);

                    writeCursor += tempStream.Length;
                }
                else
                {
                    // No relocations in this blob. Copy the blob as-is but shrink the length prefix if possible.
                    DwarfHelper.WriteULEB128(countBuffer, (ulong)blob.Size);
                    sectionStream.Position = writeCursor;
                    sectionStream.Write(countBuffer, 0, (int)DwarfHelper.SizeOfULEB128((ulong)blob.Size));
                    writeCursor = sectionStream.Position;

                    CopyOnly(src: sectionStream, srcPos: blob.Start, dest: sectionStream, destPos: writeCursor, count: blob.Size);
                    writeCursor += blob.Size;
                }
            }
            sectionStream.SetLength(writeCursor);

            sectionStream.Position = 0;

            if (_outputInfoBuilder is not null)
            {
                // Map each entry boundary's pre-shrink content offset (node offsets and lengths land on
                // these boundaries) to its final post-shrink offset so the R2R perfmap points at the
                // method's real position and length.
                for (int b = 0; b < blobs.Count; b++)
                {
                    long preEntryStart = (b == 0) ? 0 : blobs[b - 1].End;
                    _codeOffsetMap[(ulong)preEntryStart] = (ulong)postEntryStart[b];
                }
                // Map final boundary (end of the last entry) so the `End` offset of a node ending the section resolves to the post-shrunk end.
                long preTotalLength = blobs.Count > 0 ? blobs[blobs.Count - 1].End : 0;
                _codeOffsetMap[(ulong)preTotalLength] = (ulong)writeCursor;
            }

#if DEBUG
            // The number of code blobs should not have changed.
            List<CodeBlob> newBlobs = ParseCodeBlobs(sectionStream);
            Debug.Assert(newBlobs.Count == blobs.Count);
            for (int i = 0; i < newBlobs.Count; i++)
            {
                Debug.Assert(newBlobs[i].Size + blobShrink[i] == blobs[i].Size);
            }
#endif
        }

        private unsafe int ResolveReloc(int sectionIndex, MemoryStream sourceStream, long srcPos, MemoryStream destStream, long destPos, SymbolicRelocation reloc,  byte[] relocScratchBuffer, bool shrink = false)
        {
            WebcilSection? curSectionAsWebcil = null;
            uint webcilVirtualStart = 0;
            if (_sections[sectionIndex] is WebcilSection curSection)
            {
                curSectionAsWebcil = curSection;
                webcilVirtualStart = curSection.Header.VirtualAddress;
            }

            int size = Relocation.GetSize(reloc.Type);
            if (size > relocScratchBuffer.Length)
            {
                throw new InvalidOperationException($"Unsupported relocation size for relocation: {reloc.Type}");
            }

            SymbolDefinition definedSymbol = _definedSymbols[reloc.SymbolName];

            // The virtual address of the relocation we are resolving
            uint virtualRelocOffset = 0;
            if (curSectionAsWebcil is not null)
            {
                virtualRelocOffset = webcilVirtualStart + (uint)reloc.Offset;
                Debug.Assert(IsWithinSection(virtualRelocOffset, curSectionAsWebcil));
            }

            // The virtual address of the symbol this relocation refers to
            uint virtualSymbolImageOffset = 0;
            WebcilSection? symbolWebcilSection = null;

            if (_sections[definedSymbol.SectionIndex] is WebcilSection targetSection)
            {
                symbolWebcilSection = targetSection;
                virtualSymbolImageOffset = symbolWebcilSection.Header.VirtualAddress + (uint)definedSymbol.Value;
                Debug.Assert(IsWithinSection(virtualSymbolImageOffset, symbolWebcilSection));
            }

            // We need a pinned raw pointer here for manipulation with Relocation.WriteValue
            fixed (byte* pData = ReadRelocToDataSpan(reloc, relocScratchBuffer))
            {
                long addend = Relocation.ReadValue(reloc.Type, pData);
                int relocLength = Relocation.GetSize(reloc.Type);
                int? actualLength = null;

                switch (reloc.Type)
                {
                    case RelocType.WASM_TYPE_INDEX_LEB:
                    case RelocType.WASM_GLOBAL_INDEX_LEB:
                    case RelocType.WASM_TABLE_INDEX_I32:
                    case RelocType.WASM_TABLE_INDEX_I64:
                    case RelocType.WASM_TABLE_INDEX_SLEB:
                    case RelocType.WASM_TABLE_INDEX_REL_I32:
                    case RelocType.WASM_FUNCTION_INDEX_LEB:
                    {
                        // These relocations reference a wasm structural index (function, type,
                        // table entry, or well-known global). For R2R we self-resolve them here to
                        // the index assigned when the symbol was registered into its index space.
                        if (!_wasmSymbolManager.TryGetSymbol(reloc.SymbolName, out WasmSymbol symbol))
                        {
                            throw new InvalidOperationException($"Symbol '{reloc.SymbolName}' was not registered. Relocation type {reloc.Type}.");
                        }

                        if (shrink && Relocation.IsVariableLength(reloc.Type))
                        {
                            actualLength = Relocation.WriteVariableLengthValue(reloc.Type, pData, symbol.Index + addend);
                        }
                        else
                        {
                            Relocation.WriteValue(reloc.Type, pData, symbol.Index + addend);
                        }
                        break;
                    }

                    case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                        // No action required
                        break;

                    case RelocType.IMAGE_REL_BASED_DIR64:
                    case RelocType.IMAGE_REL_BASED_HIGHLOW:
                        // This is an ImageBase-relative value in PE, but our image base
                        // for Webcil is virtual address 0
                        Debug.Assert(symbolWebcilSection != null);
                        Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset + 0 + addend);
                        break;
                    case RelocType.IMAGE_REL_BASED_ADDR32NB:
                        Debug.Assert(symbolWebcilSection != null);
                        Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset + addend);
                        break;
                    case RelocType.IMAGE_REL_BASED_REL32:
                    case RelocType.IMAGE_REL_BASED_RELPTR32:
                        Debug.Assert(symbolWebcilSection != null);
                        Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset - (virtualRelocOffset + relocLength) + addend);
                        break;
                    case RelocType.IMAGE_REL_FILE_ABSOLUTE:
                        Debug.Assert(symbolWebcilSection != null);
                        long fileOffset = symbolWebcilSection.Header.PointerToRawData + definedSymbol.Value;
                        Relocation.WriteValue(reloc.Type, pData, fileOffset + addend);
                        break;
                    case RelocType.WASM_MEMORY_ADDR_REL_SLEB:
                    {
                        // These relocs should be for cases of the form:
                        //  global.get $imageBase
                        //  i32.const <reloc>
                        //  i32.add
                        //  i32.load 0
                        // So, the relocated address value should always represent an offset relative to image base.
                        // This offset should ALWAYS be equal to the actual offset from image base at runtime, due to Webcil's
                        // flag mapping
                        if (symbolWebcilSection is null)
                        {
                            throw new InvalidDataException($"WASM_MEMORY_ADDR_REL_SLEB: symbol '{reloc.SymbolName}' (sectionIndex {definedSymbol.SectionIndex}, section type {_sections[definedSymbol.SectionIndex]?.GetType().Name}) is not in a WebcilSection. Reloc in section {sectionIndex} ({_sections[sectionIndex]?.GetType().Name}), offset {reloc.Offset:X}.");
                        }

                        if (shrink)
                        {
                            actualLength = Relocation.WriteVariableLengthValue(reloc.Type, pData, virtualSymbolImageOffset + addend);
                        }
                        else
                        {
                            Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset + addend);
                        }

                        break;
                    }
                    case RelocType.WASM_MEMORY_ADDR_REL_LEB:
                    {
                        // These relocs should be for cases of the form:
                        //  global.get $imageBase
                        //  i32.load <reloc>
                        // So, the relocated address value should always represent an offset relative to image base.
                        // This offset should ALWAYS be equal to the actual offset from image base at runtime, due to Webcil's
                        // flag mapping
                        if (symbolWebcilSection is null)
                        {
                            throw new InvalidDataException($"WASM_MEMORY_ADDR_REL_LEB: symbol '{reloc.SymbolName}' (sectionIndex {definedSymbol.SectionIndex}, section type {_sections[definedSymbol.SectionIndex]?.GetType().Name}) is not in a WebcilSection. Reloc in section {sectionIndex} ({_sections[sectionIndex]?.GetType().Name}), offset {reloc.Offset:X}.");
                        }

                        if (shrink)
                        {
                            actualLength = Relocation.WriteVariableLengthValue(reloc.Type, pData, virtualSymbolImageOffset + addend);
                        }
                        else
                        {
                            Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset + addend);
                        }

                        break;
                    }
                    case RelocType.WASM_CLR_RESTORE_CONTEXT_EXCEPTION_TAG_LEB:
                    {
                        WasmSymbol symbol = _wasmSymbolManager.GetSymbol(RtlRestoreContextTagName);
                        Debug.Assert(symbol.IndexSpace == WasmIndexSpace.Tag);
                        if (shrink)
                        {
                            actualLength = Relocation.WriteVariableLengthValue(reloc.Type, pData, symbol.Index + addend);
                        }
                        else
                        {
                            Relocation.WriteValue(reloc.Type, pData, symbol.Index + addend);
                        }
                        break;
                    }
                    default:
                        // TODO-WASM: add other cases as needed;
                        // ignoring other reloc types for now
                        throw new NotSupportedException($"Relocation type {reloc.Type} not yet implemented");
                }

                return WriteRelocFromDataSpan(reloc, pData, actualLength ?? relocLength);
            }

            Span<byte> ReadRelocToDataSpan(SymbolicRelocation reloc, byte[] buffer)
            {
                Span<byte> relocContents = buffer.AsSpan(0, Relocation.GetSize(reloc.Type));
                sourceStream.Position = srcPos;
                sourceStream.ReadExactly(relocContents);
                return relocContents;
            }

            int WriteRelocFromDataSpan(SymbolicRelocation reloc, byte* pData, int length)
            {
                destStream.Position = destPos;
                destStream.Write(new Span<byte>(pData, length));
                return length;
            }
        }

        private void ResolveRelocations(int sectionIndex, Stream sectionStream, MemoryStream dstStream, List<SymbolicRelocation> relocs, long sectionStart = 0, bool shrink = false)
        {
            Debug.Assert(sectionStream.CanSeek);
            Debug.Assert(sectionStream.Length >= 0);

            if (relocs.Count == 0)
            {
                sectionStream.CopyTo(dstStream);
                return;
            }

            if (shrink && _sections[sectionIndex] is WasmSection { Type: WasmSectionType.Code })
            {
                sectionStream.Position = 0;
                sectionStream.CopyTo(dstStream);

                dstStream.Position = 0;
                List<CodeBlob> blobs = ParseCodeBlobs(dstStream);

                dstStream.Position = 0;
                ResolveCodeRelocations(sectionIndex, dstStream, blobs, relocs, shrink);
                return;
            }

            byte[] relocScratchBuffer = new byte[Relocation.MaxSize];

            // Otherwise, we can resolve relocations on top of the copied in section stream, since the size and layout of the stream won't be changing.
            long startPos = dstStream.Position;
            sectionStream.CopyTo(dstStream);
            for (int i = 0; i < relocs.Count; i++)
            {
                SymbolicRelocation reloc = relocs[i];
                ResolveReloc(sectionIndex, dstStream, srcPos: sectionStart + reloc.Offset, dstStream, destPos: sectionStart + reloc.Offset, reloc, relocScratchBuffer);
            }
            dstStream.Position = sectionStream.Length + startPos;
        }
#nullable disable

        public const int RtlRestoreContextTagIndex = 0;
        private static readonly Utf8String RtlRestoreContextTagName = new("rtlRestoreContextTag");

        private static readonly WasmFuncType RtlRestoreContextTagSignature = new(
            new([]),
            new([]));

        internal const int StackPointerGlobalIndex = 0;
        internal const int ImageBaseGlobalIndex = 1;
        internal const int TableBaseGlobalIndex = 2;
        internal const int AsyncContinuationGlobalIndex = 3;

        private WasmImport[] CreateDefaultGlobalImports()
        {
            int rtlRestoreContextTagTypeIndex = RegisterSignature(RtlRestoreContextTagSignature);

            return
            [
                new WasmImport("webcil", WasmWellKnownGlobalSymbolNode.StackPointerName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Mut), index: StackPointerGlobalIndex),
                new WasmImport("webcil", WasmWellKnownGlobalSymbolNode.ImageBaseName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Const), index: ImageBaseGlobalIndex),
                new WasmImport("webcil", WasmWellKnownGlobalSymbolNode.TableBaseName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Const), index: TableBaseGlobalIndex),
                new WasmImport("webcil", WasmWellKnownGlobalSymbolNode.AsyncContinuationName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Mut), index: AsyncContinuationGlobalIndex),
                new WasmImport("webcil", "table", import: new WasmTableImportType(), index: 0),
                new WasmImport("webcil", RtlRestoreContextTagName.ToString(), import: new WasmTagImportType(rtlRestoreContextTagTypeIndex), index: RtlRestoreContextTagIndex),
            ];
        }

        private protected override void WriteImports()
        {
            foreach (WasmImport import in CreateDefaultGlobalImports())
            {
                WriteImport(import);
            }
        }

        private void WriteMemoryImport(ulong contentSize)
        {
            uint dataPages = checked((uint)((contentSize + (1 << 16) - 1) >> 16));
            uint numPages = Math.Max(dataPages, 1); // Ensure at least one page is allocated for the minimum

            WasmImport memoryImport = new WasmImport("webcil", "memory", import: new WasmMemoryImportType(WasmLimitType.HasMin, numPages)); // memory limits: flags (0 = only minimum)
            WriteImport(memoryImport);
        }

        private protected override void WriteExports()
        {
            WriteTableExport("table", 0);

            Debug.Assert(_definedGlobals.ContainsKey("webcilVersion"));
            WriteGlobalExport("webcilVersion", _definedGlobals["webcilVersion"].Index);

            // Export only the stubs the host actually calls. Exporting every compiled function does
            // not scale: exports count towards the engine's effective-type-size limit, and a
            // framework-sized composite exceeds it, producing a module no conforming engine loads.
            // Nothing needs them - the element segment, not the export table, is what makes a
            // function reachable - and the names they used to carry now live in the name section.
            foreach (Utf8String stubName in _wasmStubNames)
            {
                WasmSymbol stubSymbol = _wasmSymbolManager.GetSymbol(stubName);
                WriteFunctionExport(stubName.ToString(), stubSymbol.Index);
            }
        }

        private protected override void WriteElements()
        {
            // Generate the function pointer table element that contains function pointers for all of our functions.
            // Function indices are assigned sequentially (0..MethodCount-1) so that
            // (image_function_pointer_base + 0) == function index 0.
            int[] functionIndices = _wasmSymbolManager.GetDefinitions(WasmIndexSpace.Function)
                .Select(symbol => symbol.Index)
                .ToArray();

            WriteElementSegment(functionIndices);
        }
    }
}
