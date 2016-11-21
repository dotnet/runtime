// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    [Flags]
    public enum TraceOptions
    {
        None = 0,
        LogicalOperationStack = 0x01,
        DateTime = 0x02,
        Timestamp = 0x04,
        ProcessId = 0x08,
        ThreadId = 0x10,
        Callstack = 0x20,
    }
}
