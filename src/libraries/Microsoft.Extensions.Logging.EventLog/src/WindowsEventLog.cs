// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging.EventLog.Internal;

namespace Microsoft.Extensions.Logging.EventLog
{
    [Obsolete("This type is obsolete and will be removed in a future version. The recommended alternative is using EventLogLoggerProvider.")]
    public class WindowsEventLog : IEventLog
    {
        // https://msdn.microsoft.com/EN-US/library/windows/desktop/aa363679.aspx
        private const int MaximumMessageSize = 31839;

        public WindowsEventLog(string logName, string machineName, string sourceName)
        {
            DiagnosticsEventLog = new System.Diagnostics.EventLog(logName, machineName, sourceName);
        }

        public System.Diagnostics.EventLog DiagnosticsEventLog { get; }

        public int MaxMessageSize
        {
            get
            {
                return MaximumMessageSize;
            }
        }

        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category)
        {
            DiagnosticsEventLog.WriteEvent(new EventInstance(eventID, category, type), message);
        }
    }
}
