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

namespace ILCompiler.ObjectWriter
{
    internal static class WasmObjectNodeSection
    {
        // TODO-WASM: Consider alignment needs for data sections
        public static readonly ObjectNodeSection DataSection = new ObjectNodeSection("wasm.data", SectionType.Writeable, needsAlign: false);
        public static readonly ObjectNodeSection CombinedDataSection = new ObjectNodeSection("wasm.alldata", SectionType.Writeable, needsAlign: false);
        public static readonly ObjectNodeSection FunctionSection = new ObjectNodeSection("wasm.function", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection TypeSection = new ObjectNodeSection("wasm.type", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ExportSection = new ObjectNodeSection("wasm.export", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection MemorySection = new ObjectNodeSection("wasm.memory", SectionType.ReadOnly, needsAlign: false);
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

        private Dictionary<WasmFuncType, int> _uniqueSignatures = new();
        private Dictionary<string, int> _uniqueSymbols = new();
        private int _signatureCount = 0;
        private int _methodCount = 0;

        private protected override void RecordMethodSignature(ISymbolDefinitionNode symbol, MethodDesc desc)
        {
            // Ensure the signature is recorded with a unique index if we haven't seen an equivalent one yet.
            MaybeWriteType(desc);
            // Use the signature index to write a new function signature index into the function signature section.
            WriteSignatureIndexForFunction(desc);

            _uniqueSymbols.Add(symbol.GetMangledName(_nodeFactory.NameMangler), _methodCount);
            _methodCount++;
        }

        private void MaybeWriteType(MethodDesc desc)
        {
            WasmFuncType signature = WasmAbiContext.GetSignature(desc);
            if (_uniqueSignatures.ContainsKey(signature))
            {
                return;
            }

            // assign the next available index for the signature
            int signatureIndex = _signatureCount;
            _uniqueSignatures[signature] = signatureIndex;
            _signatureCount++;

            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.TypeSection);
            int signatureSize = signature.EncodeSize();
            signature.Encode(writer.Buffer.GetSpan(signatureSize));
            writer.Buffer.Advance(signatureSize);
        }

        private void WriteSignatureIndexForFunction(MethodDesc desc)
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.FunctionSection);

            WasmFuncType signature = WasmAbiContext.GetSignature(desc);
            if (!_uniqueSignatures.TryGetValue(signature, out int signatureIndex))
            {
                throw new InvalidOperationException($"Signature index not found for function: {desc.GetName()}");
            }

            writer.WriteULEB128((ulong)signatureIndex);
        }

        private void WriteFunctionExport(string methodName, int functionIndex)
        {
            SectionWriter writer = GetOrCreateSection(WasmObjectNodeSection.ExportSection);
            int length = Encoding.UTF8.GetByteCount(methodName);
            writer.WriteULEB128((ulong)length);
            writer.WriteUtf8StringNoNull(methodName);
            writer.WriteByte(0x00); // export kind: function
            writer.WriteULEB128((ulong)functionIndex);
        }
  
        private List<WasmSection> _sections = new();
        private Dictionary<string, int> _sectionNameToIndex = new();
        private Dictionary<ObjectNodeSection, WasmSectionType> sectionToType = new()
        {
            { WasmObjectNodeSection.MemorySection, WasmSectionType.Memory },
            { WasmObjectNodeSection.FunctionSection, WasmSectionType.Function },
            { WasmObjectNodeSection.ExportSection, WasmSectionType.Export },
            { WasmObjectNodeSection.TypeSection, WasmSectionType.Type },
            { ObjectNodeSection.WasmCodeSection, WasmSectionType.Code }
        };

        private WasmSectionType GetWasmSectionType(ObjectNodeSection section)
        {
            if (!sectionToType.ContainsKey(section))
            {
                // All other sections map to generic data segments in Wasm
                // TODO-WASM: Consider making the mapping explicit for every possible node type.
                return WasmSectionType.Data;
            }
            return sectionToType[section];
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
            // This is a no-op for now under Wasm
        }

        private WasmDataSection CreateCombinedDataSection()
        {
            IEnumerable<WasmSection> dataSections = _sections.Where(s => s.Type == WasmSectionType.Data);
            int offset = 0;
            List<WasmDataSegment> segments = new();
            foreach (WasmSection wasmSection in dataSections)
            {
                Debug.Assert(wasmSection.Type == WasmSectionType.Data);
                WasmDataSegment segment = new WasmDataSegment(wasmSection.Stream, wasmSection.Name, WasmDataSectionType.Active,
                    new WasmConstExpr(WasmExprKind.I32Const, (long)offset));
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
            ulong contentSize = (ulong)SectionByName(WasmObjectNodeSection.CombinedDataSection.Name).ContentSize;
            WriteMemorySection(contentSize);
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

        private protected override void EmitObjectFile(Stream outputFileStream)
        {
            EmitWasmHeader(outputFileStream);

            // TODO-WASM: Consider refactoring to loop over an in-order list of sections, skipping any that are missing,
            // So that we can maintain section ordering invariants without assuming the existence of certain sections.
            
            // Type section
            SectionByName(WasmObjectNodeSection.TypeSection.Name).Emit(outputFileStream);
            // Function section
            SectionByName(WasmObjectNodeSection.FunctionSection.Name).Emit(outputFileStream);
            // Memory section
            SectionByName(WasmObjectNodeSection.MemorySection.Name).Emit(outputFileStream);
            // Export section
            SectionByName(WasmObjectNodeSection.ExportSection.Name).Emit(outputFileStream);
            // Code section
            WasmSection codeSection = SectionByName(ObjectNodeSection.WasmCodeSection.Name);
            PrependCount(codeSection, _methodCount);
            codeSection.Emit(outputFileStream);
            // Data section (all segments)
            SectionByName(WasmObjectNodeSection.CombinedDataSection.Name).Emit(outputFileStream);
        }

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            // This is a no-op for now under Wasm
        }

        // For now, this function just prepares the function, exports, and type sections for emission by prepending the counts.
        private protected override void EmitSymbolTable(IDictionary<Utf8String, SymbolDefinition> definedSymbols, SortedSet<Utf8String> undefinedSymbols)
        {
            int funcIdx = _sectionNameToIndex[WasmObjectNodeSection.FunctionSection.Name];
            PrependCount(_sections[funcIdx], _methodCount);
            int typeIdx = _sectionNameToIndex[WasmObjectNodeSection.TypeSection.Name];
            PrependCount(_sections[typeIdx], _uniqueSignatures.Count);

            string[] functionExports = _uniqueSymbols.Keys.ToArray();
            // TODO-WASM: Handle exports better (e.g., only export public methods, etc.)
            // Also, see if we could leverage definedSymbols for this instead of doing our own bookkeeping in _uniqueSymbols.
            foreach (string name in functionExports.OrderBy(name => name))
            {
                WriteFunctionExport(name, _uniqueSymbols[name]);
            }

            int exportIdx = _sectionNameToIndex[WasmObjectNodeSection.ExportSection.Name];
            PrependCount(_sections[exportIdx], _methodCount);
        }
    }

    // TODO: This is a placeholder implementation. The real implementation will derive the Wasm function signature
    // from the MethodDesc's signature and type system information.
    public static class WasmAbiContext
    {
        public static WasmFuncType GetSignature(MethodDesc method)
        {
            return PlaceholderValues.CreateWasmFunc_i32_i32();
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
                Debug.Assert(_dataStream != null);
                return _dataStream;
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
        WasmConstExpr _initExpr;

        public WasmDataSegment(Stream contents, Utf8String name, WasmDataSectionType type, WasmConstExpr initExpr)
        {
            _stream = contents;
            _type = type;
            _initExpr = initExpr;
        }

        // The header size for a data segment consists of just a byte indicating the type of data segment.
        public int HeaderSize
        {
            get
            {
                return _type switch
                {
                    WasmDataSectionType.Active =>
                        (int)DwarfHelper.SizeOfULEB128((ulong)_type) + // type indicator
                        _initExpr.EncodeSize() + // init expr size
                        (int)DwarfHelper.SizeOfULEB128((ulong)_stream.Length), // size of data length
                    WasmDataSectionType.Passive => 
                        (int)DwarfHelper.SizeOfULEB128((ulong)_type) + // type indicator
                        (int)DwarfHelper.SizeOfULEB128((ulong)_stream.Length), // size of data length
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
