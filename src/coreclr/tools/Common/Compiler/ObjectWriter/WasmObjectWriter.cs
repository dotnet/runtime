// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.Text;
using Internal.TypeSystem;
using CodeDataLayout = CodeDataLayoutMode.CodeDataLayout;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler.ObjectWriter
{
    public static class PaddingHelper
    {
        public static void PadStream(Stream s, int n, byte padByte = 0)
        {
            for (int i = 0; i < n; i++)
            {
                s.WriteByte(padByte);
            }
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
        private int _signatureCount = 0;
        private int _methodCount = 0;

        private Dictionary<SortableDependencyNode.ObjectNodeOrder, Utf8String> _wellKnownSymbols = new();
        private protected override void RecordWellKnownSymbol(Utf8String currentSymbolName, SortableDependencyNode.ObjectNodeOrder classCode)
        {
            if (classCode is SortableDependencyNode.ObjectNodeOrder.CorHeaderNode
                or SortableDependencyNode.ObjectNodeOrder.DebugDirectoryNode)
            {
                _wellKnownSymbols.Add(classCode, currentSymbolName);
            }
        }

        private protected override void RecordMethodSignature(WasmTypeNode signature)
        {
            int signatureIndex = _signatureCount;
            var mangledNameBuilder = new Utf8StringBuilder();
            signature.AppendMangledName(_nodeFactory.NameMangler, mangledNameBuilder);
            Utf8String mangledName = mangledNameBuilder.ToUtf8String();
            // Note that we do not expect duplicates here, since crossgen's node cache should handle this and all nodes representing
            // identical signatures in a module should point to the same node instance
            _uniqueSignatures.Add(mangledName, signatureIndex);
            _signatureCount++;
        }

        private protected override void RecordMethodDeclaration(ISymbolDefinitionNode symbol, MethodDesc desc)
        {
            WriteSignatureIndexForFunction(desc);

            _uniqueSymbols.Add(symbol.GetMangledName(_nodeFactory.NameMangler), _methodCount);
            _methodCount++;
        }

        private void WriteSignatureIndexForFunction(MethodDesc desc)
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.FunctionSection);

            WasmFuncType signature = Internal.JitInterface.WasmLowering.GetSignature(desc);
            Utf8String key = signature.GetMangledName(_nodeFactory.NameMangler);
            if (!_uniqueSignatures.TryGetValue(key, out int signatureIndex))
            {
                throw new InvalidOperationException($"Signature index of {key} not found for function: {desc.GetName()}");
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

        private List<WasmSection> _sections = new();
        private Dictionary<string, int> _sectionNameToIndex = new();
        private Dictionary<ObjectNodeSection, WasmSectionType> _sectionToType = new()
        {
            { WasmObjectNodeSection.MemorySection, WasmSectionType.Memory },
            { WasmObjectNodeSection.FunctionSection, WasmSectionType.Function },
            { WasmObjectNodeSection.TableSection, WasmSectionType.Table },
            { WasmObjectNodeSection.ExportSection, WasmSectionType.Export },
            { WasmObjectNodeSection.ImportSection, WasmSectionType.Import },
            { WasmObjectNodeSection.GlobalSection, WasmSectionType.Global },
            { ObjectNodeSection.WasmTypeSection, WasmSectionType.Type },
            { ObjectNodeSection.WasmCodeSection, WasmSectionType.Code },
            { WasmObjectNodeSection.DataCountSection, WasmSectionType.DataCount }
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
            // This is a no-op for now under Wasm
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
                size += WebcilHeader.EncodeSize(); // include header
                size += Sections.Length * WebcilSectionHeader.EncodeSize(); // include size of all section headers
                size = AlignmentHelper.AlignUp(size, WebcilSectionAlignment); // account for padding before first section

                foreach (WebcilSection section in Sections)
                {
                    size += (int)section.Header.SizeOfRawData; // include raw data size of each section (same as virtual size since Webcil has a flat mapping)
                }

                return size;
            }

            public long ResolveSymbolRVA(SymbolDefinition definition)
            {
                for (int i = 0; i < Sections.Length; i++)
                {
                    WebcilSection section = Sections[i];
                    if (definition.SectionIndex == section.Index)
                    {
                        return section.Header.VirtualAddress + definition.Value;
                    }
                }

                return 0;
            }
        }

        static WasmFunctionBody GetWebcilSize = new WasmFunctionBody(
            new WasmFuncType(new([WasmValueType.I32]), new([WasmValueType.I32])), // (func (destPtr i32) (result i32))
                [
                    Local.Get(0), // (local.get $destPtr)
                    I32.Const(0),
                    I32.Const(4),
                    Memory.Init(0)
                ]
        );

        static WasmFunctionBody GetWebcilPayload = new WasmFunctionBody(
            new WasmFuncType(new([WasmValueType.I32, WasmValueType.I32]), new([])), // (func ($d i32) ($n i32))
                [
                    Local.Get(0), // (local.get $d)
                    I32.Const(0),
                    Local.Get(1), // (local.get $n)
                    Memory.Init(1)
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
                Console.WriteLine($"Adding signature: {signatureKey}");
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
            Console.WriteLine($"Inserting stub: {name}");
            Console.WriteLine($"method count: {_methodCount}");
            Console.WriteLine($"unique signature count: {_uniqueSignatures.Count}");

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

        const int WebcilSectionAlignment = 16;
        private WebcilSegment BuildWebcilDataSegment()
        {
            WebcilSection[] webcilSections = _sections.Where(section => section is WebcilSection)
                                                        .Select(section => section as WebcilSection).ToArray();

            uint sizeOfHeaders = (uint)WebcilHeader.EncodeSize() + (uint)(webcilSections.Length * WebcilSectionHeader.EncodeSize());

            uint pointerToRawData = (uint)AlignmentHelper.AlignUp((int)sizeOfHeaders, (int)WebcilSectionAlignment);
            uint virtualAddress = pointerToRawData;

            for (int i = 0; i < webcilSections.Length; i++)
            {
                WebcilSection webcilSection = webcilSections[i];

                uint rawSectionSize = (uint)webcilSection.Stream.Length;
                uint alignedSectionSize = (uint)AlignmentHelper.AlignUp((int)rawSectionSize, (int)WebcilSectionAlignment);
                uint virtualSize = alignedSectionSize;
                WebcilSectionHeader sectionHeader = new WebcilSectionHeader
                {
                    VirtualAddress = virtualAddress,
                    VirtualSize = virtualSize,
                    SizeOfRawData = alignedSectionSize,
                    PointerToRawData = pointerToRawData
                };
                webcilSection.Header = sectionHeader;

                pointerToRawData += alignedSectionSize;
                virtualAddress += virtualSize;
            }

            WebcilHeader header = new WebcilHeader
            {
                Id = 0x4c496257, // 'WbCIL', little endian
                VersionMajor = 1,
                VersionMinor = 0,
                CoffSections = (ushort)webcilSections.Length,
                PeCliHeaderRva = 0, // This RVA will be resolved later
                PeCliHeaderSize = 0, // Resolved along with RVA
                PeDebugRva = 0, // This RVA will be resolved later
                PeDebugSize = 0 // Resolved along with RVA
            };


            return new WebcilSegment(header, webcilSections.ToArray());
        }

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
                // This is a section which is internally wrapping a Webcil section
                wasmSection = new WebcilSection(new Utf8String(section.Name), default(WebcilSectionHeader), sectionStream, sectionIndex);
            }
            else
            {
                wasmSection = new WasmSection(sectionType, sectionStream, new Utf8String(section.Name));
            }


            Debug.Assert(_sections.Count == sectionIndex);
            _sections.Add(wasmSection);
            _sectionNameToIndex.Add(section.Name, sectionIndex);
        }

        private void WriteMemorySection(ulong contentSize)
        {
            // TODO-WASM: Reserve an extra page or two for runtime stack as a temporary measure
            // pages are 64 kb each, so we need to calculate how many pages we need
            ulong numPages = (contentSize + (1 << 16) - 1) >> 16;

            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.MemorySection);
            writer.WriteByte(0x01); // number of memories
            writer.WriteByte(0x00); // memory limits: flags (0 = only minimum)
            writer.WriteULEB128(numPages); // memory limits: initial size in pages (64kb each)
        }

        private void WriteDataCountSection()
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.DataCountSection);
            writer.WriteULEB128(NumDataSegments); // number of data segments
        }

        private WebcilSegment _webcilSegment = null;
        private protected override void EmitSectionsAndLayout()
        {

            _webcilSegment = BuildWebcilDataSegment();
            InsertWasmStub(new Utf8String("getWebcilSize"), GetWebcilSize);
            InsertWasmStub(new Utf8String("getWebcilPayload"), GetWebcilPayload);
            WriteDataCountSection();

            Console.WriteLine("Done inserting stubs...");
            Console.WriteLine($"method count: {_methodCount}");
            Console.WriteLine($"unique signature count: {_uniqueSignatures.Count}");

            WriteTableSection();

            PrependCount(SectionByName(ObjectNodeSection.WasmCodeSection.Name), _methodCount);
        }

        private void WriteTableSection()
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.TableSection);
            writer.WriteByte(0x01); // number of tables
            writer.WriteByte(0x70); // element type: funcref
            writer.WriteByte(0x01); // table limits: flags (1 = has maximum)
            writer.WriteULEB128((ulong)0);
            writer.WriteULEB128((ulong)_methodCount); // table limits: initial size in number of entries
        }

        private int _numDefinedGlobals = 0;
        private int NextGlobalIndex()
        {

            int next = _numImportedGlobals + _numDefinedGlobals;
            _numDefinedGlobals++;
            return next;
        }

        private Dictionary<string, WasmGlobal> _definedGlobals = new();

        private void WriteGlobal(SectionWriter writer, string name, WasmValueType valueType, WasmMutabilityType mutability, WasmInstructionGroup initExpr)
        {
            WasmGlobal global = new WasmGlobal(
                index: NextGlobalIndex(), // next available index
                name: name,
                valueType,
                mutability,
                initExpr);
            if (!_definedGlobals.TryAdd(name, global))
            {
                throw new InvalidDataException($"Duplicate global name: {name}");
            }

            int size = global.EncodeSize();
            int written = global.Encode(writer.Buffer.GetSpan(size));
            Debug.Assert(written == size);
            writer.Buffer.Advance(written);
        }

        private void WriteGlobalSection()
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.GlobalSection);

            // webcilVersion: i32 const = 1
            WriteGlobal(writer, "webcilVersion", WasmValueType.I32, WasmMutabilityType.Const,
                new WasmInstructionGroup([new WasmConstExpr(WasmExprKind.I32Const, 1)]));
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
            WasmObjectNodeSection.TableSection.Name,
            WasmObjectNodeSection.GlobalSection.Name,
            WasmObjectNodeSection.ExportSection.Name,
            WasmObjectNodeSection.DataCountSection.Name,
            ObjectNodeSection.WasmCodeSection.Name,
            WasmObjectNodeSection.DataSection.Name,
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

        private void PadStream(MemoryStream s, int n, byte padding = 0)
        {
            for (int i = 0; i < n; i++)
                s.WriteByte(padding);
        }

        private protected override void EmitObjectFile(Stream outputFileStream)
        {
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

            /********************************
            Emit Webcil segment at end of file
            *********************************/

            Debug.Assert(_webcilSegment != null); // This should have been built in EmitSectionsAndLayout()

            // Populate the RVAs for the Cor header/size and debug directory/size, which are required for the runtime
            // to be able to load this segment.
            if (_wellKnownSymbols.TryGetValue(SortableDependencyNode.ObjectNodeOrder.CorHeaderNode, out Utf8String corHeaderDefName))
            {
                SymbolDefinition corHeaderNode = _definedSymbols[corHeaderDefName];
                _webcilSegment.Header.PeCliHeaderRva = (uint)_webcilSegment.ResolveSymbolRVA(corHeaderNode);
                Debug.Assert(_webcilSegment.Header.PeCliHeaderRva != 0);
                _webcilSegment.Header.PeCliHeaderSize = (uint)corHeaderNode.Size;
            }
            else
                throw new InvalidDataException($"Cor header symbol definition {SortableDependencyNode.ObjectNodeOrder.CorHeaderNode} not found");

            if (_wellKnownSymbols.TryGetValue(SortableDependencyNode.ObjectNodeOrder.DebugDirectoryNode, out Utf8String debugDirectoryDefName))
            {
                SymbolDefinition debugDirectoryDef = _definedSymbols[debugDirectoryDefName];
                _webcilSegment.Header.PeDebugRva = (uint)_webcilSegment.ResolveSymbolRVA(debugDirectoryDef);
                Debug.Assert(_webcilSegment.Header.PeDebugRva != 0);
                _webcilSegment.Header.PeDebugSize = (uint)debugDirectoryDef.Size;
            }
            else
                throw new InvalidDataException($"Debug directory symbol definition {SortableDependencyNode.ObjectNodeOrder.DebugDirectoryNode} not found");

            MemoryStream webcilStream = new(_webcilSegment.GetFlatMappedSize());
            Console.WriteLine($"webcilSection has flat mapped size: {_webcilSegment.GetFlatMappedSize()}");

            Console.WriteLine($"Header.PEDebugRVA={_webcilSegment.Header.PeDebugRva}");
            Console.WriteLine($"Header.PEDebugSize={_webcilSegment.Header.PeDebugSize}");
            Console.WriteLine($"Header.PeCliHeaderRVA={_webcilSegment.Header.PeCliHeaderRva}");
            Console.WriteLine($"Header.PeCliHeaderSize={_webcilSegment.Header.PeCliHeaderSize}");

            _webcilSegment.Header.Emit(webcilStream);

            foreach (WebcilSection section in _webcilSegment.Sections)
            {
                section.Header.Encode(webcilStream);
            }

            foreach (WebcilSection section in _webcilSegment.Sections)
            {
                // Stream position forward to pad implicitly
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
                // Write final padding
                WebcilSection lastSection = _webcilSegment.Sections[_webcilSegment.Sections.Length - 1];
                webcilStream.Seek(0, SeekOrigin.End);
                PadStream(webcilStream, (int)lastSection.Padding);
            }
            Debug.Assert(webcilStream.Position == _webcilSegment.GetFlatMappedSize(), $"Total Size Mismatch: {webcilStream.Position} != {_webcilSegment.GetFlatMappedSize()}");


            // Create passive data segment for encoding the size of the webcil payload (size must fit in 32-bit uint)
            byte[] lengthBuffer = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(lengthBuffer, (uint)_webcilSegment.GetFlatMappedSize());
            MemoryStream webcilSizeSegmentStream = new MemoryStream(lengthBuffer);
            WasmDataSegment webcilSizeSegment = new WasmDataSegment(webcilSizeSegmentStream, new Utf8String("webcilCount"),
                WasmDataSectionType.Passive, null);

            // Passive data segment for webcil payload contents
            WasmDataSegment webcilContentsSegment = new WasmDataSegment(webcilStream, new Utf8String("webcilPayload"),
                WasmDataSectionType.Passive, null);

            // Create combined data section and emit 
            WasmDataSection dataSection = new WasmDataSection([webcilSizeSegment, webcilContentsSegment], new Utf8String("data"), contentAlign: 4);
            dataSection.Emit(outputFileStream);
        }

        Dictionary<int, List<SymbolicRelocation>> _resolvableRelocations = new();

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            foreach (var reloc in relocationList)
            {
                if (!_resolvableRelocations.TryGetValue(sectionIndex, out List<SymbolicRelocation> resolvable))
                {
                    _resolvableRelocations[sectionIndex] = resolvable = new List<SymbolicRelocation>();
                }
                // Unconditionally add the reloc to our resolvable list; all relocs must be resolvable for Wasm since we are linker-less
                // and do not emit any relocations in the output object file.
                resolvable.Add(reloc);
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

            WebcilSection? webcilSection = null;
            uint webcilVirtualStart = 0;
            if (_sections[sectionIndex] is WebcilSection curSection)
            {
                webcilSection = curSection;
                webcilVirtualStart = curSection.Header.VirtualAddress;
            }

            // If we have a webcil section, we expect it to have a nonzero section start. This is because,
            // for webcil, we should have written the webcil header and each of the section headers (always non-zero size) before any
            // section contents
            Debug.Assert(webcilSection is null || sectionStart != 0);

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

                // The virtual address of the symbol this relocation refers to
                uint virtualSymbolImageOffset = 0;

                WebcilSection? symbolWebcilSection = null;

                // TODO-Wasm: Enforce the below boolean as an assert once we are emitting proper Wasm code
                // relocs for all code containing nodes
                //bool betweenWebcilSections = false;
                if (webcilSection is not null && _sections[definedSymbol.SectionIndex] is WebcilSection targetSection)
                {
                    //betweenWebcilSections = true;
                    symbolWebcilSection = targetSection;

                    virtualRelocOffset = webcilVirtualStart + (uint)reloc.Offset;
                    Debug.Assert(IsWithinSection(virtualRelocOffset, webcilSection));

                    virtualSymbolImageOffset = symbolWebcilSection.Header.VirtualAddress + (uint)definedSymbol.Value;
                    Debug.Assert(IsWithinSection(virtualSymbolImageOffset, symbolWebcilSection));
                }

                // We need a pinned raw pointer here for manipulation with Relocation.WriteValue
                fixed (byte* pData = ReadRelocToDataSpan(reloc, relocScratchBuffer, sectionStart))
                {
                    long addend = Relocation.ReadValue(reloc.Type, pData);
                    int relocLength = Relocation.GetSize(reloc.Type);

                    Console.WriteLine($"Resolving relocation: {reloc.SymbolName} at offset {reloc.Offset} in section: {_sections[sectionIndex].Name}");

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
                Console.WriteLine($"Reading reloc at {reloc.Offset} + {sectionStart} to data span");
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

        const int StackPointerGlobalIndex = 0;
        const int R2RStartGlobalIndex = 1;

        private WasmImport[] _defaultImports = new[]
        {
            null, // placeholder for memory, which is set up dynamically in WriteImports()
            new WasmImport("env", "__stack_pointer", import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Mut), index: StackPointerGlobalIndex),
            new WasmImport("env", "__r2r_start", import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Const), index: R2RStartGlobalIndex),
        };

        private void WriteImports()
        {
            // Calculate the minimum required memory size based on the combined data section size
            //ulong contentSize = (ulong)SectionByName(WasmObjectNodeSection.CombinedDataSection.Name).ContentSize;
            //uint dataPages = checked((uint)((contentSize + (1<<16) - 1) >> 16));
            //uint numPages = Math.Max(dataPages, 1); // Ensure at least one page is allocated for the minimum
            uint numPages = 2;

            _defaultImports[0] = new WasmImport("env", "memory", import: new WasmMemoryImportType(WasmLimitType.HasMin, numPages)); // memory limits: flags (0 = only minimum)

            int[] assignedImportIndices = new int[(int)WasmExternalKind.Count];
            foreach (WasmImport import in _defaultImports)
            {
                if (import.Index.HasValue)
                {
                    int assigned = assignedImportIndices[(int)import.Kind];
                    if (assigned != import.Index.Value)
                    {
                        throw new InvalidOperationException($"Import {import.Module}.{import.Name} of kind {import.Kind} assigned index {assigned}, needs {import.Index.Value}");
                    }
                }
                assignedImportIndices[(int)import.Kind]++;
                WriteImport(import);
            }

            _numImportedGlobals = assignedImportIndices[(int)WasmExternalKind.Global];
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
        // For now, this function just prepares the function, exports, and type sections for emission by prepending the counts.
        private protected override void EmitSymbolTable(IDictionary<Utf8String, SymbolDefinition> definedSymbols, SortedSet<Utf8String> undefinedSymbols)
        {
            WriteImports();
            WriteGlobalSection();
            WriteExports();

            foreach (var key in definedSymbols)
            {
                Console.WriteLine($"{key.Key}: {key.Value}");
            }

            int funcIdx = _sectionNameToIndex[WasmObjectNodeSection.FunctionSection.Name];
            PrependCount(_sections[funcIdx], _methodCount);

            int typeIdx = _sectionNameToIndex[ObjectNodeSection.WasmTypeSection.Name];
            PrependCount(_sections[typeIdx], _uniqueSignatures.Count);

            int exportIdx = _sectionNameToIndex[WasmObjectNodeSection.ExportSection.Name];
            PrependCount(_sections[exportIdx], _numExports);

            PrependCount(SectionByName(WasmObjectNodeSection.ImportSection.Name), _numImports);

            PrependCount(SectionByName(WasmObjectNodeSection.GlobalSection.Name), _numDefinedGlobals);

            // Register defined symbols for future use during relocation resolution
            _definedSymbols = new Dictionary<Utf8String, SymbolDefinition>(definedSymbols);
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
                    Console.WriteLine($"Segment Padding: {padding}");
                }
                else
                {
                    segment.Padding = 0;
                }
                segment.Emit(outputFileStream);
            }

            // Write the header (this must be done second because we first need to determine inter-segment padding)
            outputFileStream.Position = headerPosition;
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            size += EncodeHeader(headerBuffer);
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

        public WasmDataSegment(Stream contents, Utf8String name, WasmDataSectionType type, WasmInstructionGroup initExpr)
        {
            _stream = contents;
            _type = type;
            _initExpr = initExpr;
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
                    Console.WriteLine($"Header buffer length is: {headerBuffer.Length}");
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
            Console.WriteLine($"Emitting segment at: {outputFileStream.Position}");
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = EncodeHeader(headerBuffer);
            Debug.Assert(headerSize == HeaderSize);
            outputFileStream.Write(headerBuffer);

            _stream.Position = 0;
            _stream.CopyTo(outputFileStream);
            PaddingHelper.PadStream(outputFileStream, Padding);

            return headerSize + (int)_stream.Length + Padding;
        }
    }

    class WebcilSection : WasmSection
    {
        public readonly int Index;
        public WebcilSectionHeader Header;
        public readonly Stream _stream;
        public uint Padding => Header.SizeOfRawData - (uint)_stream.Length;

        public WebcilSection(Utf8String name, WebcilSectionHeader header, Stream stream, int index)
            : base(WasmSectionType.Data, stream, name)
        {
            Header = header;
            _stream = stream;
            Index = index;
        }

        public override int EncodeSize()
        {
            return (int)_stream.Length;
        }

        public override int Emit(Stream outputFileStream) => throw new NotImplementedException();
    }
}
