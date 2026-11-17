// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Buffers.Binary;
using ILCompiler.DependencyAnalysis;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.TypesDebugInfo;
using static ILCompiler.ObjectWriter.DwarfNative;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// Base implementation for ELF and Mach-O object file format writers. Implements
    /// the common code for DWARF debugging and exception handling information.
    /// </summary>
    internal abstract partial class UnixObjectWriter : ObjectWriter
    {
        // Exception handling sections
        private SectionWriter _lsdaSectionWriter;
        private int _ehFrameSectionIndex;
        private DwarfCie _dwarfCie;
        private DwarfEhFrame _dwarfEhFrame;

        // FDEs for frames described by the compact unwind table, for debuggers and other tools that need CFI
        private DwarfEhFrame _dwarfDebugFrame;

        protected int EhFrameSectionIndex => _ehFrameSectionIndex;

        private protected DwarfCie DwarfCie => _dwarfCie;

        // Offsets of the LSDAs emitted into _lsdaSectionWriter, used by compact unwind tables
        private Dictionary<Utf8String, long> _lsdaOffsets;

        // Managed code sections, which there can be several of, numbered in the order they are created
        private Dictionary<int, int> _compactUnwindCodeSections;

        // Debugging
        private DwarfBuilder _dwarfBuilder;

        private static readonly ObjectNodeSection DebugInfoSection = new ObjectNodeSection(".debug_info", SectionType.Debug);
        private static readonly ObjectNodeSection DebugStringSection = new ObjectNodeSection(".debug_str", SectionType.Debug);
        private static readonly ObjectNodeSection DebugAbbrevSection = new ObjectNodeSection(".debug_abbrev", SectionType.Debug);
        private static readonly ObjectNodeSection DebugLocSection = new ObjectNodeSection(".debug_loc", SectionType.Debug);
        private static readonly ObjectNodeSection DebugLocListsSection = new ObjectNodeSection(".debug_loclists", SectionType.Debug);
        private static readonly ObjectNodeSection DebugRangesSection = new ObjectNodeSection(".debug_ranges", SectionType.Debug);
        private static readonly ObjectNodeSection DebugRngListsSection = new ObjectNodeSection(".debug_rnglists", SectionType.Debug);
        private static readonly ObjectNodeSection DebugLineSection = new ObjectNodeSection(".debug_line", SectionType.Debug);
        private static readonly ObjectNodeSection DebugARangesSection = new ObjectNodeSection(".debug_aranges", SectionType.Debug);
        private static readonly ObjectNodeSection DebugAddrSection = new ObjectNodeSection(".debug_addr", SectionType.Debug);
        private static readonly ObjectNodeSection DebugFrameSection = new ObjectNodeSection(".debug_frame", SectionType.Debug);

        private protected virtual bool UseFrameNames => false;

        private protected virtual bool EmitCompactUnwinding(Utf8String startSymbolName, ulong length, byte[] blob) => false;

        /// <summary>
        /// Whether frames also get the platform unwind information, without an LSDA, because the platform tools
        /// only read it from the image. Otherwise their FDEs go to .debug_frame, which is stripped into the
        /// symbol file. The runtime finds managed frames in the <see cref="CompactUnwindTableNode"/> or the
        /// <see cref="CompactUnwindRecordsNode"/> records either way.
        /// </summary>
        private protected virtual bool KeepsPlatformUnwindInfo => false;

        /// <summary>
        /// Whether the blocks of the compact unwind table refer to the code through the symbol of the node
        /// that contains it, instead of through a symbol at the start of the code section. Linkers that only
        /// keep small addends inline (ld-prime) need this.
        /// </summary>
        private protected virtual bool CompactUnwindTableUsesNodeSymbols => false;

        /// <summary>
        /// Emits the compact unwind records of the frames of a node of a multi-file compilation.
        /// </summary>
        private protected virtual void AddCompactUnwindRecords(SectionWriter codeSectionWriter, INodeWithCodeInfo nodeWithCodeInfo, Utf8String currentSymbolName, bool shareSymbol, Utf8String[] lsdaSymbolNames)
            => throw new NotSupportedException();

        private protected override void CreateEhSections()
        {
            // Create sections for exception handling
            _lsdaSectionWriter = GetOrCreateSection(LsdaSection);
            _lsdaSectionWriter.EmitAlignment(8);

            bool is64Bit = _nodeFactory.Target.Architecture switch
            {
                TargetArchitecture.X86 => false,
                TargetArchitecture.ARM => false,
                _ => true
            };
            _dwarfCie = new DwarfCie(_nodeFactory.Target.Architecture);

            if (_nodeFactory.CompactUnwindTable is CompactUnwindTableNode table)
            {
                _lsdaOffsets = new Dictionary<Utf8String, long>();
                _compactUnwindCodeSections = new Dictionary<int, int>();
                _lsdaSectionWriter.EmitSymbolDefinition(ExternCName(table.GetLsdaBaseSymbolName(_nodeFactory.NameMangler)), -_lsdaSectionWriter.Position);
            }
            else if (_nodeFactory.CompactUnwindRecords is null)
            {
                throw new InvalidOperationException("Managed frames must be described by compact unwind information.");
            }

            if (KeepsPlatformUnwindInfo)
            {
                SectionWriter ehFrameSectionWriter = GetOrCreateSection(EhFrameSection);
                ehFrameSectionWriter.EmitAlignment(8);
                _ehFrameSectionIndex = ehFrameSectionWriter.SectionIndex;

                // We always use the same CIE in DWARF EH frames, so emit it now
                _dwarfEhFrame = new DwarfEhFrame(ehFrameSectionWriter, is64Bit);
                _dwarfEhFrame.AddCie(_dwarfCie);
            }
            else if (_options.HasFlag(ObjectWritingOptions.GenerateDebugInfo))
            {
                SectionWriter debugFrameSectionWriter = GetOrCreateSection(DebugFrameSection);
                _dwarfDebugFrame = new DwarfEhFrame(debugFrameSectionWriter, is64Bit, new Utf8String(DebugFrameSection.Name));
                _dwarfDebugFrame.AddCie(_dwarfCie);
            }
        }

        private void AddCompactUnwindTableEntries(SectionWriter codeSectionWriter, FrameInfo[] frameInfos, Utf8String[] lsdaSymbolNames)
        {
            if (!_compactUnwindCodeSections.TryGetValue(codeSectionWriter.SectionIndex, out int section))
                throw new InvalidOperationException("Managed code outside of the managed code sections.");

            for (int i = 0; i < frameInfos.Length; i++)
            {
                FrameInfo frameInfo = frameInfos[i];
                byte[] descriptor = GetCompactUnwindDescriptor(_dwarfCie, _nodeFactory.Target.PointerSize, frameInfo.BlobData);
                _nodeFactory.CompactUnwindTable.AddEntry(section, codeSectionWriter.Position + frameInfo.StartOffset,
                    frameInfo.EndOffset - frameInfo.StartOffset, _lsdaOffsets[lsdaSymbolNames[i]], descriptor);
            }
        }

        private protected override void RecordCodeRange(ObjectNode node, SectionWriter sectionWriter, int length)
        {
            if (_compactUnwindCodeSections is null || length == 0 || node.GetSection(_nodeFactory) != ObjectNodeSection.ManagedCodeUnixContentSection)
                return;

            CompactUnwindTableNode table = _nodeFactory.CompactUnwindTable;
            if (!_compactUnwindCodeSections.TryGetValue(sectionWriter.SectionIndex, out int section))
            {
                section = _compactUnwindCodeSections.Count;
                _compactUnwindCodeSections.Add(sectionWriter.SectionIndex, section);
                if (!CompactUnwindTableUsesNodeSymbols)
                    sectionWriter.EmitSymbolDefinition(table.GetCodeBaseSymbolName(_nodeFactory.NameMangler, section), -sectionWriter.Position);
            }

            ISymbolDefinitionNode symbol = null;
            if (CompactUnwindTableUsesNodeSymbols)
                symbol = node as ISymbolDefinitionNode ?? throw new InvalidOperationException("Managed code without a symbol.");

            table.AddCodeRange(section, sectionWriter.Position, length, symbol);
        }

        /// <summary>
        /// Computes the unwind rules that apply after the prolog the same way <see cref="DwarfFde"/> encodes them.
        /// The JIT only describes the prolog, so these rules hold for the rest of the frame.
        /// </summary>
        private protected static byte[] GetCompactUnwindDescriptor(DwarfCie cie, int pointerSize, byte[] blob)
        {
            // Initial rules from the CIE: DW_CFA_def_cfa followed by optional DW_CFA_offset
            int cfaRegister = -1;
            int cfaOffset = 0;
            var saves = new SortedDictionary<int, int>();
            byte[] cieInstructions = cie.Instructions;
            for (int i = 0; i < cieInstructions.Length;)
            {
                // The operands of the CIE instructions are single byte ULEB128 values
                byte op = cieInstructions[i++];
                if (op == DW_CFA_def_cfa && cieInstructions[i] < 0x80 && cieInstructions[i + 1] < 0x80)
                {
                    cfaRegister = cieInstructions[i++];
                    cfaOffset = cieInstructions[i++];
                }
                else if ((op & 0xC0) == DW_CFA_offset && cieInstructions[i] < 0x80)
                {
                    saves[op & 0x3F] = cieInstructions[i++] * cie.DataAlignFactor;
                }
                else
                {
                    throw new InvalidOperationException("Unexpected CIE instruction.");
                }
            }

            Debug.Assert(cfaOffset == cie.InitialCFAOffset);

            int prologSize = 0;
            bool returnAddressSigned = false;
            int offset = 0;
            while (blob is not null && offset < blob.Length)
            {
                int codeOffset = blob[offset++];
                CFI_OPCODE opcode = (CFI_OPCODE)blob[offset++];
                short dwarfReg = BinaryPrimitives.ReadInt16LittleEndian(blob.AsSpan(offset));
                offset += sizeof(short);
                int cfiOffset = BinaryPrimitives.ReadInt32LittleEndian(blob.AsSpan(offset));
                offset += sizeof(int);

                prologSize = Math.Max(prologSize, codeOffset);

                switch (opcode)
                {
                    case CFI_OPCODE.CFI_ADJUST_CFA_OFFSET:
                        cfaOffset += cfiOffset;
                        break;

                    case CFI_OPCODE.CFI_DEF_CFA_REGISTER:
                        cfaRegister = dwarfReg;
                        break;

                    case CFI_OPCODE.CFI_DEF_CFA:
                        cfaRegister = dwarfReg;
                        cfaOffset = cfiOffset;
                        break;

                    case CFI_OPCODE.CFI_REL_OFFSET:
                        saves[dwarfReg] = cfiOffset - cfaOffset;
                        break;

                    case CFI_OPCODE.CFI_NEGATE_RA_STATE:
                        // arm64 pac-ret: the return address is signed with the A key and the SP at function entry,
                        // which is the CFA
                        returnAddressSigned = !returnAddressSigned;
                        break;

                    default:
                        throw new InvalidOperationException($"Unexpected CFI opcode {opcode}.");
                }
            }

            var slots = new List<(int Register, int Slot)>(saves.Count);
            foreach ((int register, int offsetFromCfa) in saves)
            {
                if (offsetFromCfa > 0 || offsetFromCfa % pointerSize != 0)
                    throw new InvalidOperationException("Register saved above the CFA or at an unaligned offset.");

                slots.Add((register, -offsetFromCfa / pointerSize));
            }

            byte flags = returnAddressSigned ? CompactUnwindTableNode.FlagReturnAddressSigned : (byte)0;
            return CompactUnwindTableNode.EncodeDescriptor(cfaRegister, cfaOffset, prologSize, flags, slots, cie.ReturnAddressRegister);
        }

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
                    Utf8String symbolName = GetMangledName(associatedDataNode);
                    lsdaSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_RELPTR32, symbolName, 0);
                }

                if (ehInfo is not null)
                {
                    Utf8String symbolName = GetMangledName(ehInfo);
                    lsdaSectionWriter.EmitSymbolReference(RelocType.IMAGE_REL_BASED_RELPTR32, symbolName, 0);
                }

                if (nodeWithCodeInfo.GCInfo is not null)
                {
                    lsdaSectionWriter.Write(nodeWithCodeInfo.GCInfo);
                }
            }
        }

        private sealed class LsdaCache
        {
            private sealed class LsdaComparer : IEqualityComparer<INodeWithCodeInfo>
            {
                public static readonly LsdaComparer Instance = new LsdaComparer();

                public bool Equals(INodeWithCodeInfo x, INodeWithCodeInfo y)
                {
                    Debug.Assert(IsCacheable(x));
                    Debug.Assert(IsCacheable(y));
                    ReadOnlySpan<byte> xGc = x.GCInfo;
                    ReadOnlySpan<byte> yGc = y.GCInfo;
                    if (!xGc.SequenceEqual(yGc))
                        return false;

                    ReadOnlySpan<FrameInfo> xFrames = x.FrameInfos;
                    ReadOnlySpan<FrameInfo> yFrames = y.FrameInfos;
                    return xFrames.SequenceEqual(yFrames);
                }

                public int GetHashCode(INodeWithCodeInfo obj)
                {
                    Debug.Assert(IsCacheable(obj));
                    HashCode hash = default;
                    hash.AddBytes(obj.GCInfo);
                    foreach (FrameInfo f in obj.FrameInfos)
                        hash.Add(f.GetHashCode());
                    return hash.ToHashCode();
                }
            }

            private Dictionary<INodeWithCodeInfo, Utf8String[]> _lsdas = new Dictionary<INodeWithCodeInfo, Utf8String[]>(LsdaComparer.Instance);

            public static bool IsCacheable(INodeWithCodeInfo nodeWithCodeInfo)
                => nodeWithCodeInfo.EHInfo == null && !MethodAssociatedDataNode.MethodHasAssociatedData((IMethodNode)nodeWithCodeInfo);

            public Utf8String[] FindCachedLsda(INodeWithCodeInfo nodeWithCodeInfo)
            {
                Debug.Assert(IsCacheable(nodeWithCodeInfo));
                return _lsdas.GetValueOrDefault(nodeWithCodeInfo);
            }

            public void AddLsdaToCache(INodeWithCodeInfo nodeWithCodeInfo, Utf8String[] symbols)
            {
                Debug.Assert(IsCacheable(nodeWithCodeInfo));
                _lsdas.Add(nodeWithCodeInfo, symbols);
            }
        }

        private readonly LsdaCache _lsdaCache = new LsdaCache();

        private protected override void EmitUnwindInfo(
            SectionWriter sectionWriter,
            INodeWithCodeInfo nodeWithCodeInfo,
            Utf8String currentSymbolName)
        {
            if (nodeWithCodeInfo.FrameInfos is FrameInfo[] frameInfos &&
                nodeWithCodeInfo is ISymbolDefinitionNode)
            {
                bool useFrameNames = UseFrameNames;
                SectionWriter lsdaSectionWriter;

                Utf8String[] newLsdaSymbols = null;
                Utf8String[] emittedLsdaSymbols = null;
                Utf8String[] lsdaSymbolNames = new Utf8String[frameInfos.Length];
                bool shareSymbol = ShouldShareSymbol((ObjectNode)nodeWithCodeInfo);
                if (shareSymbol)
                {
                    lsdaSectionWriter = GetOrCreateSection(LsdaSection, currentSymbolName, Utf8String.Concat("_lsda0"u8, currentSymbolName.AsSpan()));
                }
                else
                {
                    lsdaSectionWriter = _lsdaSectionWriter;
                    if (LsdaCache.IsCacheable(nodeWithCodeInfo) && !useFrameNames)
                    {
                        emittedLsdaSymbols = _lsdaCache.FindCachedLsda(nodeWithCodeInfo);
                        if (emittedLsdaSymbols == null)
                            newLsdaSymbols = new Utf8String[frameInfos.Length];
                    }
                }

                long mainLsdaOffset = 0;
                Span<byte> i_str = stackalloc byte[16];
                for (int i = 0; i < frameInfos.Length; i++)
                {
                    FrameInfo frameInfo = frameInfos[i];

                    int start = frameInfo.StartOffset;
                    int end = frameInfo.EndOffset;
                    byte[] blob = frameInfo.BlobData;

                    Utf8String lsdaSymbolName;
                    if (emittedLsdaSymbols != null)
                    {
                        lsdaSymbolName = emittedLsdaSymbols[i];
                    }
                    else
                    {
                        lsdaSymbolName = _utf8StringBuilder.Clear().Append("_lsda"u8).Append(FormatUtf8Int(i_str, i)).Append(currentSymbolName).ToUtf8String();
                        if (newLsdaSymbols != null)
                            newLsdaSymbols[i] = lsdaSymbolName;
                        lsdaSectionWriter.EmitSymbolDefinition(lsdaSymbolName);
                        if (_lsdaOffsets is not null && lsdaSectionWriter.SectionIndex == _lsdaSectionWriter.SectionIndex)
                            _lsdaOffsets.Add(lsdaSymbolName, _lsdaSectionWriter.Position);
                        EmitLsda(nodeWithCodeInfo, frameInfos, i, lsdaSectionWriter, ref mainLsdaOffset);
                    }
                    lsdaSymbolNames[i] = lsdaSymbolName;

                    Utf8String framSymbolName = _utf8StringBuilder.Clear().Append("_fram"u8).Append(FormatUtf8Int(i_str, i)).Append(currentSymbolName).ToUtf8String();
                    if (useFrameNames && start != 0)
                    {
                        sectionWriter.EmitSymbolDefinition(framSymbolName, start);
                    }

                    // The platform unwind information and .debug_frame are only for tools, which don't need the LSDA
                    Utf8String startSymbolName = useFrameNames && start != 0 ? framSymbolName : currentSymbolName;
                    ulong length = (ulong)(end - start);
                    if (_dwarfEhFrame is not null ? !EmitCompactUnwinding(startSymbolName, length, blob) : _dwarfDebugFrame is not null)
                    {
                        (_dwarfEhFrame ?? _dwarfDebugFrame).AddFde(new DwarfFde(
                            _dwarfCie,
                            blob,
                            pcStartSymbolName: startSymbolName,
                            pcStartSymbolOffset: useFrameNames ? 0 : start,
                            pcLength: length,
                            lsdaSymbolName: default,
                            personalitySymbolName: default));
                    }
                }

                if (newLsdaSymbols != null)
                    _lsdaCache.AddLsdaToCache(nodeWithCodeInfo, newLsdaSymbols);

                if (_nodeFactory.CompactUnwindTable is not null)
                    AddCompactUnwindTableEntries(sectionWriter, frameInfos, lsdaSymbolNames);
                else
                    AddCompactUnwindRecords(sectionWriter, nodeWithCodeInfo, currentSymbolName, shareSymbol, lsdaSymbolNames);
            }
        }

        private protected override void EmitDebugFunctionInfo(
            uint methodTypeIndex,
            Utf8String methodDisplayName,
            Utf8String methodName,
            SymbolDefinition methodSymbol,
            INodeWithDebugInfo debugNode)
        {
            DebugEHClauseInfo[] clauses = null;
            FrameInfo[] frameInfos = null;

            if (debugNode is INodeWithCodeInfo nodeWithCodeInfo)
            {
                clauses = nodeWithCodeInfo.DebugEHClauseInfos;
                frameInfos = nodeWithCodeInfo.FrameInfos;
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
                    clauses ?? [],
                    frameInfos ?? []);

                if (debugNode.GetNativeSequencePoints().Any())
                {
                    _dwarfBuilder.EmitLineInfo(
                        methodSymbol.SectionIndex,
                        section.SymbolName,
                        methodSymbol.Value,
                        debugNode.GetNativeSequencePoints());
                }
            }
        }

        private protected override void EmitDebugSections(IDictionary<Utf8String, SymbolDefinition> definedSymbols)
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
            SectionWriter locSectionWriter = GetOrCreateSection(_dwarfBuilder.UseDwarf5 ? DebugLocListsSection : DebugLocSection);
            SectionWriter rangeSectionWriter = GetOrCreateSection(_dwarfBuilder.UseDwarf5 ? DebugRngListsSection : DebugRangesSection);
            SectionWriter lineSectionWriter = GetOrCreateSection(DebugLineSection);
            SectionWriter arangeSectionWriter = GetOrCreateSection(DebugARangesSection);

            if (_dwarfBuilder.UseDwarf5)
            {
                _dwarfBuilder.WriteAddressTable(GetOrCreateSection(DebugAddrSection));
            }

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
                    return (default, 0);
                });
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
