// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.NETCore.Client;

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

        public static void SetStartupProfilerViaIPC(int processId, Guid profilerGuid, string profilerPath)
        {
            MethodInfo startupProfiler = typeof(DiagnosticsClient).GetMethod("SetStartupProfiler", BindingFlags.Public);
            if (startupProfiler != null)
            {
                throw new Exception("You updated DiagnosticsClient to a version that supports SetStartupProfiler, please remove this nonsense and replace it with the calls commented out below.");
                // DiagnosticsClient client = new DiagnosticsClient(processId);
                // client.SetStartupProfiler(profilerGuid, profilerPath);
                // client.ResumeRuntime();
            }

            // This is to work around having to wait for an update to the DiagnosticsClient nuget before adding
            // a test. I really hope this isn't permanent
            DiagnosticsIPCWorkaround client = new DiagnosticsIPCWorkaround(processId);
            client.SetStartupProfiler(profilerGuid, profilerPath);
        }

        public static EventPipeSession AttachEventPipeSessionToSelf(IEnumerable<EventPipeProvider> providers)
        {
            int processId = Process.GetCurrentProcess().Id;
            DiagnosticsClient client = new DiagnosticsClient(processId);
            return client.StartEventPipeSession(providers, /* requestRunDown */ false);
        }
    }
}
