// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Buffers.Binary;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Base implementation for ELF and Mach-O object file format writers. Implements
    /// the common code for DWARF debugging and exception handling information.
    /// </summary>
    internal abstract class UnixObjectWriter : ObjectWriter
    {
        private sealed record UnixSectionDefinition(string SymbolName, Stream SectionStream);

        // Debugging
        private DwarfBuilder _dwarfBuilder;
        private readonly List<UnixSectionDefinition> _sections = new();

        // Exception handling sections
        private SectionWriter _lsdaSectionWriter;
        private int _ehFrameSectionIndex;
        private DwarfCie _dwarfCie;
        private DwarfEhFrame _dwarfEhFrame;

        protected int EhFrameSectionIndex => _ehFrameSectionIndex;

        private static readonly ObjectNodeSection LsdaSection = new ObjectNodeSection(".dotnet_eh_table", SectionType.ReadOnly);
        private static readonly ObjectNodeSection EhFrameSection = new ObjectNodeSection(".eh_frame", SectionType.UnwindData);
        private static readonly ObjectNodeSection DebugInfoSection = new ObjectNodeSection(".debug_info", SectionType.Debug);
        private static readonly ObjectNodeSection DebugStringSection = new ObjectNodeSection(".debug_str", SectionType.Debug);
        private static readonly ObjectNodeSection DebugAbbrevSection = new ObjectNodeSection(".debug_abbrev", SectionType.Debug);
        private static readonly ObjectNodeSection DebugLocSection = new ObjectNodeSection(".debug_loc", SectionType.Debug);
        private static readonly ObjectNodeSection DebugRangesSection = new ObjectNodeSection(".debug_ranges", SectionType.Debug);
        private static readonly ObjectNodeSection DebugLineSection = new ObjectNodeSection(".debug_line", SectionType.Debug);
        private static readonly ObjectNodeSection DebugARangesSection = new ObjectNodeSection(".debug_aranges", SectionType.Debug);

        protected UnixObjectWriter(NodeFactory factory, ObjectWritingOptions options)
            : base(factory, options)
        {
        }

        private protected override void CreateSection(ObjectNodeSection section, string comdatName, string symbolName, Stream sectionStream)
        {
            if (section.Type != SectionType.Debug &&
                section != LsdaSection &&
                section != EhFrameSection &&
                (comdatName is null || Equals(comdatName, symbolName)))
            {
                // Record code and data sections that can be referenced from debugging information
                _sections.Add(new UnixSectionDefinition(symbolName, sectionStream));
            }
            else
            {
                _sections.Add(null);
            }
        }

        private protected virtual bool EmitCompactUnwinding(string startSymbolName, ulong length, string lsdaSymbolName, byte[] blob) => false;

        private protected virtual bool UseFrameNames => false;

        private protected void EmitLsda(
            INodeWithCodeInfo nodeWithCodeInfo,
            FrameInfo[] frameInfos,
            int frameInfoIndex,
            SectionWriter lsdaSectionWriter,
            ref long mainLsdaOffset)
        {
            FrameInfo frameInfo = frameInfos[frameInfoIndex];
            FrameInfoFlags flags = frameInfo.Flags;

            if (frameInfoIndex != 0)
            {
                lsdaSectionWriter.WriteByte((byte)flags);
                lsdaSectionWriter.WriteLittleEndian<int>((int)(mainLsdaOffset - lsdaSectionWriter.Position));
                // Emit relative offset from the main function
                lsdaSectionWriter.WriteLittleEndian<uint>((uint)(frameInfo.StartOffset - frameInfos[0].StartOffset));
            }
            else
            {
                MethodExceptionHandlingInfoNode ehInfo = nodeWithCodeInfo.EHInfo;
                ISymbolNode associatedDataNode = nodeWithCodeInfo.GetAssociatedDataNode(_nodeFactory) as ISymbolNode;

                flags |= ehInfo is not null ? FrameInfoFlags.HasEHInfo : 0;
                flags |= associatedDataNode is not null ? FrameInfoFlags.HasAssociatedData : 0;

                mainLsdaOffset = lsdaSectionWriter.Position;
                lsdaSectionWriter.WriteByte((byte)flags);

                if (associatedDataNode is not null)
                {
                    string symbolName = GetMangledName(associatedDataNode);
                    lsdaSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_RELPTR32, symbolName, 0);
                }

                if (ehInfo is not null)
                {
                    string symbolName = GetMangledName(ehInfo);
                    lsdaSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_RELPTR32, symbolName, 0);
                }

                if (nodeWithCodeInfo.GCInfo is not null)
                {
                    lsdaSectionWriter.Write(nodeWithCodeInfo.GCInfo);
                }
            }
        }

        private protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            string currentSymbolName)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode)
            {
                bool useFrameNames = UseFrameNames;
                SectionWriter lsdaSectionWriter;

                if (ShouldShareSymbol((ObjectNode)nodeWithCodeInfo))
                {
                    lsdaSectionWriter = GetOrCreateSection(LsdaSection, currentSymbolName, $"_lsda0{currentSymbolName}");
                }
                else
                {
                    lsdaSectionWriter = _lsdaSectionWriter;
                }

                long mainLsdaOffset = 0;
                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    string lsdaSymbolName = $"_lsda{i}{currentSymbolName}";
                    string framSymbolName = $"_fram{i}{currentSymbolName}";

                    lsdaSectionWriter.EmitSymbolDefinition(lsdaSymbolName);
                    if (useFrameNames && start != 0)
                    {
                        sectionWriter.EmitSymbolDefinition(framSymbolName, start);
                    }

                    string startSymbolName = useFrameNames && start != 0 ? framSymbolName : currentSymbolName;
                    ulong length = (ulong)(end - start);
                    if (!EmitCompactUnwinding(startSymbolName, length, lsdaSymbolName, blob))
                    {
                        var fde = new DwarfFde(
                            _dwarfCie,
                            blob,
                            pcStartSymbolName: startSymbolName,
                            pcStartSymbolOffset: useFrameNames ? 0 : start,
                            pcLength: (ulong)(end - start),
                            lsdaSymbolName,
                            personalitySymbolName: null);
                        _dwarfEhFrame.AddFde(fde);
                    }

                    EmitLsda(nodeWithCodeInfo, frameInfos, i, _lsdaSectionWriter, ref mainLsdaOffset);
                }
            }
        }

        private protected override void EmitDebugFunctionInfo(
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

            if (_sections[methodSymbol.SectionIndex] is UnixSectionDefinition section)
            {
                _dwarfBuilder.EmitSubprogramInfo(
                    methodName,
                    section.SymbolName,
                    methodSymbol.Value,
                    methodSymbol.Size,
                    methodTypeIndex,
                    debugNode.GetDebugVars().Select(debugVar => (debugVar, GetVarTypeIndex(debugNode.IsStateMachineMoveNextMethod, debugVar))),
                    clauses ?? []);

                if (hasSequencePoints)
                {
                    _dwarfBuilder.EmitLineInfo(
                        methodSymbol.SectionIndex,
                        section.SymbolName,
                        methodSymbol.Value,
                        debugNode.GetNativeSequencePoints());
                }
            }
        }

        private protected override void EmitDebugSections(IDictionary<string, SymbolDefinition> definedSymbols)
        {
            foreach (UnixSectionDefinition section in _sections)
            {
                if (section is not null)
                {
                    _dwarfBuilder.EmitSectionInfo(section.SymbolName, (ulong)section.SectionStream.Length);
                }
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
                arangeSectionWriter,
                symbolName =>
                {
                    if (definedSymbols.TryGetValue(ExternCName(symbolName), out SymbolDefinition symbolDef) &&
                        _sections[symbolDef.SectionIndex] is UnixSectionDefinition section)
                    {
                        return (section.SymbolName, symbolDef.Value);
                    }
                    return (null, 0);
                });
        }

        private protected override void CreateEhSections()
        {
            SectionWriter ehFrameSectionWriter;

            // Create sections for exception handling
            _lsdaSectionWriter = GetOrCreateSection(LsdaSection);
            ehFrameSectionWriter = GetOrCreateSection(EhFrameSection);
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

        private protected override ITypesDebugInfoWriter CreateDebugInfoBuilder()
        {
            return _dwarfBuilder = new DwarfBuilder(
                _nodeFactory.NameMangler,
                _nodeFactory.Target,
                _options.HasFlag(ObjectWritingOptions.UseDwarf5));
        }
    }
}
