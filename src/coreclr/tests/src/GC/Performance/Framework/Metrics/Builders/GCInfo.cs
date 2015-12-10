// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if WINDOWS

namespace GCPerfTestFramework.Metrics.Builders
{
    /// <summary>
    /// GCInfo are accumulated statistics per generation.  
    /// </summary>    
    internal class GCInfo
    {
        public int GCCount;
        public int NumInduced;
        public long PinnedObjectSizes;
        public int PinnedObjectPercentage;
        public long NumGCWithPinEvents;
        public long NumGCWithPinPlugEvents;
        public double MaxPauseDurationMSec;
        public double MeanPauseDurationMSec { get { return TotalPauseTimeMSec / GCCount; } }
        public double MeanSizeAfterMB { get { return TotalSizeAfterMB / GCCount; } }
        public double MeanSizePeakMB { get { return TotalSizePeakMB / GCCount; } }
        public double MeanAllocatedMB { get { return TotalAllocatedMB / GCCount; } }
        public double RatioMeanPeakAfter { get { return MeanSizePeakMB / MeanSizeAfterMB; } }
        public double MeanGCCpuMSec { get { return TotalGCCpuMSec / GCCount; } }

        public double TotalPauseTimeMSec;
        public double MaxSuspendDurationMSec;
        public double MaxSizePeakMB;
        public double MaxAllocRateMBSec;

        public double TotalAllocatedMB;
        public double TotalGCCpuMSec;
        public double TotalPromotedMB;

        // these do not have a useful meaning so we hide them. 
        internal double TotalSizeAfterMB;
        internal double TotalSizePeakMB;
    }
}

#endif