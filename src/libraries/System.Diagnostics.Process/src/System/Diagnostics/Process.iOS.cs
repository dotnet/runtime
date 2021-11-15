// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Diagnostics
{
    public partial class Process
    {
        public TimeSpan PrivilegedProcessorTime => throw new PlatformNotSupportedException();
        public TimeSpan TotalProcessorTime => throw new PlatformNotSupportedException();
        public TimeSpan UserProcessorTime => throw new PlatformNotSupportedException();
        internal DateTime StartTimeCore => throw new PlatformNotSupportedException();
        private int ParentProcessId => throw new PlatformNotSupportedException();
        private string GetPathToOpenFile() => throw new PlatformNotSupportedException();
    }
}
