// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Internal;

namespace System.ComponentModel.Composition.Diagnostics
{
    internal static class CompositionTraceSource
    {
        private static readonly DebuggerTraceWriter Source = new DebuggerTraceWriter();

        public static bool CanWriteInformation
        {
            get { return Source.CanWriteInformation; }
        }

        public static bool CanWriteWarning
        {
            get { return Source.CanWriteWarning; }
        }

        public static bool CanWriteError
        {
            get { return Source.CanWriteError; }
        }

        public static void WriteInformation(CompositionTraceId traceId, string format, params object[] arguments)
        {
            EnsureEnabled(CanWriteInformation);

            Source.WriteInformation(traceId, format, arguments);
        }

        public static void WriteWarning(CompositionTraceId traceId, string format, params object[] arguments)
        {
            EnsureEnabled(CanWriteWarning);

            Source.WriteWarning(traceId, format, arguments);
        }

        public static void WriteError(CompositionTraceId traceId, string format, params object[] arguments)
        {
            EnsureEnabled(CanWriteError);

            Source.WriteError(traceId, format, arguments);
        }

        private static void EnsureEnabled(bool condition)
        {
            Assumes.IsTrue(condition, "To avoid unnecessary work when a trace level has not been enabled, check CanWriteXXX before calling this method.");
        }
    }
}
