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
        public WasmObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
        }

        private void EmitWasmHeader(Stream outputFileStream)
        {
            outputFileStream.Write("\0asm"u8);
            outputFileStream.Write([0x1, 0x0, 0x0, 0x0]);
        }

        // TODO: for now, we are fully overriding EmitObject. As we support more features, we should
        // see if we can re-use the base, or refactor the base method to allow for code sharing.
        public override void EmitObject(Stream outputFileStream, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
        {
            ArrayBuilder<WasmFuncType> methodSignatures = new();
            ArrayBuilder<string> methodNames = new();
            ArrayBuilder<ObjectData> methodBodies = new();
            foreach (DependencyNode node in nodes)
            {
                if (node is not ObjectNode)
                {
                    continue;
                }

                if (node is IMethodBodyNode methodNode)
                {
                    ObjectNode methodObject = (ObjectNode)node;
                    methodSignatures.Add(WasmAbiContext.GetSignature(methodNode.Method));
                    methodNames.Add(methodNode.GetMangledName(_nodeFactory.NameMangler));
                    methodBodies.Add(methodObject.GetData(_nodeFactory));
                    // TODO: record relocations and attached data
                }
            }

            IEnumerable<WasmFuncType> uniqueSignatures = methodSignatures.ToArray().Distinct();
            Dictionary<WasmFuncType, int> signatureMap = uniqueSignatures.Select((sig, i) => (sig, i)).ToDictionary();

            // TODO: The EmitSection calls here can be moved to this class'  `EmitObjectFile` implementation
            // when we can share more code with the base class.
            EmitWasmHeader(outputFileStream);
            EmitSection(() => WriteTypeSection(uniqueSignatures, logger), outputFileStream, logger);
            EmitSection(() => WriteFunctionSection(methodSignatures, signatureMap, logger), outputFileStream, logger);
            EmitSection(() => WriteExportSection(methodBodies, methodSignatures, methodNames, signatureMap, logger), outputFileStream, logger);
            EmitSection(() => WriteCodeSection(methodBodies, logger), outputFileStream, logger);
        }

        private WasmSection WriteExportSection(ArrayBuilder<ObjectData> methodNodes, ArrayBuilder<WasmFuncType> methodSignatures,
            ArrayBuilder<string> methodNames, Dictionary<WasmFuncType, int> signatureMap, Logger logger)
        {
            WasmSection exportSection = new WasmSection(WasmSectionType.Export, new SectionData(), "export");
            SectionWriter writer = exportSection.Writer;
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

            return exportSection;
        }

        private WasmSection WriteCodeSection(ArrayBuilder<ObjectData> methodBodies, Logger logger)
        {
            WasmSection codeSection = new WasmSection(WasmSectionType.Code, new SectionData(), "code");
            SectionWriter writer = codeSection.Writer;

            // Write the number of functions
            writer.WriteULEB128((ulong)methodBodies.Count);
            for (int i = 0; i < methodBodies.Count; i++)
            {
                ObjectData methodBody = methodBodies[i];
                writer.WriteULEB128((ulong)methodBody.Data.Length);
                writer.EmitData(methodBody.Data);
            }

            return codeSection;
        }

        private WasmSection WriteTypeSection(IEnumerable<WasmFuncType> functionSignatures, Logger logger)
        {
            SectionData sectionData = new();
            WasmSection typeSection = new WasmSection(WasmSectionType.Type, sectionData, "type");
            SectionWriter writer = typeSection.Writer;

            // Write the number of types
            writer.WriteULEB128((ulong)functionSignatures.Count());

            IBufferWriter<byte> buffer = sectionData.BufferWriter;
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

            return typeSection;
        }

        private WasmSection WriteFunctionSection(ArrayBuilder<WasmFuncType> allFunctionSignatures, Dictionary<WasmFuncType, int> signatureMap, Logger logger)
        {
            WasmSection functionSection = new WasmSection(WasmSectionType.Function, new SectionData(), "function");
            SectionWriter writer = functionSection.Writer;
            // Write the number of functions
            writer.WriteULEB128((ulong)allFunctionSignatures.Count);
            for (int i = 0; i < allFunctionSignatures.Count; i++)
            {
                WasmFuncType signature = allFunctionSignatures[i];
                writer.WriteULEB128((ulong)signatureMap[signature]);
            }
            return functionSection;
        }

        private void EmitSection(Func<WasmSection> writeFunc, Stream outputFileStream, Logger logger)
        {
            WasmSection section = writeFunc();
            Span<byte> headerBuffer = stackalloc byte[section.HeaderSize];

            section.EncodeHeader(headerBuffer);
            outputFileStream.Write(headerBuffer);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote section header of size {headerBuffer.Length} bytes.");
            }

            section.Data.GetReadStream().CopyTo(outputFileStream);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Emitted section: {section.Name} of type `{section.Type}` with size {section.Data.Length} bytes.");
            }
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment) => throw new NotImplementedException();
        private WasmSection CreateSection(WasmSectionType sectionType, string symbolName, int sectionIndex) => throw new NotImplementedException();
        private protected override void CreateSection(ObjectNodeSection section, Utf8String comdatName, Utf8String symbolName, int sectionIndex, Stream sectionStream) => throw new NotImplementedException();
        private protected override void EmitObjectFile(Stream outputFileStream) => throw new NotImplementedException();
        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList) => throw new NotImplementedException();
        private protected override void EmitSymbolTable(IDictionary<Utf8String, SymbolDefinition> definedSymbols, SortedSet<Utf8String> undefinedSymbols) => throw new NotImplementedException();
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
        public string Name { get; }
        public SectionData Data => _data;
        private SectionWriter? _writer;

        private SectionData _data;

        public int HeaderSize
        {
            get
            {
                ulong sectionSize = (ulong)_data.Length;
                uint sizeEncodeLength = DwarfHelper.SizeOfULEB128(sectionSize);
                return 1 + (int)sizeEncodeLength;
            }
        }

        public int EncodeHeader(Span<byte> headerBuffer)
        {
            ulong sectionSize = (ulong)_data.Length;
            uint encodeLength = DwarfHelper.SizeOfULEB128(sectionSize);

            // Section header consists of:
            // 1 byte: section type
            // ULEB128: size of section
            headerBuffer[0] = (byte)Type;
            DwarfHelper.WriteULEB128(headerBuffer.Slice(1), sectionSize);

            return 1 + (int)encodeLength;
        }

        public SectionWriter Writer
        {
            get
            {
                if (_writer == null)
                {
                    _writer = new SectionWriter(null, 0, _data);
                }
                return _writer.Value;
            }
        }

        public WasmSection(WasmSectionType type, SectionData data, string name)
        {
            Type = type;
            Name = name;
            _data = data;
        }
    }
}
