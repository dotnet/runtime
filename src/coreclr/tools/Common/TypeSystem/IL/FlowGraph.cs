// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Internal.IL
{
    internal class BasicBlock : IEquatable<BasicBlock>
    {
        public BasicBlock(int start, int size)
            => (Start, Size) = (start, size);

        // First IL offset
        public int Start { get; }
        // Number of IL bytes in this basic block
        public int Size { get; }

        public HashSet<BasicBlock> Sources { get; } = new HashSet<BasicBlock>();
        public HashSet<BasicBlock> Targets { get; } = new HashSet<BasicBlock>();

        public override string ToString() => $"Start={Start}, Size={Size}";

        public override bool Equals(object obj) => Equals(obj as BasicBlock);
        public bool Equals(BasicBlock other) => other != null && Start == other.Start;
        public override int GetHashCode() => HashCode.Combine(Start);

        public static bool operator ==(BasicBlock left, BasicBlock right) => EqualityComparer<BasicBlock>.Default.Equals(left, right);
        public static bool operator !=(BasicBlock left, BasicBlock right) => !(left == right);
    }

    internal class FlowGraph
    {
        private readonly int[] _bbKeys;

        private FlowGraph(IEnumerable<BasicBlock> bbs)
        {
            BasicBlocks = bbs.OrderBy(bb => bb.Start).ToList();
            _bbKeys = BasicBlocks.Select(bb => bb.Start).ToArray();
        }

        /// <summary>Basic blocks, ordered by start IL offset.</summary>
        public List<BasicBlock> BasicBlocks { get; }

        /// <summary>Find index of basic block containing IL offset.</summary>
        public int LookupIndex(int ilOffset)
        {
            int index = Array.BinarySearch(_bbKeys, ilOffset);
            if (index < 0)
                index = ~index - 1;

            // If ilOffset is negative (more generally, before the first BB)
            // then binarySearch will return ~0 since index 0 is the first BB
            // that's greater.
            if (index < 0)
                return -1;

            // If this is the last BB we could be after as well.
            BasicBlock bb = BasicBlocks[index];
            if (ilOffset >= bb.Start + bb.Size)
                return -1;

            return index;
        }

        public BasicBlock Lookup(int ilOffset)
            => LookupIndex(ilOffset) switch
            {
                -1 => null,
                int idx => BasicBlocks[idx]
            };

        public IEnumerable<BasicBlock> LookupRange(int ilOffsetStart, int ilOffsetEnd)
        {
            if (ilOffsetStart < BasicBlocks[0].Start)
                ilOffsetStart = BasicBlocks[0].Start;

            if (ilOffsetEnd > BasicBlocks.Last().Start)
                ilOffsetEnd = BasicBlocks.Last().Start;

            int end = LookupIndex(ilOffsetEnd);
            for (int i = LookupIndex(ilOffsetStart); i <= end; i++)
                yield return BasicBlocks[i];
        }

        public static FlowGraph Create(MethodIL il)
        {
            HashSet<int> bbStarts = GetBasicBlockStarts(il);

            List<BasicBlock> bbs = new List<BasicBlock>();
            void AddBB(int start, int count)
            {
                if (count > 0)
                    bbs.Add(new BasicBlock(start, count));
            }

            int prevStart = 0;
            foreach (int ofs in bbStarts.OrderBy(o => o))
            {
                AddBB(prevStart, ofs - prevStart);
                prevStart = ofs;
            }

            AddBB(prevStart, il.GetILBytes().Length - prevStart);

            FlowGraph fg = new FlowGraph(bbs);

            // We know where each basic block starts now. Proceed by linking them together.
            ILReader reader = new ILReader(il.GetILBytes());
            foreach (BasicBlock bb in bbs)
            {
                reader.Seek(bb.Start);
                while (reader.HasNext)
                {
                    Debug.Assert(fg.Lookup(reader.Offset) == bb);
                    ILOpcode opc = reader.ReadILOpcode();
                    if (opc.IsBranch())
                    {
                        int tar = reader.ReadBranchDestination(opc);
                        bb.Targets.Add(fg.Lookup(tar));
                        if (!opc.IsUnconditionalBranch())
                            bb.Targets.Add(fg.Lookup(reader.Offset));

                        break;
                    }

                    if (opc == ILOpcode.switch_)
                    {
                        uint numCases = reader.ReadILUInt32();
                        int jmpBase = reader.Offset + checked((int)(numCases * 4));
                        bb.Targets.Add(fg.Lookup(jmpBase));

                        for (uint i = 0; i < numCases; i++)
                        {
                            int caseOfs = jmpBase + (int)reader.ReadILUInt32();
                            bb.Targets.Add(fg.Lookup(caseOfs));
                        }

                        break;
                    }

                    if (opc == ILOpcode.ret || opc == ILOpcode.endfinally || opc == ILOpcode.endfilter || opc == ILOpcode.throw_ || opc == ILOpcode.rethrow)
                    {
                        break;
                    }

                    reader.Skip(opc);
                    // Check fall through
                    if (reader.HasNext)
                    {
                        BasicBlock nextBB = fg.Lookup(reader.Offset);
                        if (nextBB != bb)
                        {
                            // Falling through
                            bb.Targets.Add(nextBB);
                            break;
                        }
                    }
                }
            }

            foreach (BasicBlock bb in bbs)
            {
                foreach (BasicBlock tar in bb.Targets)
                    tar.Sources.Add(bb);
            }

            return fg;
        }

        /// <summary>
        /// Find IL offsets at which basic blocks begin.
        /// </summary>
        private static HashSet<int> GetBasicBlockStarts(MethodIL il)
        {
            ILReader reader = new ILReader(il.GetILBytes());
            HashSet<int> bbStarts = new HashSet<int>();
            bbStarts.Add(0);
            while (reader.HasNext)
            {
                ILOpcode opc = reader.ReadILOpcode();
                if (opc.IsBranch())
                {
                    int tar = reader.ReadBranchDestination(opc);
                    bbStarts.Add(tar);
                    // Conditional branches can fall through.
                    if (!opc.IsUnconditionalBranch())
                        bbStarts.Add(reader.Offset);
                }
                else if (opc == ILOpcode.switch_)
                {
                    uint numCases = reader.ReadILUInt32();
                    int jmpBase = reader.Offset + checked((int)(numCases * 4));
                    // Default case is at jmpBase.
                    bbStarts.Add(jmpBase);

                    for (uint i = 0; i < numCases; i++)
                    {
                        int caseOfs = jmpBase + (int)reader.ReadILUInt32();
                        bbStarts.Add(caseOfs);
                    }
                }
                else if (opc == ILOpcode.ret || opc == ILOpcode.endfinally || opc == ILOpcode.endfilter || opc == ILOpcode.throw_ || opc == ILOpcode.rethrow)
                {
                    if (reader.HasNext)
                        bbStarts.Add(reader.Offset);
                }
                else
                {
                    reader.Skip(opc);
                }
            }

            foreach (ILExceptionRegion ehRegion in il.GetExceptionRegions())
            {
                bbStarts.Add(ehRegion.TryOffset);
                bbStarts.Add(ehRegion.TryOffset + ehRegion.TryLength);
                bbStarts.Add(ehRegion.HandlerOffset);
                bbStarts.Add(ehRegion.HandlerOffset + ehRegion.HandlerLength);
                if (ehRegion.Kind.HasFlag(ILExceptionRegionKind.Filter))
                    bbStarts.Add(ehRegion.FilterOffset);
            }

            return bbStarts;
        }
    }
}
