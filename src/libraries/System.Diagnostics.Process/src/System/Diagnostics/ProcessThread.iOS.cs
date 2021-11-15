// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        public TimeSpan PrivilegedProcessorTime => throw new PlatformNotSupportedException();
        public TimeSpan TotalProcessorTime => throw new PlatformNotSupportedException();
        public TimeSpan UserProcessorTime => throw new PlatformNotSupportedException();
        private ThreadPriorityLevel PriorityLevelCore
        {
            get { throw new PlatformNotSupportedException(SR.ThreadPriorityNotSupported); }
            set { throw new PlatformNotSupportedException(SR.ThreadPriorityNotSupported); }
        }
        private DateTime GetStartTime() => throw new PlatformNotSupportedException();
    }
}
