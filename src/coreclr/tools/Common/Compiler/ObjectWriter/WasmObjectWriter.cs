// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Base class for WebAssembly object file format writers.
    /// </summary>
    internal abstract partial class WasmObjectWriter : ObjectWriter
    {
        private readonly Dictionary<ObjectNodeSection, WasmSectionType> _sectionToType = new()
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

        // Sections emitted before data segments.
        private readonly string[] _sectionOrder =
        [
            ObjectNodeSection.WasmTypeSection.Name,
            WasmObjectNodeSection.ImportSection.Name,
            WasmObjectNodeSection.FunctionSection.Name,
            WasmObjectNodeSection.TableSection.Name,
            WasmObjectNodeSection.MemorySection.Name,
            WasmObjectNodeSection.GlobalSection.Name,
            WasmObjectNodeSection.ExportSection.Name,
            WasmObjectNodeSection.ElementSection.Name,
            WasmObjectNodeSection.DataCountSection.Name,
            ObjectNodeSection.WasmCodeSection.Name,
        ];

        private protected readonly Dictionary<string, WasmGlobal> _definedGlobals = new();
        private protected readonly WasmSections _sections = new();
        private protected readonly WasmSymbolManager _wasmSymbolManager = new();
        /// <summary>
        /// Maps symbol names to their location in the object file. These definitions do not encode
        /// logical WebAssembly indices and must not be used to resolve index relocations.
        /// </summary>
        private protected Dictionary<Utf8String, SymbolDefinition> _definedSymbols;
        private int[] _sectionEmitOrder;

        /// <summary>
        /// The number of methods in the Function section.
        /// </summary>
        private protected int MethodCount => _wasmSymbolManager.GetDefinitionCount(WasmIndexSpace.Function);

        private protected int[] SectionEmitOrder
        {
            get
            {
                _sectionEmitOrder ??= _sectionOrder
                    .Where(_sections.Contains)
                    .Select(_sections.GetSectionIndex)
                    .ToArray();

                return _sectionEmitOrder;
            }
        }

        protected WasmObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder)
            : base(factory, options, outputInfoBuilder)
        {
        }

        private protected static void EmitWasmHeader(Stream outputFileStream)
        {
            outputFileStream.Write("\0asm"u8);
            outputFileStream.Write([0x1, 0x0, 0x0, 0x0]);
        }

        private protected override void CreateSection(
            ObjectNodeSection section,
            Utf8String comdatName,
            Utf8String symbolName,
            int sectionIndex,
            Stream sectionStream)
        {
            WasmSectionType sectionType = GetWasmSectionType(section);
            SectionDataEmitter wasmSection;
            if (sectionType == WasmSectionType.Data)
            {
                wasmSection = CreateDataSection(section, sectionIndex, sectionStream);
            }
            else
            {
                Utf8String sectionName = new(section.Name);
                wasmSection = sectionType switch
                {
                    WasmSectionType.Type or WasmSectionType.Code => new WasmExternallyCountedSection(sectionType, sectionStream, sectionName, sectionIndex),
                    WasmSectionType.Import => new WasmImportSection(sectionStream, sectionName, sectionIndex),
                    WasmSectionType.Function => new WasmFunctionSection(sectionStream, sectionName, sectionIndex),
                    WasmSectionType.Global => new WasmGlobalSection(sectionStream, sectionName, sectionIndex),
                    WasmSectionType.Export => new WasmExportSection(sectionStream, sectionName, sectionIndex),
                    WasmSectionType.Element => new WasmElementSection(sectionStream, sectionName, sectionIndex),
                    _ => new WasmSection(sectionType, sectionStream, sectionName, sectionIndex),
                };
            }

            Debug.Assert(_sections.Sections.Count == sectionIndex);
            _sections.Add(section.Name, sectionIndex, wasmSection);
        }

        private protected abstract SectionDataEmitter CreateDataSection(
            ObjectNodeSection section,
            int sectionIndex,
            Stream sectionStream);

        private protected override void RecordMethodSignature(WasmTypeNode signature)
        {
            Utf8StringBuilder mangledNameBuilder = new();
            signature.AppendMangledName(_nodeFactory.NameMangler, mangledNameBuilder);
            Utf8String mangledName = mangledNameBuilder.ToUtf8String();
            // Record the signature's wasm type index in the shared symbol table. The signature bytes
            // are emitted by the node's own data; here we only assign its index.
            _wasmSymbolManager.AddDefinition(mangledName, WasmIndexSpace.Type);
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
            RegisterFunctionSymbol(new Utf8String(node.GetMangledName(_nodeFactory.NameMangler)));
            if (node is INodeWithFunclets nodeWithFunclets)
            {
                RecordFunclets(nodeWithFunclets);
            }
        }

        private void RecordFunclets(INodeWithFunclets nodeWithFunclets)
        {
            FuncletKind[] funcletKinds = nodeWithFunclets.GetFuncletKinds();
            if (funcletKinds.Length < 1)
            {
                return;
            }

            WasmValueType pointerType = _nodeFactory.Target.PointerSize == 8 ? WasmValueType.I64 : WasmValueType.I32;
            string mangledNodeName = nodeWithFunclets.GetMangledName(_nodeFactory.NameMangler);

            for (int i = 0; i < funcletKinds.Length; i++)
            {
                WasmFuncType funcletSignature = GetFuncletType(funcletKinds[i], pointerType);
                RegisterFunctionSymbol(new Utf8String($"{mangledNodeName}_funclet_{i}"));
                RegisterStubIndexAndSignature(funcletSignature);
            }
        }

        private static WasmFuncType GetFuncletType(FuncletKind funcletKind, WasmValueType pointerType)
        {
            return funcletKind switch
            {
                FuncletKind.CatchOrFilterHandler or FuncletKind.Filter => new WasmFuncType(
                    new([pointerType, pointerType, pointerType]), new([pointerType])), // (FP, SP, EXN) -> RESULT
                _ => new WasmFuncType(new([pointerType, pointerType]), new([])), // (FP, SP) -> void
            };
        }

        private void WriteFunctionEntry(int signatureIndex)
        {
            WasmFunctionSection section = GetOrCreateSection<WasmFunctionSection>(
                WasmObjectNodeSection.FunctionSection,
                out SectionWriter writer);
            section.WriteEntry(writer, signatureIndex);
        }

        private void WriteSignatureIndexForFunction(
            MethodSignature managedSignature,
            WasmLowering.LoweringFlags flags,
            ISymbolNode node)
        {
            WasmFuncType signature = WasmLowering.GetSignature(managedSignature, flags).FuncType;
            Utf8String key = signature.GetMangledName(_nodeFactory.NameMangler);
            if (!_wasmSymbolManager.TryGetSymbol(key, out WasmSymbol signatureSymbol))
            {
                throw new InvalidOperationException($"Signature index of {key} not found for function: {node.ToString()}");
            }

            WriteFunctionEntry(signatureSymbol.Index);
        }

        /// <summary>
        /// Adds the given import entry, including its prefix (module/name/kind) and body (external ref).
        /// </summary>
        private protected void WriteImport(WasmImport import)
        {
            Utf8String symbolName = new(import.Name);
            _wasmSymbolManager.AddImport(symbolName, GetIndexSpace(import.Kind), import.Index);

            WasmImportSection section = GetOrCreateSection<WasmImportSection>(
                WasmObjectNodeSection.ImportSection,
                out SectionWriter writer);
            section.WriteEntry(writer, import);
        }

        /// <summary>
        /// Maps an import kind to the index space where it can be referenced.
        /// Imports are always the first logical entries in their respective index spaces.
        /// </summary>
        private static WasmIndexSpace GetIndexSpace(WasmExternalKind kind) => kind switch
        {
            WasmExternalKind.Function => WasmIndexSpace.Function,
            WasmExternalKind.Table => WasmIndexSpace.Table,
            WasmExternalKind.Memory => WasmIndexSpace.Memory,
            WasmExternalKind.Global => WasmIndexSpace.Global,
            WasmExternalKind.Tag => WasmIndexSpace.Tag,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        private void WriteExport(string name, WasmExportKind kind, int index)
        {
            WasmExportSection section = GetOrCreateSection<WasmExportSection>(
                WasmObjectNodeSection.ExportSection,
                out SectionWriter writer);
            section.WriteEntry(writer, new WasmExport(name, kind, index));
        }

        private protected void WriteFunctionExport(string name, int functionIndex) =>
            WriteExport(name, WasmExportKind.Function, functionIndex);

        private protected void WriteTableExport(string name, int tableIndex) =>
            WriteExport(name, WasmExportKind.Table, tableIndex);

        private protected void WriteMemoryExport(string name, int memoryIndex) =>
            WriteExport(name, WasmExportKind.Memory, memoryIndex);

        private protected void WriteGlobalExport(string name, int globalIndex) =>
            WriteExport(name, WasmExportKind.Global, globalIndex);

        private protected void WriteElementSegment(ReadOnlyMemory<int> functionIndices)
        {
            WasmElementSection section = GetOrCreateSection<WasmElementSection>(
                WasmObjectNodeSection.ElementSection,
                out SectionWriter writer);
            section.WriteEntry(writer, functionIndices);
        }

        private protected SectionDataEmitter GetOrCreateSection(
            ObjectNodeSection section,
            out SectionWriter writer)
        {
            return GetOrCreateSection<SectionDataEmitter>(section, out writer);
        }

        private protected TSection GetOrCreateSection<TSection>(
            ObjectNodeSection section,
            out SectionWriter writer)
            where TSection : SectionDataEmitter
        {
            writer = GetOrCreateSection(section);
            return _sections.GetSection<TSection>(writer.SectionIndex);
        }

        private WasmSectionType GetWasmSectionType(ObjectNodeSection section)
        {
            if (!_sectionToType.TryGetValue(section, out WasmSectionType sectionType))
            {
                // All other sections map to generic data segments in Wasm.
                // TODO-WASM: Consider making the mapping explicit for every possible node type.
                return WasmSectionType.Data;
            }

            return sectionType;
        }

        // TODO-WASM: In the future, we may want to consider representing Wasm globals in the dependency graph so that they
        // can be referenced by other nodes and we can make effective use of them.
        private protected void WriteGlobal(
            string name,
            WasmValueType valueType,
            WasmMutabilityType mutability,
            WasmInstructionGroup initExpr)
        {
            Utf8String symbolName = new(name);
            _wasmSymbolManager.AddDefinition(symbolName, WasmIndexSpace.Global);
            int index = _wasmSymbolManager.GetSymbol(symbolName).Index;
            WasmGlobal global = new(
                index,
                name: name,
                valueType,
                mutability,
                initExpr);
            bool added = _definedGlobals.TryAdd(name, global);
            Debug.Assert(added, $"Duplicate global name: {name}");

            WasmGlobalSection section = GetOrCreateSection<WasmGlobalSection>(
                WasmObjectNodeSection.GlobalSection,
                out SectionWriter writer);
            section.WriteEntry(writer, global);
        }

        private protected void RegisterFunctionSymbol(Utf8String name) =>
            _wasmSymbolManager.AddDefinition(name, WasmIndexSpace.Function);

        // This effectively recreates the logic of RecordMethodBody/RecordMethodDeclaration, but for manually inserted stubs that are not
        // represented by nodes in the dependency graph.
        // TODO-Wasm: for maintability, we should try and push some of this into the dependency graph when we do more stub generation.
        private protected void RegisterStubIndexAndSignature(WasmFuncType signature)
        {
            int signatureIndex = RegisterSignature(signature);
            WriteFunctionEntry(signatureIndex);
        }

        private protected void InsertWasmStub(Utf8String name, WasmFunctionBody body)
        {
            SectionWriter codeWriter = GetOrCreateSection(ObjectNodeSection.WasmCodeSection);

            int codeSize = body.EncodeSize();
            byte[] data = new byte[codeSize];
            body.Encode(data);

            codeWriter.EmitSymbolDefinition(name);
            codeWriter.EmitData(data);

            RegisterFunctionSymbol(name);
            RegisterStubIndexAndSignature(body.Signature);
        }

        private protected int RegisterSignature(WasmFuncType signature)
        {
            Utf8String signatureKey = signature.GetMangledName(_nodeFactory.NameMangler);
            if (_wasmSymbolManager.TryGetSymbol(signatureKey, out WasmSymbol signatureSymbol))
            {
                return signatureSymbol.Index;
            }

            SectionWriter typeSectionWriter = GetOrCreateSection(ObjectNodeSection.WasmTypeSection);
            byte[] encodedSignature = new byte[signature.EncodeSize()];
            signature.Encode(encodedSignature);
            _wasmSymbolManager.AddDefinition(signatureKey, WasmIndexSpace.Type);
            typeSectionWriter.EmitSymbolDefinition(signatureKey);
            typeSectionWriter.EmitData(encodedSignature);

            return _wasmSymbolManager.GetSymbol(signatureKey).Index;
        }

        // Populate sections whose entries are derived from the completed symbol table.
        private protected override void EmitSymbolTable(
            IDictionary<Utf8String, SymbolDefinition> definedSymbols,
            SortedSet<Utf8String> undefinedSymbols)
        {
            WriteImports();
            WriteGlobalSection();
            WriteExports();
            WriteElements();

            // Register defined symbols for future use during relocation resolution.
            _definedSymbols = new Dictionary<Utf8String, SymbolDefinition>(definedSymbols);
        }

        private protected abstract void WriteImports();
        private protected abstract void WriteGlobalSection();
        private protected abstract void WriteExports();
        private protected abstract void WriteElements();

        private protected void FinalizeSectionEntryCounts()
        {
            _sections.GetSection<WasmExternallyCountedSection>(ObjectNodeSection.WasmTypeSection.Name)
                .SetEntryCount(_wasmSymbolManager.GetDefinitionCount(WasmIndexSpace.Type));
            _sections.GetSection<WasmExternallyCountedSection>(ObjectNodeSection.WasmCodeSection.Name)
                .SetEntryCount(MethodCount);

            Debug.Assert(_sections.GetSection<WasmFunctionSection>(WasmObjectNodeSection.FunctionSection.Name).EntryCount == MethodCount);
            Debug.Assert(_sections.GetSection<WasmImportSection>(WasmObjectNodeSection.ImportSection.Name).EntryCount == _wasmSymbolManager.GetImportCount());
            Debug.Assert(_sections.GetSection<WasmGlobalSection>(WasmObjectNodeSection.GlobalSection.Name).EntryCount == _wasmSymbolManager.GetDefinitionCount(WasmIndexSpace.Global));
        }
    }

    internal static class WasmObjectNodeSection
    {
        // TODO-WASM: Consider alignment needs for data sections
        public static readonly ObjectNodeSection DataSection = new("wasm.data", SectionType.Writeable, needsAlign: false);
        public static readonly ObjectNodeSection DataCountSection = new("wasm.datacount", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection CombinedDataSection = new("wasm.alldata", SectionType.Writeable, needsAlign: false);
        public static readonly ObjectNodeSection FunctionSection = new("wasm.function", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ExportSection = new("wasm.export", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ElementSection = new("wasm.element", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection MemorySection = new("wasm.memory", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection TableSection = new("wasm.table", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection ImportSection = new("wasm.import", SectionType.ReadOnly, needsAlign: false);
        public static readonly ObjectNodeSection GlobalSection = new("wasm.global", SectionType.ReadOnly, needsAlign: false);
    }
}
