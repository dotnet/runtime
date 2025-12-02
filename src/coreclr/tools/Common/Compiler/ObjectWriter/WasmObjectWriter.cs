// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.ObjectWriter
{
    // This is a placeholder for now. It will need to be filled in with ABI-specific signatures
    // for any method we want to emit into the Wasm module.
    public class WasmAbiContext
    {
        private Dictionary<MethodDesc, WasmFuncType> _methodSignatureMap = new();
        public WasmFuncType GetSignature(MethodDesc method)
        {
            return DummyValues.CreateWasmFunc_i32_i32();
        }
    }

    internal class WasmSection
    {
        public WasmSectionType Type { get; }
        public string Name { get; }
        public SectionData Data => _data;
        private SectionWriter? _writer;

        private SectionData _data;

        public Span<byte> Header
        {
            get
            {
                // Section header consists of:
                // 1 byte: section type
                // ULEB128: size of section
                ulong sectionSize = (ulong)_data.Length;
                var encodeLength = DwarfHelper.SizeOfULEB128(sectionSize);

                Span<byte> header = new byte[1+encodeLength];
                header[0] = (byte)Type;
                DwarfHelper.WriteULEB128(header.Slice(1), sectionSize);

                return header;
            }
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

    /// <summary>
    /// Wasm object file format writer.
    /// </summary>
    internal sealed class WasmObjectWriter : ObjectWriter
    {
        // must be stored in little-endian order
        private const uint WasmMagicNumber = 0x6d736100;

        // must be stored in little-endian order
        private const uint WasmVersion = 0x1;

        private WasmAbiContext _wasmAbiContext;

        public WasmObjectWriter(NodeFactory factory, ObjectWritingOptions options, WasmAbiContext ctx, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
            _wasmAbiContext = ctx;
        }

        private void EmitWasmHeader(Stream outputFileStream)
        {
            SectionData data = new();
            SectionWriter headerWriter = new(this, 0, data);
            headerWriter.WriteLittleEndian(WasmMagicNumber);
            headerWriter.WriteLittleEndian(WasmVersion);

            data.GetReadStream().CopyTo(outputFileStream);
        }

        public override void EmitObject(Stream outputFileStream, IReadOnlyCollection<DependencyNode> nodes, IObjectDumper dumper, Logger logger)
        {            
            ArrayBuilder<WasmFuncType> methodSignatures = new();
            ArrayBuilder<IMethodBodyNode> methodBodies = new();
            foreach (DependencyNode node in nodes)
            {
                if (node is not ObjectNode)
                {
                    continue;
                }

                if (node is IMethodBodyNode methodNode)
                {
                    methodSignatures.Add(_wasmAbiContext.GetSignature(methodNode.Method));
                    methodBodies.Add(methodNode);
                    // TODO: record relocations and attached data
                }
            }

            Dictionary<WasmFuncType, int> signatureMap = new();
            int signatureIndex = 0;
            for (int i = 0; i < methodSignatures.Count; i++)
            {
                if (!signatureMap.ContainsKey(methodSignatures[i]))
                {
                    signatureMap[methodSignatures[i]] = signatureIndex++;
                }
            }

            WasmFuncType[] uniqueSignatures = new WasmFuncType[signatureMap.Count];
            foreach (var kvp in signatureMap)
            {
                uniqueSignatures[kvp.Value] = kvp.Key;
            }

            EmitWasmHeader(outputFileStream);
            EmitSection(() => WriteTypeSection(uniqueSignatures, logger), outputFileStream, logger);
            EmitSection(() => WriteFunctionSection(methodSignatures.ToArray(), signatureMap, logger), outputFileStream, logger);
            EmitSection(() => WriteExportSection(methodBodies.ToArray(), methodSignatures.ToArray(), signatureMap, logger), outputFileStream, logger);
            EmitSection(() => WriteCodeSection(methodBodies.ToArray(), logger),  outputFileStream, logger);
        }

        private WasmSection WriteExportSection(IReadOnlyCollection<IMethodBodyNode> methodNodes, IReadOnlyCollection<WasmFuncType> methodSignatures,
            Dictionary<WasmFuncType, int> signatureMap, Logger logger)
        {
            WasmSection exportSection = new WasmSection(WasmSectionType.Export, new SectionData(), "export");
            SectionWriter writer = exportSection.Writer;
            // Write the number of exports
            writer.WriteULEB128((ulong)methodNodes.Count);
            for (int i = 0; i < methodNodes.Count; i++)
            {
                var methodNode = methodNodes.ElementAt(i);
                var methodSignature = methodSignatures.ElementAt(i);
                string exportName = methodNode.GetMangledName(_nodeFactory.NameMangler);

                var length = Encoding.UTF8.GetByteCount(exportName);
                writer.WriteULEB128((ulong)length);
                writer.WriteUtf8StringNoNull(exportName);
                writer.WriteByte(0x00); // export kind: function
                writer.WriteULEB128((ulong)i);
                if (logger.IsVerbose)
                {
                    logger.LogMessage($"Emitting export: {exportName} for function index {signatureMap[methodSignature]}");
                }
            }

            return exportSection;
        }

        private WasmSection WriteCodeSection(IReadOnlyCollection<IMethodBodyNode> methodBodies, Logger logger)
        {
            WasmSection codeSection = new WasmSection(WasmSectionType.Code, new SectionData(), "code");

            SectionWriter writer = codeSection.Writer;
            // Write the number of functions
            writer.WriteULEB128((ulong)methodBodies.Count);
            foreach (IMethodBodyNode methodBody in methodBodies)
            {
                ObjectNode objectNode = methodBody as ObjectNode;
                ObjectNode.ObjectData body = objectNode.GetData(_nodeFactory);

                writer.WriteULEB128((ulong)body.Data.Length);
                writer.EmitData(body.Data);
            }

            return codeSection;
        }

        private WasmSection WriteTypeSection(Span<WasmFuncType> functionSignatures, Logger logger)
        {
            WasmSection typeSection = new WasmSection(WasmSectionType.Type, new SectionData(), "type");
            SectionWriter writer = typeSection.Writer;

            // Write the number of types
            writer.WriteULEB128((ulong)functionSignatures.Length);
            foreach (WasmFuncType signature in functionSignatures)
            {
                if (logger.IsVerbose)
                {
                    logger.LogMessage($"Emitting function signature: {signature}");
                }
                writer.EmitData(signature.Encode());
            }
            return typeSection;
        }

        private WasmSection WriteFunctionSection(WasmFuncType[] allFunctionSignatures, IDictionary<WasmFuncType, int> signatureMap, Logger logger)
        {
            WasmSection functionSection = new WasmSection(WasmSectionType.Function, new SectionData(), "function");
        
            SectionWriter writer = functionSection.Writer;
            // Write the number of functions
            writer.WriteULEB128((ulong)allFunctionSignatures.Length);
            for (int i = 0; i < allFunctionSignatures.Length; i++)
            {
                var signature = allFunctionSignatures[i];
                writer.WriteULEB128((ulong)signatureMap[signature]);
            }
            return functionSection;
        }

        private void EmitSection(Func<WasmSection> writeFunc, Stream outputFileStream, Logger logger)
        {
            var section = writeFunc();

            outputFileStream.Write(section.Header);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Wrote section header of size {section.Header.Length} bytes.");
            }

            section.Data.GetReadStream().CopyTo(outputFileStream);
            if (logger.IsVerbose)
            {
                logger.LogMessage($"Emitted section: {section.Name} of type `{section.Type}` with size {section.Data.Length} bytes.");
            }
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment) => throw new NotImplementedException();
        private WasmSection CreateSection(WasmSectionType sectionType, string symbolName, int sectionIndex) => throw new NotImplementedException();
        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, int sectionIndex, Stream sectionStream) => throw new NotImplementedException();
        private protected override void EmitObjectFile(Stream outputFileStream) => throw new NotImplementedException();
        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList) => throw new NotImplementedException();
        private protected override void EmitSymbolTable(IDictionary<string, SymbolDefinition> definedSymbols, SortedSet<string> undefinedSymbols) => throw new NotImplementedException();
    }
}
