// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal partial class PortableThreadPool
    {
        private class CpuUtilizationReader
        {
            private struct ProcessCpuInformation
            {
                public long idleTime;
                public long kernelTime;
                public long userTime;
            }

            private ProcessCpuInformation _processCpuInfo = new ProcessCpuInformation();

            public int CurrentUtilization
            {
                get
                {
                    if (!Interop.Kernel32.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
                    {
                        int error = Marshal.GetLastWin32Error();
                        var exception = new OutOfMemoryException();
                        exception.HResult = error;
                        throw exception;
                    }

                    long cpuTotalTime = ((long)userTime - _processCpuInfo.userTime) + ((long)kernelTime - _processCpuInfo.kernelTime);
                    long cpuBusyTime = cpuTotalTime - ((long)idleTime - _processCpuInfo.idleTime);

                    _processCpuInfo.kernelTime = (long)kernelTime;
                    _processCpuInfo.userTime = (long)userTime;
                    _processCpuInfo.idleTime = (long)idleTime;

                    if (cpuTotalTime > 0 && cpuBusyTime > 0)
                    {
                        long reading = cpuBusyTime * 100 / cpuTotalTime;
                        reading = Math.Min(reading, 100);
                        Debug.Assert(0 <= reading);
                        return (int)reading;
                    }
                    return 0;
                }
            }
        }
    }
}
