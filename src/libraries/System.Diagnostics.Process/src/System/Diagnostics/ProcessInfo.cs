// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Diagnostics
{
    /// <summary>
    /// This data structure contains information about a process that is collected
    /// in bulk by querying the operating system.  The reason to make this a separate
    /// structure from the process component is so that we can throw it away all at once
    /// when Refresh is called on the component.
    /// </summary>
    internal sealed class ProcessInfo
    {
        internal readonly List<ThreadInfo> _threadInfoList = new List<ThreadInfo>();
        internal int BasePriority { get; set; }
        internal string ProcessName { get; set; }
        internal int ProcessId { get; set; }
        internal long PoolPagedBytes { get; set; }
        internal long PoolNonPagedBytes { get; set; }
        internal long VirtualBytes { get; set; }
        internal long VirtualBytesPeak { get; set; }
        internal long WorkingSetPeak { get; set; }
        internal long WorkingSet { get; set; }
        internal long PageFileBytesPeak { get; set; }
        internal long PageFileBytes { get; set; }
        internal long PrivateBytes { get; set; }
        internal int SessionId { get; set; }
        internal int HandleCount { get; set; }

        internal ProcessInfo()
        {
            BasePriority = 0;
            ProcessName = "";
            ProcessId = 0;
            PoolPagedBytes = 0;
            PoolNonPagedBytes = 0;
            VirtualBytes = 0;
            VirtualBytesPeak = 0;
            WorkingSet = 0;
            WorkingSetPeak = 0;
            PageFileBytes = 0;
            PageFileBytesPeak = 0;
            PrivateBytes = 0;
            SessionId = 0;
            HandleCount = 0;
        }
    }
}
