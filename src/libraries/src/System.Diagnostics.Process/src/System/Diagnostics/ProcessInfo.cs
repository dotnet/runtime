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
        private int _basePriorityInternal;
        private string _processNameInternal;
        private int _processIdInternal;
        private int _handleCountInternal;
        private long _poolPagedBytesInternal;
        private long _poolNonpagedBytesInternal;
        private long _virtualBytesInternal;
        private long _virtualBytesPeakInternal;
        private long _workingSetPeakInternal;
        private long _workingSetInternal;
        private long _pageFileBytesPeakInternal;
        private long _pageFileBytesInternal;
        private long _privateBytesInternal;
        private int _sessionIdInternal;

        /*************** TODO: Rename and check that all callers are okay with these throwing or find a different fix */
        internal int _basePriority
        {
            get
            {
                if (_basePriorityInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _basePriorityInternal;
            }
            set
            {
                _basePriorityInternal = value;
            }
        }

        internal string _processName
        {
            get
            {
                if (_processNameInternal == null)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _processNameInternal;
            }
            set
            {
                _processNameInternal = value;
            }
        }

        internal int _processId
        {
            get
            {
                if (_processIdInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _processIdInternal;
            }
            set
            {
                _processIdInternal = value;
            }
        }

        internal int _handleCount
        {
            get
            {
                if (_handleCountInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _handleCountInternal;
            }
            set
            {
                _handleCountInternal = value;
            }
        }

        internal long _poolPagedBytes
        {
            get
            {
                if (_poolPagedBytesInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _poolPagedBytesInternal;
            }
            set
            {
                _poolPagedBytesInternal = value;
            }
        }
        
        internal long _poolNonpagedBytes
        {
            get
            {
                if (_poolNonpagedBytesInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _poolNonpagedBytesInternal;
            }
            set
            {
                _poolPagedBytesInternal = value;
            }
        }

        internal long _virtualBytes
        {
            get
            {
                if (_virtualBytesInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _virtualBytes;
            }
            set
            {
                _virtualBytesInternal = value;
            }
        }

        internal long _virtualBytesPeak
        {
            get
            {
                if (_virtualBytesPeakInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _virtualBytesPeakInternal;
            }
            set
            {
                _virtualBytesPeakInternal = value;
            }
        }

        internal long _workingSetPeak
        {
            get
            {
                if (_workingSetPeakInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _workingSetPeakInternal;
            }
            set
            {
                _workingSetPeakInternal = value;
            }
        }

        internal long _workingSet
        {
            get
            {
                if (_workingSetInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _workingSetInternal;
            }
            set
            {
                _workingSetInternal = value;
            }
        }

        internal long _pageFileBytesPeak
        {
            get
            {
                if (_pageFileBytesPeakInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _pageFileBytesPeakInternal;
            }
            set
            {
                _pageFileBytesPeakInternal = value;
            }
        }

        internal long _pageFileBytes
        {
            get
            {
                if (_pageFileBytesInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _pageFileBytesInternal;
            }
            set
            {
                _pageFileBytesInternal = value;
            }
        }

        internal long _privateBytes
        {
            get
            {
                if (_privateBytesInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _privateBytesInternal;
            }
            set
            {
                _privateBytesInternal = value;
            }
        }
        
        internal int _sessionId
        {
            get
            {
                if (_sessionIdInternal == -1)
                    throw new Win32Exception(SR.ProcessInformationUnavailable);
                else
                    return _sessionIdInternal;
            }
            set
            {
                _sessionIdInternal = value;
            }
        }
        /************* END TODO*/

        internal ProcessInfo()
        {
            _basePriorityInternal = -1;
            _processNameInternal = null;
            _processIdInternal = -1;
            _handleCountInternal = -1;
            _poolPagedBytesInternal = -1;
            _poolNonpagedBytesInternal = -1;
            _virtualBytesInternal = -1;
            _virtualBytesPeakInternal = -1;
            _workingSetInternal = -1;
            _workingSetPeakInternal = -1;
            _pageFileBytesInternal = -1;
            _pageFileBytesPeakInternal = -1;
            _privateBytesInternal = -1;
            _sessionIdInternal = -1;
        }
    }
}