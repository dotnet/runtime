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
        private Dictionary<BasicBlock, long> _rawSamples = new Dictionary<BasicBlock, long>();
        private Dictionary<BasicBlock, long> _smoothedSamples;
        private Dictionary<(BasicBlock, BasicBlock), long> _smoothedEdgeSamples;

        public SampleProfile(
            MethodIL methodIL,
            FlowGraph fg)
        {
            MethodIL = methodIL;
            FlowGraph = fg;
        }

        public MethodIL MethodIL { get; }
        public FlowGraph FlowGraph { get; }
        public IReadOnlyDictionary<BasicBlock, long> RawSamples => _rawSamples;
        public IReadOnlyDictionary<BasicBlock, long> SmoothedSamples => _smoothedSamples;
        public IReadOnlyDictionary<(BasicBlock, BasicBlock), long> SmoothedEdgeSamples => _smoothedEdgeSamples;
        public long AttributedSamples { get; set; }

        public bool TryAttributeSamples(int ilOffset, long count)
        {
            BasicBlock bb = FlowGraph.Lookup(ilOffset);
            if (bb == null)
                return false;

            AttributeSamples(bb, count);
            return true;
        }

        public void AttributeSamples(BasicBlock bb, long count)
        {
            Debug.Assert(FlowGraph.Lookup(bb.Start) == bb);
            CollectionsMarshal.GetValueRefOrAddDefault(_rawSamples, bb, out _) += count;
            AttributedSamples += count;
        }

        public void SmoothFlow()
        {
            foreach (BasicBlock bb in FlowGraph.BasicBlocks)
            {
                if (!_rawSamples.ContainsKey(bb))
                    _rawSamples.Add(bb, 0);
            }

            FlowSmoothing<BasicBlock> flowSmooth = new(_rawSamples, FlowGraph.Lookup(0), bb => bb.Targets, (bb, isForward) => bb.Size * (isForward ? 1 : 50) + 2);
            flowSmooth.Perform();
            _smoothedSamples = flowSmooth.NodeResults;
            _smoothedEdgeSamples = flowSmooth.EdgeResults;
        }

        public override string ToString() => $"{AttributedSamples} samples";
    }
}
