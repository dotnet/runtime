// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Text;
using System.Collections;
using System.Globalization;

namespace System.Diagnostics
{
    public partial class TraceEventCache
    {
        private long _timeStamp = -1;
        private DateTime _dateTime = DateTime.MinValue;
        private string _stackTrace = null;

        public DateTime DateTime
        {
            get
            {
                if (_dateTime == DateTime.MinValue)
                    _dateTime = DateTime.UtcNow;
                return _dateTime;
            }
        }

        public int ProcessId
        {
            get
            {
                return TraceListenerHelpers.GetProcessId();
            }
        }

        public string ThreadId
        {
            get
            {
                return TraceListenerHelpers.GetThreadId().ToString(CultureInfo.InvariantCulture);
            }
        }

        public long Timestamp
        {
            get
            {
                if (_timeStamp == -1)
                    _timeStamp = Stopwatch.GetTimestamp();
                return _timeStamp;
            }
        }

        public string Callstack 
        {
            get 
            {
                if (_stackTrace == null)
                {
                    _stackTrace = Environment.StackTrace;
                }

                return _stackTrace;
            }
        }

        public Stack LogicalOperationStack 
        {
            get 
            {
                return Trace.CorrelationManager.LogicalOperationStack;
            }
        }
    }
}

