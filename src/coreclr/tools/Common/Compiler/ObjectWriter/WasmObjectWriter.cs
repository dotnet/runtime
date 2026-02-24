// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;

using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;
using CodeDataLayout = CodeDataLayoutMode.CodeDataLayout;
using System.Collections.Immutable;
using ILCompiler.ObjectWriter.WasmInstructions;
using ILCompiler.DependencyAnalysis.Wasm;
using System.Runtime.CompilerServices;

namespace ILCompiler.ObjectWriter
{
    internal static class WasmObjectNodeSection
    {
        // TODO-WASM: Consider alignment needs for data sections
        public static readonly ObjectNodeSection DataSection = new ObjectNodeSection("wasm.data", SectionType.Writeable, needsAlign: false);
        public static readonly ObjectNodeSection CombinedDataSection = new ObjectNodeSection("wasm.alldata", SectionType.Writeable, needsAlign: false);
        public static readonly ObjectNodeSection FunctionSection = new ObjectNodeSection("wasm.function", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ExportSection = new ObjectNodeSection("wasm.export", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection MemorySection = new ObjectNodeSection("wasm.memory", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection TableSection = new ObjectNodeSection("wasm.table", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ImportSection = new ObjectNodeSection("wasm.import", SectionType.ReadOnly, needsAlign: false);
    }

    /// <summary>
    /// Wasm object file format writer.
    /// </summary>
    internal sealed class WasmObjectWriter : ObjectWriter
    {
        protected override CodeDataLayout LayoutMode => CodeDataLayout.Separate;

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

        private protected override void RecordMethodSignature(WasmTypeNode signature)
        {
            int signatureIndex = _signatureCount;
            Utf8String mangledName = new Utf8String(signature.GetMangledName(_nodeFactory.NameMangler));
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
            { ObjectNodeSection.WasmTypeSection, WasmSectionType.Type },
            { ObjectNodeSection.WasmCodeSection, WasmSectionType.Code }
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

        private WasmDataSection CreateCombinedDataSection()
        {
            WasmInstructionGroup GetR2RStartOffset(int offset)
            {
                return new WasmInstructionGroup([
                    Global.Get(R2RStartGlobalIndex),
                    I32.Const(offset),
                    I32.Add,
                ]);
            }

            IEnumerable<WasmSection> dataSections = _sections.Where(s => s.Type == WasmSectionType.Data);
            int offset = 0;
            List<WasmDataSegment> segments = new();
            foreach (WasmSection wasmSection in dataSections)
            {
                Debug.Assert(wasmSection.Type == WasmSectionType.Data);
                WasmDataSegment segment = new WasmDataSegment(wasmSection.Stream, wasmSection.Name, WasmDataSectionType.Active,
                    GetR2RStartOffset(offset));
                segments.Add(segment);
                offset += segment.ContentSize;
            }

            return new WasmDataSection(segments, new Utf8String(WasmObjectNodeSection.CombinedDataSection.Name));
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
            WasmSection wasmSection;
            if (section == WasmObjectNodeSection.CombinedDataSection)
            {
                wasmSection = CreateCombinedDataSection();
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
            ulong numPages = (contentSize + (1<<16) - 1) >> 16;

            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.MemorySection);
            writer.WriteByte(0x01); // number of memories
            writer.WriteByte(0x00); // memory limits: flags (0 = only minimum)
            writer.WriteULEB128(numPages); // memory limits: initial size in pages (64kb each)
        }

        private protected override void EmitSectionsAndLayout()
        {
            GetOrCreateSection(WasmObjectNodeSection.CombinedDataSection);
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

        private void PrependCount(WasmSection section, int count)
        {
            section.PrependCount = count;
        }

        private WasmSection SectionByName(string name)
        {
            int index = _sectionNameToIndex[name];
            return _sections[index];
        }

        readonly string[] SectionOrder =
        [
            ObjectNodeSection.WasmTypeSection.Name,
            WasmObjectNodeSection.ImportSection.Name,
            WasmObjectNodeSection.FunctionSection.Name,
            WasmObjectNodeSection.TableSection.Name,
            WasmObjectNodeSection.ExportSection.Name,
            ObjectNodeSection.WasmCodeSection.Name,
            WasmObjectNodeSection.CombinedDataSection.Name,
        ];

        private int[] _sectionEmitOrder = null;
        private int[] SectionEmitOrder
        {
            get
            {
                if (_sectionEmitOrder == null)
                {
                    _sectionEmitOrder = SectionOrder.Select(name => _sectionNameToIndex[name]).ToArray();
                }

                return _sectionEmitOrder;
            }
        }

        private protected override void EmitObjectFile(Stream outputFileStream)
        {
            EmitWasmHeader(outputFileStream);
            foreach (int index in SectionEmitOrder)
            {
                WasmSection section = _sections[index];
                // TODO-WASM: handle data section relocations (this is dependent on the WebCIL structure being in place)
                if (_resolvableRelocations.TryGetValue(index, out List<SymbolicRelocation> relocations) &&
                    section.Type is not WasmSectionType.Data)
                {
                    using (Stream originalStream = section.Stream)
                    {
                        MemoryStream stream = new MemoryStream((int)originalStream.Length);
                        originalStream.Position = 0;
                        originalStream.CopyTo(stream);
                        ResolveRelocations(stream, relocations);
                        section.Stream = stream;
                        // originalStream may be disposed, section.Stream now points to resolved stream
                    }
                }

                section.Emit(outputFileStream);
            }
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

        // TODO-WASM: Currently, all Wasm relocs are resolved to 5 byte values unconditionally (the same size as the original placeholder padding), which is wasteful.
        // We should remove the padding and shrink the resolved values to their minimal size so we don't bloat the binary size.
        private unsafe void ResolveRelocations(MemoryStream sectionStream, List<SymbolicRelocation> relocs)
        {
            byte[] relocScratchBuffer = new byte[Relocation.MaxSize];

            foreach (SymbolicRelocation reloc in relocs)
            {
                int size = Relocation.GetSize(reloc.Type);
                if (size > relocScratchBuffer.Length)
                {
                    throw new InvalidOperationException($"Unsupported relocation size for relocation: {reloc.Type}");
                }

                // We need a pinned raw pointer here for manipulation with Relocation.WriteValue
                fixed (byte* pData = ReadRelocToDataSpan(reloc, relocScratchBuffer))
                {
                    switch (reloc.Type)
                    {
                        case RelocType.WASM_TYPE_INDEX_LEB:
                        {
                            if (_uniqueSignatures.TryGetValue(reloc.SymbolName, out int index))
                            {
                                Relocation.WriteValue(reloc.Type, pData, index);
                                WriteRelocFromDataSpan(reloc, pData);
                            }
                            else
                            {
                                throw new InvalidDataException($"Type signature symbol definition '{reloc.SymbolName}' not found");
                            }

                            break;
                        }
                        default:
                            // TODO-WASM: add other cases as needed;
                            // ignoring other reloc types for now
                            throw new NotSupportedException($"Relocation type {reloc.Type} not yet implemented");
                    }
                }
            }

            Span<byte> ReadRelocToDataSpan(SymbolicRelocation reloc, byte[] buffer)
            {
                Span<byte> relocContents = buffer.AsSpan(0, Relocation.GetSize(reloc.Type)); 
                sectionStream.Position = reloc.Offset;
                sectionStream.ReadExactly(relocContents);
                return relocContents;
            }

            void WriteRelocFromDataSpan(SymbolicRelocation reloc, byte *pData)
            {
                sectionStream.Position = reloc.Offset;
                sectionStream.Write(new Span<byte>(pData, Relocation.GetSize(reloc.Type)));
            }
        }

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
            ulong contentSize = (ulong)SectionByName(WasmObjectNodeSection.CombinedDataSection.Name).ContentSize;
            uint dataPages = checked((uint)((contentSize + (1<<16) - 1) >> 16));
            uint numPages = Math.Max(dataPages, 1); // Ensure at least one page is allocated for the minimum

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
        }

        private void WriteExports()
        {
            WriteTableExport("table", 0);
            string[] functionExports = _uniqueSymbols.Keys.ToArray();
            // TODO-WASM: Handle exports better (e.g., only export public methods, etc.)
            // Also, see if we could leverage definedSymbols for this instead of doing our own bookkeeping in _uniqueSymbols.
            foreach (string name in functionExports.OrderBy(name => name))
            {
                WriteFunctionExport(name, _uniqueSymbols[name]);
            }
        }

        // For now, this function just prepares the function, exports, and type sections for emission by prepending the counts.
        private protected override void EmitSymbolTable(IDictionary<Utf8String, SymbolDefinition> definedSymbols, SortedSet<Utf8String> undefinedSymbols)
        {
            WriteImports();
            WriteExports();

            int funcIdx = _sectionNameToIndex[WasmObjectNodeSection.FunctionSection.Name];
            PrependCount(_sections[funcIdx], _methodCount);

            int typeIdx = _sectionNameToIndex[ObjectNodeSection.WasmTypeSection.Name];
            PrependCount(_sections[typeIdx], _uniqueSignatures.Count);

            int exportIdx = _sectionNameToIndex[WasmObjectNodeSection.ExportSection.Name];
            PrependCount(_sections[exportIdx], _numExports);

            PrependCount(SectionByName(WasmObjectNodeSection.ImportSection.Name), _numImports);
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
        public WasmDataSection(List<WasmDataSegment> segments, Utf8String name)
            : base(WasmSectionType.Data, null, name)
        {
            _segments = segments;
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

        public override int Emit(Stream outputFileStream)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            base.EncodeHeader(headerBuffer);

            outputFileStream.Write(headerBuffer);

            // Write number of segments
            Span<byte> countBuffer = stackalloc byte[(int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count)];
            int countSize = DwarfHelper.WriteULEB128(countBuffer, (ulong)_segments.Count);
            outputFileStream.Write(countBuffer.Slice(0, countSize));
            int totalSize = HeaderSize + countSize;
            foreach (WasmDataSegment segment in _segments)
            {
                totalSize += segment.Emit(outputFileStream);
            }
            return totalSize;
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

        // The header encodeSize for a data segment consists of just a byte indicating the type of data segment.
        public int HeaderSize
        {
            get
            {
                return _type switch
                {
                    WasmDataSectionType.Active =>
                        (int)DwarfHelper.SizeOfULEB128((ulong)_type) + // type indicator
                        _initExpr.EncodeSize() + // init expr encodeSize
                        (int)DwarfHelper.SizeOfULEB128((ulong)_stream.Length), // encodeSize of data length
                    WasmDataSectionType.Passive =>
                        (int)DwarfHelper.SizeOfULEB128((ulong)_type) + // type indicator
                        (int)DwarfHelper.SizeOfULEB128((ulong)_stream.Length), // encodeSize of data length
                    _ =>
                        throw new NotImplementedException()
                };
            }
        }

        public int EncodeSize()
        {
            return HeaderSize + (int)_stream.Length;
        }

        public int ContentSize => (int)_stream.Length;

        public int EncodeHeader(Span<byte> headerBuffer)
        {
            switch (_type)
            {
                case WasmDataSectionType.Active:
                {
                    int len = 0;
                    len = DwarfHelper.WriteULEB128(headerBuffer, (ulong)_type);
                    len += _initExpr.Encode(headerBuffer.Slice(len));
                    len += DwarfHelper.WriteULEB128(headerBuffer.Slice(len), (ulong)_stream.Length);
                    return len;
                }
                case WasmDataSectionType.Passive:
                {
                    int len = 0;
                    len = DwarfHelper.WriteULEB128(headerBuffer, (ulong)_type);
                    len += DwarfHelper.WriteULEB128(headerBuffer.Slice(len), (ulong)_stream.Length);
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

            _stream.CopyTo(outputFileStream);

            return headerSize + (int)_stream.Length;
        }
    }
}
