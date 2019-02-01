// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Describes the status of a particular log with respect to
    /// an instantiated EventLogReader.  Since it is possible to
    /// instantiate an EventLogReader with a query containing
    /// multiple logs and the reader can be configured to tolerate
    /// errors in attaching to those logs, this class allows the
    /// user to determine exactly what the status of those logs is.
    /// </summary>
    public sealed class EventLogStatus
    {
        internal EventLogStatus(string channelName, int win32ErrorCode)
        {
            LogName = channelName;
            StatusCode = win32ErrorCode;
        }

        public string LogName { get; }

        public int StatusCode { get; }
    }
}
