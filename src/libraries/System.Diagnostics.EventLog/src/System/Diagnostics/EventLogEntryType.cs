// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    public enum EventLogEntryType
    {
        Error = 1,
        Warning = 2,
        Information = 4,
        SuccessAudit = 8,
        FailureAudit = 16,
    }
}
