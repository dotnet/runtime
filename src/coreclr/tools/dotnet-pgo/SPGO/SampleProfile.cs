// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    internal class SampleProfile
    {
        public SampleProfile(
            MethodIL methodIL,
            FlowGraph fg,
            Dictionary<BasicBlock, long> samples,
            Dictionary<BasicBlock, long> smoothedSamples,
            Dictionary<(BasicBlock, BasicBlock), long> smoothedEdgeSamples)
        {
            MethodIL = methodIL;
            FlowGraph = fg;
            Samples = samples;
            SmoothedSamples = smoothedSamples;
            SmoothedEdgeSamples = smoothedEdgeSamples;
        }

        public MethodIL MethodIL { get; }
        public FlowGraph FlowGraph { get; }
        public Dictionary<BasicBlock, long> Samples { get; }
        public Dictionary<BasicBlock, long> SmoothedSamples { get; }
        public Dictionary<(BasicBlock, BasicBlock), long> SmoothedEdgeSamples { get; }

        /// <summary>
        /// Given pairs of runs (as relative IPs in this function), create a sample profile.
        /// </summary>
        public static SampleProfile CreateFromLbr(MethodIL il, FlowGraph fg, NativeToILMap map, IEnumerable<(uint fromRva, uint toRva, long count)> runs)
        {
            Dictionary<BasicBlock, long> bbSamples = fg.BasicBlocks.ToDictionary(bb => bb, bb => 0L);
            foreach ((uint from, uint to, long count) in runs)
            {
                foreach (BasicBlock bb in map.LookupRange(from, to).Select(fg.Lookup).Distinct())
                {
                    if (bb != null)
                        bbSamples[bb] += count;
                }
            }

            FlowSmoothing<BasicBlock> flowSmooth = new FlowSmoothing<BasicBlock>(bbSamples, fg.Lookup(0), bb => bb.Targets, (bb, isForward) => bb.Size * (isForward ? 1 : 50) + 2);
            flowSmooth.Perform();

            return new SampleProfile(il, fg, bbSamples, flowSmooth.NodeResults, flowSmooth.EdgeResults);
        }

        /// <summary>
        /// Given some IL offset samples into a method, construct a profile.
        /// </summary>
        public static SampleProfile Create(MethodIL il, FlowGraph fg, IEnumerable<int> ilOffsetSamples)
        {
            // Now associate raw IL-offset samples with basic blocks.
            Dictionary<BasicBlock, long> bbSamples = fg.BasicBlocks.ToDictionary(bb => bb, bb => 0L);
            foreach (int ofs in ilOffsetSamples)
            {
                if (ofs == -1)
                    continue;

                BasicBlock bb = fg.Lookup(ofs);
                if (bb != null)
                    bbSamples[bb]++;
            }

            // Smooth the graph to produce something that satisfies flow conservation.
            FlowSmoothing<BasicBlock> flowSmooth = new FlowSmoothing<BasicBlock>(bbSamples, fg.Lookup(0), bb => bb.Targets, (bb, isForward) => bb.Size * (isForward ? 1 : 50) + 2);
            flowSmooth.Perform();

            return new SampleProfile(il, fg, bbSamples, flowSmooth.NodeResults, flowSmooth.EdgeResults);
        }
    }
}
