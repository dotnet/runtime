// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Runtime
{
    public enum GCLargeObjectHeapCompactionMode
    {
        Default = 1,
        CompactOnce = 2
    }

    // These settings are the same format as in the GC in the runtime.
    public enum GCLatencyMode
    {
        Batch = 0,
        Interactive = 1,
        LowLatency = 2,
        SustainedLowLatency = 3,
        NoGCRegion = 4
    }

    public static partial class GCSettings
    {
        private enum SetLatencyModeStatus
        {
            Succeeded = 0,
            NoGCInProgress = 1 // NoGCRegion is in progress, can't change pause mode.
        };

        public static GCLatencyMode LatencyMode
        {
            get => GetGCLatencyMode();
            set
            {
                if ((value < GCLatencyMode.Batch) ||
                    (value > GCLatencyMode.SustainedLowLatency))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_Enum);
                }

                SetLatencyModeStatus status = SetGCLatencyMode(value);
                if (status == SetLatencyModeStatus.NoGCInProgress)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_SetLatencyModeNoGC);
                }

                Debug.Assert(status == SetLatencyModeStatus.Succeeded, $"Unexpected return value '{status}' from {nameof(SetGCLatencyMode)}.");
            }
        }

        public static GCLargeObjectHeapCompactionMode LargeObjectHeapCompactionMode
        {
            get => GetLOHCompactionMode();
            set
            {
                if ((value < GCLargeObjectHeapCompactionMode.Default) ||
                    (value > GCLargeObjectHeapCompactionMode.CompactOnce))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_Enum);
                }

                SetLOHCompactionMode(value);
            }
        }
    }
}
