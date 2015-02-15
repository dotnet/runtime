// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                return GetProcessId();
            }
        }

        public string ThreadId
        {
            get
            {
                return GetThreadId().ToString(CultureInfo.InvariantCulture);
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

        internal static int GetThreadId()
        {
            return Environment.CurrentManagedThreadId;
        }
    }
}

