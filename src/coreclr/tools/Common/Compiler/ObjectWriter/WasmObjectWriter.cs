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

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Wasm object file format writer.
    /// </summary>
    internal sealed class WasmObjectWriter : ObjectWriter
    {
        protected override CodeDataLayoutMode LayoutMode => CodeDataLayoutMode.Separate;

        public WasmObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
        }

        private void EmitWasmHeader(Stream outputFileStream)
        {
            outputFileStream.Write("\0asm"u8);
            outputFileStream.Write([0x1, 0x0, 0x0, 0x0]);
        }

        private ArrayBuilder<WasmFuncType> _methodSignatures = new();
        private ArrayBuilder<string> _methodNames = new();
        private ArrayBuilder<ObjectData> _methodBodies = new();

        private protected override void RecordMethod(ISymbolDefinitionNode symbol, MethodDesc desc, ObjectData methodBody)
        {
            _methodBodies.Add(methodBody);
            _methodNames.Add(symbol.GetMangledName(_nodeFactory.NameMangler));
            _methodSignatures.Add(WasmAbiContext.GetSignature(desc));
        }

        private WasmSection WriteExportSection(ArrayBuilder<ObjectData> methodNodes, ArrayBuilder<WasmFuncType> methodSignatures,
            ArrayBuilder<string> methodNames, Dictionary<WasmFuncType, int> signatureMap, Logger logger)
        {
            // This gets a section writer for this particular section. We should have already created
            // the section earlier, so assert this:
            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.WasmExportSection);

            // Write the number of exports
            writer.WriteULEB128((ulong)methodNodes.Count);
            for (int i = 0; i < methodNodes.Count; i++)
            {
                ObjectData methodNode = methodNodes[i];
                WasmFuncType methodSignature = methodSignatures[i];
                string exportName = methodNames[i];

                int length = Encoding.UTF8.GetByteCount(exportName);
                writer.WriteULEB128((ulong)length);
                writer.WriteUtf8StringNoNull(exportName);
                writer.WriteByte(0x00); // export kind: function
                writer.WriteULEB128((ulong)i);
                if (logger.IsVerbose)
                {
                    logger.LogMessage($"Emitting export: {exportName} for function index {i}");
                }
            }

            int idx = _sectionNameToIndex[ObjectNodeSection.WasmExportSection.Name];
            return _sections[idx];
        }

        private WasmSection WriteCodeSection(ArrayBuilder<ObjectData> methodBodies, Logger logger)
        {
            string name = ObjectNodeSection.TextSection.Name;
            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.TextSection);

            // Write the number of functions
            writer.WriteULEB128((ulong)methodBodies.Count);
            for (int i = 0; i < methodBodies.Count; i++)
            {
                ObjectData methodBody = methodBodies[i];
                writer.WriteULEB128((ulong)methodBody.Data.Length);
                writer.EmitData(methodBody.Data);
            }

            int idx = _sectionNameToIndex[name];
            return _sections[idx];
        }

        private WasmSection WriteTypeSection(IEnumerable<WasmFuncType> functionSignatures, Logger logger)
        {
            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.WasmTypeSection);

            // Write the number of types
            writer.WriteULEB128((ulong)functionSignatures.Count());

            IBufferWriter<byte> buffer = writer.Buffer;
            foreach (WasmFuncType signature in functionSignatures)
            {
                if (logger.IsVerbose)
                {
                    logger.LogMessage($"Emitting function signature: {signature}");
                }

                int signatureSize = signature.EncodeSize();
                signature.Encode(buffer.GetSpan(signatureSize));
                buffer.Advance(signatureSize);
            }

            return _sections[writer.SectionIndex];
        }

        private WasmSection WriteFunctionSection(ArrayBuilder<WasmFuncType> allFunctionSignatures, Dictionary<WasmFuncType, int> signatureMap, Logger logger)
        {
            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.WasmFunctionSection);
            // Write the number of functions
            writer.WriteULEB128((ulong)allFunctionSignatures.Count);
            for (int i = 0; i < allFunctionSignatures.Count; i++)
            {
                WasmFuncType signature = allFunctionSignatures[i];
                writer.WriteULEB128((ulong)signatureMap[signature]);
            }

            return _sections[writer.SectionIndex];
        }

        private WasmDataSection WriteDataSection(IEnumerable<WasmSection> dataSections)
        {
            int encodedSize = 0;
            List<WasmDataSegment> segments = new();
            foreach (WasmSection section in dataSections)
            {
                Debug.Assert(section.Type == WasmSectionType.Data);
                WasmDataSegment segment = new WasmDataSegment(section.Stream, section.Name, WasmDataSectionType.Active,
                    new WasmConstExpr(WasmExprKind.I32Const, (long)encodedSize));
                segments.Add(segment);
                encodedSize += segment.EncodeSize();
            }

            return new WasmDataSection(segments, ObjectNodeSection.WasmDataSection.Name);
        }

        private List<WasmSection> _sections = new();
        private Dictionary<Utf8String, int> _sectionNameToIndex = new();
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

        private protected override void CreateSection(ObjectNodeSection section, Utf8String comdatName, Utf8String symbolName, int sectionIndex, Stream sectionStream)
        {
            WasmSectionType sectionType = GetWasmSectionType(section);
            WasmSection wasmSection = new WasmSection(sectionType, sectionStream, section.Name);
            Debug.Assert(_sections.Count == sectionIndex);
            _sections.Add(wasmSection);
            _sectionNameToIndex.Add(section.Name, sectionIndex);
        }

        private protected override void EmitSectionsAndLayout()
        {
            // Text section will have already been created by the base class when this method is called
            GetOrCreateSection(ObjectNodeSection.WasmMemorySection); 
            GetOrCreateSection(ObjectNodeSection.WasmTypeSection);
            GetOrCreateSection(ObjectNodeSection.WasmFunctionSection);
            GetOrCreateSection(ObjectNodeSection.WasmExportSection);
        }

        private WasmSection WriteMemorySection(int contentSize)
        {
            // pages are 64 kb each, so we need to calculate how many pages we need
            int numPages = (contentSize + (1<<16) - 1) >> 16;

            SectionWriter writer = GetOrCreateSection(ObjectNodeSection.WasmMemorySection);
            writer.WriteByte(0x01); // number of memories
            writer.WriteByte(0x00); // memory limits: flags (0 = only minimum)
            writer.WriteULEB128((ulong)numPages); // memory limits: initial size in pages (64kb each)
            return _sections[writer.SectionIndex];
        }

        private protected override void EmitObjectFile(Stream outputFileStream, Logger logger = null)
        {
            IEnumerable<WasmFuncType> uniqueSignatures = _methodSignatures.ToArray().Distinct();
            Dictionary<WasmFuncType, int> signatureMap = uniqueSignatures.Select((sig, i) => (sig, i)).ToDictionary();

            if (logger.IsVerbose)
            {
                foreach (var section in _sections)
                {
                    logger.LogMessage($"Section: {section.Name} of kind {section.Type}");
                }
            }

            WasmDataSection dataSection = WriteDataSection(_sections.Where(s => s.Type == WasmSectionType.Data));
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Data contents size: {dataSection.ContentSize}");
            }

            EmitWasmHeader(outputFileStream);
            WriteTypeSection(uniqueSignatures, logger).Emit(outputFileStream, logger);
            WriteFunctionSection(_methodSignatures, signatureMap, logger).Emit(outputFileStream, logger);
            WriteMemorySection(dataSection.ContentSize).Emit(outputFileStream, logger);
            WriteExportSection(_methodBodies, _methodSignatures, _methodNames, signatureMap, logger).Emit(outputFileStream, logger);
            WriteCodeSection(_methodBodies, logger).Emit(outputFileStream, logger);
            dataSection.Emit(outputFileStream, logger);
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

        private protected override void EmitSymbolTable(IDictionary<Utf8String, SymbolDefinition> definedSymbols, SortedSet<Utf8String> undefinedSymbols)
        {
            // No-op for Wasm for now
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

        public virtual int ContentSize => (int)_dataStream.Length;

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

            Stream.CopyTo(outputFileStream);

            if (logger.IsVerbose)
            {
                logger.LogMessage($"Emitted section: {Name} of type `{Type}` with size {Stream.Length} bytes.");
            }

            return HeaderSize + (int)Stream.Length;
        }

        public WasmSection(WasmSectionType type, Stream stream, Utf8String name)
        {
            Type = type;
            Name = name;
            _dataStream = stream;
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
