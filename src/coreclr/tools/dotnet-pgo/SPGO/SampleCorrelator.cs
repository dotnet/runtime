// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    /// <summary>
    /// A class that handles correlating IP samples/LBR samples back to managed methods.
    /// </summary>
    internal class SampleCorrelator
    {
        private readonly Dictionary<MethodDesc, PerMethodInfo> _methodInf = new Dictionary<MethodDesc, PerMethodInfo>();

        private readonly MethodMemoryMap _memMap;

        public SampleCorrelator(MethodMemoryMap memMap)
        {
            _memMap = memMap;
        }

        public long SamplesOutsideManagedCode { get; private set; }
        public long SamplesInManagedCodeWithoutAnyMappings { get; private set; }
        public long SamplesInManagedCodeOutsideMappings { get; private set; }
        public long SamplesInUnknownInlinees { get; private set; }
        public long SamplesInManagedCodeWithoutIL { get; private set; }
        public long SamplesInManagedCodeOutsideFlowGraph { get; private set; }
        public long TotalAttributedSamples { get; private set; }

        private PerMethodInfo GetOrCreateInfo(MethodDesc md)
        {
            if (!_methodInf.TryGetValue(md, out PerMethodInfo pmi))
            {
                MethodIL il =
                    md switch
                    {
                        EcmaMethod em => EcmaMethodIL.Create(em),
                        _ => new InstantiatedMethodIL(md, EcmaMethodIL.Create((EcmaMethod)md.GetTypicalMethodDefinition())),
                    };

                if (il == null)
                {
                    return null;
                }

                _methodInf.Add(md, pmi = new PerMethodInfo());
                pmi.IL = il;
                pmi.FlowGraph = FlowGraph.Create(il);
                pmi.Profile = new SampleProfile(pmi.IL, pmi.FlowGraph);
            }

            return pmi;
        }

        public SampleProfile GetProfile(MethodDesc md)
            => _methodInf.GetValueOrDefault(md)?.Profile;

        public void SmoothAllProfiles()
        {
            foreach (PerMethodInfo pmi in _methodInf.Values)
                pmi.Profile.SmoothFlow();
        }

        public void AttributeSamplesToIP(ulong ip, long numSamples)
        {
            MemoryRegionInfo region = _memMap.GetInfo(ip);
            if (region == null)
            {
                SamplesOutsideManagedCode += numSamples;
                return;
            }

            if (region.NativeToILMap == null)
            {
                SamplesInManagedCodeWithoutAnyMappings += numSamples;
                return;
            }

            if (!region.NativeToILMap.TryLookup(checked((uint)(ip - region.StartAddress)), out IPMapping mapping))
            {
                SamplesInManagedCodeOutsideMappings += numSamples;
                return;
            }

            if (mapping.InlineeMethod == null)
            {
                SamplesInUnknownInlinees += numSamples;
                return;
            }

            PerMethodInfo pmi = GetOrCreateInfo(mapping.InlineeMethod);
            if (pmi == null)
            {
                SamplesInManagedCodeWithoutIL += numSamples;
                return;
            }

            if (pmi.Profile.TryAttributeSamples(mapping.ILOffset, 1))
            {
                TotalAttributedSamples += numSamples;
            }
            else
            {
                SamplesInManagedCodeOutsideFlowGraph += numSamples;
            }
        }

        private LbrEntry64[] _convertedEntries;
        public void AttributeSampleToLbrRuns(Span<LbrEntry32> lbr)
        {
            if (_convertedEntries == null || _convertedEntries.Length < lbr.Length)
            {
                Array.Resize(ref _convertedEntries, lbr.Length);
            }

            Span<LbrEntry64> convertedEntries = _convertedEntries[..lbr.Length];
            for (int i = 0; i < lbr.Length; i++)
            {
                ref LbrEntry64 entry = ref convertedEntries[i];
                entry.FromAddress = lbr[i].FromAddress;
                entry.ToAddress = lbr[i].ToAddress;
                entry.Reserved = lbr[i].Reserved;
            }

            AttributeSampleToLbrRuns(convertedEntries);
        }

        private readonly List<(BasicBlock, int)> _callStack = new();
        private readonly HashSet<(InlineContext, BasicBlock)> _seenOnRun = new();
        public void AttributeSampleToLbrRuns(Span<LbrEntry64> lbr)
        {
            // LBR record represents branches taken by the CPU, in
            // chronological order with most recent branches first. Using this
            // data we can construct the 'runs' of instructions executed by the
            // CPU. We attribute a sample to all basic blocks in each run.
            //
            // As an example, if we see a branch A -> B followed by a branch C -> D,
            // we conclude that the CPU executed the instructions from B to C.
            //
            // Note that we need some special logic to handle calls. If we see
            // a call A -> B followed by a return B -> A, a straightforward
            // attribution process would attribute multiple samples to the
            // block containing A. To deal with this we track in a list the
            // basic blocks + offsets that we left, and if we see a return to
            // the same basic block at a later offset, we skip that basic
            // block.
            // Note that this is an approximation of the call stack as we
            // cannot differentiate between tailcalls and calls, so there
            // may be BBs we left in here that we never return to.
            // Therefore we cannot just use a straightforward stack.
            List<(BasicBlock, int)> callStack = _callStack;
            callStack.Clear();

            // On each run we translate the endpoint RVAs to all IL offset
            // mappings we have for that range. It is possible (and happens
            // often) that we see multiple IL offsets corresponding to the same
            // basic block.
            //
            // Therefore, we keep track of the basic blocks we have seen in each
            // run to make sure we only attribute once. However, due to inlinees
            // we sometimes may want to attribute twice, for example if A is inlined in
            // A(); A();
            // Therefore, we also key by the inline context.
            HashSet<(InlineContext, BasicBlock)> seenOnRun = _seenOnRun;

            MethodMemoryMap memMap = _memMap;

            for (int i = lbr.Length - 2; i >= 0; i--)
            {
                ref LbrEntry64 prev = ref lbr[i + 1];
                ref LbrEntry64 cur = ref lbr[i];

                MemoryRegionInfo prevToInf = memMap.GetInfo(prev.ToAddress);
                MemoryRegionInfo curFromInf = memMap.GetInfo(cur.FromAddress);

                // If this run is not in the same function then ignore it.
                // This probably means IP was changed out from beneath us while
                // recording.
                if (prevToInf == null || prevToInf != curFromInf)
                    continue;

                if (curFromInf.NativeToILMap == null)
                    continue;

                // Attribute samples to run.
                seenOnRun.Clear();
                uint rvaMin = checked((uint)(prev.ToAddress - prevToInf.StartAddress));
                uint rvaMax = checked((uint)(cur.FromAddress - curFromInf.StartAddress));
                int lastILOffs = -1;
                BasicBlock lastBB = null;
                bool isFirst = true;
                foreach (IPMapping mapping in curFromInf.NativeToILMap.LookupRange(rvaMin, rvaMax))
                {
                    bool isFirstMapping = isFirst;
                    isFirst = false;

                    if (mapping.InlineeMethod == null)
                        continue;

                    PerMethodInfo pmi = GetOrCreateInfo(mapping.InlineeMethod);
                    if (pmi == null)
                        continue;

                    BasicBlock bb = pmi.FlowGraph.Lookup(mapping.ILOffset);
                    if (bb == null)
                        continue;

                    lastBB = bb;
                    lastILOffs = mapping.ILOffset;

                    if (seenOnRun.Add((mapping.InlineContext, bb)))
                    {
                        if (isFirstMapping)
                        {
                            // This is the first mapping in the run. Check to
                            // see if we returned to this BB in the callstack,
                            // and if so, skip attributing anything to the
                            // first BB.

                            bool skip = false;

                            for (int j = callStack.Count - 1; j >= 0; j++)
                            {
                                (BasicBlock callFromBB, int callFromILOffs) = callStack[j];
                                if (callFromBB == bb && mapping.ILOffset >= callFromILOffs)
                                {
                                    // Yep, we previously left 'bb' at
                                    // 'callFromILOffs', and now we are jumping
                                    // back to the same BB at a later offset.
                                    skip = true;
                                    callStack.RemoveRange(j, callStack.Count - j);
                                    break;
                                }
                            }

                            if (skip)
                                continue;
                        }

                        pmi.Profile.AttributeSamples(bb, 1);
                    }
                }

                // Now see if this is a cross-function jump.
                MemoryRegionInfo curToInf = memMap.GetInfo(cur.ToAddress);
                // TODO: This check and above skipping logic does not handle recursion.
                if (curFromInf != curToInf)
                {
                    // Yes, either different managed function or not managed function (e.g. prestub).
                    // Record this.
                    if (lastBB != null)
                    {
                        callStack.Add((lastBB, lastILOffs));
                    }
                }
            }
        }

        public override string ToString() => $"{TotalAttributedSamples} samples in {_methodInf.Count} methods";

        private class PerMethodInfo
        {
            public MethodIL IL { get; set; }
            public FlowGraph FlowGraph { get; set; }
            public SampleProfile Profile { get; set; }

            public override string ToString() => IL.OwningMethod.ToString();
        }
    }
}
