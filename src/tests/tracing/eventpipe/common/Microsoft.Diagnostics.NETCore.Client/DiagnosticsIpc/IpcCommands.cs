// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.NETCore.Client
{
    internal enum DiagnosticsServerCommandSet : byte
    {
        Dump           = 0x01,
        EventPipe      = 0x02,
        Profiler       = 0x03,
        Process        = 0x04,

        Server         = 0xFF,
    }

    internal enum DiagnosticsServerResponseId : byte
    {
        OK            = 0x00,
        // future
        Error         = 0xFF,
    }

    internal enum EventPipeCommandId : byte
    {
        StopTracing     = 0x01,
        CollectTracing  = 0x02,
        CollectTracing2 = 0x03,
    }

    internal enum DumpCommandId : byte
    {
        GenerateCoreDump = 0x01,
        GenerateCoreDump2 = 0x02,
        GenerateCoreDump3 = 0x03,
    }

    internal enum ProfilerCommandId : byte
    {
        AttachProfiler = 0x01,
        StartupProfiler = 0x02,
    }

    internal enum ProcessCommandId : byte
    {
        GetProcessInfo = 0x00,
        ResumeRuntime  = 0x01,
        GetProcessEnvironment = 0x02,
        SetEnvironmentVariable = 0x03,
        GetProcessInfo2 = 0x04
    }
}
