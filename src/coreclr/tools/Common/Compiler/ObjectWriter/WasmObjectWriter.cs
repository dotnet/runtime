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
            Console.WriteLine($"writer.Position: {writer.Position}");
 
            // Write the number of functions
            writer.WriteULEB128((ulong)methodBodies.Count);
            for (int i = 0; i < methodBodies.Count; i++)
            {
                logger.LogMessage("Writing method: {i}");
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

        private void EmitSection(Func<WasmSection> getSection, Stream outputFileStream, Logger logger)
        {
            WasmSection section = getSection();

            Span<byte> headerBuffer = stackalloc byte[section.HeaderSize];
            section.EncodeHeader(headerBuffer);

            outputFileStream.Write(headerBuffer);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote section header of size {headerBuffer.Length} bytes.");
            }

            section.Stream.CopyTo(outputFileStream);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Emitted section: {section.Name} of type `{section.Type}` with size {section.Stream.Length} bytes.");
            }
        }

        private List<WasmSection> _sections = new();
        private Dictionary<Utf8String, int> _sectionNameToIndex = new();
        private Dictionary<ObjectNodeSection, WasmSectionType> sectionToType = new()
        {
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
            GetOrCreateSection(ObjectNodeSection.WasmTypeSection);
            GetOrCreateSection(ObjectNodeSection.WasmFunctionSection);
            GetOrCreateSection(ObjectNodeSection.WasmExportSection);
        }

        private protected override void EmitObjectFile(Stream outputFileStream, Logger logger = null)
        {
            IEnumerable<WasmFuncType> uniqueSignatures = _methodSignatures.ToArray().Distinct();
            Dictionary<WasmFuncType, int> signatureMap = uniqueSignatures.Select((sig, i) => (sig, i)).ToDictionary();

            foreach (var section in _sections)
            {
                logger.LogMessage($"Section: {section.Name} of kind {section.Type}");
            }

            EmitWasmHeader(outputFileStream);
            EmitSection(() => WriteTypeSection(uniqueSignatures, logger), outputFileStream, logger);
            EmitSection(() => WriteFunctionSection(_methodSignatures, signatureMap, logger), outputFileStream, logger);
            EmitSection(() => WriteExportSection(_methodBodies, _methodSignatures, _methodNames, signatureMap, logger), outputFileStream, logger);
            EmitSection(() => WriteCodeSection(_methodBodies, logger), outputFileStream, logger);
        }
        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            // This is a no-op for now under Wasm

        }
        private protected override void EmitSymbolTable(IDictionary<Utf8String, SymbolDefinition> definedSymbols, SortedSet<Utf8String> undefinedSymbols)
        {
            // This is a no-op for now under wasm
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

        Stream _dataStream;

        public Stream Stream {  get { return _dataStream; } }

        public int HeaderSize
        {
            get
            {
                ulong sectionSize = (ulong)_dataStream.Length;
                uint sizeEncodeLength = DwarfHelper.SizeOfULEB128(sectionSize);
                return 1 + (int)sizeEncodeLength;
            }
        }

        public int EncodeHeader(Span<byte> headerBuffer)
        {
            ulong sectionSize = (ulong)_dataStream.Length;
            uint encodeLength = DwarfHelper.SizeOfULEB128(sectionSize);

            // Section header consists of:
            // 1 byte: section type
            // ULEB128: size of section
            headerBuffer[0] = (byte)Type;
            DwarfHelper.WriteULEB128(headerBuffer.Slice(1), sectionSize);

            return 1 + (int)encodeLength;
        }

        public WasmSection(WasmSectionType type, Stream dataStream, Utf8String name)
        {
            Type = type;
            Name = name;
            _dataStream = dataStream;
        }
    }
}
