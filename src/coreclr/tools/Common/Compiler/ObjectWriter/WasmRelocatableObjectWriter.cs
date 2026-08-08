// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using ILCompiler.DependencyAnalysis;
using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem.TypesDebugInfo;
using ILCompiler.DependencyAnalysis.Wasm;

namespace ILCompiler.ObjectWriter
{
    internal sealed partial class WasmRelocatableObjectWriter : WasmObjectWriter
    {
        public WasmRelocatableObjectWriter(NodeFactory factory, ObjectWritingOptions options, OutputInfoBuilder outputInfoBuilder = null) : base(factory, options, outputInfoBuilder)
        {
        }

        private protected override void EmitObjectFile(Stream outputFileStream)
        {
            Debug.Assert(outputFileStream.CanSeek, $"EmitObjectFile requires seekable output stream");

            FinalizeSectionEntryCounts();

            EmitWasmHeader(outputFileStream);

            foreach (int index in SectionEmitOrder)
            {
                SectionDataEmitter section = _sections[index];
                if (_resolvableRelocations.TryGetValue(index, out List<SymbolicRelocation> relocations) &&
                    section is WasmSection)
                {
                    using (Stream originalStream = section.ContentReadStream)
                    {
                        MemoryStream stream = new MemoryStream((int)originalStream.Length);
                        originalStream.Position = 0;
                        originalStream.CopyTo(stream);
                        ResolveRelocations(index, stream, relocations, sectionStart: 0);
                        section.ContentReadStream = stream;
                        // originalStream may be disposed, section.Stream now points to resolved stream
                    }
                }

                section.EmitToStream(outputFileStream);
            }
        }

        private Dictionary<int, List<SymbolicRelocation>> _resolvableRelocations = new();
        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            foreach (var reloc in relocationList)
            {
                if (!_resolvableRelocations.TryGetValue(sectionIndex, out List<SymbolicRelocation> resolvable))
                {
                    _resolvableRelocations[sectionIndex] = resolvable = new List<SymbolicRelocation>();
                }
                // Unconditionally add the reloc to our resolvable list; we do some amount of relocation resolution
                // for all relocation types.
                resolvable.Add(reloc);
            }
        }

        private unsafe void ResolveRelocations(int sectionIndex, MemoryStream sectionStream, List<SymbolicRelocation> relocs, long sectionStart = 0)
        {
            byte[] relocScratchBuffer = new byte[Relocation.MaxSize];

            foreach (SymbolicRelocation reloc in relocs)
            {
                int size = Relocation.GetSize(reloc.Type);
                if (size > relocScratchBuffer.Length)
                {
                    throw new InvalidOperationException($"Unsupported relocation size for relocation: {reloc.Type}");
                }

                SymbolDefinition definedSymbol = _definedSymbols[reloc.SymbolName];

                // We need a pinned raw pointer here for manipulation with Relocation.WriteValue
                fixed (byte* pData = ReadRelocToDataSpan(reloc, relocScratchBuffer, sectionStart))
                {
                    long addend = Relocation.ReadValue(reloc.Type, pData);
                    int relocLength = Relocation.GetSize(reloc.Type);

                    switch (reloc.Type)
                    {
                        case RelocType.WASM_TYPE_INDEX_LEB:
                        case RelocType.WASM_GLOBAL_INDEX_LEB:
                        case RelocType.WASM_TABLE_INDEX_I32:
                        case RelocType.WASM_TABLE_INDEX_I64:
                        case RelocType.WASM_TABLE_INDEX_SLEB:
                        case RelocType.WASM_TABLE_INDEX_REL_I32:
                        case RelocType.WASM_FUNCTION_INDEX_LEB:
                        case RelocType.WASM_MEMORY_ADDR_REL_SLEB when _sections.GetSection<WasmSection>(definedSymbol.SectionIndex).Type == WasmSectionType.Code:
                        {
                            // These relocations reference a wasm structural index (function, type,
                            // table entry, or well-known global). For R2R we self-resolve them here to
                            // the index assigned when the symbol was registered into its index space.
                            if (!_wasmSymbolManager.TryGetSymbol(reloc.SymbolName, out WasmSymbol symbol))
                            {
                                throw new InvalidOperationException($"Symbol '{reloc.SymbolName}' was not registered. Relocation type {reloc.Type}.");
                            }
                            Relocation.WriteValue(reloc.Type, pData, symbol.Index + addend);
                            break;
                        }

                        default:
                            // TODO-WASM: add other cases as needed;
                            // ignoring other reloc types for now
                            throw new NotSupportedException($"Relocation type {reloc.Type} not yet implemented");
                    }

                    WriteRelocFromDataSpan(reloc, pData, sectionStart);
                }
            }

            Span<byte> ReadRelocToDataSpan(SymbolicRelocation reloc, byte[] buffer, long sectionStart)
            {
                Span<byte> relocContents = buffer.AsSpan(0, Relocation.GetSize(reloc.Type));
                sectionStream.Position = reloc.Offset + sectionStart;
                sectionStream.ReadExactly(relocContents);
                return relocContents;
            }

            void WriteRelocFromDataSpan(SymbolicRelocation reloc, byte* pData, long sectionStart)
            {
                sectionStream.Position = reloc.Offset + sectionStart;
                sectionStream.Write(new Span<byte>(pData, Relocation.GetSize(reloc.Type)));
            }
        }

        private protected override SectionDataEmitter CreateDataSection(
            ObjectNodeSection section,
            int sectionIndex,
            Stream sectionStream)
        {
            return new WasmSection(WasmSectionType.Data, sectionStream, new Utf8String("data"), sectionIndex);
        }

        protected internal override void UpdateSectionAlignment(int sectionIndex, int alignment)
        {
        }
        private protected override void WriteGlobalSection()
        {
        }

        private const int RtlRestoreContextTagIndex = 0;
        private static readonly WasmFuncType RtlRestoreContextTagSignature = new(
            new([]),
            new([]));
        private const int StackPointerGlobalIndex = 0;
        private const int ImageBaseGlobalIndex = 1;
        private const int TableBaseGlobalIndex = 2;
        private const int AsyncContinuationGlobalIndex = 3;
        private static readonly Utf8String RtlRestoreContextTagName = new Utf8String("RtlRestoreContextTag");
        private WasmImport[] CreateDefaultGlobalImports()
        {
            int rtlRestoreContextTagTypeIndex = RegisterSignature(RtlRestoreContextTagSignature);

            return
            [
                new WasmImport("webcil", WasmWellKnownGlobalSymbolNode.StackPointerName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Mut), index: StackPointerGlobalIndex),
                new WasmImport("webcil", WasmWellKnownGlobalSymbolNode.ImageBaseName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Const), index: ImageBaseGlobalIndex),
                new WasmImport("webcil", WasmWellKnownGlobalSymbolNode.TableBaseName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Const), index: TableBaseGlobalIndex),
                new WasmImport("webcil", WasmWellKnownGlobalSymbolNode.AsyncContinuationName, import: new WasmGlobalImportType(WasmValueType.I32, WasmMutabilityType.Mut), index: AsyncContinuationGlobalIndex),
                new WasmImport("webcil", "table", import: new WasmTableImportType(), index: 0),
                new WasmImport("webcil", RtlRestoreContextTagName.ToString(), import: new WasmTagImportType(rtlRestoreContextTagTypeIndex), index: RtlRestoreContextTagIndex),
                new WasmImport("webcil", "memory", import: new WasmMemoryImportType(WasmLimitType.HasMin, /* TODO: This is an arbitrary number */ 32))
            ];
        }

        private protected override void WriteImports()
        {
            foreach (WasmImport import in CreateDefaultGlobalImports())
            {
                WriteImport(import);
            }
        }

        private protected override void WriteExports()
        {
        }

        private protected override void WriteElements()
        {
        }
    }

    // AOT
    internal sealed partial class WasmRelocatableObjectWriter : WasmObjectWriter
    {
        private protected override void EmitUnwindInfo(SectionWriter sectionWriter, INodeWithCodeInfo nodeWithCodeInfo, Utf8String currentSymbolName)
        {
        }

        private protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            return null;
        }

        private protected override void EmitDebugFunctionInfo(uint methodTypeIndex, Utf8String methodName, SymbolDefinition methodSymbol, INodeWithDebugInfo debugNode, bool hasSequencePoints)
        {
        }

        private protected override void EmitDebugSections(IDictionary<Utf8String, SymbolDefinition> definedSymbols)
        {
        }

        private protected override void CreateEhSections()
        {
        }
    }
}
