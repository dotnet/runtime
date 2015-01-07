// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

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
        public readonly List<ThreadInfo> threadInfoList = new List<ThreadInfo>();
        public int basePriority;
        public string processName;
        public int processId;
        public int handleCount;
        public long poolPagedBytes;
        public long poolNonpagedBytes;
        public long virtualBytes;
        public long virtualBytesPeak;
        public long workingSetPeak;
        public long workingSet;
        public long pageFileBytesPeak;
        public long pageFileBytes;
        public long privateBytes;
        public int sessionId;
    }
}