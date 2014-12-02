// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TraceHelpers.cs
//
//
// Common routines used to trace information about execution, the state of things, etc.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Diagnostics.Contracts;

namespace System.Linq.Parallel
{
    internal static class TraceHelpers
    {
#if PFXTRACE
        // If tracing is turned on, we create a new trace source.
        private static TraceSource s_traceSource = new TraceSource("PFX", SourceLevels.All);

        // Some constants used to set trace settings via environment variables.
        private const string s_traceDefaultEnableEnvironmentVariable = "PLINQ_TRACE_DEFAULT_ENABLE";
        private const string s_traceOutputEnvironmentVariable = "PLINQ_TRACE_OUT";
        private const string s_traceLevelEnvironmentVariable = "PLINQ_TRACE_LEVEL";

        //---------------------------------------------------------------------------------------
        // Clear the trace source's listeners so that, by default, all traces >NUL.
        //

        static TraceHelpers()
        {
            s_traceSource.Listeners.Clear();

            // If trace output is requested in the environment, set it.
            string traceOutput = Environment.GetEnvironmentVariable(s_traceOutputEnvironmentVariable);
            if (traceOutput != null && !String.IsNullOrEmpty(traceOutput.Trim()))
            {
                s_traceSource.Listeners.Add(new TextWriterTraceListener(
                    new StreamWriter(File.Open(traceOutput, FileMode.OpenOrCreate, FileAccess.ReadWrite))));
            }

            string traceEnable = Environment.GetEnvironmentVariable(s_traceDefaultEnableEnvironmentVariable);
            if (traceEnable != null)
            {
                s_traceSource.Listeners.Add(new DefaultTraceListener());
            }

            // If verbose tracing was requested, turn it on.
            string traceLevel = Environment.GetEnvironmentVariable(s_traceLevelEnvironmentVariable);
            if ("0".Equals(traceLevel))
            {
                SetVerbose();
            }
        }
#endif

        //---------------------------------------------------------------------------------------
        // Adds a listener to the PLINQ trace source, but only in PFXTRACE builds.
        //

#if PFXTRACE
        [Conditional("PFXTRACE")]
        internal static void AddListener(TraceListener listener)
        {

            s_traceSource.Listeners.Add(listener);
        }
#endif

        //---------------------------------------------------------------------------------------
        // Turns on verbose output for all current listeners. This includes a ton of information,
        // like the call-stack, date-time, thread-ids, ....
        //

        [Conditional("PFXTRACE")]
        internal static void SetVerbose()
        {
#if PFXTRACE
            foreach (TraceListener l in s_traceSource.Listeners)
            {
                l.TraceOutputOptions = TraceOptions.Callstack | TraceOptions.DateTime | TraceOptions.ThreadId;
            }
#endif
        }

        //---------------------------------------------------------------------------------------
        // Tracing helpers. These are all conditionally enabled for PFXTRACE builds only.
        //

        [Conditional("PFXTRACE")]
        internal static void TraceInfo(string msg, params object[] args)
        {
#if PFXTRACE
            lock (s_traceSource)
            {
                s_traceSource.TraceEvent(TraceEventType.Information, 0, msg, args);
            }
            s_traceSource.Flush();
#endif
        }

        [Conditional("PFXTRACE")]
        internal static void TraceWarning(string msg, params object[] args)
        {
#if PFXTRACE
            lock (s_traceSource)
            {
                s_traceSource.TraceEvent(TraceEventType.Warning, 0, msg, args);
            }
            s_traceSource.Flush();
#endif
        }

        [Conditional("PFXTRACE")]
        internal static void TraceError(string msg, params object[] args)
        {
#if PFXTRACE
            lock (s_traceSource)
            {
                s_traceSource.TraceEvent(TraceEventType.Error, 0, msg, args);
            }
            s_traceSource.Flush();
#endif
        }
    }
}
