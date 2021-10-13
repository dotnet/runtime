// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;

namespace System.Diagnostics
{
    public partial class TraceEventCache
    {
        private long _timeStamp = -1;
        private DateTime _dateTime;
        private string? _stackTrace;

        public DateTime DateTime
        {
            get
            {
                if (_dateTime.Ticks == 0)
                    _dateTime = DateTime.UtcNow;
                return _dateTime;
            }
        }

        public int ProcessId => Environment.ProcessId;

        public string ThreadId => Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture);

        public long Timestamp
        {
            get
            {
                if (_timeStamp == -1)
                    _timeStamp = Stopwatch.GetTimestamp();
                return _timeStamp;
            }
        }

        public string Callstack => _stackTrace ??= Environment.StackTrace;

        public Stack LogicalOperationStack => Trace.CorrelationManager.LogicalOperationStack;
    }
}
