// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Microsoft.NET.WebAssembly.Webcil;
using CodeDataLayout = CodeDataLayoutMode.CodeDataLayout;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

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
            ArgumentOutOfRangeException.ThrowIfGreaterThan(n, _padding.Length);
            ArgumentOutOfRangeException.ThrowIfLessThan(n, 0);
            s.Write(_padding, 0, n);
        }
    }

    internal static class WasmObjectNodeSection
    {
        // TODO-WASM: Consider alignment needs for data sections
        public static readonly ObjectNodeSection DataSection = new ObjectNodeSection("wasm.data", SectionType.Writeable, needsAlign: false);
        public static readonly ObjectNodeSection DataCountSection = new ObjectNodeSection("wasm.datacount", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection CombinedDataSection = new ObjectNodeSection("wasm.alldata", SectionType.Writeable, needsAlign: false);
        public static readonly ObjectNodeSection FunctionSection = new ObjectNodeSection("wasm.function", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ExportSection = new ObjectNodeSection("wasm.export", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ElementSection = new ObjectNodeSection("wasm.element", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection MemorySection = new ObjectNodeSection("wasm.memory", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection TableSection = new ObjectNodeSection("wasm.table", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ImportSection = new ObjectNodeSection("wasm.import", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection GlobalSection = new ObjectNodeSection("wasm.global", SectionType.ReadOnly, needsAlign: false);
    }

    /// <summary>
    /// Wasm object file format writer.
    /// </summary>
    internal sealed class WasmObjectWriter : ObjectWriter
    {
        protected override CodeDataLayout LayoutMode => CodeDataLayout.Separate;

        // We use 2 Wasm data segments for webcil,
        // 1 for the payload size, and the second for the payload itself.
        const int NumDataSegments = 2;

        public WasmObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
        }

        private void EmitWasmHeader(Stream outputFileStream)
        {
            outputFileStream.Write("\0asm"u8);
            outputFileStream.Write([0x1, 0x0, 0x0, 0x0]);
        }

        private Dictionary<Utf8String, int> _uniqueSignatures = new();
        private Dictionary<string, int> _uniqueSymbols = new();
        private int _methodCount = 0;

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

        private protected override void RecordMethodSignature(WasmTypeNode signature)
        {
            var mangledNameBuilder = new Utf8StringBuilder();
            signature.AppendMangledName(_nodeFactory.NameMangler, mangledNameBuilder);
            Utf8String mangledName = mangledNameBuilder.ToUtf8String();
            // Note that we do not expect duplicates here, crossgen should deduplicate signatures already
            // using the node cache, so we can simply add the new signature with the next available index.
            _uniqueSignatures.Add(mangledName, _uniqueSignatures.Count);
        }

        private protected override void RecordMethodDeclaration(INodeWithTypeSignature node)
        {
            WasmLowering.LoweringFlags flags = WasmLowering.LoweringFlags.None;
            if (node.HasGenericContextArg)
            {
                flags |= WasmLowering.LoweringFlags.HasGenericContextArg;
            }
            if (node.IsAsyncCall)
            {
                flags |= WasmLowering.LoweringFlags.IsAsyncCall;
            }
            if (node.IsUnmanagedCallersOnly)
            {
                flags |= WasmLowering.LoweringFlags.IsUnmanagedCallersOnly;
            }
            WriteSignatureIndexForFunction(node.Signature, flags, node);

            _uniqueSymbols.Add(node.GetMangledName(_nodeFactory.NameMangler), _methodCount);
            _methodCount++;
        }

        private void WriteSignatureIndexForFunction(MethodSignature managedSignature, WasmLowering.LoweringFlags flags, ISymbolNode node)
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.FunctionSection);

            WasmFuncType signature = WasmLowering.GetSignature(managedSignature, flags);
            Utf8String key = signature.GetMangledName(_nodeFactory.NameMangler);
            if (!_uniqueSignatures.TryGetValue(key, out int signatureIndex))
            {
                throw new InvalidOperationException($"Signature index of {key} not found for function: {node.ToString()}");
            }

            writer.WriteULEB128((ulong)signatureIndex);
        }

        private int _numImports;
        private int _numImportedGlobals;
        /// <summary>
        /// Writes the given import entry, including its prefix (module/name/kind) and body (external ref).
        /// </summary>
        private SectionWriter WriteImport(WasmImport import)
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.ImportSection);
            writer.WriteUtf8WithLength(import.Module);
            writer.WriteUtf8WithLength(import.Name);
            writer.WriteByte((byte)import.Kind);

            int encodeSize = import.EncodeSize();
            int bytesWritten = import.Encode(writer.Buffer.GetSpan(encodeSize));
            Debug.Assert(bytesWritten == encodeSize);
            writer.Buffer.Advance((int)bytesWritten);

            _numImports++;
            return writer;
        }

        /// <summary>
        /// WebAssembly export descriptor kinds per the spec.
        /// </summary>
        internal enum WasmExportKind : byte
        {
            Function = 0x00,
            Table = 0x01,
            Memory = 0x02,
            Global = 0x03
        }

        private int _numExports;
        private void WriteExport(string name, WasmExportKind kind, int index)
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.ExportSection);
            int length = Encoding.UTF8.GetByteCount(name);
            writer.WriteULEB128((ulong)length);
            writer.WriteUtf8StringNoNull(name);
            writer.WriteByte((byte)kind);
            writer.WriteULEB128((ulong)index);
            _numExports++;
        }

        // Convenience methods for specific export types
        private void WriteFunctionExport(string name, int functionIndex) =>
            WriteExport(name, WasmExportKind.Function, functionIndex);

        private void WriteTableExport(string name, int tableIndex) =>
            WriteExport(name, WasmExportKind.Table, tableIndex);

        private void WriteMemoryExport(string name, int memoryIndex) =>
            WriteExport(name, WasmExportKind.Memory, memoryIndex);

        private void WriteGlobalExport(string name, int globalIndex) =>
            WriteExport(name, WasmExportKind.Global, globalIndex);

        private int _numElements;
        private void WriteRefFuncFunctionElement(ReadOnlySpan<int> functionIndices)
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.ElementSection);
            // e0:expr y*:list(funcidx)
            //  elem (ref func) (ref.func y)* (passive 0 e0)
            writer.WriteByte(1); // Passive element segment
            writer.WriteByte(0); // element type: ref func


            writer.WriteULEB128((ulong)functionIndices.Length);

            foreach (int index in functionIndices)
                writer.WriteULEB128((ulong)index);

            _numElements++;
        }

        private List<WasmSection> _sections = new();
        private Dictionary<string, int> _sectionNameToIndex = new();
        private Dictionary<ObjectNodeSection, WasmSectionType> _sectionToType = new()
        {
            { WasmObjectNodeSection.MemorySection, WasmSectionType.Memory },
            { WasmObjectNodeSection.FunctionSection, WasmSectionType.Function },
            { WasmObjectNodeSection.TableSection, WasmSectionType.Table },
            { WasmObjectNodeSection.ElementSection, WasmSectionType.Element },
            { WasmObjectNodeSection.ExportSection, WasmSectionType.Export },
            { WasmObjectNodeSection.ImportSection, WasmSectionType.Import },
            { WasmObjectNodeSection.GlobalSection, WasmSectionType.Global },
            { ObjectNodeSection.WasmTypeSection, WasmSectionType.Type },
            { ObjectNodeSection.WasmCodeSection, WasmSectionType.Code },
            { WasmObjectNodeSection.DataCountSection, WasmSectionType.DataCount },
        };

        private WasmSectionType GetWasmSectionType(ObjectNodeSection section)
        {
            if (!_sectionToType.ContainsKey(section))
            {
                // All other sections map to generic data segments in Wasm
                // TODO-WASM: Consider making the mapping explicit for every possible node type.
                return WasmSectionType.Data;
            }
            return _sectionToType[section];
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
                    Global.Get(WasmObjectWriter.TableBaseGlobalIndex),
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
                    Global.Get(WasmObjectWriter.TableBaseGlobalIndex), // (global.get $tableBase)
                    I32.Store((ulong)WebcilEncoder.TableBaseOffset), // i32.store offset=TableBaseOffset
                    Block.End
                ]
        );

        // This effectively recreates the logic of RecordMethodBody/RecordMethodDeclaration, but for manually inserted stubs that are not
        // represented by nodes in the dependency graph.
        // TODO-Wasm: for maintability, we should try and push some of this into the dependency graph when we do more stub generation.
        private void RegisterStubIndexAndSignature(WasmFunctionBody body)
        {
            Utf8String signatureKey = body.Signature.GetMangledName(_nodeFactory.NameMangler);
            if (!_uniqueSignatures.TryGetValue(signatureKey, out int signatureIndex))
            {
                signatureIndex = _uniqueSignatures.Count;
                _uniqueSignatures.Add(signatureKey, signatureIndex);

                SectionWriter typeSectionWriter = GetOrCreateSection(ObjectNodeSection.WasmTypeSection);
                byte[] encodedSignature = new byte[body.Signature.EncodeSize()];
                body.Signature.Encode(encodedSignature);
                typeSectionWriter.EmitData(encodedSignature);
            }

            SectionWriter functionSectionWriter = GetOrCreateSection(WasmObjectNodeSection.FunctionSection);
            functionSectionWriter.WriteULEB128((ulong)signatureIndex);
        }

        private void InsertWasmStub(Utf8String name, WasmFunctionBody body)
        {
            SectionWriter codeWriter = GetOrCreateSection(ObjectNodeSection.WasmCodeSection);

            int codeSize = body.EncodeSize();
            byte[] data = new byte[codeSize];
            body.Encode(data);

            // The code writer should already be set up to write the function body size as a prefix, so just emit the function body here
            Debug.Assert(codeWriter.HasLengthPrefix);
            codeWriter.EmitData(data);
            _uniqueSymbols.Add(name.ToString(), _methodCount);
            _methodCount++;

            RegisterStubIndexAndSignature(body);

        }
        private long ResolveSymbolRVA(WebcilSection[] sections, SymbolDefinition definition)
        {
            for (int i = 0; i < sections.Length; i++)
            {
                WebcilSection section = sections[i];
                if (definition.SectionIndex == section.Index)
                {
                    return section.Header.VirtualAddress + definition.Value;
                }
            }

            return 0;
        }

        public const int WebcilSectionAlignment = 16;

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

                uint rawSectionSize = (uint)webcilSection.Stream.Length;
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
            WebcilSection[] webcilSections = _sections.OfType<WebcilSection>().ToArray();

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
                Debug.Assert(webcilSections[webcilSections.Length - 1].Name.ToString() == "reloc");
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

        private protected override SectionWriter.Params WriterParams(ObjectNodeSection section)
        {
            if (section == ObjectNodeSection.WasmCodeSection)
            {
                return new SectionWriter.Params
                {
                    LengthEncodeFormat = LengthEncodeFormat.ULEB128
                };
            }

            return new SectionWriter.Params
            {
                LengthEncodeFormat = LengthEncodeFormat.None
            };
        }


        private protected override void CreateSection(ObjectNodeSection section, Utf8String comdatName, Utf8String symbolName, int sectionIndex, Stream sectionStream)
        {
            WasmSectionType sectionType = GetWasmSectionType(section);
            WasmSection wasmSection = null;
            if (sectionType == WasmSectionType.Data)
            {
#if READYTORUN
                // This is a section which is internally wrapping a Webcil section
                wasmSection = new WebcilSection(new Utf8String(section.Name), default(WebcilSectionHeader), sectionStream, sectionIndex);
#else
                wasmSection = new WasmSection(WasmSectionType.Data, sectionStream, new Utf8String(section.Name));
#endif
            }
            else
            {
                wasmSection = new WasmSection(sectionType, sectionStream, new Utf8String(section.Name));
            }


            Debug.Assert(_sections.Count == sectionIndex);
            _sections.Add(wasmSection);
            _sectionNameToIndex.Add(section.Name, sectionIndex);
        }

        private void WriteDataCountSection()
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.DataCountSection);
            writer.WriteULEB128(NumDataSegments); // number of data segments
        }

        private WebcilSegment _webcilSegment = null;
        private protected override void EmitSectionsAndLayout()
        {
            int totalMethodCount = _methodCount + 3;
            InsertWasmStub(new Utf8String("getWebcilSize"), GetWebcilSize);
            InsertWasmStub(new Utf8String("getWebcilPayload"), GetWebcilPayload);
            InsertWasmStub(new Utf8String("fillWebcilTable"), FillWebcilTable(totalMethodCount));
            Debug.Assert(_methodCount == totalMethodCount);

            WriteDataCountSection();

            PrependCount(SectionByName(ObjectNodeSection.WasmCodeSection.Name), _methodCount);
        }

        private int _numDefinedGlobals = 0;
        private int NextGlobalIndex()
        {

            int next = _numImportedGlobals + _numDefinedGlobals;
            _numDefinedGlobals++;
            return next;
        }

        private Dictionary<string, WasmGlobal> _definedGlobals = new();

        // TODO-Wasm: In the future, we may want to consider representing Wasm globals in the dependency graph so that they
        // can be referenced by other nodes and we can make effective use of them.  
        private void WriteGlobal(SectionWriter writer, string name, WasmValueType valueType, WasmMutabilityType mutability, WasmInstructionGroup initExpr)
        {
            WasmGlobal global = new WasmGlobal(
                index: NextGlobalIndex(), // next available index
                name: name,
                valueType,
                mutability,
                initExpr);
            bool added = _definedGlobals.TryAdd(name, global);
            Debug.Assert(added, $"Duplicate global name: {name}");

            int size = global.EncodeSize();
            int written = global.Encode(writer.Buffer.GetSpan(size));
            Debug.Assert(written == size);
            writer.Buffer.Advance(written);
        }


        private void WriteGlobalSection()
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.GlobalSection);

            // webcilVersion: i32 const = 0
            WriteGlobal(writer, "webcilVersion", WasmValueType.I32, WasmMutabilityType.Const,
                new WasmInstructionGroup([new WasmConstExpr(WasmExprKind.I32Const, WebcilConstants.WC_VERSION_MAJOR)]));
        }

        private void PrependCount(WasmSection section, int count)
        {
            section.PrependCount = count;
        }

        private WasmSection SectionByName(string name)
        {
            int index = _sectionNameToIndex[name];
            return _sections[index];
        }

        // Sections excluding Webcil Data segment
        readonly string[] SectionOrder =
        [
            ObjectNodeSection.WasmTypeSection.Name,
            WasmObjectNodeSection.ImportSection.Name,
            WasmObjectNodeSection.FunctionSection.Name,
            WasmObjectNodeSection.GlobalSection.Name,
            WasmObjectNodeSection.ExportSection.Name,
            WasmObjectNodeSection.ElementSection.Name,
            WasmObjectNodeSection.DataCountSection.Name,
            ObjectNodeSection.WasmCodeSection.Name,
        ];

        private int[] _sectionEmitOrder = null;
        private int[] SectionEmitOrder
        {
            get
            {
                if (_sectionEmitOrder == null)
                {
                    _sectionEmitOrder = SectionOrder
                        .Where(name => _sectionNameToIndex.ContainsKey(name))
                        .Select(name => _sectionNameToIndex[name])
                        .ToArray();
                }

                return _sectionEmitOrder;
            }
        }

        private static readonly ObjectNodeSection WebcilRelocSection = new ObjectNodeSection("reloc", SectionType.ReadOnly);
        private void EmitRelocSectionData()
        {
            var writer = GetOrCreateSection(WebcilRelocSection);
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
                GetOrCreateSection(WebcilRelocSection);
            }

            WebcilSection[] webcilSections = _sections.OfType<WebcilSection>().ToArray();
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
            // Writing element counts <- imports being finalized.
            EmitSectionElementCounts();

           /*********************************************************************
           * Write Wasm Sections, Excluding Data
           *********************************************************************/

            EmitWasmHeader(outputFileStream);
            foreach (int index in SectionEmitOrder)
            {
                WasmSection section = _sections[index];
                if (_resolvableRelocations.TryGetValue(index, out List<SymbolicRelocation> relocations) &&
                    section.Type is not WasmSectionType.Data)
                {
                    using (Stream originalStream = section.Stream)
                    {
                        MemoryStream stream = new MemoryStream((int)originalStream.Length);
                        originalStream.Position = 0;
                        originalStream.CopyTo(stream);
                        ResolveRelocations(index, stream, relocations, sectionStart: 0);
                        section.Stream = stream;
                        // originalStream may be disposed, section.Stream now points to resolved stream
                    }
                }

                section.Emit(outputFileStream);
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
                section.Stream.Position = 0;
                section.Stream.CopyTo(webcilStream);
                long bytesWritten = (long)webcilStream.Position - (long)section.Header.PointerToRawData;
                Debug.Assert(section.Header.SizeOfRawData - bytesWritten == section.Padding, $"Unexpected padding: {section.Header.SizeOfRawData - bytesWritten} != {section.Padding}");

                if (_resolvableRelocations.TryGetValue(section.Index, out List<SymbolicRelocation> relocations))
                {
                    // We emit all Webcil sections into one stream, and resolve relocations directly into this combined stream.
                    // As a result, the section-relative offsets that relocs in our list have need to be calculated based on the section's
                    // position within the Webcil segment
                    ResolveRelocations(section.Index, webcilStream, relocations, sectionStart: (long)section.Header.PointerToRawData);
                }
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
            BinaryPrimitives.WriteUInt32LittleEndian(lengthBuffer.AsSpan().Slice(4), (uint)_uniqueSymbols.Count);
            MemoryStream webcilSizeSegmentStream = new MemoryStream(lengthBuffer);
            WasmDataSegment webcilSizeSegment = new WasmDataSegment(webcilSizeSegmentStream, new Utf8String("webcilCount"),
                WasmDataSectionType.Passive, null);

            // Passive data segment for webcil payload contents
            WasmDataSegment webcilContentsSegment = new WasmDataSegment(webcilStream, new Utf8String("webcilPayload"),
                WasmDataSectionType.Passive, null);

            // Create combined data section and emit 
            WasmDataSection dataSection = new WasmDataSection([webcilSizeSegment, webcilContentsSegment], new Utf8String("data"), contentAlign: 4);
            dataSection.Emit(outputFileStream);
#endif
        }

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

        // TODO-WASM: Currently, all Wasm relocs are resolved to 5 byte values unconditionally (the same size as the original placeholder padding), which is wasteful.
        // We should remove the padding and shrink the resolved values to their minimal size so we don't bloat the binary size.
#nullable enable
        private unsafe void ResolveRelocations(int sectionIndex, MemoryStream sectionStream, List<SymbolicRelocation> relocs, long sectionStart = 0)
        {
            byte[] relocScratchBuffer = new byte[Relocation.MaxSize];

            WebcilSection? curSectionAsWebcil = null;
            uint webcilVirtualStart = 0;
            if (_sections[sectionIndex] is WebcilSection curSection)
            {
                curSectionAsWebcil = curSection;
                webcilVirtualStart = curSection.Header.VirtualAddress;
            }

            // If we have a webcil section, we expect it to have a nonzero section start. This is because for webcil,
            // we should have written the webcil header and each of the section headers (always non-zero size) before any
            // section contents
            Debug.Assert(curSectionAsWebcil is null || sectionStart != 0);

            foreach (SymbolicRelocation reloc in relocs)
            {
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

                // TODO-Wasm: Enforce the below boolean as an assert once we are emitting proper Wasm code
                // relocs for all code containing nodes
                // ---> bool betweenWebcilSections = false;
                if (_sections[definedSymbol.SectionIndex] is WebcilSection targetSection)
                {
                    symbolWebcilSection = targetSection;
                    virtualSymbolImageOffset = symbolWebcilSection.Header.VirtualAddress + (uint)definedSymbol.Value;
                    Debug.Assert(IsWithinSection(virtualSymbolImageOffset, symbolWebcilSection));
                }

                // We need a pinned raw pointer here for manipulation with Relocation.WriteValue
                fixed (byte* pData = ReadRelocToDataSpan(reloc, relocScratchBuffer, sectionStart))
                {
                    long addend = Relocation.ReadValue(reloc.Type, pData);
                    int relocLength = Relocation.GetSize(reloc.Type);

                    switch (reloc.Type)
                    {
                        case RelocType.WASM_TYPE_INDEX_LEB:
                        {
                            if (_uniqueSignatures.TryGetValue(reloc.SymbolName, out int index))
                            {
                                Relocation.WriteValue(reloc.Type, pData, index);
                            }
                            else
                            {
                                throw new InvalidDataException($"Type signature symbol definition '{reloc.SymbolName}' not found");
                            }

                            break;
                        }

                        // TODO-Wasm: None of the IMAGE_REL type relocs should occur in Wasm
                        // code, and we should add asserts for this once we've updated the necessary
                        // dependency nodes to emit the proper reloc type on Wasm.
                        case RelocType.IMAGE_REL_BASED_ABSOLUTE:
                            // No action required
                            break;

                        case RelocType.IMAGE_REL_BASED_DIR64:
                        case RelocType.IMAGE_REL_BASED_HIGHLOW:
                            //       Debug.Assert(betweenWebcilSections);
                            // This is an ImageBase-relative value in PE, but our image base
                            // for Webcil is virtual address 0
                            Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset + 0 + addend);
                            break;
                        case RelocType.IMAGE_REL_BASED_ADDR32NB:
                            //       Debug.Assert(betweenWebcilSections);
                            Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset + addend);
                            break;
                        case RelocType.IMAGE_REL_BASED_REL32:
                        case RelocType.IMAGE_REL_BASED_RELPTR32:
                            //      Debug.Assert(betweenWebcilSections);
                            Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset - (virtualRelocOffset + relocLength) + addend);
                            break;
                        case RelocType.IMAGE_REL_FILE_ABSOLUTE:
                            //       Debug.Assert(betweenWebcilSections && symbolWebcilSection != null);
                            Debug.Assert(symbolWebcilSection != null);
                            long fileOffset = symbolWebcilSection.Header.PointerToRawData + definedSymbol.Value;
                            Relocation.WriteValue(reloc.Type, pData, fileOffset + addend);
                            break;
                        case RelocType.WASM_MEMORY_ADDR_REL_SLEB:
                        {
                            // These relocs should be for cases of the form:
                            //  global.get __image_base
                            //  i32.const <reloc>
                            //  i32.add
                            //  i32.load 0
                            // So, the relocated address value should always represent an offset relative to image base. 
                            // This offset should ALWAYS be equal to the actual offset from image base at runtime, due to Webcil's
                            // flag mapping
                            if (symbolWebcilSection is null)
                            {
                                throw new InvalidDataException();
                            }

                            Relocation.WriteValue(reloc.Type, pData, virtualSymbolImageOffset + addend);
                            break;
                        }
                        case RelocType.WASM_TABLE_INDEX_I32:
                        case RelocType.WASM_TABLE_INDEX_I64:
                        case RelocType.WASM_TABLE_INDEX_SLEB:
                        {
                            string symbolName = reloc.SymbolName.ToString();
                            int index = _uniqueSymbols[symbolName];
                            // Here, we are effectively writing a table offset relative to the table_base.
                            // These will need to be fixed up by the runtime after load by adding __image_function_pointer_base
                            // TODO-WASM: We need to emit these for fixup with an addend at runtime
                            Relocation.WriteValue(reloc.Type, pData, index);
                            break;
                        }
                        case RelocType.WASM_FUNCTION_INDEX_LEB:
                        {
                            string symbolName = reloc.SymbolName.ToString();
                            int index = _uniqueSymbols[symbolName];

                            // These are module-local function pointer indices, so we can simply write out the assigned function index
                            // for this particular symbol
                            Relocation.WriteValue(reloc.Type, pData, index);
                            break;
                        }
                        default:
                            // TODO-WASM: add other cases as needed;
                            // ignoring other reloc types for now
                            throw new NotSupportedException($"Relocation type {reloc.Type} not yet implemented");
                    }

                    WriteRelocFromDataSpan(reloc, pData, sectionStart);
                }
            }

            Span<byte> ReadRelocToDataSpan(SymbolicRelocation reloc, byte[] buffer, long sectionStart)
            {
                Span<byte> relocContents = buffer.AsSpan(0, Relocation.GetSize(reloc.Type));
                sectionStream.Position = reloc.Offset + sectionStart;
                sectionStream.ReadExactly(relocContents);
                return relocContents;
            }

            void WriteRelocFromDataSpan(SymbolicRelocation reloc, byte* pData, long sectionStart)
            {
                sectionStream.Position = reloc.Offset + sectionStart;
                sectionStream.Write(new Span<byte>(pData, Relocation.GetSize(reloc.Type)));
            }
        }
#nullable disable

        public const int StackPointerGlobalIndex = 0;
        public const int ImageBaseGlobalIndex = 1;
        public const int TableBaseGlobalIndex = 2;

        private WasmImport[] _defaultGlobalImports = new[]
        {
            new WasmImport("webcil", "stackPointer", import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Mut), index: StackPointerGlobalIndex),
            new WasmImport("webcil", "imageBase", import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Const), index: ImageBaseGlobalIndex),
            new WasmImport("webcil", "tableBase", import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Const), index: TableBaseGlobalIndex),
            new WasmImport("webcil", "table", import: new WasmTableImportType(), index: 0),
        };

        private void WriteImports()
        {
            int[] assignedImportIndices = new int[(int)WasmExternalKind.Count];
            foreach (WasmImport import in _defaultGlobalImports)
            {
                if (import.Index.HasValue)
                {
                    int assigned = assignedImportIndices[(int)import.Kind];
                    Debug.Assert(assigned == import.Index.Value, $"Import {import.Module}.{import.Name} of kind {import.Kind} assigned index {assigned}, needs {import.Index.Value}");
                }
                assignedImportIndices[(int)import.Kind]++;
                WriteImport(import);
            }

            _numImportedGlobals = assignedImportIndices[(int)WasmExternalKind.Global];
        }

        private void WriteMemoryImport(ulong contentSize)
        {
            uint dataPages = checked((uint)((contentSize + (1 << 16) - 1) >> 16));
            uint numPages = Math.Max(dataPages, 1); // Ensure at least one page is allocated for the minimum

            WasmImport memoryImport = new WasmImport("webcil", "memory", import: new WasmMemoryImportType(WasmLimitType.HasMin, numPages)); // memory limits: flags (0 = only minimum)
            WriteImport(memoryImport);
        }

        private void WriteExports()
        {
            WriteTableExport("table", 0);

            Debug.Assert(_definedGlobals.ContainsKey("webcilVersion"));
            WriteGlobalExport("webcilVersion", _definedGlobals["webcilVersion"].Index);

            string[] functionExports = _uniqueSymbols.Keys.ToArray();
            // TODO-WASM: Handle exports better (e.g., only export public methods, etc.)
            // Also, see if we could leverage definedSymbols for this instead of doing our own bookkeeping in _uniqueSymbols.
            foreach (string name in functionExports.OrderBy(name => name))
            {
                WriteFunctionExport(name, _uniqueSymbols[name]);
            }
        }

        private Dictionary<Utf8String, SymbolDefinition> _definedSymbols;
        private void WriteElements()
        {
            // Generate the function pointer table element that contains function pointers for all of our functions
            int[] functionIndices = new int[_uniqueSymbols.Count];
            // NOTE: This relies on items in _uniqueSymbols being assigned sequentially and that iteration over Values is order-preserving.
            // BCL Dictionary preserves insertion order so as long as we keep using it, we would get the function indices in the order they were added.
            _uniqueSymbols.Values.CopyTo(functionIndices, 0);
            // Enforce that the function pointers are sequential so that (image_function_pointer_base + 0) == ftn index 0
#if DEBUG
            for (int i = 0; i < _uniqueSymbols.Count; i++) {
                Debug.Assert(functionIndices[i] == i);
            }
#endif
            WriteRefFuncFunctionElement(functionIndices);
        }

        // For now, this function just prepares the function, exports, and type sections for emission by prepending the counts.
        private protected override void EmitSymbolTable(IDictionary<Utf8String, SymbolDefinition> definedSymbols, SortedSet<Utf8String> undefinedSymbols)
        {
            WriteImports();
            WriteGlobalSection();
            WriteExports();
            WriteElements();

            // Register defined symbols for future use during relocation resolution
            _definedSymbols = new Dictionary<Utf8String, SymbolDefinition>(definedSymbols);
        }

        private void EmitSectionElementCounts()
        {
            int funcIdx = _sectionNameToIndex[WasmObjectNodeSection.FunctionSection.Name];
            PrependCount(_sections[funcIdx], _methodCount);

            int typeIdx = _sectionNameToIndex[ObjectNodeSection.WasmTypeSection.Name];
            PrependCount(_sections[typeIdx], _uniqueSignatures.Count);

            int exportIdx = _sectionNameToIndex[WasmObjectNodeSection.ExportSection.Name];
            PrependCount(_sections[exportIdx], _numExports);

            if (_sectionNameToIndex.TryGetValue(WasmObjectNodeSection.ElementSection.Name, out int elementIdx))
            {
                PrependCount(_sections[elementIdx], _numElements);
            }

            PrependCount(SectionByName(WasmObjectNodeSection.ImportSection.Name), _numImports);

            PrependCount(SectionByName(WasmObjectNodeSection.GlobalSection.Name), _numDefinedGlobals);
        }
    }


    internal class WasmSection
    {
        public WasmSectionType Type { get; }
        public Utf8String Name { get; }

        public int? PrependCount = null;
        public int PrependCountSize => PrependCount.HasValue ? (int)DwarfHelper.SizeOfULEB128((ulong)PrependCount.Value) : 0;

        private int EncodePrependCount(Span<byte> dest)
        {
            if (PrependCount.HasValue)
            {
                return DwarfHelper.WriteULEB128(dest, (ulong)PrependCount.Value);
            }

            return 0;
        }

        public Stream Stream
        {
            get
            {
                Debug.Assert(_dataStream != null, $"{this.Name} has null data stream");
                return _dataStream;
            }

            set
            {
                Debug.Assert(value != null);
                _dataStream = value;
            }
        }

        Stream _dataStream;

        public virtual int HeaderSize
        {
            get
            {
                uint sizeEncodeLength = DwarfHelper.SizeOfULEB128((ulong)ContentSize);
                return 1 + (int)sizeEncodeLength;
            }
        }

        public virtual int ContentSize => (int)_dataStream.Length + PrependCountSize;

        public virtual int EncodeSize()
        {
            return HeaderSize + ContentSize;
        }

        public virtual int EncodeHeader(Span<byte> headerBuffer)
        {
            ulong contentSize = (ulong)ContentSize;
            uint encodeLength = DwarfHelper.SizeOfULEB128(contentSize);

            // Section header consists of:
            // 1 byte: section type
            // ULEB128: size of section
            headerBuffer[0] = (byte)Type;
            DwarfHelper.WriteULEB128(headerBuffer.Slice(1), contentSize);

            return 1 + (int)encodeLength;
        }

        public virtual int Emit(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            EncodeHeader(headerBuffer);

            outputFileStream.Write(headerBuffer);

            if (PrependCount.HasValue)
            {
                Span<byte> prependCount = stackalloc byte[PrependCountSize];
                int encoded = EncodePrependCount(prependCount);
                outputFileStream.Write(prependCount);
            }

            Stream.Position = 0;
            Stream.CopyTo(outputFileStream);

            return HeaderSize + (int)(PrependCountSize + Stream.Length);
        }

        public WasmSection(WasmSectionType type, Stream stream, Utf8String name, int? prependCount = null)
        {
            Type = type;
            Name = name;
            _dataStream = stream;
            PrependCount = prependCount;
        }
    }

    internal class WasmDataSection : WasmSection
    {
        private List<WasmDataSegment> _segments;
        public List<WasmDataSegment> Segments => _segments;
        private int _contentAlign = 1;
        public WasmDataSection(List<WasmDataSegment> segments, Utf8String name, int contentAlign = 1)
            : base(WasmSectionType.Data, null, name)
        {
            _segments = segments;
            _contentAlign = contentAlign;
        }

        public override int ContentSize
        {
            get
            {
                int size = 0;
                size += (int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count);
                foreach (WasmDataSegment segment in _segments)
                {
                    size += segment.EncodeSize();
                }

                return size;
            }
        }

        public override int EncodeHeader(Span<byte> headerBuffer)
        {
            uint encodeLength = Relocation.WASM_PADDED_RELOC_SIZE_32;

            headerBuffer[0] = (byte)Type;
            DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(1), (ulong)ContentSize);
            Debug.Assert(headerBuffer.Slice(1).Length == Relocation.WASM_PADDED_RELOC_SIZE_32);
            ulong readCheck = DwarfHelper.ReadULEB128(headerBuffer.Slice(1));
            Debug.Assert((int)readCheck == ContentSize);

            return 1 + (int)encodeLength;
        }

        public override int HeaderSize => 1 + Relocation.WASM_PADDED_RELOC_SIZE_32;

        public override int Emit(Stream outputFileStream)
        {
            int size = 0;
            int headerPosition = (int)outputFileStream.Position;

            // seek forward past pre-allocated header portion
            outputFileStream.Position += (int)HeaderSize;
            size += (int)HeaderSize;

            Span<byte> countBuffer = stackalloc byte[(int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count)];
            int countSize = DwarfHelper.WriteULEB128(countBuffer, (ulong)_segments.Count);
            outputFileStream.Write(countBuffer.Slice(0, countSize));
            size += countSize;

            for (int i = 0; i < _segments.Count; i++)
            {
                WasmDataSegment segment = _segments[i];
                // Do we have a next segment?
                if ((i + 1) < _segments.Count)
                {
                    // Calculate end padding to insert after end of this segment's contents, before the wasm header for the next section
                    // to ensure that the next section's content is aligned at the file level
                    int position = (int)outputFileStream.Position + segment.HeaderSize + (int)segment.RawContentSize + _segments[i + 1].HeaderSize;
                    int padding = AlignmentHelper.AlignUp(position, _contentAlign) - position;
                    segment.Padding = padding;
                }
                else
                {
                    segment.Padding = 0;
                }
                size += segment.Emit(outputFileStream);
            }

            // Write the header (this must be done second because we first need to determine inter-segment padding based on file placement)
            outputFileStream.Position = headerPosition;
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int wroteHeaderSize = EncodeHeader(headerBuffer);
            Debug.Assert(wroteHeaderSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            outputFileStream.Seek(0, SeekOrigin.End);

            return size;
        }
    }

    internal enum WasmDataSectionType : byte
    {
        Active = 0,  // (data list(byte) (active offset-expr))
        Passive = 1, // (data list(byte) passive)
        ActiveMemorySpecified = 2 // (data list(byte) (active memidx offset-expr))
    }

    internal class WasmDataSegment
    {
        // The segments are not sections per se, but they represent data segments within the data section.
        Stream _stream;
        WasmDataSectionType _type;
        WasmInstructionGroup _initExpr;
        private PaddingHelper _paddingHelper;

        public WasmDataSegment(Stream contents, Utf8String name, WasmDataSectionType type, WasmInstructionGroup initExpr)
        {
            _stream = contents;
            _type = type;
            _initExpr = initExpr;
            _paddingHelper = new PaddingHelper(4);
        }

        public int HeaderSize
        {
            get
            {
                return _type switch
                {
                    WasmDataSectionType.Active =>
                        (int)DwarfHelper.SizeOfULEB128((ulong)_type) + // type indicator
                        _initExpr.EncodeSize() + // init expr encodeSize
                        Relocation.WASM_PADDED_RELOC_SIZE_32, // encode size of data length
                    WasmDataSectionType.Passive =>
                        (int)DwarfHelper.SizeOfULEB128((ulong)_type) +
                        Relocation.WASM_PADDED_RELOC_SIZE_32, // encode size of data length
                    _ =>
                        throw new NotImplementedException()
                };
            }
        }

        public int EncodeSize()
        {
            return HeaderSize + ContentSize;
        }

        private bool _paddingSet = false;
        int _padding = 0;
        public int Padding
        {
            set
            {
                _paddingSet = true;
                _padding = value;
            }
            get
            {
                Debug.Assert(_paddingSet);
                return _padding;
            }
        }

        public int ContentSize => (int)_stream.Length + Padding;
        public int RawContentSize => (int)_stream.Length;

        public int EncodeHeader(Span<byte> headerBuffer)
        {
            switch (_type)
            {
                case WasmDataSectionType.Active:
                {
                    int len = 0;
                    len = DwarfHelper.WriteULEB128(headerBuffer, (ulong)_type);
                    len += _initExpr.Encode(headerBuffer.Slice(len));
                    Debug.Assert(headerBuffer.Slice(len).Length == Relocation.WASM_PADDED_RELOC_SIZE_32);
                    DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(len), (ulong)ContentSize);
                    len += headerBuffer.Slice(len).Length;
                    return len;
                }
                case WasmDataSectionType.Passive:
                {
                    int len = 0;
                    len = DwarfHelper.WriteULEB128(headerBuffer, (ulong)_type);
                    Debug.Assert(headerBuffer.Slice(len).Length == Relocation.WASM_PADDED_RELOC_SIZE_32, $"{headerBuffer.Slice(len).Length} != {Relocation.WASM_PADDED_RELOC_SIZE_32}");
                    DwarfHelper.WritePaddedULEB128(headerBuffer.Slice(len), (ulong)ContentSize);
                    len += headerBuffer.Slice(len).Length;
                    return len;
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public int Emit(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = EncodeHeader(headerBuffer);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            _stream.Position = 0;
            _stream.CopyTo(outputFileStream);
            _paddingHelper.PadStream(outputFileStream, (int)Padding);

            return headerSize + (int)_stream.Length + Padding;
        }
    }

#if READYTORUN
    class WebcilSection : WasmSection
    {
        public readonly int Index;
        public WebcilSectionHeader Header;
        public readonly Stream _stream;
        private PaddingHelper _paddingHelper;
        public int MinAlignment = 1;

        public uint Padding => Header.SizeOfRawData - (uint)_stream.Length;

        public WebcilSection(Utf8String name, WebcilSectionHeader header, Stream stream, int index)
            : base(WasmSectionType.Data, stream, name)
        {
            Header = header;
            _stream = stream;
            Index = index;
            _paddingHelper = new PaddingHelper(WasmObjectWriter.WebcilSectionAlignment);
        }

        public override int EncodeSize()
        {
            return (int)_stream.Length;
        }

        public override int Emit(Stream outputFileStream)
        {
            // Emit the raw contents of this Webcil section followed by any required padding.
            _stream.Position = 0;
            _stream.CopyTo(outputFileStream);
            _paddingHelper.PadStream(outputFileStream, (int)Padding);

            return (int)_stream.Length + (int)Padding;
        }
    }
#endif
}
