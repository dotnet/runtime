// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition.Diagnostics
{
    internal abstract class TraceWriter
    {
        public abstract bool CanWriteInformation
        {
            get;
        }

        public abstract bool CanWriteWarning
        {
            get;
        }

        public abstract bool CanWriteError
        {
            get;
        }

        public abstract void WriteInformation(CompositionTraceId traceId, string format, params object[] arguments);

        public abstract void WriteWarning(CompositionTraceId traceId, string format, params object[] arguments);

        public abstract void WriteError(CompositionTraceId traceId, string format, params object[] arguments);
    }
}

