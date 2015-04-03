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

        internal int BasePriority
        {
            get
            {
                if (_basePriority == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_processName == null)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_processId == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_handleCount == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_poolPagedBytes == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_poolNonpagedBytes == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_virtualBytes == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_virtualBytesPeak == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_workingSetPeak == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_workingSet == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_pageFileBytesPeak == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_pageFileBytes == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_privateBytes == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
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
                if (_sessionId == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _sessionId;
            }
            set
            {
                _sessionId = value;
            }
        }


        internal ProcessInfo()
        {
            _basePriority = -1;
            _processName = null;
            _processId = -1;
            _handleCount = -1;
            _poolPagedBytes = -1;
            _poolNonpagedBytes = -1;
            _virtualBytes = -1;
            _virtualBytesPeak = -1;
            _workingSet = -1;
            _workingSetPeak = -1;
            _pageFileBytes = -1;
            _pageFileBytesPeak = -1;
            _privateBytes = -1;
            _sessionId = -1;
        }
    }
}