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
        internal readonly List<ThreadInfo> _threadInfoList = new List<ThreadInfo>();
        internal int _basePriority;
        internal string _processName;
        internal int _processId;
        internal int _handleCount;
        internal long _poolPagedBytes;
        internal long _poolNonpagedBytes;
        internal long _virtualBytes;
        internal long _virtualBytesPeak;
        internal long _workingSetPeak;
        internal long _workingSet;
        internal long _pageFileBytesPeak;
        internal long _pageFileBytes;
        internal long _privateBytes;
        internal int _sessionId;
    }
}