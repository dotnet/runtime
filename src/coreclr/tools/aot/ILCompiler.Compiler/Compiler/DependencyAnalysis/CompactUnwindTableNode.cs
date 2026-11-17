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
    /// Lookup table from managed code ranges to compact unwind descriptors and LSDAs of a single-file compilation,
    /// or of a module of a Mach-O multi-file compilation, used instead of DWARF FDEs for managed frames, whose
    /// unwind information doesn't change after the prolog. The object writer fills the table while it emits the
    /// code. ELF multi-file compilations use <see cref="CompactUnwindRecordsNode"/> instead.
    /// </summary>
    /// <remarks>
    /// Layout (all fields little endian, kept in sync with UnixNativeCodeManager and UnwindHelpers):
    ///   int32  relative pointer to the start of the LSDA section
    ///   uint32 number of entries
    ///   uint32 number of descriptors
    ///   uint32 number of blocks
    ///   uint32 number of bits of the entries
    ///   uint8  log2 of the largest number of entries in a block, chosen to make the table smallest
    ///   uint8[3] reserved
    ///   uint32 offset of each descriptor from the start of the table
    ///   descriptors, 4 byte aligned, most used first:
    ///     uint8 DWARF number of the CFA register, uint8 number of saved registers, uint8 flags, uint8 reserved,
    ///     uint32 prolog size, int32 CFA offset, then for each saved register:
    ///     uint16 DWARF register number, uint16 slot (the register is saved at CFA - slot * pointer size),
    ///     or with the wide slots flag: uint16 DWARF register number, uint16 reserved, uint32 slot.
    ///     With the register run flag the number of saved registers is 0 and the registers are described instead by
    ///     uint8 slot of the return address register (0 when the return address stays in its register),
    ///     uint8 DWARF number of one more saved register (0xFF for none) and uint8 its slot, uint8 first slot of the run,
    ///     uint32 mask of the DWARF registers 0-31 of the run. The registers of the run are saved at consecutive slots
    ///     from the first slot, in ascending register order, or with the descending run flag in descending order.
    ///     Most frames save the frame pointer and the return address next to each other and the other callee-saved
    ///     registers next to them in the order of their numbers, so the runtime can restore them without reading
    ///     a record per register.
    ///   blocks sorted by address, unaligned: int32 relative pointer to the code of the first entry, uint32 smallest
    ///     LSDA offset, uint32 bit offset of the first entry, uint16 number of bits of the code, LSDA and descriptor
    ///     index fields of the entries of the block minus one (5 bits each)
    ///   bit stream of the entries sorted by address, least significant bit first, followed by 8 zero bytes.
    ///     The entries of a block have the same size: the distance from the code of the first entry, the
    ///     distance from the smallest LSDA offset and the descriptor index.
    /// Managed code can be split into several sections with linker generated code between them. An entry covers
    /// the code up to the start of the next entry, and a block never spans two sections. Code without unwind
    /// information is covered by entries whose descriptor index is the number of descriptors, and such an
    /// entry ends each section.
    /// </remarks>
    public sealed class CompactUnwindTableNode : ObjectNode, ISymbolDefinitionNode
    {
        /// <summary>The return address was signed with the A key and the CFA as modifier (arm64 pac-ret).</summary>
        public const byte FlagReturnAddressSigned = 0x01;

        /// <summary>The saved registers are 8 bytes with a 32 bit slot, used when a slot doesn't fit 16 bits.</summary>
        private const byte FlagWideSlots = 0x02;

        /// <summary>The saved registers are described by a register run instead of a record per register.</summary>
        private const byte FlagRegisterRun = 0x04;

        /// <summary>The registers of the run are saved in descending register order.</summary>
        private const byte FlagRunDescending = 0x08;

        private const byte NoRegister = 0xFF;

        private const int HeaderSize = 24;
        private const int DescriptorHeaderSize = 12;
        private const int RegisterRunSize = 8;
        private const int BlockHeaderSize = 14;
        private const int MinBlockShift = 3;
        private const int MaxBlockShift = 10;

        private readonly List<(int Section, uint CodeOffset, uint LsdaOffset, uint Length, int Descriptor)> _frames = new();
        private readonly List<(int Section, uint Start, uint Length, ISymbolDefinitionNode Symbol)> _codeRanges = new();
        private readonly Dictionary<string, int> _descriptorIndices = new();
        private readonly List<byte[]> _descriptors = new();
        private bool _dataEmitted;

        /// <summary>
        /// Encodes the unwind state that applies after the prolog.
        /// </summary>
        /// <param name="saves">Saved registers and their slots, ordered by register number.</param>
        /// <param name="returnAddressRegister">DWARF number of the register that holds the return address.</param>
        public static byte[] EncodeDescriptor(int cfaRegister, int cfaOffset, int prologSize, byte flags, IReadOnlyList<(int Register, int Slot)> saves, int returnAddressRegister)
        {
            if (cfaRegister is < 0 or > byte.MaxValue || saves.Count > byte.MaxValue || prologSize < 0)
                throw new InvalidOperationException("Frame can't be described by a compact unwind descriptor.");

            if (TryEncodeRegisterRun(cfaRegister, cfaOffset, prologSize, flags, saves, returnAddressRegister) is byte[] run)
                return run;

            bool wide = false;
            foreach ((int register, int slot) in saves)
            {
                if (register is < 0 or > ushort.MaxValue || slot < 0)
                    throw new InvalidOperationException("Frame can't be described by a compact unwind descriptor.");
                wide |= slot > ushort.MaxValue;
            }

            int saveSize = wide ? 8 : 4;
            byte[] descriptor = new byte[DescriptorHeaderSize + saveSize * saves.Count];
            descriptor[0] = (byte)cfaRegister;
            descriptor[1] = (byte)saves.Count;
            descriptor[2] = (byte)(flags | (wide ? FlagWideSlots : 0));
            BitConverter.TryWriteBytes(descriptor.AsSpan(4), (uint)prologSize);
            BitConverter.TryWriteBytes(descriptor.AsSpan(8), cfaOffset);

            for (int i = 0; i < saves.Count; i++)
            {
                (int register, int slot) = saves[i];
                int offset = DescriptorHeaderSize + saveSize * i;
                BitConverter.TryWriteBytes(descriptor.AsSpan(offset), (ushort)register);
                if (wide)
                    BitConverter.TryWriteBytes(descriptor.AsSpan(offset + 4), (uint)slot);
                else
                    BitConverter.TryWriteBytes(descriptor.AsSpan(offset + 2), (ushort)slot);
            }

            return descriptor;
        }

        /// <summary>
        /// Encodes the saved registers as the return address slot, at most one more register and a run of registers
        /// saved at consecutive slots in the order of their numbers, or returns null when they don't have that shape.
        /// </summary>
        private static byte[] TryEncodeRegisterRun(int cfaRegister, int cfaOffset, int prologSize, byte flags, IReadOnlyList<(int Register, int Slot)> saves, int returnAddressRegister)
        {
            int returnAddressSlot = 0;
            var others = new List<(int Register, int Slot)>(saves.Count);
            foreach ((int register, int slot) in saves)
            {
                if (register == returnAddressRegister)
                    returnAddressSlot = slot;
                else
                    others.Add((register, slot));
            }

            if ((returnAddressSlot == 0 && others.Count != saves.Count) || returnAddressSlot > byte.MaxValue)
                return null;

            // Try the shape without the extra register first, then with each register as the extra one from the
            // highest number, which is the frame pointer in the usual shape
            for (int attempt = -1; attempt < others.Count; attempt++)
            {
                int extra = attempt < 0 ? -1 : others.Count - 1 - attempt;
                if (extra >= 0 && (others[extra].Register >= NoRegister || others[extra].Slot is < 1 or > byte.MaxValue))
                    continue;

                var run = new List<(int Register, int Slot)>(others.Count);
                for (int i = 0; i < others.Count; i++)
                {
                    if (i != extra)
                        run.Add(others[i]);
                }

                if (run.Exists(r => r.Register is < 0 or >= 32))
                    continue;

                bool ascending = true, descending = true;
                for (int i = 0; i < run.Count; i++)
                {
                    ascending &= run[i].Slot == run[0].Slot + i;
                    descending &= run[i].Slot == run[^1].Slot + (run.Count - 1 - i);
                }

                int firstSlot = run.Count == 0 ? 0 : ascending ? run[0].Slot : run[^1].Slot;
                if ((!ascending && !descending) || (run.Count != 0 && (firstSlot < 1 || firstSlot + run.Count - 1 > byte.MaxValue)))
                    continue;

                uint mask = 0;
                foreach ((int register, _) in run)
                    mask |= 1u << register;

                byte[] descriptor = new byte[DescriptorHeaderSize + RegisterRunSize];
                descriptor[0] = (byte)cfaRegister;
                descriptor[2] = (byte)(flags | FlagRegisterRun | (ascending ? 0 : FlagRunDescending));
                BitConverter.TryWriteBytes(descriptor.AsSpan(4), (uint)prologSize);
                BitConverter.TryWriteBytes(descriptor.AsSpan(8), cfaOffset);
                descriptor[DescriptorHeaderSize] = (byte)returnAddressSlot;
                descriptor[DescriptorHeaderSize + 1] = extra >= 0 ? (byte)others[extra].Register : NoRegister;
                descriptor[DescriptorHeaderSize + 2] = extra >= 0 ? (byte)others[extra].Slot : (byte)0;
                descriptor[DescriptorHeaderSize + 3] = (byte)firstSlot;
                BitConverter.TryWriteBytes(descriptor.AsSpan(DescriptorHeaderSize + 4), mask);
                return descriptor;
            }

            return null;
        }

        /// <summary>
        /// Name of the symbol at the start of the managed code section with the given number, in the order the
        /// object writer creates them.
        /// </summary>
        public Utf8String GetCodeBaseSymbolName(NameMangler nameMangler, int section)
            => Utf8String.Concat(nameMangler.CompilationUnitPrefix.AsSpan(), "__compact_unwind_code_base_"u8, new Utf8String(section.ToString()).AsSpan());

        public Utf8String GetLsdaBaseSymbolName(NameMangler nameMangler)
            => Utf8String.Concat(nameMangler.CompilationUnitPrefix.AsSpan(), "__compact_unwind_lsda_base"u8);

        public void AddEntry(int section, long codeOffset, long length, long lsdaOffset, byte[] descriptor)
        {
            if (_dataEmitted)
                throw new InvalidOperationException("Compact unwind entries must be added before the table is emitted.");

            uint code = checked((uint)codeOffset);
            uint lsda = checked((uint)lsdaOffset);
            uint frameLength = checked((uint)length);

            string key = Convert.ToHexString(descriptor);
            if (!_descriptorIndices.TryGetValue(key, out int index))
            {
                index = _descriptors.Count;
                _descriptors.Add(descriptor);
                _descriptorIndices.Add(key, index);
            }

            _frames.Add((section, code, lsda, frameLength, index));
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
            int noDescriptor = _descriptors.Count;

            // Number the descriptors by use, so that the indices in most blocks are small
            int[] uses = new int[_descriptors.Count];
            foreach (Entry entry in entries)
            {
                if (entry.Descriptor != noDescriptor)
                    uses[entry.Descriptor]++;
            }

            int[] order = new int[_descriptors.Count];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            Array.Sort(order, (x, y) => uses[x] != uses[y] ? uses[y].CompareTo(uses[x]) : x.CompareTo(y));
            int[] rank = new int[_descriptors.Count + 1];
            for (int i = 0; i < order.Length; i++)
                rank[order[i]] = i;
            rank[noDescriptor] = noDescriptor;

            // Pick the block size that makes the table smallest
            int blockShift = MinBlockShift;
            long smallest = long.MaxValue;
            for (int shift = MinBlockShift; shift <= MaxBlockShift; shift++)
            {
                long size = 0;
                foreach (Block block in GetBlocks(entries, rank, shift))
                    size += BlockHeaderSize * 8 + (long)block.Count * block.EntryBits;

                if (size < smallest)
                {
                    smallest = size;
                    blockShift = shift;
                }
            }

            var blocks = new List<Block>();
            var stream = new BitWriter();
            foreach (Block block in GetBlocks(entries, rank, blockShift))
            {
                Block written = block;
                written.BitOffset = checked((uint)stream.BitCount);
                blocks.Add(written);

                for (int i = block.Start; i < block.Start + block.Count; i++)
                {
                    Entry entry = entries[i];
                    stream.WriteBits(entry.CodeOffset - block.CodeBase, block.CodeBits);
                    stream.WriteBits(entry.Descriptor == noDescriptor ? 0 : entry.LsdaOffset - block.LsdaBase, block.LsdaBits);
                    stream.WriteBits((uint)rank[entry.Descriptor], block.IndexBits);
                }
            }

            builder.EmitUInt((uint)entries.Count);
            builder.EmitUInt((uint)_descriptors.Count);
            builder.EmitUInt((uint)blocks.Count);
            builder.EmitUInt(checked((uint)stream.BitCount));
            builder.EmitByte((byte)blockShift);
            builder.EmitByte(0);
            builder.EmitShort(0);

            int descriptorOffset = HeaderSize + 4 * _descriptors.Count;
            foreach (int descriptor in order)
            {
                builder.EmitInt(descriptorOffset);
                descriptorOffset += _descriptors[descriptor].Length;
            }

            foreach (int descriptor in order)
                builder.EmitBytes(_descriptors[descriptor]);

            Debug.Assert(builder.CountBytes == descriptorOffset && descriptorOffset % 4 == 0);

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
                builder.EmitShort((short)((block.CodeBits - 1) | ((block.LsdaBits - 1) << 5) | ((block.IndexBits - 1) << 10)));
            }

            builder.EmitBytes(stream.ToArray());
            builder.EmitZeros(8);

            return builder.ToObjectData();
        }

        private record struct Entry(int Section, uint CodeOffset, uint LsdaOffset, int Descriptor);

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
            public int IndexBits;
            public readonly int EntryBits => CodeBits + LsdaBits + IndexBits;
        }

        /// <summary>
        /// Splits the entries into blocks of at most 1 &lt;&lt; shift entries that don't span sections.
        /// </summary>
        private static IEnumerable<Block> GetBlocks(List<Entry> entries, int[] rank, int shift)
        {
            int noDescriptor = rank.Length - 1;
            int start = 0;
            while (start < entries.Count)
            {
                int section = entries[start].Section;
                int end = start + 1;
                while (end < entries.Count && end - start < 1 << shift && entries[end].Section == section)
                    end++;

                uint lsdaBase = uint.MaxValue, maxLsda = 0, maxIndex = 0;
                for (int i = start; i < end; i++)
                {
                    maxIndex = Math.Max(maxIndex, (uint)rank[entries[i].Descriptor]);
                    if (entries[i].Descriptor == noDescriptor)
                        continue;

                    lsdaBase = Math.Min(lsdaBase, entries[i].LsdaOffset);
                    maxLsda = Math.Max(maxLsda, entries[i].LsdaOffset);
                }

                if (lsdaBase == uint.MaxValue)
                    lsdaBase = 0;

                yield return new Block
                {
                    Section = section,
                    Start = start,
                    Count = end - start,
                    CodeBase = entries[start].CodeOffset,
                    LsdaBase = lsdaBase,
                    CodeBits = BitsToEncode(entries[end - 1].CodeOffset - entries[start].CodeOffset),
                    LsdaBits = BitsToEncode(maxLsda - lsdaBase),
                    IndexBits = BitsToEncode(maxIndex),
                };

                start = end;
            }
        }

        /// <summary>
        /// Sorts the frames by section and offset, and covers the recorded code that isn't in any frame and the end
        /// of each section with entries without a descriptor, so that each frame ends where the next entry starts.
        /// </summary>
        private List<Entry> BuildEntries()
        {
            int noDescriptor = _descriptors.Count;
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
            foreach ((int section, uint codeOffset, uint lsdaOffset, uint length, int index) in _frames)
            {
                for (; hole < holes.Count && (holes[hole].Section < section || (holes[hole].Section == section && holes[hole].Offset < codeOffset)); hole++)
                    AddHole(holes[hole]);

                Debug.Assert(entries.Count == 0 || entries[^1].Section != section || entries[^1].CodeOffset < codeOffset);
                entries.Add(new Entry(section, codeOffset, lsdaOffset, index));
            }

            for (; hole < holes.Count; hole++)
                AddHole(holes[hole]);

            return entries;

            void AddHole((int Section, uint Offset) h)
            {
                if (entries.Count == 0 || entries[^1].Descriptor != noDescriptor || entries[^1].Section != h.Section)
                    entries.Add(new Entry(h.Section, h.Offset, 0, noDescriptor));
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
