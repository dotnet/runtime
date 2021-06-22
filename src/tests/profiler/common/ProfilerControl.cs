// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.NETCore.Client;
using Tracing.Tests.Common;

namespace Profiler.Tests
{
    /// <summary>
    /// Used by managed profilees to control the profiler
    /// </summary>
    public static class ProfilerControlHelpers
    {
        public static void AttachProfilerToSelf(Guid profilerGuid, string profilerPath)
        {
            int processId = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(processId);
            client.AttachProfiler(TimeSpan.MaxValue, profilerGuid, profilerPath, null);
        }

        public static EventPipeSession AttachEventPipeSessionToSelf(IEnumerable<EventPipeProvider> providers)
        {
            int processId = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(processId);
            return client.StartEventPipeSession(providers, /* requestRunDown */ false);
        }
    }
}
