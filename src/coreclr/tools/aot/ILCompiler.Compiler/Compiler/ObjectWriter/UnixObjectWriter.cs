// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Buffers.Binary;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;

namespace ILCompiler.ObjectWriter
{
    public abstract class UnixObjectWriter : ObjectWriter
    {
        // Debugging
        private DwarfBuilder _dwarfBuilder;

        // Exception handling sections
        private SectionWriter _lsdaSectionWriter;
        private int _ehFrameSectionIndex;
        private DwarfCie _dwarfCie;
        private DwarfEhFrame _dwarfEhFrame;

        protected int EhFrameSectionIndex => _ehFrameSectionIndex;

        private static ObjectNodeSection LsdaSection = new ObjectNodeSection(".dotnet_eh_table", SectionType.ReadOnly, null);

        protected UnixObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
        }

        protected virtual bool EmitCompactUnwinding(string startSymbolName, ulong length, string lsdaSymbolName, byte[] blob) => false;

        protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode symbolDefinitionNode)
            {
                SectionWriter lsdaSectionWriter;
                Span<byte> tempBuffer = stackalloc byte[4];

                if (ShouldShareSymbol((ObjectNode)nodeWithCodeInfo))
                {
                    lsdaSectionWriter = GetOrCreateSection(GetSharedSection(LsdaSection, currentSymbolName));
                }
                else
                {
                    lsdaSectionWriter = _lsdaSectionWriter;
                }

                long mainLsdaOffset = lsdaSectionWriter.Stream.Position;
                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    int len = frameInfo.BlobData.Length;
                    byte[] blob = frameInfo.BlobData;

                    string lsdaSymbolName = $"_lsda{i}{currentSymbolName}";
                    string framSymbolName = $"_fram{i}{currentSymbolName}";

                    lsdaSectionWriter.EmitSymbolDefinition(lsdaSymbolName);
                    if (start != 0)
                    {
                        sectionWriter.EmitSymbolDefinition(framSymbolName, start);
                    }

                    FrameInfoFlags flags = frameInfo.Flags;

                    if (i != 0)
                    {
                        lsdaSectionWriter.Stream.WriteByte((byte)flags);

                        BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, (uint)(mainLsdaOffset - lsdaSectionWriter.Stream.Position));
                        lsdaSectionWriter.Stream.Write(tempBuffer);

                        // Emit relative offset from the main function
                        BinaryPrimitives.WriteUInt32LittleEndian(tempBuffer, (uint)(start - frameInfos[0].StartOffset));
                        lsdaSectionWriter.Stream.Write(tempBuffer);
                    }
                    else
                    {
                        MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                        ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                        flags |= ehInfo != null ? FrameInfoFlags.HasEHInfo : 0;
                        flags |= associatedDataNode != null ? FrameInfoFlags.HasAssociatedData : 0;

                        lsdaSectionWriter.Stream.WriteByte((byte)flags);

                        if (associatedDataNode != null)
                        {
                            string symbolName = GetMangledName(associatedDataNode);
                            lsdaSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_RELPTR32, symbolName, 0);
                        }

                        if (ehInfo != null)
                        {
                            string symbolName = GetMangledName(ehInfo);
                            lsdaSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_RELPTR32, symbolName, 0);
                        }

                        if (nodeWithCodeInfo.GCInfo != null)
                        {
                            lsdaSectionWriter.Stream.Write(nodeWithCodeInfo.GCInfo);
                        }
                    }

                    string startSymbolName = start != 0 ? framSymbolName : currentSymbolName;
                    ulong length = (ulong)(end - start);
                    if (!EmitCompactUnwinding(startSymbolName, length, lsdaSymbolName, blob))
                    {
                        var fde = new DwarfFde(_dwarfCie, DwarfFde.CfiCodeToInstructions(_dwarfCie, blob))
                        {
                            PcStartSymbolName = startSymbolName,
                            PcLength = (ulong)(end - start),
                            LsdaSymbolName = lsdaSymbolName,
                        };

                        _dwarfEhFrame.AddFde(fde);
                    }
                }
            }
        }

        protected override void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            string methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode,
            bool hasSequencePoints)
        {
            DebugEHClauseInfo[] clauses = null;

            if (debugNode is INodeWithCodeInfo nodeWithCodeInfo)
            {
                clauses = nodeWithCodeInfo.DebugEHClauseInfos;
            }

            _dwarfBuilder.EmitSubprogramInfo(
                methodName,
                methodSymbol.Size,
                methodTypeIndex,
                debugNode.GetDebugVars().Select(debugVar => (debugVar, GetVarTypeIndex(debugNode.IsStateMachineMoveNextMethod, debugVar))),
                clauses ?? []);

            if (hasSequencePoints)
            {
                _dwarfBuilder.EmitLineInfo(
                    methodSymbol.SectionIndex,
                    GetSectionSymbolName(methodSymbol.SectionIndex),
                    (ulong)methodSymbol.Value,
                    debugNode.GetNativeSequencePoints());
            }
        }

        //protected abstract void EmitDebugSections(DwarfFile dwarfFile);

        private readonly ObjectNodeSection DebugInfoSection = new ObjectNodeSection(".debug_info", SectionType.Debug);
        private readonly ObjectNodeSection DebugStringSection = new ObjectNodeSection(".debug_str", SectionType.Debug);
        private readonly ObjectNodeSection DebugAbbrevSection = new ObjectNodeSection(".debug_abbrev", SectionType.Debug);
        private readonly ObjectNodeSection DebugLocSection = new ObjectNodeSection(".debug_loc", SectionType.Debug);
        private readonly ObjectNodeSection DebugRangesSection = new ObjectNodeSection(".debug_ranges", SectionType.Debug);
        private readonly ObjectNodeSection DebugLineSection = new ObjectNodeSection(".debug_line", SectionType.Debug);
        private readonly ObjectNodeSection DebugARangesSection = new ObjectNodeSection(".debug_aranges", SectionType.Debug);

        protected override void EmitDebugSections()
        {
            for (int i = 0; i < _sectionIndexToStream.Count; i++)
            {
                _dwarfBuilder.EmitSectionInfo(GetSectionSymbolName(i), (ulong)_sectionIndexToStream[i].Length);
            }

            SectionWriter infoSectionWriter = GetOrCreateSection(DebugInfoSection);
            SectionWriter stringSectionWriter = GetOrCreateSection(DebugStringSection);
            SectionWriter abbrevSectionWriter = GetOrCreateSection(DebugAbbrevSection);
            SectionWriter locSectionWriter = GetOrCreateSection(DebugLocSection);
            SectionWriter rangeSectionWriter = GetOrCreateSection(DebugRangesSection);
            SectionWriter lineSectionWriter = GetOrCreateSection(DebugLineSection);
            SectionWriter arangeSectionWriter = GetOrCreateSection(DebugARangesSection);

            _dwarfBuilder.Write(
                infoSectionWriter,
                stringSectionWriter,
                abbrevSectionWriter,
                locSectionWriter,
                rangeSectionWriter,
                lineSectionWriter,
                arangeSectionWriter);
        }

        protected override void CreateEhSections()
        {
            SectionWriter ehFrameSectionWriter;

            // Create sections for exception handling
            _lsdaSectionWriter = GetOrCreateSection(LsdaSection);
            ehFrameSectionWriter = GetOrCreateSection(new ObjectNodeSection(".eh_frame", SectionType.ReadOnly, null));
            _lsdaSectionWriter.EmitAlignment(8);
            ehFrameSectionWriter.EmitAlignment(8);
            _ehFrameSectionIndex = ehFrameSectionWriter.SectionIndex;

            // We always use the same CIE in DWARF EH frames, so create and emit it now
            bool is64Bit = _nodeFactory.Target.Architecture switch
            {
                TargetArchitecture.X86 => false,
                TargetArchitecture.ARM => false,
                _ => true
            };
            _dwarfCie = new DwarfCie(_nodeFactory.Target.Architecture);
            _dwarfEhFrame = new DwarfEhFrame(ehFrameSectionWriter, is64Bit);
            _dwarfEhFrame.AddCie(_dwarfCie);
        }

        protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            return _dwarfBuilder = new DwarfBuilder(
                _nodeFactory.NameMangler,
                _nodeFactory.Target.Architecture,
                _options.HasFlag(ObjectWritingOptions.UseDwarf5));
        }
    }
}
