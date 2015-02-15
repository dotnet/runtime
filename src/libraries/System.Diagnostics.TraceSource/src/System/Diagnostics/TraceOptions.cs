// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    [Flags]
    public enum TraceOptions
    {
        None = 0,
        DateTime = 0x02,
        Timestamp = 0x04,
        ProcessId = 0x08,
        ThreadId = 0x10
    }
}
