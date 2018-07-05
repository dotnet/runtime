// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Composition.Diagnostics
{
    internal static class CompositionTraceSource
    {
        private static readonly DebuggerTraceWriter s_source = new DebuggerTraceWriter();

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
            if (!condition)
            {
                throw new Exception(SR.Format(SR.Diagnostic_InternalExceptionMessage, SR.Diagnostic_TraceUnnecessaryWork));
            }
        }
    }
}
