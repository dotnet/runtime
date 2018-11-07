// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.Extensions.Logging.EventLog.Internal
{
    public interface IEventLog
    {
        int MaxMessageSize { get; }

        void WriteEntry(string message, EventLogEntryType type, int eventID, short category);
    }
}
