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
                byte[] unwindInfo = GetSharedUnwindInfo(frameInfo.BlobData, out bool packed);
                _nodeFactory.CompactUnwindTable.AddEntry(section, codeSectionWriter.Position + frameInfo.StartOffset,
                    frameInfo.EndOffset - frameInfo.StartOffset, _lsdaOffsets[lsdaSymbolNames[i]], unwindInfo, packed);
            }
        }

        /// <summary>
        /// Returns the unwind information of a frame in the form the compact unwind table shares between frames: the
        /// packed arm64 unwind data when it describes the frame, and without the function length, which the table
        /// provides.
        /// </summary>
        private byte[] GetSharedUnwindInfo(byte[] blob, out bool packed)
        {
            packed = false;
            if (_nodeFactory.Target.Architecture == TargetArchitecture.X64)
                return blob;

            if (_nodeFactory.Target.Architecture == TargetArchitecture.ARM64 && UnwindInfoToCfi.TryPackArm64(blob, out uint word))
            {
                packed = true;
                return BitConverter.GetBytes(word & ~(0x7FFu << 2));
            }

            byte[] normalized = (byte[])blob.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(normalized, BinaryPrimitives.ReadUInt32LittleEndian(normalized) & ~0x3FFFFu);
            if (GetTableEpilogStarts(blob, out int scopesOffset) is ushort[] starts)
            {
                for (int i = 0; i < starts.Length; i++)
                {
                    Span<byte> scope = normalized.AsSpan(scopesOffset + 4 * i);
                    BinaryPrimitives.WriteUInt32LittleEndian(scope, BinaryPrimitives.ReadUInt32LittleEndian(scope) & ~0x3FFFFu);
                }
            }
            return normalized;
        }

        /// <summary>
        /// Returns the epilog start offsets that the compact unwind table moves from the arm64 style unwind
        /// information of a frame to right before its LSDA, so that frames with the same prolog and epilogs share
        /// the unwind information, or null when the unwind information keeps them.
        /// </summary>
        private ushort[] GetTableEpilogStarts(byte[] blob, out int scopesOffset)
        {
            scopesOffset = 4;
            TargetArchitecture arch = _nodeFactory.Target.Architecture;
            if (arch is not (TargetArchitecture.ARM64 or TargetArchitecture.LoongArch64 or TargetArchitecture.RiscV64))
                return null;
            if (arch == TargetArchitecture.ARM64 && UnwindInfoToCfi.TryPackArm64(blob, out _))
                return null;

            uint header = BinaryPrimitives.ReadUInt32LittleEndian(blob);
            int epilogScopes = (int)((header >> 22) & 0x1F);
            if ((header >> 27) == 0 && epilogScopes == 0)
            {
                epilogScopes = (int)(BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(4)) & 0xFFFF);
                scopesOffset = 8;
            }
            if ((header & (1u << 21)) != 0 || epilogScopes == 0)
                return null;

            ushort[] starts = new ushort[epilogScopes];
            for (int i = 0; i < epilogScopes; i++)
            {
                uint start = BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(scopesOffset + 4 * i)) & 0x3FFFF;
                if (start > ushort.MaxValue)
                {
                    // The runtime reads the start offsets before the LSDA when the first one is 0
                    if (i > 0 && starts[0] == 0)
                        throw new NotSupportedException("Epilog at the start of a large frame");
                    return null;
                }
                starts[i] = (ushort)start;
            }
            return starts;
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
            // Without the unwind information, the LSDAs of the frames only depend on the GC info, the flags, the
            // start offsets of the funclets and the epilog start offsets before them
            private sealed class LsdaComparer(bool unwindInfoInLsda, Func<byte[], ushort[]> getEpilogStarts) : IEqualityComparer<INodeWithCodeInfo>
            {
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
                    if (unwindInfoInLsda)
                        return xFrames.SequenceEqual(yFrames);

                    if (xFrames.Length != yFrames.Length)
                        return false;
                    for (int i = 0; i < xFrames.Length; i++)
                    {
                        if (xFrames[i].Flags != yFrames[i].Flags || xFrames[i].StartOffset - xFrames[0].StartOffset != yFrames[i].StartOffset - yFrames[0].StartOffset)
                            return false;
                        ushort[] xStarts = getEpilogStarts(xFrames[i].BlobData);
                        ushort[] yStarts = getEpilogStarts(yFrames[i].BlobData);
                        if ((xStarts is null) != (yStarts is null) || !xStarts.AsSpan().SequenceEqual(yStarts))
                            return false;
                    }
                    return true;
                }

                public int GetHashCode(INodeWithCodeInfo obj)
                {
                    Debug.Assert(IsCacheable(obj));
                    HashCode hash = default;
                    hash.AddBytes(obj.GCInfo);
                    foreach (FrameInfo f in obj.FrameInfos)
                    {
                        if (unwindInfoInLsda)
                            hash.Add(f.GetHashCode());
                        else
                            hash.Add(f.Flags);
                    }
                    return hash.ToHashCode();
                }
            }

            private readonly Dictionary<INodeWithCodeInfo, Utf8String[]> _lsdas;

            public LsdaCache(bool unwindInfoInLsda, Func<byte[], ushort[]> getEpilogStarts)
            {
                _lsdas = new Dictionary<INodeWithCodeInfo, Utf8String[]>(new LsdaComparer(unwindInfoInLsda, getEpilogStarts));
            }

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

        private LsdaCache _lsdaCache;

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
                    _lsdaCache ??= new LsdaCache(unwindInfoInLsda: _nodeFactory.CompactUnwindTable is null, blob => GetTableEpilogStarts(blob, out _));
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

                        // The compact unwind table refers to the unwind information and the LSDA separately. Otherwise
                        // the blob of a frame is its unwind information followed by its LSDA, like .xdata on Windows,
                        // and the unwinders tell the packed form of the arm style unwind information from a
                        // reference to the full one by the low bits of its address.
                        bool unwindInfoInLsda = _nodeFactory.CompactUnwindTable is null;
                        if (unwindInfoInLsda && _nodeFactory.Target.Architecture != TargetArchitecture.X64)
                            lsdaSectionWriter.EmitAlignment(4);
                        if (!unwindInfoInLsda && GetTableEpilogStarts(blob, out _) is ushort[] epilogStarts)
                        {
                            foreach (ushort epilogStart in epilogStarts)
                                lsdaSectionWriter.WriteLittleEndian<ushort>(epilogStart);
                        }
                        lsdaSectionWriter.EmitSymbolDefinition(lsdaSymbolName);
                        if (_lsdaOffsets is not null && lsdaSectionWriter.SectionIndex == _lsdaSectionWriter.SectionIndex)
                            _lsdaOffsets.Add(lsdaSymbolName, _lsdaSectionWriter.Position);

                        if (unwindInfoInLsda)
                            lsdaSectionWriter.Write(blob);
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
                    byte[] cfi = _dwarfEhFrame is not null || _dwarfDebugFrame is not null ? UnwindInfoToCfi.Convert(_nodeFactory.Target.Architecture, _nodeFactory.Target.OperatingSystem, blob) : null;
                    if (_dwarfEhFrame is not null ? !EmitCompactUnwinding(startSymbolName, length, cfi) : _dwarfDebugFrame is not null)
                    {
                        (_dwarfEhFrame ?? _dwarfDebugFrame).AddFde(new DwarfFde(
                            _dwarfCie,
                            cfi,
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
