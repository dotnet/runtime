// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Diagnostics
{
    public sealed class PerformanceCounterManager : ICollectData
    {
        [ObsoleteAttribute("PerformanceCounterManager has been deprecated. Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.")]
        public PerformanceCounterManager()
        {
        }

        [ObsoleteAttribute("PerformanceCounterManager has been deprecated. Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.")]
        void ICollectData.CollectData(int callIdx, IntPtr valueNamePtr, IntPtr dataPtr, int totalBytes, out IntPtr res)
        {
            res = (IntPtr)(-1);
        }

        [ObsoleteAttribute("PerformanceCounterManager has been deprecated. Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.")]
        void ICollectData.CloseData()
        {
        }
    }
}
