// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        private static ThreadPriorityLevel PriorityLevelCore
        {
            get { throw new PlatformNotSupportedException(); }
            set { throw new PlatformNotSupportedException(); }
        }

        private static TimeSpan GetPrivilegedProcessorTime() => throw new PlatformNotSupportedException();

        private static DateTime GetStartTime() => throw new PlatformNotSupportedException();

        private static TimeSpan GetTotalProcessorTime() => throw new PlatformNotSupportedException();

        private static TimeSpan GetUserProcessorTime() => throw new PlatformNotSupportedException();
    }
}
