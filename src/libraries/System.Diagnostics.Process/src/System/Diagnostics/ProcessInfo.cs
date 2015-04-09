// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private int _basePriority;
        private string _processName;
        private int _processId;
        private int _handleCount;
        private long _poolPagedBytes;
        private long _poolNonpagedBytes;
        private long _virtualBytes;
        private long _virtualBytesPeak;
        private long _workingSetPeak;
        private long _workingSet;
        private long _pageFileBytesPeak;
        private long _pageFileBytes;
        private long _privateBytes;
        private int _sessionId;

        internal ProcessInfo()
        {
            _basePriority = 0;
            _processName = "";
            _processId = 0;
            _handleCount = 0;
            _poolPagedBytes = 0;
            _poolNonpagedBytes = 0;
            _virtualBytes = 0;
            _virtualBytesPeak = 0;
            _workingSet = 0;
            _workingSetPeak = 0;
            _pageFileBytes = 0;
            _pageFileBytesPeak = 0;
            _privateBytes = 0;
            _sessionId = 0;
        }

        internal int BasePriority
        {
            get
            {
                return _basePriority;
            }
            set
            {
                _basePriority = value;
            }
        }

        internal string ProcessName
        {
            get
            {
                return _processName;
            }
            set
            {
                _processName = value;
            }
        }

        internal int ProcessId
        {
            get
            {
                return _processId;
            }
            set
            {
                _processId = value;
            }
        }

        internal int HandleCount
        {
            get
            {
                return _handleCount;
            }
            set
            {
                _handleCount = value;
            }
        }

        internal long PoolPagedBytes
        {
            get
            {
                return _poolPagedBytes;
            }
            set
            {
                _poolPagedBytes = value;
            }
        }
        
        internal long PoolNonpagedBytes
        {
            get
            {
                return _poolNonpagedBytes;
            }
            set
            {
                _poolPagedBytes = value;
            }
        }

        internal long VirtualBytes
        {
            get
            {
                return _virtualBytes;
            }
            set
            {
                _virtualBytes = value;
            }
        }

        internal long VirtualBytesPeak
        {
            get
            {
                return _virtualBytesPeak;
            }
            set
            {
                _virtualBytesPeak = value;
            }
        }

        internal long WorkingSetPeak
        {
            get
            {
                return _workingSetPeak;
            }
            set
            {
                _workingSetPeak = value;
            }
        }

        internal long WorkingSet
        {
            get
            {
                return _workingSet;
            }
            set
            {
                _workingSet = value;
            }
        }

        internal long PageFileBytesPeak
        {
            get
            {
                return _pageFileBytesPeak;
            }
            set
            {
                _pageFileBytesPeak = value;
            }
        }

        internal long PageFileBytes
        {
            get
            {
                return _pageFileBytes;
            }
            set
            {
                _pageFileBytes = value;
            }
        }

        internal long PrivateBytes
        {
            get
            {
                return _privateBytes;
            }
            set
            {
                _privateBytes = value;
            }
        }
        
        internal int SessionId
        {
            get
            {
                return _sessionId;
            }
            set
            {
                _sessionId = value;
            }
        }
    }
}
