// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Diagnostics
{
    public sealed class PerformanceCounterManager : ICollectData
    {
        [Obsolete("PerformanceCounterManager has been deprecated. Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.")]
        public PerformanceCounterManager()
        {
        }

        [Obsolete("PerformanceCounterManager has been deprecated. Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.")]
        void ICollectData.CollectData(int callIdx, IntPtr valueNamePtr, IntPtr dataPtr, int totalBytes, out IntPtr res)
        {
            res = (IntPtr)(-1);
        }

        [Obsolete("PerformanceCounterManager has been deprecated. Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.")]
        void ICollectData.CloseData()
        {
        }
    }
}
