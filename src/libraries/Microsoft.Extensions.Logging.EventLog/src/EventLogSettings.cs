// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.EventLog
{
    /// <summary>
    /// Settings for <see cref="EventLogLogger"/>.
    /// </summary>
    public class EventLogSettings
    {
        /// <summary>
        /// Gets or sets the name of the event log. If <see langword="null" /> or not specified, "Application" is the default.
        /// </summary>
        public string? LogName { get; set; }

        /// <summary>
        /// Gets or sets the name of the event log source. If <see langword="null" /> or not specified, ".NET Runtime" is the default.
        /// </summary>
        public string? SourceName { get; set; }

        /// <summary>
        /// Gets or sets the name of the machine with the event log. If <see langword="null" /> or not specified, local machine is the default.
        /// </summary>
        public string? MachineName { get; set; }

        /// <summary>
        /// Gets or sets the function used to filter events based on the log level.
        /// </summary>
        public Func<string, LogLevel, bool>? Filter { get; set; }

        internal IEventLog EventLog
        {
            get => field ??= CreateDefaultEventLog();

            // For unit testing purposes only.
            set => field = value;
        }

        private WindowsEventLog CreateDefaultEventLog()
        {
            string logName = string.IsNullOrEmpty(LogName) ? "Application" : LogName;
            string machineName = string.IsNullOrEmpty(MachineName) ? "." : MachineName;
            string sourceName = string.IsNullOrEmpty(SourceName) ? ".NET Runtime" : SourceName;
            int? defaultEventId = null;

            if (string.IsNullOrEmpty(SourceName))
            {
                sourceName = ".NET Runtime";
                defaultEventId = 1000;
            }

            return new WindowsEventLog(logName, machineName, sourceName) { DefaultEventId = defaultEventId };
        }
    }
}
