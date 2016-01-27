// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS

using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GCPerfTestFramework.Metrics.Builders
{
    internal class GCEvent
    {
        #region Public Fields
        //  Time it takes to do the suspension. Before 4.0 we didn't have a SuspendEnd event so we calculate it by just 
        // substracting GC duration from pause duration. For concurrent GC this would be inaccurate so we just return 0.
        public double _SuspendDurationMSec;

        public double[] AllocedSinceLastGCBasedOnAllocTickMB = { 0.0, 0.0 };
        public long duplicatedPinningReports;
        public double DurationSinceLastRestartMSec;
        // The amount of CPU time this GC consumed.
        public float GCCpuMSec;

        public float[] GCCpuServerGCThreads = null;
        public double GCDurationMSec;
        public int GCGeneration;
        // Primary fields (set in the callbacks)
        public int GCNumber;

        public double GCStartRelativeMSec;
        public GCGlobalHeapHistoryTraceData GlobalHeapHistory;
        public bool HasAllocTickEvents = false;
        public GCHeapStatsTraceData HeapStats;
        public int Index;
        // In 2.0 we didn't have all the events. I try to keep the code not version specific and this is really
        // for debugging/verification purpose.
        public bool is20Event;

        // Did we get the complete event sequence for this GC?
        // For BGC this is the HeapStats event; for other GCs this means you have both the HeapStats and RestartEEEnd event.
        public bool isComplete;

        //  Set in Start, does not include suspension.  
        //  Set in Stop This is JUST the GC time (not including suspension) That is Stop-Start.  
        // This only applies to 2.0. I could just set the type to Background GC but I'd rather not disturb
        // the code that handles background GC.
        public bool isConcurrentGC;

        public GCProcess Parent;                //process that did that GC
        public double PauseDurationMSec;

        public double PauseStartRelativeMSec;

        public List<GCPerHeapHistoryTraceData> PerHeapHistories;

        // The dictionary of heap number and info on time it takes to mark various roots.
        public Dictionary<int, MarkInfo> PerHeapMarkTimes;

        public Dictionary<ulong, long> PinnedObjects = new Dictionary<ulong, long>();

        public List<PinnedPlug> PinnedPlugs = new List<PinnedPlug>();

        // For background GC we need to remember when the GC before it ended because
        // when we get the GCStop event some foreground GCs may have happened.
        public float ProcessCpuAtLastGC;

        //  Total time EE is suspended (can be less than GC time for background)
        // The amount of CPU time the process consumed since the last GC.
        public float ProcessCpuMSec;

        public GCReason Reason;

        public long totalPinnedPlugSize;

        public long totalUserPinnedPlugSize;

        //  Set in GCStop(Generation 0, 1 or 2)
        public GCType Type;

        private GCCondemnedReasons[] _PerHeapCondemnedReasons;

        private GCPerHeapHistoryGenData[][] _PerHeapGenData;

        #endregion

        #region Private Fields
        //  Set in GCStart
        private double _TotalGCTimeMSec = -1;

        //  Set in GCStart
        // When we are using Server GC we store the CPU spent on each thread
        // so we can see if there's an imbalance. We concurrently don't do this
        // for server background GC as the imbalance there is much less important.
        int heapCount = -1;

        private long pinnedObjectSizes;

        //  Set in GCStart
        //  Set in GCStart
        //list of workload histories per server GC heap
        private List<ServerGcHistory> ServerGcHeapHistories = new List<ServerGcHistory>();

        private PerHeapEventVersion Version = PerHeapEventVersion.V0;
        #endregion

        #region Constructors
        public GCEvent(GCProcess owningProcess)
        {
            Parent = owningProcess;
            heapCount = owningProcess.heapCount;

            if (heapCount > 1)
            {
                GCCpuServerGCThreads = new float[heapCount];
            }

            pinnedObjectSizes = -1;
            totalPinnedPlugSize = -1;
            totalUserPinnedPlugSize = -1;
            duplicatedPinningReports = 0;
        }

        #endregion

        #region Private Enums
        private enum InducedType
        {
            Blocking = 1,
            NotForced = 2,
        }

        // TODO: get rid of the remaining version checking here - convert the leftover checks with using the Has* methods 
        // to determine whether that particular data is available.
        private enum PerHeapEventVersion
        {
            V0, // Not set
            V4_0,
            V4_5,
            V4_6,
        }

        #endregion

        #region Public Properties
        public double AllocedSinceLastGCMB
        {
            get
            {
                return GetUserAllocated(Gens.Gen0) + GetUserAllocated(Gens.GenLargeObj);
            }
        }

        public double AllocRateMBSec { get { return AllocedSinceLastGCMB * 1000.0 / DurationSinceLastRestartMSec; } }

        public double CondemnedMB
        {
            get
            {
                double ret = GenSizeBeforeMB(0);
                if (1 <= GCGeneration)
                    ret += GenSizeBeforeMB(Gens.Gen1);
                if (2 <= GCGeneration)
                    ret += GenSizeBeforeMB(Gens.Gen2) + GenSizeBeforeMB(Gens.GenLargeObj);
                return ret;
            }
        }

        // Index into the list of GC events
        // The list that contains this event
        private List<GCEvent> Events
        {
            get
            {
                return Parent.Events.OfType<GCEvent>().ToList();
            }
        }

        public double FragmentationMB
        {
            get
            {
                double ret = 0;
                for (Gens gen = Gens.Gen0; gen <= Gens.GenLargeObj; gen++)
                    ret += GenFragmentationMB(gen);
                return ret;
            }
        }

        public int Generation => GCNumber;

        public bool HasServerGcThreadingInfo
        {
            get
            {
                foreach (var heap in ServerGcHeapHistories)
                {
                    if (heap.SampleSpans.Count > 0 || heap.SwitchSpans.Count > 0)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// This include fragmentation
        /// </summary>
        public double HeapSizeAfterMB
        {
            get
            {
                if (null != HeapStats)
                {
                    return (HeapStats.GenerationSize0 + HeapStats.GenerationSize1 + HeapStats.GenerationSize2 + HeapStats.GenerationSize3) / 1000000.0;
                }
                else
                {
                    return -1.0;
                }
            }
        }

        public double HeapSizeBeforeMB
        {
            get
            {
                double ret = 0;
                for (Gens gen = Gens.Gen0; gen <= Gens.GenLargeObj; gen++)
                    ret += GenSizeBeforeMB(gen);
                return ret;
            }
        }

        public double HeapSizePeakMB
        {
            get
            {
                var ret = HeapSizeBeforeMB;
                if (Type == GCType.BackgroundGC)
                {
                    var BgGcEndedRelativeMSec = PauseStartRelativeMSec + GCDurationMSec;
                    for (int i = Index + 1; i < Events.Count; i++)
                    {
                        var _event = Events[i];
                        if (BgGcEndedRelativeMSec < _event.PauseStartRelativeMSec)
                            break;
                        ret = Math.Max(ret, _event.HeapSizeBeforeMB);
                    }
                }
                return ret;
            }
        }

        //  Set in GCStart (starts at 1, unique for process)
        // Of all the CPU, how much as a percentage is spent in the GC since end of last GC.
        public double PercentTimeInGC { get { return (GetTotalGCTime() * 100 / ProcessCpuMSec); } }
        public double PromotedMB
        {
            get
            {
                return (HeapStats.TotalPromotedSize0 + HeapStats.TotalPromotedSize1 +
                       HeapStats.TotalPromotedSize2 + HeapStats.TotalPromotedSize3) / 1000000.0;
            }
        }

        public double RatioPeakAfter { get { if (HeapSizeAfterMB == 0) return 0; return HeapSizePeakMB / HeapSizeAfterMB; } }

        private GCCondemnedReasons[] PerHeapCondemnedReasons
        {
            get
            {
                if ((PerHeapHistories != null) && (_PerHeapCondemnedReasons == null))
                {
                    GetVersion();

                    int NumHeaps = PerHeapHistories.Count;
                    _PerHeapCondemnedReasons = new GCCondemnedReasons[NumHeaps];

                    for (int HeapIndex = 0; HeapIndex < NumHeaps; HeapIndex++)
                    {
                        _PerHeapCondemnedReasons[HeapIndex] = new GCCondemnedReasons();
                        _PerHeapCondemnedReasons[HeapIndex].EncodedReasons.Reasons = PerHeapHistories[HeapIndex].CondemnReasons0;
                        if (Version != PerHeapEventVersion.V4_0)
                        {
                            _PerHeapCondemnedReasons[HeapIndex].EncodedReasons.ReasonsEx = PerHeapHistories[HeapIndex].CondemnReasons1;
                        }
                        _PerHeapCondemnedReasons[HeapIndex].CondemnedReasonGroups = new byte[(int)CondemnedReasonGroup.CRG_Max];
                        _PerHeapCondemnedReasons[HeapIndex].Decode(Version);
                    }
                }

                return _PerHeapCondemnedReasons;
            }
        }

        // There's a list of things we need to get from the events we collected. 
        // To increase the efficiency so we don't need to go back to TraceEvent
        // too often we construct the generation data all at once here.
        private GCPerHeapHistoryGenData[][] PerHeapGenData
        {
            get
            {
                if ((PerHeapHistories != null) && (_PerHeapGenData == null))
                {
                    GetVersion();

                    int NumHeaps = PerHeapHistories.Count;
                    _PerHeapGenData = new GCPerHeapHistoryGenData[NumHeaps][];
                    for (int HeapIndex = 0; HeapIndex < NumHeaps; HeapIndex++)
                    {
                        _PerHeapGenData[HeapIndex] = new GCPerHeapHistoryGenData[(int)Gens.GenLargeObj + 1];
                        for (Gens GenIndex = Gens.Gen0; GenIndex <= Gens.GenLargeObj; GenIndex++)
                        {
                            _PerHeapGenData[HeapIndex][(int)GenIndex] = PerHeapHistories[HeapIndex].GenData(GenIndex);
                        }
                    }
                }

                return _PerHeapGenData;
            }
        }

        #endregion

        #region Public Methods
        public void AddLOHWaitThreadInfo(int TID, double time, int reason, bool IsStart)
        {
#if HAS_PRIVATE_GC_EVENTS
            BGCAllocWaitReason ReasonLOHAlloc = (BGCAllocWaitReason)reason;

            if ((ReasonLOHAlloc == BGCAllocWaitReason.GetLOHSeg) ||
                (ReasonLOHAlloc == BGCAllocWaitReason.AllocDuringSweep))
            {
                if (LOHWaitThreads == null)
                {
                    LOHWaitThreads = new Dictionary<int, BGCAllocWaitInfo>();
                }

                BGCAllocWaitInfo info;

                if (LOHWaitThreads.TryGetValue(TID, out info))
                {
                    if (IsStart)
                    {
                        // If we are finding the value it means we are hitting the small
                        // window where BGC sweep finished and BGC itself finished, discard
                        // this.
                    }
                    else
                    {
                        Debug.Assert(info.Reason == ReasonLOHAlloc);
                        info.WaitStopRelativeMSec = time;
                    }
                }
                else
                {
                    info = new BGCAllocWaitInfo();
                    if (IsStart)
                    {
                        info.Reason = ReasonLOHAlloc;
                        info.WaitStartRelativeMSec = time;
                    }
                    else
                    {
                        // We are currently not displaying this because it's incomplete but I am still adding 
                        // it so we could display if we want to.
                        info.WaitStopRelativeMSec = time;
                    }

                    LOHWaitThreads.Add(TID, info);
                }
            }
#endif
        }

        public void AddServerGcSample(ThreadWorkSpan sample)
        {
            if (sample.ProcessorNumber >= 0 && sample.ProcessorNumber < ServerGcHeapHistories.Count)
                ServerGcHeapHistories[sample.ProcessorNumber].AddSampleEvent(sample);
        }

        public void AddServerGcThreadSwitch(ThreadWorkSpan cswitch)
        {
            if (cswitch.ProcessorNumber >= 0 && cswitch.ProcessorNumber < ServerGcHeapHistories.Count)
                ServerGcHeapHistories[cswitch.ProcessorNumber].AddSwitchEvent(cswitch);
        }

        public void AddServerGCThreadTime(int heapIndex, float cpuMSec)
        {
            if (GCCpuServerGCThreads != null)
                GCCpuServerGCThreads[heapIndex] += cpuMSec;
        }

        // Unfortunately sometimes we just don't get mark events from all heaps, even for GCs that we have seen GCStart for.
        // So accommodating this scenario.
        public bool AllHeapsSeenMark()
        {
            if (PerHeapMarkTimes != null)
                return (heapCount == PerHeapMarkTimes.Count);
            else
                return false;
        }

        public bool DetailedGenDataAvailable()
        {
            return (PerHeapHistories != null);
        }

        public void GCEnd()
        {
            ConvertMarkTimes();

            if (ServerGcHeapHistories != null)
            {
                foreach (var serverHeap in ServerGcHeapHistories)
                {
                    serverHeap.GCEnd();
                }
            }
        }

        public double GenBudgetMB(Gens gen)
        {
            if (PerHeapHistories == null)
                return double.NaN;
            double budget = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                budget += PerHeapGenData[HeapIndex][(int)gen].Budget / 1000000.0;
            return budget;
        }

        public double GenFragmentationMB(Gens gen)
        {
            if (PerHeapHistories == null)
                return double.NaN;
            double ret = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                ret += PerHeapGenData[HeapIndex][(int)gen].Fragmentation / 1000000.0;
            return ret;
        }

        public double GenFragmentationPercent(Gens gen)
        {
            return (GenFragmentationMB(gen) * 100.0 / GenSizeAfterMB(gen));
        }

        public double GenFreeListAfter(Gens gen)
        {
            if (PerHeapHistories == null)
                return double.NaN;
            double ret = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                ret += PerHeapGenData[HeapIndex][(int)gen].FreeListSpaceAfter;
            return ret;
        }

        public double GenFreeListBefore(Gens gen)
        {
            if (PerHeapHistories == null)
                return double.NaN;
            double ret = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                ret += PerHeapGenData[HeapIndex][(int)gen].FreeListSpaceBefore;
            return ret;
        }

        public double GenInMB(Gens gen)
        {
            if (PerHeapHistories == null)
                return double.NaN;
            double ret = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                ret += PerHeapGenData[HeapIndex][(int)gen].In / 1000000.0;
            return ret;
        }

        public double GenNonePinnedSurv(Gens gen)
        {
            if ((PerHeapHistories == null) || !(PerHeapGenData[0][0].HasNonePinnedSurv()))
                return double.NaN;
            double ret = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                ret += PerHeapGenData[HeapIndex][(int)gen].NonePinnedSurv;
            return ret;
        }

        public double GenOut(Gens gen)
        {
            if (PerHeapHistories == null)
                return double.NaN;
            double ret = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                ret += PerHeapGenData[HeapIndex][(int)gen].Out;
            return ret;
        }

        public double GenOutMB(Gens gen)
        {
            if (PerHeapHistories == null)
                return double.NaN;
            double ret = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                ret += PerHeapGenData[HeapIndex][(int)gen].Out / 1000000.0;
            return ret;
        }

        public double GenPinnedSurv(Gens gen)
        {
            if ((PerHeapHistories == null) || !(PerHeapGenData[0][0].HasPinnedSurv()))
                return double.NaN;
            double ret = 0.0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                ret += PerHeapGenData[HeapIndex][(int)gen].PinnedSurv;
            return ret;
        }

        // Note that in 4.0 TotalPromotedSize is not entirely accurate (since it doesn't
        // count the pins that got demoted. We could consider using the PerHeap event data
        // to compute the accurate promoted size. 
        // In 4.5 this is accurate.
        public double GenPromotedMB(Gens gen)
        {
            if (gen == Gens.GenLargeObj)
                return HeapStats.TotalPromotedSize3 / 1000000.0;
            if (gen == Gens.Gen2)
                return HeapStats.TotalPromotedSize2 / 1000000.0;
            if (gen == Gens.Gen1)
                return HeapStats.TotalPromotedSize1 / 1000000.0;
            if (gen == Gens.Gen0)
                return HeapStats.TotalPromotedSize0 / 1000000.0;
            Debug.Assert(false);
            return double.NaN;
        }

        public double GenSizeAfterMB(Gens gen)
        {
            if (gen == Gens.GenLargeObj)
                return HeapStats.GenerationSize3 / 1000000.0;
            if (gen == Gens.Gen2)
                return HeapStats.GenerationSize2 / 1000000.0;
            if (gen == Gens.Gen1)
                return HeapStats.GenerationSize1 / 1000000.0;
            if (gen == Gens.Gen0)
                return HeapStats.GenerationSize0 / 1000000.0;
            Debug.Assert(false);
            return double.NaN;
        }

        // Per generation stats.  
        public double GenSizeBeforeMB(Gens gen)
        {
            if (PerHeapHistories != null)
            {
                double ret = 0.0;
                for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                    ret += PerHeapGenData[HeapIndex][(int)gen].SizeBefore / 1000000.0;
                return ret;
            }

            // When we don't have perheap history we can only estimate for gen0 and gen3.
            double Gen0SizeBeforeMB = 0;
            if (gen == Gens.Gen0)
                Gen0SizeBeforeMB = AllocedSinceLastGCBasedOnAllocTickMB[0];
            if (Index == 0)
            {
                return ((gen == Gens.Gen0) ? Gen0SizeBeforeMB : 0);
            }

            // Find a previous HeapStats.  
            GCHeapStatsTraceData heapStats = null;
            for (int j = Index - 1; ; --j)
            {
                if (j == 0)
                    return 0;
                heapStats = Events[j].HeapStats;
                if (heapStats != null)
                    break;
            }
            if (gen == Gens.Gen0)
                return Math.Max((heapStats.GenerationSize0 / 1000000.0), Gen0SizeBeforeMB);
            if (gen == Gens.Gen1)
                return heapStats.GenerationSize1 / 1000000.0;
            if (gen == Gens.Gen2)
                return heapStats.GenerationSize2 / 1000000.0;

            Debug.Assert(gen == Gens.GenLargeObj);

            if (HeapStats != null)
                return Math.Max(heapStats.GenerationSize3, HeapStats.GenerationSize3) / 1000000.0;
            else
                return heapStats.GenerationSize3 / 1000000.0;
        }

        public void GetCondemnedReasons(Dictionary<CondemnedReasonGroup, int> ReasonsInfo)
        {
            // Older versions of the runtime does not have this event. So even for a complete GC, we may not have this
            // info.
            if (PerHeapCondemnedReasons == null)
                return;

            int HeapIndexHighestGen = 0;
            if (PerHeapCondemnedReasons.Length != 1)
            {
                HeapIndexHighestGen = FindFirstHighestCondemnedHeap();
            }

            byte[] ReasonGroups = PerHeapCondemnedReasons[HeapIndexHighestGen].CondemnedReasonGroups;

            // These 2 reasons indicate a gen number. If the number is the same as the condemned gen, we 
            // include this reason.
            for (int i = (int)CondemnedReasonGroup.CRG_Alloc_Exceeded; i <= (int)CondemnedReasonGroup.CRG_Time_Tuning; i++)
            {
                if (ReasonGroups[i] == GCGeneration)
                    AddCondemnedReason(ReasonsInfo, (CondemnedReasonGroup)i);
            }

            if (ReasonGroups[(int)CondemnedReasonGroup.CRG_Induced] != 0)
            {
                if (ReasonGroups[(int)CondemnedReasonGroup.CRG_Initial_Generation] == GCGeneration)
                {
                    AddCondemnedReason(ReasonsInfo, CondemnedReasonGroup.CRG_Induced);
                }
            }

            // The rest of the reasons are conditions so include the ones that are set.
            for (int i = (int)CondemnedReasonGroup.CRG_Low_Ephemeral; i < (int)CondemnedReasonGroup.CRG_Max; i++)
            {
                if (ReasonGroups[i] != 0)
                    AddCondemnedReason(ReasonsInfo, (CondemnedReasonGroup)i);
            }
        }

        //
        // Approximations we do in this function for V4_5 and prior:
        // On 4.0 we didn't seperate free list from free obj, so we just use fragmentation (which is the sum)
        // as an approximation. This makes the efficiency value a bit larger than it actually is.
        // We don't actually update in for the older gen - this means we only know the out for the younger 
        // gen which isn't necessarily all allocated into the older gen. So we could see cases where the 
        // out is > 0, yet the older gen's free list doesn't change. Using the younger gen's out as an 
        // approximation makes the efficiency value larger than it actually is.
        //
        // For V4_6 this requires no approximation.
        //
        public bool GetFreeListEfficiency(Gens gen, ref double Allocated, ref double FreeListConsumed)
        {
            // I am not worried about gen0 or LOH's free list efficiency right now - it's 
            // calculated differently.
            if ((PerHeapHistories == null) ||
                (gen == Gens.Gen0) ||
                (gen == Gens.GenLargeObj) ||
                (Index <= 0) ||
                !(PerHeapHistories[0].VersionRecognized))
            {
                return false;
            }

            int YoungerGen = (int)gen - 1;

            if (GCGeneration != YoungerGen)
                return false;

            if (PerHeapHistories[0].V4_6)
            {
                Allocated = 0;
                FreeListConsumed = 0;
                for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
                {
                    GCPerHeapHistoryTraceData3 hist = (GCPerHeapHistoryTraceData3)PerHeapHistories[HeapIndex];
                    Allocated += hist.FreeListAllocated;
                    FreeListConsumed += hist.FreeListAllocated + hist.FreeListRejected;
                }
                return true;
            }

            // I am not using MB here because what's promoted from gen1 can easily be less than a MB.
            double YoungerGenOut = 0;
            double FreeListBefore = 0;
            double FreeListAfter = 0;
            // Includes fragmentation. This lets us know if we had to expand the size.
            double GenSizeBefore = 0;
            double GenSizeAfter = 0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
            {
                YoungerGenOut += PerHeapGenData[HeapIndex][YoungerGen].Out;
                GenSizeBefore += PerHeapGenData[HeapIndex][(int)gen].SizeBefore;
                GenSizeAfter += PerHeapGenData[HeapIndex][(int)gen].SizeAfter;
                if (Version == PerHeapEventVersion.V4_0)
                {
                    // Occasionally I've seen a GC in the middle that simply missed some events,
                    // some of which are PerHeap hist events so we don't have data.
                    if (Events[Index - 1].PerHeapGenData == null)
                        return false;
                    FreeListBefore += Events[Index - 1].PerHeapGenData[HeapIndex][(int)gen].Fragmentation;
                    FreeListAfter += PerHeapGenData[HeapIndex][(int)gen].Fragmentation;
                }
                else
                {
                    FreeListBefore += PerHeapGenData[HeapIndex][(int)gen].FreeListSpaceBefore;
                    FreeListAfter += PerHeapGenData[HeapIndex][(int)gen].FreeListSpaceAfter;
                }
            }

            double GenSizeGrown = GenSizeAfter - GenSizeBefore;

            // This is the most accurate situation we can calculuate (if it's not accurate it means
            // we are over estimating which is ok.
            if ((GenSizeGrown == 0) && ((FreeListBefore > 0) && (FreeListAfter >= 0)))
            {
                Allocated = YoungerGenOut;
                FreeListConsumed = FreeListBefore - FreeListAfter;
                // We don't know how much of the survived is pinned so we are overestimating here.
                if (Allocated < FreeListConsumed)
                    return true;
            }

            return false;
        }

        public void GetGenDataObjSizeAfterMB(ref double[] GenData)
        {
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
            {
                for (int GenIndex = 0; GenIndex <= (int)Gens.GenLargeObj; GenIndex++)
                    GenData[GenIndex] += PerHeapGenData[HeapIndex][GenIndex].ObjSizeAfter / 1000000.0;
            }
        }

        public void GetGenDataSizeAfterMB(ref double[] GenData)
        {
            for (int GenIndex = 0; GenIndex <= (int)Gens.GenLargeObj; GenIndex++)
                GenData[GenIndex] = GenSizeAfterMB((Gens)GenIndex);
        }

        public double GetMaxGen0ObjSizeMB()
        {
            double MaxGen0ObjSize = 0;
            for (int HeapIndex = 0; HeapIndex < PerHeapHistories.Count; HeapIndex++)
            {
                MaxGen0ObjSize = Math.Max(MaxGen0ObjSize, PerHeapGenData[HeapIndex][(int)Gens.Gen0].ObjSizeAfter / 1000000.0);
            }
            return MaxGen0ObjSize;
        }

        // This represents the percentage time spent paused for this GC since the last GC completed.
        public double GetPauseTimePercentageSinceLastGC()
        {
            double pauseTimePercentage;

            if (Type == GCType.BackgroundGC)
            {
                // Find all GCs that occurred during the current background GC.
                double startTimeRelativeMSec = this.GCStartRelativeMSec;
                double endTimeRelativeMSec = this.GCStartRelativeMSec + this.GCDurationMSec;

                // Calculate the pause time for this BGC.
                // Pause time is defined as pause time for the BGC + pause time for all FGCs that ran during the BGC.
                double totalPauseTime = this.PauseDurationMSec;

                if (Index + 1 < Events.Count)
                {
                    GCEvent gcEvent;
                    for (int i = Index + 1; i < Events.Count; ++i)
                    {
                        gcEvent = Events[i];
                        if ((gcEvent.GCStartRelativeMSec >= startTimeRelativeMSec) && (gcEvent.GCStartRelativeMSec < endTimeRelativeMSec))
                        {
                            totalPauseTime += gcEvent.PauseDurationMSec;
                        }
                        else
                        {
                            // We've finished processing all FGCs that occurred during this BGC.
                            break;
                        }
                    }
                }

                // Get the elapsed time since the previous GC finished.
                int previousGCIndex = Index - 1;
                double previousGCStopTimeRelativeMSec;
                if (previousGCIndex >= 0)
                {
                    GCEvent previousGCEvent = Events[previousGCIndex];
                    previousGCStopTimeRelativeMSec = previousGCEvent.GCStartRelativeMSec + previousGCEvent.GCDurationMSec;
                }
                else
                {
                    // Backstop in case this is the first GC.
                    previousGCStopTimeRelativeMSec = Events[0].GCStartRelativeMSec;
                }

                double totalTime = (GCStartRelativeMSec + GCDurationMSec) - previousGCStopTimeRelativeMSec;
                pauseTimePercentage = (totalPauseTime * 100) / (totalTime);
            }
            else
            {
                double totalTime = PauseDurationMSec + DurationSinceLastRestartMSec;
                pauseTimePercentage = (PauseDurationMSec * 100) / (totalTime);
            }

            Debug.Assert(pauseTimePercentage <= 100);
            return pauseTimePercentage;
        }

        public int GetPinnedObjectPercentage()
        {
            if (totalPinnedPlugSize == -1)
            {
                totalPinnedPlugSize = 0;
                totalUserPinnedPlugSize = 0;

                foreach (KeyValuePair<ulong, long> item in PinnedObjects)
                {
                    ulong Address = item.Key;

                    for (int i = 0; i < PinnedPlugs.Count; i++)
                    {
                        if ((Address >= PinnedPlugs[i].Start) && (Address < PinnedPlugs[i].End))
                        {
                            PinnedPlugs[i].PinnedByUser = true;
                            break;
                        }
                    }
                }

                for (int i = 0; i < PinnedPlugs.Count; i++)
                {
                    long Size = (long)(PinnedPlugs[i].End - PinnedPlugs[i].Start);
                    totalPinnedPlugSize += Size;
                    if (PinnedPlugs[i].PinnedByUser)
                    {
                        totalUserPinnedPlugSize += Size;
                    }
                }
            }

            return ((totalPinnedPlugSize == 0) ? -1 : (int)((double)pinnedObjectSizes * 100 / (double)totalPinnedPlugSize));
        }

        public long GetPinnedObjectSizes()
        {
            if (pinnedObjectSizes == -1)
            {
                pinnedObjectSizes = 0;
                foreach (KeyValuePair<ulong, long> item in PinnedObjects)
                {
                    pinnedObjectSizes += item.Value;
                }
            }
            return pinnedObjectSizes;
        }

        public double GetTotalGCTime()
        {
            if (_TotalGCTimeMSec < 0)
            {
                _TotalGCTimeMSec = 0;
                if (GCCpuServerGCThreads != null)
                {
                    for (int i = 0; i < GCCpuServerGCThreads.Length; i++)
                    {
                        _TotalGCTimeMSec += GCCpuServerGCThreads[i];
                    }
                }
                _TotalGCTimeMSec += GCCpuMSec;
            }

            Debug.Assert(_TotalGCTimeMSec >= 0);
            return _TotalGCTimeMSec;
        }

        /// <summary>
        /// Get what's allocated into gen0 or gen3. For server GC this gets the total for 
        /// all heaps.
        /// </summary>
        public double GetUserAllocated(Gens gen)
        {
            Debug.Assert((gen == Gens.Gen0) || (gen == Gens.GenLargeObj));

            if ((Type == GCType.BackgroundGC) && (gen == Gens.Gen0))
            {
                return AllocedSinceLastGCBasedOnAllocTickMB[(int)gen];
            }

            if (PerHeapHistories != null && Index > 0 && Events[Index - 1].PerHeapHistories != null)
            {
                double TotalAllocated = 0;
                if (Index > 0)
                {
                    for (int i = 0; i < PerHeapHistories.Count; i++)
                    {
                        double Allocated = GetUserAllocatedPerHeap(i, gen);

                        TotalAllocated += Allocated / 1000000.0;
                    }

                    return TotalAllocated;
                }
                else
                {
                    return GenSizeBeforeMB(gen);
                }
            }

            return AllocedSinceLastGCBasedOnAllocTickMB[(gen == Gens.Gen0) ? 0 : 1];
        }

        public bool IsLowEphemeral()
        {
            return CondemnedReasonGroupSet(CondemnedReasonGroup.CRG_Low_Ephemeral);
        }

        public bool IsNotCompacting()
        {
            return ((GlobalHeapHistory.GlobalMechanisms & (GCGlobalMechanisms.Compaction)) != 0);
        }

        public double ObjSizeAfter(Gens gen)
        {
            double TotalObjSizeAfter = 0;

            if (PerHeapHistories != null)
            {
                for (int i = 0; i < PerHeapHistories.Count; i++)
                {
                    TotalObjSizeAfter += PerHeapGenData[i][(int)gen].ObjSizeAfter;
                }
            }

            return TotalObjSizeAfter;
        }

        // Set in HeapStats
        public void SetHeapCount(int count)
        {
            if (heapCount == -1)
            {
                heapCount = count;
            }
        }
        public void SetUpServerGcHistory()
        {
            for (int i = 0; i < heapCount; i++)
            {
                int gcThreadId = 0;
                int gcThreadPriority = 0;
                Parent.ServerGcHeap2ThreadId.TryGetValue(i, out gcThreadId);
                Parent.ThreadId2Priority.TryGetValue(gcThreadId, out gcThreadPriority);
                ServerGcHeapHistories.Add(new ServerGcHistory
                {
                    Parent = this,
                    ProcessId = Parent.ProcessID,
                    HeapId = i,
                    GcWorkingThreadId = gcThreadId,
                    GcWorkingThreadPriority = gcThreadPriority
                });
            }
        }

        public double SurvivalPercent(Gens gen)
        {
            double retSurvRate = double.NaN;

            long SurvRate = 0;

            if (gen == Gens.GenLargeObj)
            {
                if (GCGeneration < 2)
                {
                    return retSurvRate;
                }
            }
            else if ((int)gen > GCGeneration)
            {
                return retSurvRate;
            }

            if (PerHeapHistories != null)
            {
                for (int i = 0; i < PerHeapHistories.Count; i++)
                {
                    SurvRate += PerHeapGenData[i][(int)gen].SurvRate;
                }

                SurvRate /= PerHeapHistories.Count;
            }

            retSurvRate = SurvRate;

            return retSurvRate;
        }

#endregion

        #region Internal Methods
        internal void AddGcJoin(GCJoinTraceData data)
        {
            if (data.Heap >= 0 && data.Heap < ServerGcHeapHistories.Count)
                ServerGcHeapHistories[data.Heap].AddJoin(data);
            else
            {
                foreach (var heap in ServerGcHeapHistories)
                    heap.AddJoin(data);
            }

        }
        #endregion

        #region Private Methods
        private void AddCondemnedReason(Dictionary<CondemnedReasonGroup, int> ReasonsInfo, CondemnedReasonGroup Reason)
        {
            if (!ReasonsInfo.ContainsKey(Reason))
                ReasonsInfo.Add(Reason, 1);
            else
                (ReasonsInfo[Reason])++;
        }

        // For true/false groups, return whether that group is set.
        private bool CondemnedReasonGroupSet(CondemnedReasonGroup Group)
        {
            if (PerHeapCondemnedReasons == null)
            {
                return false;
            }

            int HeapIndexHighestGen = 0;
            if (PerHeapCondemnedReasons.Length != 1)
            {
                HeapIndexHighestGen = FindFirstHighestCondemnedHeap();
            }

            return (PerHeapCondemnedReasons[HeapIndexHighestGen].CondemnedReasonGroups[(int)Group] != 0);
        }

        // We recorded these as the timestamps when we saw the mark events, now convert them 
        // to the actual time that it took for each mark.
        private void ConvertMarkTimes()
        {
            if (PerHeapMarkTimes != null)
            {
                foreach (KeyValuePair<int, MarkInfo> item in PerHeapMarkTimes)
                {
                    if (item.Value.MarkTimes[(int)MarkRootType.MarkSizedRef] == 0.0)
                        item.Value.MarkTimes[(int)MarkRootType.MarkSizedRef] = GCStartRelativeMSec;

                    if (GCGeneration == 2)
                        item.Value.MarkTimes[(int)MarkRootType.MarkOlder] = 0;
                    else
                        item.Value.MarkTimes[(int)MarkRootType.MarkOlder] -= item.Value.MarkTimes[(int)MarkRootType.MarkHandles];

                    item.Value.MarkTimes[(int)MarkRootType.MarkHandles] -= item.Value.MarkTimes[(int)MarkRootType.MarkFQ];
                    item.Value.MarkTimes[(int)MarkRootType.MarkFQ] -= item.Value.MarkTimes[(int)MarkRootType.MarkStack];
                    item.Value.MarkTimes[(int)MarkRootType.MarkStack] -= item.Value.MarkTimes[(int)MarkRootType.MarkSizedRef];
                    item.Value.MarkTimes[(int)MarkRootType.MarkSizedRef] -= GCStartRelativeMSec;
                }
            }
        }

        // When survival rate is 0, for certain releases (see comments for GetUserAllocatedPerHeap)
        // we need to estimate.
        private double EstimateAllocSurv0(int HeapIndex, Gens gen)
        {
            if (HasAllocTickEvents)
            {
                return AllocedSinceLastGCBasedOnAllocTickMB[(gen == Gens.Gen0) ? 0 : 1];
            }
            else
            {
                if (Index > 0)
                {
                    // If the prevous GC has that heap get its size.  
                    var perHeapGenData = Events[Index - 1].PerHeapGenData;
                    if (HeapIndex < perHeapGenData.Length)
                        return perHeapGenData[HeapIndex][(int)gen].Budget;
                }
                return 0;
            }
        }

        private int FindFirstHighestCondemnedHeap()
        {
            int GenNumberHighest = (int)GCGeneration;
            for (int HeapIndex = 0; HeapIndex < PerHeapCondemnedReasons.Length; HeapIndex++)
            {
                int gen = PerHeapCondemnedReasons[HeapIndex].CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Final_Generation];
                if (gen == GenNumberHighest)
                {
                    return HeapIndex;
                }
            }

            return 0;
        }

        /// <summary>
        /// For a given heap, get what's allocated into gen0 or gen3.
        /// We calculate this differently on 4.0, 4.5 Beta and 4.5 RC+.
        /// The caveat with 4.0 and 4.5 Beta is that when survival rate is 0,
        /// We don't know how to calculate the allocated - so we just use the
        /// last GC's budget (We should indicate this in the tool)
        /// </summary>
        private double GetUserAllocatedPerHeap(int HeapIndex, Gens gen)
        {
            long prevObjSize = 0;
            if (Index > 0)
            {
                // If the prevous GC has that heap get its size.  
                var perHeapGenData = Events[Index - 1].PerHeapGenData;
                if (HeapIndex < perHeapGenData.Length)
                    prevObjSize = perHeapGenData[HeapIndex][(int)gen].ObjSizeAfter;
            }
            GCPerHeapHistoryGenData currentGenData = PerHeapGenData[HeapIndex][(int)gen];
            long survRate = currentGenData.SurvRate;
            long currentObjSize = currentGenData.ObjSizeAfter;
            double Allocated;

            if (Version == PerHeapEventVersion.V4_0)
            {
                if (survRate == 0)
                    Allocated = EstimateAllocSurv0(HeapIndex, gen);
                else
                    Allocated = (currentGenData.Out + currentObjSize) * 100 / survRate - prevObjSize;
            }
            else
            {
                Allocated = currentGenData.ObjSpaceBefore - prevObjSize;
            }

            return Allocated;
        }

        private void GetVersion()
        {
            if (Version == PerHeapEventVersion.V0)
            {
                if (PerHeapHistories[0].V4_0)
                    Version = PerHeapEventVersion.V4_0;
                else if (PerHeapHistories[0].V4_5)
                {
                    Version = PerHeapEventVersion.V4_5;
                    Debug.Assert(PerHeapHistories[0].Version == 2);
                }
                else
                {
                    Version = PerHeapEventVersion.V4_6;
                    Debug.Assert(PerHeapHistories[0].Version == 3);
                }
            }
        }
        #endregion

        #region Inner Private Structs
        private struct EncodedCondemnedReasons
        {
            public int Reasons;
            public int ReasonsEx;
        }
        #endregion

        #region Inner Public Classes
        public class MarkInfo
        {
            public long[] MarkPromoted;

            // Note that in 4.5 and prior (ie, from GCMark events, not GCMarkWithType), the first stage of the time 
            // includes scanning sizedref handles(which can be very significant). We could distinguish that by interpreting 
            // the Join events which I haven't done yet.
            public double[] MarkTimes;
            public MarkInfo(bool initPromoted = true)
            {
                MarkTimes = new double[(int)MarkRootType.MarkMax];
                if (initPromoted)
                    MarkPromoted = new long[(int)MarkRootType.MarkMax];
            }
        };

        public class PinnedPlug
        {
            public ulong End;
            public bool PinnedByUser;
            public ulong Start;
            public PinnedPlug(ulong s, ulong e)
            {
                Start = s;
                End = e;
                PinnedByUser = false;
            }
        };

        public class ServerGcHistory
        {
            public int GcWorkingThreadId;
            public int GcWorkingThreadPriority;
            public int HeapId;
            public GCEvent Parent;
            public int ProcessId;
            public List<GcWorkSpan> SampleSpans = new List<GcWorkSpan>();
            public List<GcWorkSpan> SwitchSpans = new List<GcWorkSpan>();

            //list of times in msc starting from GC start when GCJoin events were fired for this heap
            private List<GcJoin> GcJoins = new List<GcJoin>();
            public enum WorkSpanType
            {
                GcThread,
                RivalThread,
                LowPriThread,
                Idle
            }

            public double TimeBaseMsc { get { return Parent.PauseStartRelativeMSec; } }
            public void AddSampleEvent(ThreadWorkSpan sample)
            {
                GcWorkSpan lastSpan = SampleSpans.Count > 0 ? SampleSpans[SampleSpans.Count - 1] : null;
                if (lastSpan != null && lastSpan.ThreadId == sample.ThreadId && lastSpan.ProcessId == sample.ProcessId)
                {
                    lastSpan.DurationMsc++;
                }
                else
                {
                    SampleSpans.Add(new GcWorkSpan(sample)
                    {
                        Type = GetSpanType(sample),
                        RelativeTimestampMsc = sample.AbsoluteTimestampMsc - TimeBaseMsc,
                        DurationMsc = 1
                    });
                }
            }

            public void AddSwitchEvent(ThreadWorkSpan switchData)
            {
                GcWorkSpan lastSpan = SwitchSpans.Count > 0 ? SwitchSpans[SwitchSpans.Count - 1] : null;
                if (switchData.ThreadId == GcWorkingThreadId && switchData.ProcessId == ProcessId)
                {
                    //update gc thread priority since we have new data
                    GcWorkingThreadPriority = switchData.Priority;
                }

                if (lastSpan != null)
                {
                    //updating duration of the last one, based on a timestamp from the new one
                    lastSpan.DurationMsc = switchData.AbsoluteTimestampMsc - lastSpan.AbsoluteTimestampMsc;

                    //updating wait readon of the last one
                    lastSpan.WaitReason = switchData.WaitReason;
                }

                SwitchSpans.Add(new GcWorkSpan(switchData)
                {
                    Type = GetSpanType(switchData),
                    RelativeTimestampMsc = switchData.AbsoluteTimestampMsc - TimeBaseMsc,
                    Priority = switchData.Priority
                });
            }

            internal void AddJoin(GCJoinTraceData data)
            {
                GcJoins.Add(new GcJoin
                {
                    Heap = data.ProcessorNumber, //data.Heap is not reliable for reset events, so we use ProcessorNumber
                    AbsoluteTimestampMsc = data.TimeStampRelativeMSec,
                    RelativeTimestampMsc = data.TimeStampRelativeMSec - Parent.PauseStartRelativeMSec,
                    Type = data.JoinType,
                    Time = data.JoinTime,
                });
            }

            internal void GCEnd()
            {
                GcWorkSpan lastSpan = SwitchSpans.Count > 0 ? SwitchSpans[SwitchSpans.Count - 1] : null;
                if (lastSpan != null)
                {
                    lastSpan.DurationMsc = Parent.PauseDurationMSec - lastSpan.RelativeTimestampMsc;
                }
            }

            private WorkSpanType GetSpanType(ThreadWorkSpan span)
            {
                if (span.ThreadId == GcWorkingThreadId && span.ProcessId == ProcessId)
                    return WorkSpanType.GcThread;
                if (span.ProcessId == 0)
                    return WorkSpanType.Idle;

                if (span.Priority >= GcWorkingThreadPriority || span.Priority == -1)
                    return WorkSpanType.RivalThread;
                else
                    return WorkSpanType.LowPriThread;
            }

            public class GcJoin
            {
                public double AbsoluteTimestampMsc;
                public int Heap;
                public double RelativeTimestampMsc;
                public GcJoinTime Time;
                public GcJoinType Type;
            }

            public class GcWorkSpan : ThreadWorkSpan
            {
                public double RelativeTimestampMsc;
                public WorkSpanType Type;
                public GcWorkSpan(ThreadWorkSpan span)
                    : base(span)
                {
                }
            }
        }
#endregion

        #region Inner Private Classes
        private class GCCondemnedReasons
        {
            /// <summary>
            /// This records which reasons are used and the value. Since the biggest value
            /// we need to record is the generation number a byte is sufficient.
            /// </summary>
            public byte[] CondemnedReasonGroups;

            public EncodedCondemnedReasons EncodedReasons;

#if HAS_PRIVATE_GC_EVENTS
            public Dictionary<int, BGCAllocWaitInfo> LOHWaitThreads;
#endif

            enum Condemned_Reason_Condition
            {
                CRC_induced_fullgc_p = 0,
                CRC_expand_fullgc_p = 1,
                CRC_high_mem_p = 2,
                CRC_very_high_mem_p = 3,
                CRC_low_ephemeral_p = 4,
                CRC_low_card_p = 5,
                CRC_eph_high_frag_p = 6,
                CRC_max_high_frag_p = 7,
                CRC_max_high_frag_e_p = 8,
                CRC_max_high_frag_m_p = 9,
                CRC_max_high_frag_vm_p = 10,
                CRC_max_gen1 = 11,
                CRC_before_oom = 12,
                CRC_gen2_too_small = 13,
                CRC_induced_noforce_p = 14,
                CRC_before_bgc = 15,
                CRC_max = 16,
            };

            // These values right now are the same as the first 4 in CondemnedReasonGroup.
            enum Condemned_Reason_Generation
            {
                CRG_initial = 0,
                CRG_final_per_heap = 1,
                CRG_alloc_budget = 2,
                CRG_time_tuning = 3,
                CRG_max = 4,
            };
            public void Decode(PerHeapEventVersion Version)
            {
                // First decode the reasons that return us a generation number. 
                // It's the same in 4.0 and 4.5.
                for (Condemned_Reason_Generation i = 0; i < Condemned_Reason_Generation.CRG_max; i++)
                {
                    CondemnedReasonGroups[(int)i] = (byte)GetReasonWithGenNumber(i);
                }

                // Then decode the reasons that just indicate true or false.
                for (Condemned_Reason_Condition i = 0; i < Condemned_Reason_Condition.CRC_max; i++)
                {
                    if (GetReasonWithCondition(i, Version))
                    {
                        switch (i)
                        {
                            case Condemned_Reason_Condition.CRC_induced_fullgc_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Induced] = (byte)InducedType.Blocking;
                                break;
                            case Condemned_Reason_Condition.CRC_induced_noforce_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Induced] = (byte)InducedType.NotForced;
                                break;
                            case Condemned_Reason_Condition.CRC_low_ephemeral_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Low_Ephemeral] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_low_card_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Internal_Tuning] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_eph_high_frag_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Fragmented_Ephemeral] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_max_high_frag_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Fragmented_Gen2] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_max_high_frag_e_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Fragmented_Gen1_To_Gen2] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_max_high_frag_m_p:
                            case Condemned_Reason_Condition.CRC_max_high_frag_vm_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Fragmented_Gen2_High_Mem] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_max_gen1:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Alloc_Exceeded] = 2;
                                break;
                            case Condemned_Reason_Condition.CRC_expand_fullgc_p:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Expand_Heap] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_before_oom:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_GC_Before_OOM] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_gen2_too_small:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Too_Small_For_BGC] = 1;
                                break;
                            case Condemned_Reason_Condition.CRC_before_bgc:
                                CondemnedReasonGroups[(int)CondemnedReasonGroup.CRG_Ephemeral_Before_BGC] = 1;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected reason");
                                break;
                        }
                    }
                }
            }

            private bool GetReasonWithCondition(Condemned_Reason_Condition Reason_Condition, PerHeapEventVersion Version)
            {
                bool ConditionIsSet = false;
                if (Version == PerHeapEventVersion.V4_0)
                {
                    Debug.Assert((int)Reason_Condition < 16);
                    ConditionIsSet = ((EncodedReasons.Reasons & (1 << (int)(Reason_Condition + 16))) != 0);
                }
                else
                {
                    ConditionIsSet = ((EncodedReasons.ReasonsEx & (1 << (int)Reason_Condition)) != 0);
                }
                return ConditionIsSet;
            }

            private int GetReasonWithGenNumber(Condemned_Reason_Generation Reason_GenNumber)
            {
                int GenNumber = ((EncodedReasons.Reasons >> ((int)Reason_GenNumber * 2)) & 0x3);
                return GenNumber;
            }
        }
        #endregion
    }
}

#endif
