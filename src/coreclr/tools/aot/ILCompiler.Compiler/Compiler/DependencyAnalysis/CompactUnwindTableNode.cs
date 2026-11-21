// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;

using ILCompiler.DependencyAnalysisFramework;

using Internal.Text;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Lookup table from managed code ranges to the Windows unwind information and the NativeAOT LSDA (unwind block
    /// flags, associated data, EH info and GC info) of the frames of a single-file compilation, or of a module of a
    /// Mach-O multi-file compilation, used instead of DWARF FDEs for managed frames. The table keeps each distinct
    /// unwind information once and the frames refer to it and to their LSDA separately. The object writer fills the
    /// table while it emits the code. ELF multi-file compilations use <see cref="CompactUnwindRecordsNode"/> instead.
    /// </summary>
    /// <remarks>
    /// Layout (all fields little endian, kept in sync with UnixNativeCodeManager):
    ///   int32  relative pointer to the start of the LSDA section
    ///   uint32 number of entries
    ///   uint32 number of blocks
    ///   uint32 number of bits of the entries
    ///   uint8  log2 of the largest number of entries in a block, chosen to make the table smallest
    ///   uint8  flags: 1 when the entries have a packed bit, 2 when the unwind information offsets are 32-bit
    ///   uint8  log2 of the unit of the unwind information offsets
    ///   uint8  reserved
    ///   uint32 number of unwind informations
    ///   blocks sorted by address, unaligned: int32 relative pointer to the code of the first entry, uint32 smallest
    ///     LSDA offset, uint32 bit offset of the first entry, uint16 number of bits of the code, LSDA and unwind
    ///     fields of the entries of the block minus one (5 bits each)
    ///   bit stream of the entries sorted by address, least significant bit first, followed by 8 zero bytes.
    ///     The entries of a block have the same size: the distance from the code of the first entry, the LSDA field,
    ///     which is 0 for code without unwind information and otherwise one plus the distance from the smallest LSDA
    ///     offset, and the unwind field, which is the index of the unwind information, shifted left by one and or'ed
    ///     with the packed bit when the entries have one.
    ///   padding to 4 bytes, then the offset of each unwind information from the first one, in units, uint16 or
    ///     uint32, most used first, padded to 4 bytes
    ///   unwind informations, each aligned to the unit. The function length of the arm style unwind information
    ///     is 0, the table provides it. When the start offset of its first epilog scope is 0 too, the start offsets
    ///     of the epilog scopes of each frame are uint16 values right before the LSDA of the frame, so that frames
    ///     with the same prolog and epilogs share the unwind information. With the packed bit, the unwind
    ///     information is packed arm64 unwind data (the Flag = 1 form of the arm64 .pdata) without the function
    ///     length.
    /// Managed code can be split into several sections with linker generated code between them. An entry covers
    /// the code up to the start of the next entry, and a block never spans two sections. Code without unwind
    /// information is covered by entries without an LSDA, and such an entry ends each section.
    /// </remarks>
    public sealed class CompactUnwindTableNode : ObjectNode, ISymbolDefinitionNode
    {
        private const int HeaderSize = 24;
        private const int BlockHeaderSize = 14;
        private const int MinBlockShift = 3;
        private const int MaxBlockShift = 10;

        private readonly List<(int Section, uint CodeOffset, uint LsdaOffset, uint Length, int Unwind)> _frames = new();
        private readonly List<(int Section, uint Start, uint Length, ISymbolDefinitionNode Symbol)> _codeRanges = new();
        private readonly Dictionary<string, int> _unwindIndices = new();
        private readonly List<(byte[] Data, bool Packed)> _unwindInfos = new();
        private bool _dataEmitted;

        /// <summary>
        /// Name of the symbol at the start of the managed code section with the given number, in the order the
        /// object writer creates them.
        /// </summary>
        public Utf8String GetCodeBaseSymbolName(NameMangler nameMangler, int section)
            => Utf8String.Concat(nameMangler.CompilationUnitPrefix.AsSpan(), "__compact_unwind_code_base_"u8, new Utf8String(section.ToString()).AsSpan());

        public Utf8String GetLsdaBaseSymbolName(NameMangler nameMangler)
            => Utf8String.Concat(nameMangler.CompilationUnitPrefix.AsSpan(), "__compact_unwind_lsda_base"u8);

        public void AddEntry(int section, long codeOffset, long length, long lsdaOffset, byte[] unwindInfo, bool packed)
        {
            if (_dataEmitted)
                throw new InvalidOperationException("Compact unwind entries must be added before the table is emitted.");

            string key = (packed ? "P" : "U") + Convert.ToHexString(unwindInfo);
            if (!_unwindIndices.TryGetValue(key, out int index))
            {
                index = _unwindInfos.Count;
                _unwindInfos.Add((unwindInfo, packed));
                _unwindIndices.Add(key, index);
            }

            _frames.Add((section, checked((uint)codeOffset), checked((uint)lsdaOffset), checked((uint)length), index));
        }

        /// <summary>
        /// Records code emitted into a managed code section, with or without frames. When the symbol of the
        /// node is given, the blocks in the range refer to the code through it instead of through the symbol at
        /// the start of the section.
        /// </summary>
        public void AddCodeRange(int section, long start, int length, ISymbolDefinitionNode symbol = null)
        {
            if (_dataEmitted)
                throw new InvalidOperationException("Compact unwind code ranges must be added before the table is emitted.");

            _codeRanges.Add((section, checked((uint)start), (uint)length, symbol));
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__compact_unwind_table"u8);
        }

        public int Offset => 0;
        public override bool IsShareable => false;
        public override bool StaticDependenciesAreComputed => true;
        protected internal override int Phase => (int)ObjectNodePhase.Late;
        public override int ClassCode => 0x2f6c1d43;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.ReadOnlyDataSection;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialAlignment(4);
            builder.AddSymbol(this);

            builder.EmitReloc(factory.ExternDataSymbol(GetLsdaBaseSymbolName(factory.NameMangler)), RelocType.IMAGE_REL_BASED_RELPTR32);

            if (relocsOnly)
                return builder.ToObjectData();

            _dataEmitted = true;
            List<Entry> entries = BuildEntries();

            // Number the unwind informations by use, so that the indices in most blocks are small
            int[] uses = new int[_unwindInfos.Count];
            foreach (Entry entry in entries)
            {
                if (entry.HasBlob)
                    uses[entry.Unwind]++;
            }
            int[] order = new int[_unwindInfos.Count];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            Array.Sort(order, (x, y) => uses[x] != uses[y] ? uses[y].CompareTo(uses[x]) : x.CompareTo(y));
            int[] rank = new int[_unwindInfos.Count];
            for (int i = 0; i < order.Length; i++)
                rank[order[i]] = i;

            int packedBits = _unwindInfos.Exists(u => u.Packed) ? 1 : 0;
            uint UnwindField(Entry entry) => entry.HasBlob ? ((uint)rank[entry.Unwind] << packedBits) | (_unwindInfos[entry.Unwind].Packed ? 1u : 0u) : 0;

            // Pick the block size that makes the table smallest
            int blockShift = MinBlockShift;
            long smallest = long.MaxValue;
            for (int shift = MinBlockShift; shift <= MaxBlockShift; shift++)
            {
                long size = 0;
                foreach (Block block in GetBlocks(entries, shift, UnwindField))
                    size += BlockHeaderSize * 8 + (long)block.Count * block.EntryBits;

                if (size < smallest)
                {
                    smallest = size;
                    blockShift = shift;
                }
            }

            var blocks = new List<Block>();
            var stream = new BitWriter();
            foreach (Block block in GetBlocks(entries, blockShift, UnwindField))
            {
                Block written = block;
                written.BitOffset = checked((uint)stream.BitCount);
                blocks.Add(written);

                for (int i = block.Start; i < block.Start + block.Count; i++)
                {
                    Entry entry = entries[i];
                    stream.WriteBits(entry.CodeOffset - block.CodeBase, block.CodeBits);
                    stream.WriteBits(entry.HasBlob ? entry.LsdaOffset - block.LsdaBase + 1 : 0, block.LsdaBits);
                    stream.WriteBits(UnwindField(entry), block.UnwindBits);
                }
            }

            // The unwind informations are multiples of a unit, which their offsets don't encode
            int unitShift = 2;
            while (unitShift > 0 && !_unwindInfos.TrueForAll(u => (u.Data.Length & ((1 << unitShift) - 1)) == 0))
                unitShift--;
            var offsets = new uint[_unwindInfos.Count];
            uint poolSize = 0;
            foreach (int index in order)
            {
                offsets[rank[index]] = poolSize >> unitShift;
                poolSize += (uint)_unwindInfos[index].Data.Length;
            }
            bool wideOffsets = poolSize >> unitShift > ushort.MaxValue;

            builder.EmitUInt((uint)entries.Count);
            builder.EmitUInt((uint)blocks.Count);
            builder.EmitUInt(checked((uint)stream.BitCount));
            builder.EmitByte((byte)blockShift);
            builder.EmitByte((byte)(packedBits | (wideOffsets ? 2 : 0)));
            builder.EmitByte((byte)unitShift);
            builder.EmitByte(0);
            builder.EmitUInt((uint)_unwindInfos.Count);
            Debug.Assert(builder.CountBytes == HeaderSize);


            var codeBases = new List<CodeBaseSymbol>();
            int codeRange = 0;
            foreach (Block block in blocks)
            {
                // The last code range of the section that starts at or before the block, which contains the block
                // or ends where it starts
                while (codeRange + 1 < _codeRanges.Count && (_codeRanges[codeRange + 1].Section < block.Section
                    || (_codeRanges[codeRange + 1].Section == block.Section && _codeRanges[codeRange + 1].Start <= block.CodeBase)))
                    codeRange++;

                if (_codeRanges[codeRange].Symbol is ISymbolDefinitionNode symbol)
                {
                    Debug.Assert(_codeRanges[codeRange].Section == block.Section && _codeRanges[codeRange].Start <= block.CodeBase);
                    long delta = (long)block.CodeBase - _codeRanges[codeRange].Start - symbol.Offset - ((ISymbolNode)symbol).Offset;
                    builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_RELPTR32, checked((int)delta));
                }
                else
                {
                    while (codeBases.Count <= block.Section)
                        codeBases.Add(new CodeBaseSymbol(GetCodeBaseSymbolName(factory.NameMangler, codeBases.Count)));

                    builder.EmitReloc(codeBases[block.Section], RelocType.IMAGE_REL_BASED_RELPTR32, checked((int)block.CodeBase));
                }
                builder.EmitUInt(block.LsdaBase);
                builder.EmitUInt(block.BitOffset);
                builder.EmitShort((short)((block.CodeBits - 1) | ((block.LsdaBits - 1) << 5) | ((block.UnwindBits - 1) << 10)));
            }

            builder.EmitBytes(stream.ToArray());
            builder.EmitZeros(8);
            builder.EmitZeros((4 - builder.CountBytes % 4) % 4);

            foreach (uint offset in offsets)
            {
                if (wideOffsets)
                    builder.EmitUInt(offset);
                else
                    builder.EmitShort((short)offset);
            }
            builder.EmitZeros((4 - builder.CountBytes % 4) % 4);
            foreach (int index in order)
                builder.EmitBytes(_unwindInfos[index].Data);

            return builder.ToObjectData();
        }

        private record struct Entry(int Section, uint CodeOffset, uint LsdaOffset, bool HasBlob, int Unwind = 0);

        private struct Block
        {
            public int Section;
            public int Start;
            public int Count;
            public uint CodeBase;
            public uint LsdaBase;
            public uint BitOffset;
            public int CodeBits;
            public int LsdaBits;
            public int UnwindBits;
            public readonly int EntryBits => CodeBits + LsdaBits + UnwindBits;
        }

        /// <summary>
        /// Splits the entries into blocks of at most 1 &lt;&lt; shift entries that don't span sections.
        /// </summary>
        private static IEnumerable<Block> GetBlocks(List<Entry> entries, int shift, Func<Entry, uint> unwindField)
        {
            int start = 0;
            while (start < entries.Count)
            {
                int section = entries[start].Section;
                int end = start + 1;
                while (end < entries.Count && end - start < 1 << shift && entries[end].Section == section)
                    end++;

                uint lsdaBase = uint.MaxValue, maxLsda = 0, maxUnwind = 0;
                for (int i = start; i < end; i++)
                {
                    maxUnwind = Math.Max(maxUnwind, unwindField(entries[i]));
                    if (!entries[i].HasBlob)
                        continue;

                    lsdaBase = Math.Min(lsdaBase, entries[i].LsdaOffset);
                    maxLsda = Math.Max(maxLsda, entries[i].LsdaOffset);
                }

                if (lsdaBase == uint.MaxValue)
                    lsdaBase = maxLsda = 0;

                yield return new Block
                {
                    Section = section,
                    Start = start,
                    Count = end - start,
                    CodeBase = entries[start].CodeOffset,
                    LsdaBase = lsdaBase,
                    CodeBits = BitsToEncode(entries[end - 1].CodeOffset - entries[start].CodeOffset),
                    LsdaBits = BitsToEncode(maxLsda - lsdaBase + 1),
                    UnwindBits = BitsToEncode(maxUnwind),
                };

                start = end;
            }
        }

        /// <summary>
        /// Sorts the frames by section and offset, and covers the recorded code that isn't in any frame and the end
        /// of each section with entries without a blob, so that each frame ends where the next entry starts.
        /// </summary>
        private List<Entry> BuildEntries()
        {
            _frames.Sort((x, y) => x.Section != y.Section ? x.Section.CompareTo(y.Section) : x.CodeOffset.CompareTo(y.CodeOffset));
            _codeRanges.Sort((x, y) => x.Section != y.Section ? x.Section.CompareTo(y.Section) : x.Start.CompareTo(y.Start));

            var holes = new List<(int Section, uint Offset)>();
            var sectionEnds = new Dictionary<int, uint>();
            int frame = 0;
            foreach ((int section, uint start, uint length, _) in _codeRanges)
            {
                uint end = start + length;
                sectionEnds[section] = Math.Max(sectionEnds.GetValueOrDefault(section), end);

                uint position = start;
                while (position < end)
                {
                    while (frame < _frames.Count && (_frames[frame].Section < section
                        || (_frames[frame].Section == section && _frames[frame].CodeOffset + _frames[frame].Length <= position)))
                        frame++;

                    bool inSection = frame < _frames.Count && _frames[frame].Section == section;
                    if (inSection && _frames[frame].CodeOffset <= position)
                    {
                        position = _frames[frame].CodeOffset + _frames[frame].Length;
                        continue;
                    }

                    holes.Add((section, position));
                    position = inSection ? Math.Min(_frames[frame].CodeOffset, end) : end;
                }
            }

            foreach (var f in _frames)
                sectionEnds[f.Section] = Math.Max(sectionEnds.GetValueOrDefault(f.Section), f.CodeOffset + f.Length);
            foreach ((int section, uint end) in sectionEnds)
                holes.Add((section, end));

            holes.Sort((x, y) => x.Section != y.Section ? x.Section.CompareTo(y.Section) : x.Offset.CompareTo(y.Offset));

            var entries = new List<Entry>(_frames.Count + holes.Count);
            int hole = 0;
            foreach ((int section, uint codeOffset, uint lsdaOffset, _, int unwind) in _frames)
            {
                for (; hole < holes.Count && (holes[hole].Section < section || (holes[hole].Section == section && holes[hole].Offset < codeOffset)); hole++)
                    AddHole(holes[hole]);

                Debug.Assert(entries.Count == 0 || entries[^1].Section != section || entries[^1].CodeOffset < codeOffset);
                entries.Add(new Entry(section, codeOffset, lsdaOffset, true, unwind));
            }

            for (; hole < holes.Count; hole++)
                AddHole(holes[hole]);

            return entries;

            void AddHole((int Section, uint Offset) h)
            {
                if (entries.Count == 0 || entries[^1].HasBlob || entries[^1].Section != h.Section)
                    entries.Add(new Entry(h.Section, h.Offset, 0, false));
            }
        }

        private static int BitsToEncode(uint maxValue) => Math.Max(1, 32 - BitOperations.LeadingZeroCount(maxValue));

        /// <summary>
        /// Symbol the object writer defines at the start of a managed code section. The sections are only known
        /// once the code is emitted, after dependency analysis, so the symbol is not part of the dependency graph.
        /// </summary>
        private sealed class CodeBaseSymbol(Utf8String name) : ISymbolNode
        {
            public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb) => sb.Append(name);
            public int Offset => 0;
            public bool RepresentsIndirectionCell => false;
            public bool Marked => true;
            public bool InterestingForDynamicDependencyAnalysis => false;
            public bool HasDynamicDependencies => false;
            public bool HasConditionalStaticDependencies => false;
            public bool StaticDependenciesAreComputed => true;
            public void AddStaticDependencies(DependencyNodeCore<NodeFactory>.DependencySink sink, NodeFactory context) { }
            public void AddConditionalDependencies(DependencyNodeCore<NodeFactory>.DependencySink sink, NodeFactory context) { }
            public void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencyNodeCore<NodeFactory>.DependencySink sink, NodeFactory context) { }
            public string GetName(NodeFactory context) => name.ToString();
        }

        private sealed class BitWriter
        {
            private readonly List<byte> _bytes = new();

            public long BitCount { get; private set; }

            public void WriteBits(ulong value, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    if ((BitCount & 7) == 0)
                        _bytes.Add(0);
                    if (((value >> i) & 1) != 0)
                        _bytes[_bytes.Count - 1] |= (byte)(1 << (int)(BitCount & 7));
                    BitCount++;
                }
            }

            public byte[] ToArray() => _bytes.ToArray();
        }
    }
}
