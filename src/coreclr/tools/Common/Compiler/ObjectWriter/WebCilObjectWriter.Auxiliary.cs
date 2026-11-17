// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        // Maps each function index of the combined module to its index in the module that owns it, or null when
        // all functions stay in the primary module.
        private int[] _partitionedFunctionIndices;
        private bool[] _isAuxiliaryFunction;
        private int _auxiliarySlotStart;
        private int _auxiliarySlotEnd;

        private protected override bool AreFunctionBodiesEquivalent(ObjectNode left, ObjectNode right) =>
            (left is MethodWithGCInfo { IsAuxiliary: true }) ==
            (right is MethodWithGCInfo { IsAuxiliary: true });

        private long GetPartitionedFunctionIndex(int index) =>
            _partitionedFunctionIndices is null ? index : _partitionedFunctionIndices[index];

        /// <summary>
        /// Computes the auxiliary table-slot range and the per-module function index remapping.
        /// Returns false when no method was placed in an auxiliary module.
        /// </summary>
        private bool PrepareAuxiliaryPartition()
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

            if (slotEnd - slotStart != slotCount)
                throw new InvalidDataException("Auxiliary methods must occupy a contiguous range of the WebAssembly function table.");

            int functionImports = _wasmSymbolManager.GetImportCount(WasmIndexSpace.Function);
            int functionCount = functionImports + _wasmSymbolManager.GetDefinitionCount(WasmIndexSpace.Function);
            int[] tableFunctions = GetTableSlotFunctionIndices();

            bool[] isAuxiliaryFunction = new bool[functionCount];
            for (int slot = slotStart; slot < slotEnd; slot++)
                isAuxiliaryFunction[tableFunctions[slot]] = true;

            for (int slot = 0; slot < tableFunctions.Length; slot++)
            {
                if ((slot < slotStart || slot >= slotEnd) && isAuxiliaryFunction[tableFunctions[slot]])
                    throw new InvalidDataException("A WebAssembly function is referenced by both primary and auxiliary table slots.");
            }

            int[] partitionedIndices = new int[functionCount];
            int primaryOrdinal = 0;
            int auxiliaryOrdinal = 0;
            for (int index = 0; index < functionCount; index++)
            {
                if (index < functionImports)
                    partitionedIndices[index] = index;
                else if (isAuxiliaryFunction[index])
                    partitionedIndices[index] = functionImports + auxiliaryOrdinal++;
                else
                    partitionedIndices[index] = functionImports + primaryOrdinal++;
            }

            _auxiliarySlotStart = slotStart;
            _auxiliarySlotEnd = slotEnd;
            _isAuxiliaryFunction = isAuxiliaryFunction;
            _partitionedFunctionIndices = partitionedIndices;
            return true;
        }

        private void EmitPartitionedObjectFile(Stream primaryOutput, WebcilPayloadDataSegment payloadSegment, WebcilSection[] webcilSections)
        {
            if (primaryOutput is not FileStream primaryFile)
                throw new InvalidOperationException("Auxiliary WebAssembly modules require a file output stream.");

            int functionImports = _wasmSymbolManager.GetImportCount(WasmIndexSpace.Function);
            List<WasmSymbol> functions = new();
            foreach (WasmSymbol symbol in _wasmSymbolManager.GetDefinitions(WasmIndexSpace.Function))
                functions.Add(symbol);

            List<int> functionTypes = new();
            Stream functionSection = _sections.GetSection<WasmFunctionSection>(WasmObjectNodeSection.FunctionSection.Name).ContentReadStream;
            functionSection.Position = 0;
            while (functionSection.Position < functionSection.Length)
                functionTypes.Add(checked((int)DwarfHelper.ReadULEB128(functionSection, out _).Value));
            Debug.Assert(functionTypes.Count == functions.Count);

            // Resolve the code section once: function index relocations resolve to the index in the owning module,
            // which is correct for both modules as long as no direct call crosses modules.
            int codeSectionIndex = _sections.GetSectionIndex(ObjectNodeSection.WasmCodeSection.Name);
            Stream originalCode = _sections[codeSectionIndex].ContentReadStream;
            List<SymbolicRelocation> codeRelocations = _resolvableRelocations.TryGetValue(codeSectionIndex, out List<SymbolicRelocation> relocations)
                ? relocations
                : new List<SymbolicRelocation>();
            originalCode.Position = 0;
            ValidateAuxiliaryReferences(ParseCodeBlobs(originalCode), codeRelocations, functionImports);

            MemoryStream resolvedCode = new MemoryStream((int)originalCode.Length);
            originalCode.Position = 0;
            ResolveRelocations(codeSectionIndex, originalCode, resolvedCode, codeRelocations, sectionStart: 0, shrink: true);
            resolvedCode.Position = 0;
            List<CodeBlob> codeBlobs = ParseCodeBlobs(resolvedCode);
            if (codeBlobs.Count != functions.Count)
                throw new InvalidDataException("The WebAssembly code section does not match the function index space.");

            List<int> primaryOrdinals = new();
            List<int> auxiliaryOrdinals = new();
            for (int ordinal = 0; ordinal < functions.Count; ordinal++)
                (_isAuxiliaryFunction[functions[ordinal].Index] ? auxiliaryOrdinals : primaryOrdinals).Add(ordinal);

            int[] tableFunctions = GetTableSlotFunctionIndices();
            int metadataRva = checked((int)ResolveSymbolRVA(webcilSections,
                _definedSymbols[GetMangledName(_nodeFactory.AuxiliaryModuleMetadata)]));

            /*********************************************************************
             * Primary module
             *********************************************************************/

            EmitWasmHeader(primaryOutput);
            long codeContentFileOffset = 0;
            Dictionary<ulong, ulong> primaryCodeOffsets = new();
            foreach (int index in SectionEmitOrder)
            {
                SectionDataEmitter section = _sections[index];
                string name = section.SectionName.ToString();
                if (name == WasmObjectNodeSection.FunctionSection.Name)
                {
                    WriteRawSection(primaryOutput, WasmSectionType.Function, content =>
                    {
                        WriteUnsigned(content, primaryOrdinals.Count);
                        foreach (int ordinal in primaryOrdinals)
                            WriteUnsigned(content, functionTypes[ordinal]);
                    });
                }
                else if (name == WasmObjectNodeSection.ExportSection.Name)
                {
                    WriteRawSection(primaryOutput, WasmSectionType.Export, content =>
                    {
                        WriteUnsigned(content, _wasmStubNames.Count + 2);
                        WriteExport(content, "table", WasmExportKind.Table, 0);
                        WriteExport(content, "webcilVersion", WasmExportKind.Global, _definedGlobals["webcilVersion"].Index);
                        foreach (Utf8String stubName in _wasmStubNames)
                        {
                            int stubIndex = _wasmSymbolManager.GetSymbol(stubName).Index;
                            WriteExport(content, stubName.ToString(), WasmExportKind.Function, _partitionedFunctionIndices[stubIndex]);
                        }
                    });
                }
                else if (name == WasmObjectNodeSection.ElementSection.Name)
                {
                    WriteRawSection(primaryOutput, WasmSectionType.Element, content =>
                    {
                        bool hasLeadingSegment = _auxiliarySlotStart > 0;
                        bool hasTrailingSegment = _auxiliarySlotEnd < tableFunctions.Length;
                        WriteUnsigned(content, (hasLeadingSegment ? 1 : 0) + (hasTrailingSegment ? 1 : 0));
                        if (hasLeadingSegment)
                            WriteElementSegment(content, tableFunctions, 0, _auxiliarySlotStart, passive: false);
                        if (hasTrailingSegment)
                            WriteElementSegment(content, tableFunctions, _auxiliarySlotEnd, tableFunctions.Length - _auxiliarySlotEnd, passive: false);
                    });
                }
                else if (index == codeSectionIndex)
                {
                    WriteRawSection(primaryOutput, WasmSectionType.Code, content =>
                    {
                        WriteUnsigned(content, primaryOrdinals.Count);
                        codeContentFileOffset = primaryOutput.Position + 1 + DwarfHelper.SizeOfULEB128((ulong)ComputeCodeContentSize(primaryOrdinals)) + content.Length;
                        long contentStart = content.Length;
                        int primaryCursor = 0;
                        for (int ordinal = 0; ordinal < codeBlobs.Count; ordinal++)
                        {
                            CodeBlob blob = codeBlobs[ordinal];
                            long entryStart = blob.Start - DwarfHelper.SizeOfULEB128((ulong)blob.Size);
                            primaryCodeOffsets[(ulong)entryStart] = (ulong)(content.Length - contentStart);
                            if (primaryCursor < primaryOrdinals.Count && primaryOrdinals[primaryCursor] == ordinal)
                            {
                                WriteCodeBlob(content, resolvedCode, blob);
                                primaryCursor++;
                            }
                        }
                        primaryCodeOffsets[(ulong)resolvedCode.Length] = (ulong)(content.Length - contentStart);
                    });
                }
                else
                {
                    if (_resolvableRelocations.TryGetValue(index, out List<SymbolicRelocation> sectionRelocations) &&
                        section is WasmSection)
                    {
                        using Stream originalStream = section.ContentReadStream;
                        MemoryStream destStream = new MemoryStream((int)originalStream.Length);
                        originalStream.Position = 0;
                        ResolveRelocations(index, originalStream, destStream, sectionRelocations, sectionStart: 0, shrink: true);
                        section.ContentReadStream = destStream;
                    }

                    section.EmitToStream(primaryOutput);
                }
            }

            EmitWebcilDataSection(primaryOutput, payloadSegment);

            if (_emitNameSection)
                new WasmNameSection(GetPartitionedFunctionNames(functions, primaryOrdinals)).EmitToStream(primaryOutput);

            if (_outputInfoBuilder is not null)
            {
                // Only the primary module's code is described: auxiliary method nodes resolve to the position
                // where their bodies were removed from the primary code section.
                for (int i = 0; i < _sections.Count; i++)
                {
                    SectionDataEmitter emittedSection = _sections[i];
                    ulong fileOffset = (i == codeSectionIndex) ? (ulong)codeContentFileOffset : 0;
                    _outputSectionLayout.Add(new OutputSection(
                        emittedSection.SectionName.ToString(), fileOffset, fileOffset, (ulong)emittedSection.ContentReadStream.Length));
                }

                Dictionary<ulong, ulong> codeOffsetMap = new();
                foreach (KeyValuePair<ulong, ulong> entry in _codeOffsetMap)
                    codeOffsetMap[entry.Key] = primaryCodeOffsets[entry.Value];
                _outputInfoBuilder.RemapMethodNodeOffsets(codeSectionIndex, codeOffsetMap);
            }

            /*********************************************************************
             * Auxiliary module
             *********************************************************************/

            string auxiliaryPath = Path.Combine(
                Path.GetDirectoryName(primaryFile.Name),
                AuxiliaryModuleMetadataNode.GetAuxiliaryModuleFileName(primaryFile.Name, 0));
            using FileStream auxiliaryOutput = new FileStream(auxiliaryPath, FileMode.Create);

            int activationSignature = RegisterSignature(RtlRestoreContextTagSignature);
            int activationFunctionIndex = functionImports + auxiliaryOrdinals.Count;

            EmitWasmHeader(auxiliaryOutput);
            _sections.GetSection<WasmExternallyCountedSection>(ObjectNodeSection.WasmTypeSection.Name).EmitToStream(auxiliaryOutput);
            _sections.GetSection<WasmImportSection>(WasmObjectNodeSection.ImportSection.Name).EmitToStream(auxiliaryOutput);
            WriteRawSection(auxiliaryOutput, WasmSectionType.Function, content =>
            {
                WriteUnsigned(content, auxiliaryOrdinals.Count + 1);
                foreach (int ordinal in auxiliaryOrdinals)
                    WriteUnsigned(content, functionTypes[ordinal]);
                WriteUnsigned(content, activationSignature);
            });
            WriteRawSection(auxiliaryOutput, WasmSectionType.Export, content =>
            {
                WriteUnsigned(content, 1);
                WriteExport(content, ActivateModuleExportName, WasmExportKind.Function, activationFunctionIndex);
            });
            WriteRawSection(auxiliaryOutput, WasmSectionType.Element, content =>
            {
                WriteUnsigned(content, 1);
                WriteElementSegment(content, tableFunctions, _auxiliarySlotStart, _auxiliarySlotEnd - _auxiliarySlotStart, passive: true);
            });
            WriteRawSection(auxiliaryOutput, WasmSectionType.Code, content =>
            {
                WriteUnsigned(content, auxiliaryOrdinals.Count + 1);
                foreach (int ordinal in auxiliaryOrdinals)
                    WriteCodeBlob(content, resolvedCode, codeBlobs[ordinal]);
                WriteFunctionBody(content, CreateActivateModuleBody(metadataRva + AuxiliaryModuleMetadataNode.GetActivationStatusOffset(0)));
            });

            if (_emitNameSection)
            {
                List<WasmSymbol> names = GetPartitionedFunctionNames(functions, auxiliaryOrdinals);
                names.Add(new WasmSymbol(new Utf8String(ActivateModuleExportName), WasmIndexSpace.Function, activationFunctionIndex, IsImport: false, HasExplicitName: false));
                new WasmNameSection(names).EmitToStream(auxiliaryOutput);
            }

            long ComputeCodeContentSize(List<int> ordinals)
            {
                long size = DwarfHelper.SizeOfULEB128((ulong)ordinals.Count);
                foreach (int ordinal in ordinals)
                    size += DwarfHelper.SizeOfULEB128((ulong)codeBlobs[ordinal].Size) + codeBlobs[ordinal].Size;
                return size;
            }
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

                bool callerIsAuxiliary = _isAuxiliaryFunction[functionImports + ordinal];
                switch (relocation.Type)
                {
                    case RelocType.WASM_FUNCTION_INDEX_LEB:
                        WasmSymbol target = _wasmSymbolManager.GetSymbol(relocation.SymbolName);
                        if (_isAuxiliaryFunction[target.Index] != callerIsAuxiliary)
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
                I32.Const(_auxiliarySlotEnd - _auxiliarySlotStart),
                Table.Init(0),
                Global.Get(ImageBaseGlobalIndex),
                I32.Const(1),
                I32.Store((ulong)activationStatusRva),
                Block.End,
            ]);

        private List<WasmSymbol> GetPartitionedFunctionNames(List<WasmSymbol> functions, List<int> ordinals)
        {
            List<WasmSymbol> names = new(ordinals.Count);
            foreach (int ordinal in ordinals)
            {
                WasmSymbol symbol = functions[ordinal];
                names.Add(symbol with { Index = _partitionedFunctionIndices[symbol.Index] });
            }
            return names;
        }

        private void WriteElementSegment(Stream stream, int[] tableFunctions, int start, int count, bool passive)
        {
            if (passive)
            {
                stream.WriteByte(1); // passive, elemkind
                stream.WriteByte(0); // funcref
            }
            else
            {
                stream.WriteByte(0); // active, table 0, offset expression
                stream.WriteByte((byte)WasmExprKind.GlobalGet);
                WriteUnsigned(stream, TableBaseGlobalIndex);
                stream.WriteByte((byte)WasmExprKind.I32Const);
                WriteSigned(stream, start);
                stream.WriteByte((byte)WasmExprKind.I32Add);
                stream.WriteByte((byte)WasmExprKind.End);
            }

            WriteUnsigned(stream, count);
            for (int slot = start; slot < start + count; slot++)
                WriteUnsigned(stream, _partitionedFunctionIndices[tableFunctions[slot]]);
        }

        private static void WriteCodeBlob(Stream stream, MemoryStream code, CodeBlob blob)
        {
            WriteUnsigned(stream, checked((int)blob.Size));
            stream.Write(code.GetBuffer().AsSpan(checked((int)blob.Start), checked((int)blob.Size)));
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

        private static void WriteSigned(Stream stream, int value)
        {
            Span<byte> bytes = stackalloc byte[10];
            int length = DwarfHelper.WriteSLEB128(bytes, value);
            stream.Write(bytes.Slice(0, length));
        }
    }
}
