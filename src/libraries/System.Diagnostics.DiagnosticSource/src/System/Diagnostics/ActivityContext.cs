// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityContext representation conforms to the w3c TraceContext specification. It contains two identifiers
    /// a TraceId and a SpanId - along with a set of common TraceFlags and system-specific TraceState values.
    /// </summary>
    public readonly struct ActivityContext : IEquatable<ActivityContext>
    {
        /// <summary>
        /// Construct a new object of ActivityContext.
        /// </summary>
        /// <param name="traceId"> A trace identifier.</param>
        /// <param name="spanId"> A span identifier </param>
        /// <param name="traceFlags">Contain details about the trace.</param>
        /// <param name="traceState"> Carries system-specific configuration data.</param>
        public ActivityContext(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags traceFlags, string? traceState = null)
        {
            if (traceId == default || spanId == default)
            {
                throw new ArgumentException(SR.SpanIdOrTraceIdInvalid, traceId == default ? nameof(traceId) : nameof(spanId));
            }

            TraceId = traceId;
            SpanId = spanId;
            TraceFlags = traceFlags;
            TraceState = traceState;
        }

        /// <summary>
        /// The trace identifier
        /// </summary>
        public ActivityTraceId TraceId { get; }

        /// <summary>
        /// The span identifier
        /// </summary>
        public ActivitySpanId SpanId { get; }

        /// <summary>
        /// The flags for the details about the trace.
        /// </summary>
        public ActivityTraceFlags TraceFlags { get; }

        /// <summary>
        /// system-specific configuration data.
        /// </summary>
        public string? TraceState { get; }

        public static bool operator ==(ActivityContext context1, ActivityContext context2)
        {
            return context1.SpanId == context2.SpanId &&
                   context1.TraceId == context2.TraceId &&
                   context1.TraceFlags == context2.TraceFlags &&
                   context1.TraceState == context2.TraceState;
        }

        public static bool operator !=(ActivityContext context1, ActivityContext context2) => !(context1 == context2);

        public bool Equals(ActivityContext context)
        {
            return this == context;
        }

        public override bool Equals(object? obj)
        {
            if (obj is ActivityContext context)
                return this == context;
            return false;
        }

        public override int GetHashCode()
        {
            if (this == default)
                return 0;

            // HashCode.Combine would be the best but we need to compile for the full framework which require adding dependency
            // on the extensions package. Considering this simple type and hashing is not expected to be used, we are implementing
            // the hashing manually.
            int hash = 5381;
            hash = ((hash << 5) + hash) + TraceId.GetHashCode();
            hash = ((hash << 5) + hash) + SpanId.GetHashCode();
            hash = ((hash << 5) + hash) + (int) TraceFlags;
            hash = ((hash << 5) + hash) + (TraceState == null ? 0 : TraceState.GetHashCode());

            return hash;
        }
    }
}