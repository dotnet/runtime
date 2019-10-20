// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    internal partial class PortableThreadPool
    {
        private class CpuUtilizationReader
        {
            private Interop.Sys.ProcessCpuInformation _cpuInfo;

            public int CurrentUtilization => Interop.Sys.GetCpuUtilization(ref _cpuInfo); // Updates cpuInfo as a side effect for the next call
        }
    }
}
