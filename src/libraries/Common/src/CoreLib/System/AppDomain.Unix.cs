// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public sealed partial class AppDomain
    {
        public TimeSpan MonitoringTotalProcessorTime
        {
            get
            {
                Interop.Sys.ProcessCpuInformation cpuInfo = default;
                Interop.Sys.GetCpuUtilization(ref cpuInfo);

                ulong userTime100Nanoseconds = cpuInfo.lastRecordedUserTime / 100; // nanoseconds to 100-nanoseconds
                if (userTime100Nanoseconds > long.MaxValue)
                {
                    userTime100Nanoseconds = long.MaxValue;
                }

                return new TimeSpan((long)userTime100Nanoseconds);
            }
        }
    }
}
