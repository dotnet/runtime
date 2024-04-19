// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security;

namespace Microsoft.Extensions.Logging.EventLog
{
    /// <summary>
    /// The windows event log.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class WindowsEventLog : IEventLog
    {
        // https://msdn.microsoft.com/EN-US/library/windows/desktop/aa363679.aspx
        private const int MaximumMessageSize = 31839;
        private bool _enabled = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsEventLog"/> class.
        /// </summary>
        public WindowsEventLog(string logName, string machineName, string sourceName)
        {
            DiagnosticsEventLog = new System.Diagnostics.EventLog(logName, machineName, sourceName);
        }

        /// <summary>
        /// The diagnostics event log.
        /// </summary>
        public System.Diagnostics.EventLog DiagnosticsEventLog { get; }

        /// <summary>
        /// The maximum message size.
        /// </summary>
        public int MaxMessageSize => MaximumMessageSize;

        public int? DefaultEventId { get; set; }

        public void WriteEntry(string message, EventLogEntryType type, int eventID, short category)
        {
            try
            {
                if (_enabled)
                {
                    DiagnosticsEventLog.WriteEvent(new EventInstance(eventID, category, type), message);
                }
            }
            catch (SecurityException sx)
            {
                _enabled = false;
                // We couldn't create the log or source name. Disable logging.
                try
                {
                    using (var backupLog = new System.Diagnostics.EventLog("Application", ".", "Application"))
                    {
                        backupLog.WriteEvent(new EventInstance(instanceId: 0, categoryId: 0, EventLogEntryType.Error),
                            $"Unable to log .NET application events. {sx.Message}");
                    }
                }
                catch (Exception)
                {

                }
            }
        }
    }
}
