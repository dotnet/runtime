// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILCompiler;
using Internal.Pgo;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    internal class PgoCompareMethodFlowGraph
    {
        public bool ProfilesHadBasicBlocks { get; init; }
        public bool ProfilesHadEdges { get; init; }
        public PgoCompareMethodBasicBlock EntryBasicBlock { get; init; }
        public List<PgoCompareMethodBasicBlock> BasicBlocks { get; init; }

        public long TotalBlockCount1 => BasicBlocks.Sum(bb => bb.BlockCount1);
        public long TotalBlockCount2 => BasicBlocks.Sum(bb => bb.BlockCount2);
        public long TotalEdgeCount1 => BasicBlocks.Sum(bb => bb.Edges.Sum(e => e.Value.Count1));
        public long TotalEdgeCount2 => BasicBlocks.Sum(bb => bb.Edges.Sum(e => e.Value.Count2));

        public double ComputeBlockOverlap()
        {
            long total1 = TotalBlockCount1;
            long total2 = TotalBlockCount2;

            if (total1 == 0 && total2 == 0)
                return 1;

            if (total1 == 0 || total2 == 0)
                return 0;

            double overlap =
                BasicBlocks
                .Sum(bb => Math.Min(bb.BlockCount1 / (double)total1, bb.BlockCount2 / (double)total2));
            return overlap;
        }

        public double ComputeEdgeOverlap()
        {
            long total1 = TotalEdgeCount1;
            long total2 = TotalEdgeCount2;

            if (total1 == 0 && total2 == 0)
                return 1;

            if (total1 == 0 || total2 == 0)
                return 0;

            double overlap =
                BasicBlocks
                .Sum(bb => bb.Edges.Values.Sum(e => Math.Min(e.Count1 / (double)total1, e.Count2 / (double)total2)));
            return overlap;
        }

        public string Dump(string title)
        {
            long totalBlockCount1 = TotalBlockCount1;
            long totalBlockCount2 = TotalBlockCount2;

            string createWeightLabel(long weight1, long totalWeight1, long weight2, long totalWeight2)
            {
                string label = "";
                if (totalWeight1 == 0)
                {
                    label += "N/A";
                }
                else
                {
                    double pw = weight1 / (double)totalWeight1;
                    label += $"{pw * 100:F2}%";
                }

                label += " vs ";
                if (totalWeight2 == 0)
                {
                    label += "N/A";
                }
                else
                {
                    double pw = weight2 / (double)totalWeight2;
                    label += $"{pw * 100:F2}%";
                }

                return label;
            }

            string getLabel(PgoCompareMethodBasicBlock bb)
            {
                string label = $"@ {bb.ILOffset:x3}";
                if (ProfilesHadBasicBlocks && (totalBlockCount1 != 0 || totalBlockCount2 != 0))
                {
                    label += $"\\n{createWeightLabel(bb.BlockCount1, totalBlockCount1, bb.BlockCount2, totalBlockCount2)}";
                }

                return label;
            }

            long totalEdgeCount1 = TotalEdgeCount1;
            long totalEdgeCount2 = TotalEdgeCount2;

            string getEdgeLabel((PgoCompareMethodBasicBlock from, PgoCompareMethodBasicBlock to) edge)
            {
                if (!ProfilesHadEdges)
                    return "";

                (long weight1, long weight2) = edge.from.Edges[edge.to];
                return createWeightLabel(weight1, totalEdgeCount1, weight2, totalEdgeCount2);
            }

            string dot =
                FlowGraphHelper.DumpGraph(
                    BasicBlocks, EntryBasicBlock,
                    bb => new HashSet<PgoCompareMethodBasicBlock>(bb.Edges.Keys),
                    title,
                    getLabel,
                    getEdgeLabel);
            return dot;
        }

        public static PgoCompareMethodFlowGraph Create(
            MethodProfileData profile1,
            string name1,
            MethodProfileData profile2,
            string name2,
            out List<string> errors)
        {
            errors = new List<string>();
            if (profile1?.SchemaData == null)
            {
                errors.Add($"Profile data missing from {name1}");
                return null;
            }
            if (profile2?.SchemaData == null)
            {
                errors.Add($"Profile data missing from {name2}");
                return null;
            }

            var (blocks1, blocks2) = (GroupBlocks(profile1), GroupBlocks(profile2));
            var (edges1, edges2) = (GroupEdges(profile1), GroupEdges(profile2));
            bool hasBlocks1 = blocks1.Count != 0;
            bool hasBlocks2 = blocks2.Count != 0;
            bool hasEdges1 = edges1.Count != 0;
            bool hasEdges2 = edges2.Count != 0;
            if (!hasBlocks1 && !hasBlocks2 && !hasEdges1 && !hasEdges2)
            {
                errors.Add($"No profile data present in either {name1} or {name2}");
                return null;
            }

            bool hasComparableProfileData =
                (hasBlocks1 && hasBlocks2) ||
                (hasEdges1 && hasEdges2);

            if (!hasComparableProfileData)
            {
                errors.Add($"No comparable profile data present");
                return null;
            }

            if (hasBlocks1 && hasBlocks2)
            {
                var in1 = blocks1.Keys.Where(k => !blocks2.ContainsKey(k)).ToList();
                var in2 = blocks2.Keys.Where(k => !blocks1.ContainsKey(k)).ToList();

                foreach (var m1 in in1)
                    errors.Add($"{name1} has a block at {m1:x} not present in {name2}");
                foreach (var m2 in in2)
                    errors.Add($"{name2} has a block at {m2:x} not present in {name1}");
            }

            if (hasEdges1 && hasEdges2)
            {
                var in1 = edges1.Keys.Where(k => !edges2.ContainsKey(k)).ToList();
                var in2 = edges2.Keys.Where(k => !edges1.ContainsKey(k)).ToList();

                foreach (var (from, to) in in1)
                    errors.Add($"{name1} has an edge {from:x}->{to:x} not present in {name2}");
                foreach (var (from, to) in in2)
                    errors.Add($"{name2} has an edge {from:x}->{to:x} not present in {name1}");
            }

            if (errors.Count > 0)
            {
                // Do not continue if flow graphs do not match
                return null;
            }

            // Note: We permit missing data in one of the two profiles (e.g.
            // instrumentation will typically not contain edges if we asked for
            // BBs, but we can still compare with SPGO with arg
            // --include-full-graphs this way).

            Dictionary<int, PgoCompareMethodBasicBlock> ilToBB = new();
            foreach ((int ilOffs, _) in hasBlocks1 ? blocks1 : blocks2)
            {
                ilToBB.Add(
                    ilOffs,
                    new PgoCompareMethodBasicBlock
                    {
                        ILOffset = ilOffs,
                        BlockCount1 = blocks1.TryGetValue(ilOffs, out PgoSchemaElem elem) ? elem.DataLong : 0,
                        BlockCount2 = blocks2.TryGetValue(ilOffs, out elem) ? elem.DataLong : 0,
                    });
            }

            foreach (((int ilFrom, int ilTo), _) in hasEdges1 ? edges1 : edges2)
            {
                if (!ilToBB.TryGetValue(ilFrom, out PgoCompareMethodBasicBlock bbFrom))
                {
                    if (hasBlocks1 || hasBlocks2)
                    {
                        errors.Add($"There is an edge from {ilFrom} -> {ilTo}, but no basic block found at {ilFrom}");
                    }
                    else
                    {
                        // If we have no BBs at all use the edges to construct BBs.
                        ilToBB.Add(ilFrom, bbFrom = new PgoCompareMethodBasicBlock
                        {
                            ILOffset = ilFrom
                        });
                    }
                }

                if (!ilToBB.TryGetValue(ilTo, out PgoCompareMethodBasicBlock bbTo))
                {
                    if (hasBlocks1 || hasBlocks2)
                    {
                        errors.Add($"There is an edge from {ilFrom} -> {ilTo}, but no basic block found at {ilTo}");
                    }
                    else
                    {
                        // If we have no BBs at all use the edges to construct BBs.
                        ilToBB.Add(ilTo, bbTo = new PgoCompareMethodBasicBlock
                        {
                            ILOffset = ilTo
                        });
                    }
                }

                long edgeCount1 = edges1.TryGetValue((ilFrom, ilTo), out PgoSchemaElem elem) ? elem.DataLong : 0;
                long edgeCount2 = edges2.TryGetValue((ilFrom, ilTo), out elem) ? elem.DataLong : 0;
                bbFrom.Edges.Add(bbTo, (edgeCount1, edgeCount2));
            }

            if (!ilToBB.TryGetValue(0, out PgoCompareMethodBasicBlock entryBasicBlock))
            {
                errors.Add("No entry block found");
                return null;
            }

            return new PgoCompareMethodFlowGraph
            {
                BasicBlocks = ilToBB.Values.ToList(),
                EntryBasicBlock = entryBasicBlock,
                ProfilesHadBasicBlocks = hasBlocks1 && hasBlocks2,
                ProfilesHadEdges = hasEdges1 && hasEdges2,
            };
        }

        private static Dictionary<int, PgoSchemaElem> GroupBlocks(MethodProfileData data)
        {
            return data.SchemaData
               .Where(e => e.InstrumentationKind == PgoInstrumentationKind.BasicBlockIntCount || e.InstrumentationKind == PgoInstrumentationKind.BasicBlockLongCount)
               .ToDictionary(e => e.ILOffset);
        }

        private static Dictionary<(int, int), PgoSchemaElem> GroupEdges(MethodProfileData data)
        {
            return data.SchemaData
               .Where(e => e.InstrumentationKind == PgoInstrumentationKind.EdgeIntCount || e.InstrumentationKind == PgoInstrumentationKind.EdgeLongCount)
               .ToDictionary(e => (e.ILOffset, e.Other));
        }
    }

    internal sealed class PgoCompareMethodBasicBlock
    {
        public int ILOffset { get; init; }
        public long BlockCount1 { get; init; }
        public long BlockCount2 { get; init; }

        public Dictionary<PgoCompareMethodBasicBlock, (long Count1, long Count2)> Edges { get; } = new();

        public override bool Equals(object obj) => obj is PgoCompareMethodBasicBlock block && ILOffset == block.ILOffset;
        public override int GetHashCode() => HashCode.Combine(ILOffset);
    }
}
