// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public sealed partial class AppDomain
    {
        public TimeSpan MonitoringTotalProcessorTime =>
            Interop.Kernel32.GetProcessTimes(Interop.Kernel32.GetCurrentProcess(), out _, out _, out _, out long userTime100Nanoseconds) ?
                new TimeSpan(userTime100Nanoseconds) :
                TimeSpan.Zero;
    }
}
