// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.NETCore.Client
{
    public enum WriteDumpFlags
    {
        None = 0x00,
        LoggingEnabled = 0x01,
        VerboseLoggingEnabled = 0x02,
        CrashReportEnabled = 0x04
    }
}