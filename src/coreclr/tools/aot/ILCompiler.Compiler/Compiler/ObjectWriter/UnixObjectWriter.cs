// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Numerics;
using System.Buffers;
using System.Buffers.Binary;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using Internal.JitInterface;
using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

using LibObjectFile;
using LibObjectFile.Dwarf;
using static ILCompiler.ObjectWriter.DwarfNative;

namespace ILCompiler.ObjectWriter
{
    public abstract class UnixObjectWriter : ObjectWriter
    {
        // Debugging
        private DwarfBuilder _dwarfBuilder;

        // Exception handling sections
        private int _lsdaSectionIndex;
        private int _ehFrameSectionIndex;
        private Stream _lsdaStream;
        private List<SymbolicRelocation> _lsdaRelocations;
        private DwarfCie _dwarfCie;
        private DwarfEhFrame _dwarfEhFrame;

        protected int EhFrameSectionIndex => _ehFrameSectionIndex;

        protected UnixObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
        }

        protected virtual bool EmitCompactUnwinding(DwarfFde fde) => false;

        protected override void EmitUnwindInfo(int sectionIndex, long methodStart, INodeWithCodeInfo nodeWithCodeInfo)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode symbolDefinitionNode)
            {
                Span<byte> tempBuffer = stackalloc byte[4];
                string currentSymbolName = ExternCName(symbolDefinitionNode.GetMangledName(_nodeFactory.NameMangler));
                long mainLsdaOffset = _lsdaStream.Position;

                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    int len = frameInfo.BlobData.Length;
                    byte[] blob = frameInfo.BlobData;

                    string lsdaSymbolName = $"_lsda{i}{currentSymbolName}";
                    string framSymbolName = $"_fram{i}{currentSymbolName}";

                    EmitSymbolDefinition(lsdaSymbolName, new SymbolDefinition(_lsdaSectionIndex, _lsdaStream.Position));
                    if (start != 0)
                    {
                        EmitSymbolDefinition(framSymbolName, new SymbolDefinition(sectionIndex, methodStart + start, 0));
                    }

                    FrameInfoFlags flags = frameInfo.Flags;

                    if (i != 0)
                    {
                        _lsdaStream.WriteByte((byte)flags);

                        BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, (uint)(mainLsdaOffset - _lsdaStream.Position));
                        _lsdaStream.Write(tempBuffer);

                        // Emit relative offset from the main function
                        BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, (uint)(start - frameInfos[0].StartOffset));
                        _lsdaStream.Write(tempBuffer);
                    }
                    else
                    {
                        MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                        ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                        flags |= ehInfo != null ? FrameInfoFlags.HasEHInfo : 0;
                        flags |= associatedDataNode != null ? FrameInfoFlags.HasAssociatedData : 0;

                        _lsdaStream.WriteByte((byte)flags);

                        if (associatedDataNode != null)
                        {
                            string symbolName = ExternCName(associatedDataNode.GetMangledName(_nodeFactory.NameMangler));
                            tempBuffer.Clear();
                            EmitRelocation(
                                _lsdaSectionIndex,
                                _lsdaRelocations,
                                (int)_lsdaStream.Position,
                                tempBuffer,
                                RelocType.IMAGE_REL_BASED_RELPTR32,
                                symbolName,
                                0);
                            _lsdaStream.Write(tempBuffer);
                        }

                        if (ehInfo != null)
                        {
                            string symbolName = ExternCName(ehInfo.GetMangledName(_nodeFactory.NameMangler));
                            tempBuffer.Clear();
                            EmitRelocation(
                                _lsdaSectionIndex,
                                _lsdaRelocations,
                                (int)_lsdaStream.Position,
                                tempBuffer,
                                RelocType.IMAGE_REL_BASED_RELPTR32,
                                symbolName,
                                0);
                            _lsdaStream.Write(tempBuffer);
                        }

                        if (nodeWithCodeInfo.GCInfo != null)
                        {
                            _lsdaStream.Write(nodeWithCodeInfo.GCInfo);
                        }
                    }

                    var fde = new DwarfFde(_dwarfCie, DwarfFde.CfiCodeToInstructions(_dwarfCie, frameInfo.BlobData))
                    {
                        PcStartSymbolName = start != 0 ? framSymbolName : currentSymbolName,
                        PcLength = (ulong)(end - start),
                        LsdaSymbolName = lsdaSymbolName,
                    };

                    _dwarfEhFrame.AddFde(fde);

                    EmitCompactUnwinding(fde);
                }
            }
        }

        protected override void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode)
        {
            var lowPC = GetSectionVirtualAddress(methodSymbol.SectionIndex) + (ulong)methodSymbol.Value;
            DebugEHClauseInfo[] clauses = null;

            if (debugNode is INodeWithCodeInfo nodeWithCodeInfo)
            {
                clauses = nodeWithCodeInfo.DebugEHClauseInfos;
            }

            _dwarfBuilder.EmitSubprogramInfo(
                methodName,
                lowPC,
                methodSymbol.Size,
                methodTypeIndex,
                debugNode.GetDebugVars().Select(debugVar => (debugVar, GetVarTypeIndex(debugNode.IsStateMachineMoveNextMethod, debugVar))),
                clauses ?? Array.Empty<DebugEHClauseInfo>());

            _dwarfBuilder.EmitLineInfo(methodSymbol.SectionIndex, lowPC, debugNode.GetNativeSequencePoints());
        }

        protected abstract void EmitDebugSections(DwarfFile dwarfFile);

        protected override void EmitDebugSections()
        {
            EmitDebugSections(_dwarfBuilder.DwarfFile);
        }

        protected override void CreateEhSections()
        {
            // Create sections for exception handling
            _lsdaSectionIndex = GetOrCreateSection(
                new ObjectNodeSection(".dotnet_eh_table", SectionType.ReadOnly, null),
                out _lsdaStream, out _lsdaRelocations);
            _ehFrameSectionIndex = GetOrCreateSection(
                new ObjectNodeSection(".eh_frame", SectionType.ReadOnly, null),
                out var ehFrameStream, out var ehFrameRelocations);
            UpdateSectionAlignment(_lsdaSectionIndex, 8, out _);
            UpdateSectionAlignment(_ehFrameSectionIndex, 8, out _);

            // We always use the same CIE in DWARF EH frames, so create and emit it now
            bool is64Bit = _nodeFactory.Target.Architecture switch
            {
                TargetArchitecture.X86 => false,
                TargetArchitecture.ARM => false,
                _ => true
            };
            _dwarfCie = new DwarfCie(_nodeFactory.Target.Architecture);
            _dwarfEhFrame = new DwarfEhFrame(
                ehFrameStream,
                (offset, data, relocType, symbolName) => EmitRelocation(
                    _ehFrameSectionIndex, ehFrameRelocations,
                    offset, data, relocType, symbolName, 0),
                is64Bit);
            _dwarfEhFrame.AddCie(_dwarfCie);
        }

        protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            return _dwarfBuilder = new DwarfBuilder(_nodeFactory.NameMangler, _nodeFactory.Target.Architecture);
        }

        protected override void EmitDebugStaticVars()
        {
            var definedSymbols = GetDefinedSymbols();
            _dwarfBuilder.EmitStaticVars(
                symbolName =>
                {
                    if (definedSymbols.TryGetValue(ExternCName(symbolName), out var symbolDef))
                        return GetSectionVirtualAddress(symbolDef.SectionIndex) + (ulong)symbolDef.Value;
                    return 0;
                }
            );
        }
    }
}
