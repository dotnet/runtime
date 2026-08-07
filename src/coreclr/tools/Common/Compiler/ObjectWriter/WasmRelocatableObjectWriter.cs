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
                section.EmitToStream(outputFileStream);
            }
        }

        private protected override void EmitRelocations(int sectionIndex, List<SymbolicRelocation> relocationList)
        {
            // foreach (var reloc in relocationList)
            // {
            //     if (!_resolvableRelocations.TryGetValue(sectionIndex, out List<SymbolicRelocation> resolvable))
            //     {
            //         _resolvableRelocations[sectionIndex] = resolvable = new List<SymbolicRelocation>();
            //     }
            //     // Unconditionally add the reloc to our resolvable list; we do some amount of relocation resolution
            //     // for all relocation types.
            //     resolvable.Add(reloc);

            //     // A few relocation types (table indices and IMAGE_REL type relocs in Webcil) need
            //     // an additional runtime reloc as well to add a base address.
            //     // We defer the actual RVA computation to EmitObjectFile, where webcil section
            //     // VirtualAddresses will have been assigned. Here we just record the raw info.
            //     RelocType fileRelocType = Relocation.GetFileRelocationType(reloc.Type);
            //     if (fileRelocType is not RelocType.IMAGE_REL_BASED_ABSOLUTE)
            //     {
            //         Debug.Assert(WasmSections[sectionIndex] is WebcilSection);
            //         _pendingBaseRelocs.Add(new PendingBaseReloc(sectionIndex, reloc.Offset, fileRelocType));
            //     }
            // }
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

        private protected override void WriteImports() => throw new NotImplementedException();
        private protected override void WriteExports() => throw new NotImplementedException();
        private protected override void WriteElements() => throw new NotImplementedException();

    }

    // AOT
    internal sealed partial class WasmRelocatableObjectWriter : WasmObjectWriter
    {
        private protected override void EmitUnwindInfo(SectionWriter sectionWriter, INodeWithCodeInfo nodeWithCodeInfo, Utf8String currentSymbolName) => throw new NotImplementedException();
        private protected override ITypesDebugInfoWriter CreateDebugInfoBuilder() => throw new NotImplementedException();
        private protected override void EmitDebugFunctionInfo(uint methodTypeIndex, Utf8String methodName, SymbolDefinition methodSymbol, INodeWithDebugInfo debugNode, bool hasSequencePoints) => throw new NotImplementedException();
        private protected override void EmitDebugSections(IDictionary<Utf8String, SymbolDefinition> definedSymbols) => throw new NotImplementedException();
        private protected override void CreateEhSections() => throw new NotImplementedException();
    }
}
