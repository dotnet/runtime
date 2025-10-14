// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using static ILCompiler.DependencyAnalysis.RelocType;
using static ILCompiler.ObjectWriter.CoffObjectWriter.CoffRelocationType;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// COFF object file format writer for Windows targets.
    /// </summary>
    /// <remarks>
    /// The PE/COFF object format is described in the official specifciation at
    /// https://learn.microsoft.com/windows/win32/debug/pe-format. However,
    /// numerous extensions are missing in the specification. The most notable
    /// ones are listed below.
    ///
    /// Object files with more than 65279 sections use an extended big object
    /// file format that is recognized by the Microsoft linker. Many of the
    /// internal file structures are different. The code below denotes it by
    /// "BigObj" in parameters and variables.
    ///
    /// Section names longer than 8 bytes need to be written indirectly in the
    /// string table. The PE/COFF specification describes the /NNNNNNN syntax
    /// for referencing them. However, if the string table gets big enough the
    /// syntax no longer works. There's an undocumented //BBBBBB syntax where
    /// base64 offset is used instead.
    ///
    /// CodeView debugging format uses 16-bit section index relocations. Once
    /// the number of sections exceeds 2^16 the same file format is still used.
    /// The linker treats the CodeView relocations symbolically.
    /// </remarks>
    internal partial class CoffObjectWriter : ObjectWriter
    {
        // Debugging
        private SectionWriter _debugTypesSectionWriter;
        private SectionWriter _debugSymbolSectionWriter;
        private CodeViewFileTableBuilder _debugFileTableBuilder;
        private CodeViewSymbolsBuilder _debugSymbolsBuilder;
        private CodeViewTypesBuilder _debugTypesBuilder;

        // Exception handling
        private SectionWriter _pdataSectionWriter;

        private protected override void CreateEhSections()
        {
            // Create .pdata
            _pdataSectionWriter = GetOrCreateSection(ObjectNodeSection.PDataSection);
        }

        private protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode)
            {
                SectionWriter xdataSectionWriter;
                SectionWriter pdataSectionWriter;
                bool shareSymbol = ShouldShareSymbol((ObjectNode)nodeWithCodeInfo);

                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    string unwindSymbolName = $"_unwind{i}{currentSymbolName}";

                    if (shareSymbol)
                    {
                        // Produce an associative COMDAT symbol.
                        xdataSectionWriter = GetOrCreateSection(ObjectNodeSection.XDataSection, currentSymbolName, unwindSymbolName);
                        pdataSectionWriter = GetOrCreateSection(ObjectNodeSection.PDataSection, currentSymbolName, null);
                    }
                    else
                    {
                        // Produce a COMDAT section for each unwind symbol and let linker
                        // do the deduplication across the ones with identical content.
                        xdataSectionWriter = GetOrCreateSection(ObjectNodeSection.XDataSection, unwindSymbolName, unwindSymbolName);
                        pdataSectionWriter = _pdataSectionWriter;
                    }

                    // Need to emit the UNWIND_INFO at 4-byte alignment to ensure that the
                    // pointer has the lower two bits in .pdata section set to zero. On ARM64
                    // non-zero bits would mean a compact encoding.
                    xdataSectionWriter.EmitAlignment(4);

                    xdataSectionWriter.EmitSymbolDefinition(unwindSymbolName);

                    // Emit UNWIND_INFO
                    xdataSectionWriter.Write(blob);

                    FrameInfoFlags flags = frameInfo.Flags;

                    if (i != 0)
                    {
                        xdataSectionWriter.WriteByte((byte)flags);
                    }
                    else
                    {
                        MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                        ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                        flags |= ehInfo is not null ? FrameInfoFlags.HasEHInfo : 0;
                        flags |= associatedDataNode is not null ? FrameInfoFlags.HasAssociatedData : 0;

                        xdataSectionWriter.WriteByte((byte)flags);

                        if (associatedDataNode is not null)
                        {
                            xdataSectionWriter.EmitSymbolReference(
                                IMAGE_REL_BASED_ADDR32NB,
                                GetMangledName(associatedDataNode));
                        }

                        if (ehInfo is not null)
                        {
                            xdataSectionWriter.EmitSymbolReference(
                                IMAGE_REL_BASED_ADDR32NB,
                                GetMangledName(ehInfo));
                        }

                        if (nodeWithCodeInfo.GCInfo is not null)
                        {
                            xdataSectionWriter.Write(nodeWithCodeInfo.GCInfo);
                        }
                    }

                    // Emit RUNTIME_FUNCTION
                    pdataSectionWriter.EmitAlignment(4);
                    pdataSectionWriter.EmitSymbolReference(IMAGE_REL_BASED_ADDR32NB, currentSymbolName, start);
                    // Only x86/x64 has the End symbol
                    if (_machine is Machine.I386 or Machine.Amd64)
                    {
                        pdataSectionWriter.EmitSymbolReference(IMAGE_REL_BASED_ADDR32NB, currentSymbolName, end);
                    }
                    // Unwind info pointer
                    pdataSectionWriter.EmitSymbolReference(IMAGE_REL_BASED_ADDR32NB, unwindSymbolName, 0);
                }
            }
        }

        private protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            _debugFileTableBuilder = new CodeViewFileTableBuilder();

            _debugSymbolSectionWriter = GetOrCreateSection(DebugSymbolSection);
            _debugSymbolSectionWriter.EmitAlignment(4);
            _debugSymbolsBuilder = new CodeViewSymbolsBuilder(
                _nodeFactory.Target.Architecture,
                _debugSymbolSectionWriter);

            _debugTypesSectionWriter = GetOrCreateSection(DebugTypesSection);
            _debugTypesSectionWriter.EmitAlignment(4);
            _debugTypesBuilder = new CodeViewTypesBuilder(
                _nodeFactory.NameMangler, _nodeFactory.Target.PointerSize,
                _debugTypesSectionWriter);
            return _debugTypesBuilder;
        }

        private protected override void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode,
            bool hasSequencePoints)
        {
            DebugEHClauseInfo[] clauses = null;
            CodeViewSymbolsBuilder debugSymbolsBuilder;

            if (debugNode is INodeWithCodeInfo nodeWithCodeInfo)
            {
                clauses = nodeWithCodeInfo.DebugEHClauseInfos;
            }

            if (ShouldShareSymbol((ObjectNode)debugNode))
            {
                // If the method is emitted in COMDAT section then we need to create an
                // associated COMDAT section for the debugging symbols.
                var sectionWriter = GetOrCreateSection(DebugSymbolSection, methodName, null);
                debugSymbolsBuilder = new CodeViewSymbolsBuilder(_nodeFactory.Target.Architecture, sectionWriter);
            }
            else
            {
                debugSymbolsBuilder = _debugSymbolsBuilder;
            }

            debugSymbolsBuilder.EmitSubprogramInfo(
                methodName,
                methodSymbol.Size,
                methodTypeIndex,
                debugNode.GetDebugVars().Select(debugVar => (debugVar, GetVarTypeIndex(debugNode.IsStateMachineMoveNextMethod, debugVar))),
                clauses ?? Array.Empty<DebugEHClauseInfo>());

            if (hasSequencePoints)
            {
                debugSymbolsBuilder.EmitLineInfo(
                    _debugFileTableBuilder,
                    methodName,
                    methodSymbol.Size,
                    debugNode.GetNativeSequencePoints());
            }
        }

        private protected override void EmitDebugThunkInfo(
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode)
        {
            if (!debugNode.GetNativeSequencePoints().Any())
                return;

            CodeViewSymbolsBuilder debugSymbolsBuilder;

            if (ShouldShareSymbol((ObjectNode)debugNode))
            {
                // If the method is emitted in COMDAT section then we need to create an
                // associated COMDAT section for the debugging symbols.
                var sectionWriter = GetOrCreateSection(DebugSymbolSection, methodName, null);
                debugSymbolsBuilder = new CodeViewSymbolsBuilder(_nodeFactory.Target.Architecture, sectionWriter);
            }
            else
            {
                debugSymbolsBuilder = _debugSymbolsBuilder;
            }

            debugSymbolsBuilder.EmitLineInfo(
                _debugFileTableBuilder,
                methodName,
                methodSymbol.Size,
                debugNode.GetNativeSequencePoints());
        }

        private protected override void EmitDebugSections(IDictionary<string, SymbolDefinition> definedSymbols)
        {
            _debugSymbolsBuilder.WriteUserDefinedTypes(_debugTypesBuilder.UserDefinedTypes);
            _debugFileTableBuilder.Write(_debugSymbolSectionWriter);
        }
    }
}
