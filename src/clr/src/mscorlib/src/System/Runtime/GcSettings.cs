// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace System.Runtime
{
    // These settings are the same format as in clr\src\vm\gcpriv.h
    // make sure you change that file if you change this file!

    public enum GCLargeObjectHeapCompactionMode
    {
        Default = 1,
        CompactOnce = 2
    }

    public enum GCLatencyMode
    {
        Batch = 0,
        Interactive = 1,
        LowLatency = 2,
        SustainedLowLatency = 3,
        NoGCRegion = 4
    }

    public static class GCSettings
    {
        private enum SetLatencyModeStatus
        {
            Succeeded = 0,
            NoGCInProgress = 1 // NoGCRegion is in progress, can't change pause mode.
        };

        public static GCLatencyMode LatencyMode
        {
            get
            {
                return (GCLatencyMode)(GC.GetGCLatencyMode());
            }

            // We don't want to allow this API when hosted.
            set
            {
                if ((value < GCLatencyMode.Batch) || (value > GCLatencyMode.SustainedLowLatency))
                {
                    throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_Enum);
                }

                if (GC.SetGCLatencyMode((int)value) == (int)SetLatencyModeStatus.NoGCInProgress)
                    throw new InvalidOperationException("The NoGCRegion mode is in progress. End it and then set a different mode.");
            }
        }

        public static GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode
        {
            get
            {
                return (GCLargeObjectHeapCompactionMode)(GC.GetLOHCompactionMode());
            }

            // We don't want to allow this API when hosted.
            set
            {
                if ((value < GCLargeObjectHeapCompactionMode.Default) ||
                    (value > GCLargeObjectHeapCompactionMode.CompactOnce))
                {
                    throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_Enum);
                }

                GC.SetLOHCompactionMode((int)value);
            }
        }

        public static bool IsServerGC
        {
            get
            {
                return GC.IsServerGC();
            }
        }
    }
}
