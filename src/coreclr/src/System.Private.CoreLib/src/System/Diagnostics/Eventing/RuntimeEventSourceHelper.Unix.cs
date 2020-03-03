// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Diagnostics.Tracing
{
    internal static class RuntimeEventSourceHelper
    {
        private static Interop.Sys.ProcessCpuInformation s_cpuInfo;

        internal static int GetCpuUsage() =>
            Interop.Sys.GetCpuUtilization(ref s_cpuInfo) / Environment.ProcessorCount;
    }
}
