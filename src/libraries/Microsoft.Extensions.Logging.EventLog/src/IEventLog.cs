// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.Extensions.Logging.EventLog
{
    internal interface IEventLog
    {
        int? DefaultEventId { get; }

        int MaxMessageSize { get; }

        void WriteEntry(string message, EventLogEntryType type, int eventID, short category);
    }
}
