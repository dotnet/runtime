// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Composition.Diagnostics
{
    internal abstract class TraceWriter
    {
        public abstract bool CanWriteInformation { get; }

        public abstract bool CanWriteWarning { get; }

        public abstract bool CanWriteError { get; }

        public abstract void WriteInformation(CompositionTraceId traceId, string format, params object[] arguments);

        public abstract void WriteWarning(CompositionTraceId traceId, string format, params object[] arguments);

        public abstract void WriteError(CompositionTraceId traceId, string format, params object[] arguments);
    }
}

