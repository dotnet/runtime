// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.Text;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Splits the bodies of auxiliary methods into a separate WebAssembly module. The primary module keeps the
    /// Webcil payload, every primary function, and the table slots of primary functions. The auxiliary module
    /// (<c>&lt;name&gt;.1.wasm</c>) only contains the auxiliary function bodies and an <c>ActivateModule</c>
    /// export, which installs them into their reserved slots of the shared function table and marks the module
    /// active in the primary module's auxiliary module metadata.
    /// </summary>
    internal sealed partial class WebCilObjectWriter
    {
        private const string ActivateModuleExportName = "ActivateModule";

        private int _firstAuxiliaryFunctionIndex = -1;
        private int _auxiliarySlotStart;
        private int _auxiliarySlotCount;
        private int _activationFunctionIndex;

        private protected override bool CanIdenticalMethodBodiesBeDeduplicated(ObjectNode left, ObjectNode right) =>
            (left is MethodWithGCInfo { IsAuxiliary: true }) ==
            (right is MethodWithGCInfo { IsAuxiliary: true });

        private bool IsAuxiliaryFunctionIndex(int index) =>
            _firstAuxiliaryFunctionIndex >= 0 && index >= _firstAuxiliaryFunctionIndex;

        private int GetPartitionedFunctionIndex(int index) => IsAuxiliaryFunctionIndex(index)
            ? index - _firstAuxiliaryFunctionIndex + _wasmSymbolManager.GetImportCount(WasmIndexSpace.Function)
            : index;

        /// <summary>
        /// Computes the range of the function pointer table allotted to the auxiliary module,
        /// as well as the range of entries in the Code and Function section that belong to the
        /// auxiliary module.
        /// </summary>
        /// <param name="tableFunctions">
        /// Maps each module-relative function-table slot to its WebAssembly function index.
        /// </param>
        /// <returns>
        /// True if any methods were placed in the auxiliary module; otherwise, false.
        /// </returns>
        /// <remarks>
        /// Auxiliary methods must come after all primary methods in the function pointer table
        /// and the Code and Function sections.
        /// </remarks>
        private bool PrepareAuxiliaryPartition(int[] tableFunctions)
        {
            if (_nodeFactory.AuxiliaryModuleMetadata is null)
                return false;

            int slotStart = int.MaxValue;
            int slotEnd = 0;
            int slotCount = 0;
            foreach (MethodWithGCInfo method in _nodeFactory.EnumerateCompiledMethods())
            {
                if (!method.IsAuxiliary)
                    continue;

                int slot = GetTableSlot(new Utf8String(method.GetMangledName(_nodeFactory.NameMangler)));
                slotStart = Math.Min(slotStart, slot);
                slotEnd = Math.Max(slotEnd, slot + method.FrameInfos.Length);
                slotCount += method.FrameInfos.Length;
            }

            if (slotCount == 0)
                return false;

            if (slotEnd != tableFunctions.Length || slotEnd - slotStart != slotCount)
                throw new InvalidDataException("Auxiliary methods must occupy a contiguous suffix of the WebAssembly function table.");

            int functionImports = _wasmSymbolManager.GetImportCount(WasmIndexSpace.Function);
            int functionCount = functionImports + _wasmSymbolManager.GetDefinitionCount(WasmIndexSpace.Function);
            bool[] isAuxiliaryFunction = new bool[functionCount];
            int firstAuxiliaryFunctionIndex = functionCount;
            for (int slot = slotStart; slot < slotEnd; slot++)
            {
                int index = tableFunctions[slot];
                isAuxiliaryFunction[index] = true;
                firstAuxiliaryFunctionIndex = Math.Min(firstAuxiliaryFunctionIndex, index);
            }

            for (int slot = 0; slot < slotStart; slot++)
            {
                if (tableFunctions[slot] >= firstAuxiliaryFunctionIndex)
                    throw new InvalidDataException("A WebAssembly function is referenced by both primary and auxiliary table slots.");
            }

            if (firstAuxiliaryFunctionIndex < functionImports)
                throw new InvalidDataException("An auxiliary slot points to an imported function.");
            for (int index = firstAuxiliaryFunctionIndex; index < functionCount; index++)
            {
                if (!isAuxiliaryFunction[index])
                    throw new InvalidDataException("Auxiliary methods must occupy a contiguous suffix of WebAssembly function definitions.");
            }

            _auxiliarySlotStart = slotStart;
            _auxiliarySlotCount = slotCount;
            _firstAuxiliaryFunctionIndex = firstAuxiliaryFunctionIndex;
            return true;
        }

        private sealed class AuxiliaryPartition : IDisposable
        {
            public required WasmExternallyCountedSection PrimaryFunctionSection;
            public required WasmExternallyCountedSection PrimaryCodeSection;
            public required WasmExternallyCountedSection AuxiliaryFunctionEntries;
            public required WasmExternallyCountedSection AuxiliaryCode;
            public required WasmExternallyCountedSection AuxiliaryExportSection;
            public WasmNameSection? AuxiliaryNameSection;
            public Dictionary<ulong, ulong>? PrimaryCodeOffsetMap;

            public void Dispose()
            {
                PrimaryFunctionSection.ContentReadStream.Dispose();
                PrimaryCodeSection.ContentReadStream.Dispose();
                AuxiliaryFunctionEntries.ContentReadStream.Dispose();
                AuxiliaryCode.ContentReadStream.Dispose();
                AuxiliaryExportSection.ContentReadStream.Dispose();
            }
        }

        /// <summary>
        /// Splits the Code and Function sections into primary and auxiliary partitions based on the indices calculated in <see cref="PrepareAuxiliaryPartition"/>.
        /// </summary>
        private AuxiliaryPartition PreparePartitionedSections(WebcilSection[] webcilSections)
        {
            int functionImports = _wasmSymbolManager.GetImportCount(WasmIndexSpace.Function);
            int functionCount = _wasmSymbolManager.GetDefinitionCount(WasmIndexSpace.Function);
            int primaryFunctionCount = _firstAuxiliaryFunctionIndex - functionImports;
            int functionSectionIndex = _sections.GetSectionIndex(WasmObjectNodeSection.FunctionSection.Name);
            Stream functionSection = _sections[functionSectionIndex].ContentReadStream;
            functionSection.Position = 0;
            for (int i = 0; i < primaryFunctionCount; i++)
                DwarfHelper.ReadULEB128(functionSection, out _);
            int functionBoundary = checked((int)functionSection.Position);
            byte[] functionEntries = new byte[checked((int)functionSection.Length)];
            functionSection.Position = 0;
            functionSection.ReadExactly(functionEntries);

            // Resolve the code section once: function index relocations resolve to the index in the owning module,
            // which is correct for both modules as long as no direct call crosses modules.
            int codeSectionIndex = _sections.GetSectionIndex(ObjectNodeSection.WasmCodeSection.Name);
            Stream originalCode = _sections[codeSectionIndex].ContentReadStream;
            List<SymbolicRelocation> codeRelocations = _resolvableRelocations.TryGetValue(codeSectionIndex, out List<SymbolicRelocation>? relocations)
                ? relocations
                : new List<SymbolicRelocation>();
            originalCode.Position = 0;
            ValidateAuxiliaryReferences(ParseCodeBlobs(originalCode), codeRelocations, functionImports);

            MemoryStream resolvedCode = new MemoryStream((int)originalCode.Length);
            originalCode.Position = 0;
            ResolveRelocations(codeSectionIndex, originalCode, resolvedCode, codeRelocations, sectionStart: 0, shrink: true);
            resolvedCode.Position = 0;
            List<CodeBlob> codeBlobs = ParseCodeBlobs(resolvedCode);
            if (codeBlobs.Count != functionCount)
                throw new InvalidDataException("The WebAssembly code section does not match the function index space.");
            int codeBoundary = GetEntryStart(codeBlobs[primaryFunctionCount]);

            int metadataRva = checked((int)ResolveSymbolRVA(webcilSections,
                _definedSymbols[GetMangledName(_nodeFactory.AuxiliaryModuleMetadata)]));

            Dictionary<ulong, ulong>? codeOffsetMap = null;
            if (_outputInfoBuilder is not null)
            {
                codeOffsetMap = new(_codeOffsetMap.Count);
                foreach (KeyValuePair<ulong, ulong> entry in _codeOffsetMap)
                    codeOffsetMap[entry.Key] = Math.Min(entry.Value, (ulong)codeBoundary);
            }

            // Create WASM section structures for the primary Function and Code sections
            MemoryStream primaryFunctionSectionData = new(functionEntries, 0, functionBoundary, writable: false);
            MemoryStream primaryCodeSectionData = new(resolvedCode.GetBuffer(), 0, codeBoundary, writable: false);
            WasmExternallyCountedSection primaryFunctionSection = new(
                WasmSectionType.Function,
                primaryFunctionSectionData,
                _sections[functionSectionIndex].SectionName,
                functionSectionIndex);
            primaryFunctionSection.SetEntryCount(primaryFunctionCount);
            WasmExternallyCountedSection primaryCodeSection = new(
                WasmSectionType.Code,
                primaryCodeSectionData,
                _sections[codeSectionIndex].SectionName,
                codeSectionIndex);
            primaryCodeSection.SetEntryCount(primaryFunctionCount);

            // Copy existing Function section entries, and add the signature for ActivateModule () -> ()
            int activationSignature = RegisterSignature(RtlRestoreContextTagSignature);
            MemoryStream auxiliaryTypes = new();
            auxiliaryTypes.Write(functionEntries.AsSpan(functionBoundary));
            WriteUnsigned(auxiliaryTypes, activationSignature);

            // Copy existing method bodies and append the ActivateModule() method body
            MemoryStream auxiliaryCode = new();
            int auxiliaryCodeLength = checked((int)(resolvedCode.Length - codeBoundary));
            auxiliaryCode.Write(resolvedCode.GetBuffer().AsSpan(codeBoundary, auxiliaryCodeLength));
            // Write the ActivateModule method into the auxiliary code section
            int activationStatusRva = metadataRva
                + AuxiliaryModuleMetadataNode.GetActivationStatusOffset(0);
            WriteFunctionBody(auxiliaryCode, CreateActivateModuleBody(activationStatusRva));

            // Create the sections
            WasmExternallyCountedSection auxiliaryFunctionSection = new(WasmSectionType.Function, auxiliaryTypes,
                _sections[functionSectionIndex].SectionName, functionSectionIndex);
            auxiliaryFunctionSection.SetEntryCount(functionCount - primaryFunctionCount + 1 /* ActivateModule() */);
            WasmExternallyCountedSection auxiliaryCodeSection = new(WasmSectionType.Code, auxiliaryCode,
                _sections[codeSectionIndex].SectionName, codeSectionIndex);
            auxiliaryCodeSection.SetEntryCount(functionCount - primaryFunctionCount + 1 /* ActivateModule() */);

            // Both modules use the same function imports, and ActivateModule is the last definition.
            _activationFunctionIndex = functionImports + auxiliaryCodeSection.EntryCount - 1;
            MemoryStream auxiliaryExports = new();
            WriteExport(auxiliaryExports, ActivateModuleExportName, WasmExportKind.Function, _activationFunctionIndex);
            WasmExternallyCountedSection auxiliaryExportSection = new(WasmSectionType.Export, auxiliaryExports, default, sectionIndex: -1);
            auxiliaryExportSection.SetEntryCount(1);

            WasmNameSection? auxiliaryNameSection = _emitNameSection
                ? new WasmNameSection(GetPartitionedFunctionNames(auxiliary: true))
                : null;

            return new AuxiliaryPartition
            {
                PrimaryFunctionSection = primaryFunctionSection,
                PrimaryCodeSection = primaryCodeSection,
                AuxiliaryFunctionEntries = auxiliaryFunctionSection,
                AuxiliaryCode = auxiliaryCodeSection,
                AuxiliaryExportSection = auxiliaryExportSection,
                AuxiliaryNameSection = auxiliaryNameSection,
                PrimaryCodeOffsetMap = codeOffsetMap,
            };

            static int GetEntryStart(CodeBlob blob) =>
                checked((int)(blob.Start - DwarfHelper.SizeOfULEB128((ulong)blob.Size)));
        }

        private void EmitAuxiliaryModule(Stream primaryOutput, AuxiliaryPartition partition)
        {
            if (primaryOutput is not FileStream primaryFile)
                throw new InvalidOperationException("Auxiliary WebAssembly modules require a file output stream.");

            int[] tableFunctions = GetTableSlotFunctionIndices();
            string auxiliaryPath = GetAuxiliaryModulePath(primaryFile.Name);
            using FileStream auxiliaryOutput = new FileStream(auxiliaryPath, FileMode.Create);

            EmitWasmHeader(auxiliaryOutput);
            // Type and Import sections are exact duplicates of the primary module's sections.
            _sections.GetSection<WasmExternallyCountedSection>(ObjectNodeSection.WasmTypeSection.Name)
                .EmitToStream(auxiliaryOutput);
            _sections.GetSection<WasmImportSection>(WasmObjectNodeSection.ImportSection.Name)
                .EmitToStream(auxiliaryOutput);

            partition.AuxiliaryFunctionEntries.EmitToStream(auxiliaryOutput);
            partition.AuxiliaryExportSection.EmitToStream(auxiliaryOutput);

            WriteRawSection(auxiliaryOutput, WasmSectionType.Element, content =>
            {
                WriteUnsigned(content, 1);
                WritePassiveElementSegment(content, tableFunctions, _auxiliarySlotStart, _auxiliarySlotCount);
            });

            partition.AuxiliaryCode.EmitToStream(auxiliaryOutput);
            partition.AuxiliaryNameSection?.EmitToStream(auxiliaryOutput);
        }

        /// <summary>
        /// Rejects direct references that would cross a module boundary: direct calls between primary and
        /// auxiliary functions, and auxiliary references to globals the auxiliary module does not import.
        /// </summary>
        private void ValidateAuxiliaryReferences(List<CodeBlob> blobs, List<SymbolicRelocation> relocations, int functionImports)
        {
            List<SymbolicRelocation> sorted = new(relocations);
            sorted.Sort((a, b) => a.Offset.CompareTo(b.Offset));
            int ordinal = 0;
            foreach (SymbolicRelocation relocation in sorted)
            {
                while (ordinal < blobs.Count && relocation.Offset >= blobs[ordinal].End)
                    ordinal++;
                if (ordinal >= blobs.Count)
                    break;

                bool callerIsAuxiliary = IsAuxiliaryFunctionIndex(functionImports + ordinal);
                switch (relocation.Type)
                {
                    case RelocType.WASM_FUNCTION_INDEX_LEB:
                        WasmSymbol target = _wasmSymbolManager.GetSymbol(relocation.SymbolName);
                        if (IsAuxiliaryFunctionIndex(target.Index) != callerIsAuxiliary)
                            throw new InvalidDataException($"Direct WebAssembly call to '{target.Name}' crosses an auxiliary module boundary.");
                        break;
                    case RelocType.WASM_GLOBAL_INDEX_LEB:
                        if (callerIsAuxiliary && !_wasmSymbolManager.GetSymbol(relocation.SymbolName).IsImport)
                            throw new InvalidDataException($"Auxiliary WebAssembly function references the primary module global '{relocation.SymbolName}'.");
                        break;
                }
            }
        }

        /// <summary>
        /// Creates the body of <c>ActivateModule</c>: on the first call, copies the passive element segment into the
        /// module's reserved table range and sets the module's <c>ActivationStatus</c> in the primary image.
        /// </summary>
        private WasmFunctionBody CreateActivateModuleBody(int activationStatusRva) =>
            new WasmFunctionBody(RtlRestoreContextTagSignature,
            [
                Global.Get(ImageBaseGlobalIndex),
                I32.Load((ulong)activationStatusRva),
                I32.Eqz,
                Block.If(WasmBlockType.Empty),
                Global.Get(TableBaseGlobalIndex),
                I32.Const(_auxiliarySlotStart),
                I32.Add,
                I32.Const(0),
                I32.Const(_auxiliarySlotCount),
                Table.Init(0),
                Global.Get(ImageBaseGlobalIndex),
                I32.Const(1),
                I32.Store((ulong)activationStatusRva),
                Block.End,
            ]);

        private static string GetAuxiliaryModulePath(string primaryModulePath) =>
            Path.Combine(Path.GetDirectoryName(primaryModulePath)!,
                AuxiliaryModuleMetadataNode.GetAuxiliaryModuleFileName(primaryModulePath, 0));

        public static string GetAuxiliarySymbolMapPath(string primaryModulePath) =>
            Path.ChangeExtension(GetAuxiliaryModulePath(primaryModulePath), ".symbols");

        private IEnumerable<WasmSymbol> GetPartitionedFunctionNames(bool auxiliary, bool includeSharedImports = false)
        {
            foreach (WasmSymbol symbol in _wasmSymbolManager.GetDefinitions(WasmIndexSpace.Function))
                if (IsAuxiliaryFunctionIndex(symbol.Index) == auxiliary || (includeSharedImports && symbol.IsImport))
                    yield return symbol with { Index = GetPartitionedFunctionIndex(symbol.Index) };

            if (auxiliary)
                yield return new WasmSymbol(new Utf8String(ActivateModuleExportName), WasmIndexSpace.Function, _activationFunctionIndex, IsImport: false, HasExplicitName: false);
        }

        private void WritePassiveElementSegment(Stream stream, int[] tableFunctions, int start, int count)
        {
            stream.WriteByte(1); // passive, elemkind
            stream.WriteByte(0); // funcref
            WriteUnsigned(stream, count);
            for (int slot = start; slot < start + count; slot++)
                WriteUnsigned(stream, GetPartitionedFunctionIndex(tableFunctions[slot]));
        }

        private static void WriteFunctionBody(Stream stream, WasmFunctionBody body)
        {
            byte[] bytes = new byte[body.EncodeSize()];
            body.Encode(bytes);
            stream.Write(bytes);
        }

        private static void WriteExport(Stream stream, string name, WasmExportKind kind, int index)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(name);
            WriteUnsigned(stream, bytes.Length);
            stream.Write(bytes);
            stream.WriteByte((byte)kind);
            WriteUnsigned(stream, index);
        }

        private static void WriteRawSection(Stream output, WasmSectionType type, Action<MemoryStream> emit)
        {
            using MemoryStream content = new();
            emit(content);
            output.WriteByte((byte)type);
            WriteUnsigned(output, checked((int)content.Length));
            content.Position = 0;
            content.CopyTo(output);
        }

        private static void WriteUnsigned(Stream stream, int value)
        {
            Span<byte> bytes = stackalloc byte[10];
            int length = DwarfHelper.WriteULEB128(bytes, checked((ulong)value));
            stream.Write(bytes.Slice(0, length));
        }
    }
}
