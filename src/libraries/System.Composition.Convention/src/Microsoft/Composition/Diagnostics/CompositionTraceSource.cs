// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Internal;

namespace Microsoft.Composition.Diagnostics
{
    internal static class CompositionTraceSource
    {
#if FEATURE_TRACING
        private static readonly TraceSourceTraceWriter s_source = new TraceSourceTraceWriter();
#else
        private static readonly DebuggerTraceWriter s_source = new DebuggerTraceWriter();
#endif

        public static bool CanWriteInformation
        {
            get { return s_source.CanWriteInformation; }
        }

        public static bool CanWriteWarning
        {
            get { return s_source.CanWriteWarning; }
        }

        public static bool CanWriteError
        {
            get { return s_source.CanWriteError; }
        }

        public static void WriteInformation(CompositionTraceId traceId, string format, params object[] arguments)
        {
            EnsureEnabled(CanWriteInformation);

            s_source.WriteInformation(traceId, format, arguments);
        }

        public static void WriteWarning(CompositionTraceId traceId, string format, params object[] arguments)
        {
            EnsureEnabled(CanWriteWarning);

            s_source.WriteWarning(traceId, format, arguments);
        }

        public static void WriteError(CompositionTraceId traceId, string format, params object[] arguments)
        {
            EnsureEnabled(CanWriteError);

            s_source.WriteError(traceId, format, arguments);
        }

        private static void EnsureEnabled(bool condition)
        {
            Assumes.IsTrue(condition, "To avoid unnecessary work when a trace level has not been enabled, check CanWriteXXX before calling this method.");
        }
    }
}
