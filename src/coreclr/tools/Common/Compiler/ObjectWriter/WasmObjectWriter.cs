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
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;

using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;
using CodeDataLayout = CodeDataLayoutMode.CodeDataLayout;

namespace ILCompiler.ObjectWriter
{
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
        private SortedDictionary<string, int> _uniqueSymbols = new();
        private int _signatureCount = 0;
        private int _methodBodyCount = 0;

        private protected override void RecordMethod(ISymbolDefinitionNode symbol, MethodDesc desc, ObjectData methodBody, Logger logger)
        {
            WasmFuncType signature = WasmAbiContext.GetSignature(desc);
            if (!_uniqueSignatures.ContainsKey(signature))
            {
                // assign an index to the signature
                _uniqueSignatures[signature] = _signatureCount;
                _signatureCount++;
                WriteType(signature, logger);
            }

            int signatureIndex = _uniqueSignatures[signature];

            _uniqueSymbols.Add(symbol.GetMangledName(_nodeFactory.NameMangler), _methodBodyCount);
            WriteCode(methodBody, logger);
            WriteFunctionIndex(signatureIndex);
        }

        private void WriteCode(ObjectData methodBody, Logger logger)
        {
            string name = ObjectNodeSection.TextSection.Name;
            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.TextSection);
            writer.WriteULEB128((ulong)methodBody.Data.Length);
            writer.EmitData(methodBody.Data);
            _methodBodyCount++;
        }

        private void WriteFunctionIndex(int signatureIndex)
        {
            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.WasmFunctionSection);
            writer.WriteULEB128((ulong)signatureIndex);
        }

        private void WriteExport(string methodName, int functionIndex)
        {
            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.WasmExportSection);
            int length = Encoding.UTF8.GetByteCount(methodName);
            writer.WriteULEB128((ulong)length);
            writer.WriteUtf8StringNoNull(methodName);
            writer.WriteByte(0x00); // export kind: function
            writer.WriteULEB128((ulong)functionIndex);
        }

        private void WriteType(WasmFuncType signature, Logger logger)
        {
            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.WasmTypeSection);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Emitting function signature: {signature}");
            }
            int signatureSize = signature.EncodeSize();
            signature.Encode(writer.Buffer.GetSpan(signatureSize));
            writer.Buffer.Advance(signatureSize);
        }

        private List<WasmSection> _sections = new();
        private Dictionary<string, int> _sectionNameToIndex = new();
        private Dictionary<ObjectNodeSection, WasmSectionType> sectionToType = new()
        {
            { ObjectNodeSection.WasmMemorySection, WasmSectionType.Memory },
            { ObjectNodeSection.WasmFunctionSection, WasmSectionType.Function },
            { ObjectNodeSection.WasmExportSection, WasmSectionType.Export },
            { ObjectNodeSection.WasmTypeSection, WasmSectionType.Type },
            { ObjectNodeSection.TextSection, WasmSectionType.Code }
        };

        private WasmSectionType GetWasmSectionType(ObjectNodeSection section)
        {
            if (!sectionToType.ContainsKey(section))
            {
                // All other sections map to generic data segments in Wasm
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

            return new WasmDataSection(segments, new Utf8String(ObjectNodeSection.WasmCombinedDataSection.Name));
        }

        private protected override void CreateSection(ObjectNodeSection section, Utf8String comdatName, Utf8String symbolName, int sectionIndex, Stream sectionStream)
        {
            WasmSectionType sectionType = GetWasmSectionType(section);
            WasmSection wasmSection;
            if (section == ObjectNodeSection.WasmCombinedDataSection)
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
            // pages are 64 kb each, so we need to calculate how many pages we need
            ulong numPages = (contentSize + (1<<16) - 1) >> 16;

            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.WasmMemorySection);
            writer.WriteByte(0x01); // number of memories
            writer.WriteByte(0x00); // memory limits: flags (0 = only minimum)
            writer.WriteULEB128(numPages); // memory limits: initial size in pages (64kb each)
        }

        private protected override void EmitSectionsAndLayout()
        {
            GetOrCreateSection(ObjectNodeSection.WasmCombinedDataSection);
            ulong contentSize = (ulong)SectionByName(ObjectNodeSection.WasmCombinedDataSection.Name).ContentSize;
            WriteMemorySection(contentSize);
        }

        private void PrependCount(WasmSection section, int count)
        {
            ArrayBufferWriter<byte> prependBuffer = new();
            int writtenSize = (int)DwarfHelper.SizeOfULEB128((ulong)count);
            Span<byte> prepend = prependBuffer.GetSpan(writtenSize); // max ULEB128 size for count
            DwarfHelper.WriteULEB128(prepend, (ulong)count);
            prependBuffer.Advance(writtenSize);

            // create stream from buffer
            MemoryStream prependStream = new(prependBuffer.WrittenMemory.ToArray());
            section.PrependStream = prependStream;
        }

        private WasmSection SectionByName(string name)
        {
            int index = _sectionNameToIndex[name];
            return _sections[index];
        } 

        private protected override void EmitObjectFile(Stream outputFileStream, Logger logger = null)
        {
            Debug.Assert(logger != null);
            if (logger.IsVerbose)
            {
                foreach (var section in _sections)
                {
                    logger.LogMessage($"Section: {section.Name} of kind {section.Type}");
                }
            }

            EmitWasmHeader(outputFileStream);
            // Type section
            SectionByName(ObjectNodeSection.WasmTypeSection.Name).Emit(outputFileStream, logger);
            // Function section
            SectionByName(ObjectNodeSection.WasmFunctionSection.Name).Emit(outputFileStream, logger);
            // Memory section
            SectionByName(ObjectNodeSection.WasmMemorySection.Name).Emit(outputFileStream, logger);
            // Export section
            SectionByName(ObjectNodeSection.WasmExportSection.Name).Emit(outputFileStream, logger);
            // Code section
            WasmSection codeSection = SectionByName(ObjectNodeSection.TextSection.Name);
            PrependCount(codeSection, _methodBodyCount);
            codeSection.Emit(outputFileStream, logger);
            // Data section (all segments)
            SectionByName(ObjectNodeSection.WasmCombinedDataSection.Name).Emit(outputFileStream, logger);
        }

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList, Logger logger)
        {
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Emitting relocations for section index {sectionIndex}: {_sections[sectionIndex].Name}");
                // This is a no-op for now under Wasm
                foreach (SymbolicRelocation reloc in relocationList)
                {
                    logger.LogMessage($"Emitting reloc: {reloc.SymbolName} for offset {reloc.Offset}");
                }
            }
        }

        // For now, this function just prepares the function, exports, and type sections for emission by prepending the counts.
        private protected override void EmitSymbolTable(IDictionary<Utf8String, SymbolDefinition> definedSymbols, SortedSet<Utf8String> undefinedSymbols)
        {
            int funcIdx = _sectionNameToIndex[ObjectNodeSection.WasmFunctionSection.Name];
            PrependCount(_sections[funcIdx], _methodBodyCount);


            int typeIdx = _sectionNameToIndex[ObjectNodeSection.WasmTypeSection.Name];
            PrependCount(_sections[typeIdx], _uniqueSignatures.Count);

            // TODO-WASM: Handle exports better (e.g., only export public methods, etc.)
            // Also, see if we could leverage defindSymbols for this instead of doing our own bookkeeping in _uniqueSymbols.
            foreach ((string name, int idx) in _uniqueSymbols)
            {
                WriteExport(name, idx);
            }

            int exportIdx = _sectionNameToIndex[ObjectNodeSection.WasmExportSection.Name];
            PrependCount(_sections[exportIdx], _methodBodyCount);
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

        // Prepend stream is used for sections that need to write some data before the main data stream.
        // These are generally array-based sections which need to write a count before their actual data.
        private Stream _prependStream;

        public Stream PrependStream
        {
            get { return _prependStream ?? MemoryStream.Null; }
            set { _prependStream = value ?? MemoryStream.Null; }
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

        public virtual int ContentSize => (int)(PrependStream.Length + Stream.Length);

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

        public virtual int Emit(Stream outputFileStream, Logger logger)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            EncodeHeader(headerBuffer);

            outputFileStream.Write(headerBuffer);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote section header of size {headerBuffer.Length} bytes.");
            }

            PrependStream.CopyTo(outputFileStream);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote prepend stream of size {PrependStream.Length} bytes.");
            }

            Stream.CopyTo(outputFileStream);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Emitted section: {Name} of type `{Type}` with size {Stream.Length} bytes.");
            }

            return HeaderSize + (int)(PrependStream.Length + Stream.Length);
        }

        public WasmSection(WasmSectionType type, Stream stream, Utf8String name, Stream prepend = null)
        {
            Type = type;
            Name = name;
            _dataStream = stream;
            _prependStream = prepend;
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

        public override int Emit(Stream outputFileStream, Logger logger)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            base.EncodeHeader(headerBuffer);

            outputFileStream.Write(headerBuffer);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote data section header of size {headerBuffer.Length} bytes.");
            }

            // Write number of segments
            Span<byte> countBuffer = stackalloc byte[(int)DwarfHelper.SizeOfULEB128((ulong)_segments.Count)];
            int countSize = DwarfHelper.WriteULEB128(countBuffer, (ulong)_segments.Count);
            outputFileStream.Write(countBuffer.Slice(0, countSize));
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote data segment count of size {countSize} bytes.");
            }
            int totalSize = HeaderSize + countSize;
            foreach (WasmDataSegment segment in _segments)
            {
                totalSize += segment.Emit(outputFileStream, logger);
            }
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Emitted data section: {Name} with total size {totalSize} bytes.");
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

        public int Emit(Stream outputFileStream, Logger logger)
        {
            Span<byte> headerBuffer = stackalloc byte[HeaderSize];
            int headerSize = EncodeHeader(headerBuffer);
            Debug.Assert(headerSize == HeaderSize);

            outputFileStream.Write(headerBuffer);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote data segment header of size {headerBuffer.Length} bytes.");
            }

            _stream.CopyTo(outputFileStream);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote data segment contents of size {(int)_stream.Length} bytes.");
            }

            return headerSize + (int)_stream.Length;
        }
    }
}
