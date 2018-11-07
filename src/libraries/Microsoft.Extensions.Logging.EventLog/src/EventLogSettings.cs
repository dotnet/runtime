// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.EventLog.Internal;

namespace Microsoft.Extensions.Logging.EventLog
{
    /// <summary>
    /// Settings for <see cref="EventLogLogger"/>.
    /// </summary>
    public class EventLogSettings
    {
        /// <summary>
        /// Name of the event log. If <c>null</c> or not specified, "Application" is the default.
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// Name of the event log source. If <c>null</c> or not specified, "Application" is the default.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Name of the machine having the event log. If <c>null</c> or not specified, local machine is the default.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// The function used to filter events based on the log level.
        /// </summary>
        public Func<string, LogLevel, bool> Filter { get; set; }

        /// <summary>
        /// For unit testing purposes only.
        /// </summary>
        public IEventLog EventLog { get; set; }
    }
}
