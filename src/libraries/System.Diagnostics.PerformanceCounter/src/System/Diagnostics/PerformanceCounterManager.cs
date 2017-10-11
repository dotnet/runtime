// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Diagnostics
{
    public sealed class PerformanceCounterManager : ICollectData
    {
        [ObsoleteAttribute("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        public PerformanceCounterManager()
        {
        }

        [ObsoleteAttribute("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        void ICollectData.CollectData(int callIdx, IntPtr valueNamePtr, IntPtr dataPtr, int totalBytes, out IntPtr res)
        {
            res = (IntPtr)(-1);
        }

        [ObsoleteAttribute("This class has been deprecated.  Use the PerformanceCounters through the System.Diagnostics.PerformanceCounter class instead.  http://go.microsoft.com/fwlink/?linkid=14202")]
        void ICollectData.CloseData()
        {
        }
    }
}
