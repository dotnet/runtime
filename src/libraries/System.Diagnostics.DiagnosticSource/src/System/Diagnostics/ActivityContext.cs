// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityContext representation conforms to the w3c TraceContext specification. It contains two identifiers
    /// a TraceId and a SpanId - along with a set of common TraceFlags and system-specific TraceState values.
    /// </summary>
    public readonly partial struct ActivityContext : IEquatable<ActivityContext>
    {
        /// <summary>
        /// Construct a new object of ActivityContext.
        /// </summary>
        /// <param name="traceId">A trace identifier.</param>
        /// <param name="spanId">A span identifier.</param>
        /// <param name="traceFlags">Contain details about the trace.</param>
        /// <param name="traceState">Carries system-specific configuration data.</param>
        /// <param name="isRemote">Indicate the context is propagated from remote parent.</param>
        /// <remarks>
        /// isRemote is not a part of W3C specification. It is needed for the OpenTelemetry scenarios.
        /// </remarks>
        public ActivityContext(ActivityTraceId traceId, ActivitySpanId spanId, ActivityTraceFlags traceFlags, string? traceState = null, bool isRemote = false)
        {
            TraceId = traceId;
            SpanId = spanId;
            TraceFlags = traceFlags;
            TraceState = traceState;
            IsRemote = isRemote;
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
        /// These flags are defined by the W3C standard along with the ID for the activity.
        /// </summary>
        public ActivityTraceFlags TraceFlags { get; }

        /// <summary>
        /// Holds the W3C 'tracestate' header as a string.
        /// </summary>
        public string? TraceState { get; }

        /// <summary>
        /// IsRemote indicates if the ActivityContext was propagated from a remote parent.
        /// </summary>
        /// <remarks>
        /// IsRemote is not a part of W3C specification. It is needed for the OpenTelemetry scenarios.
        /// </remarks>
        public bool IsRemote { get; }

        public bool Equals(ActivityContext value) =>  SpanId.Equals(value.SpanId) && TraceId.Equals(value.TraceId) && TraceFlags == value.TraceFlags && TraceState == value.TraceState;

        public override bool Equals(object? obj) => (obj is ActivityContext context) ? Equals(context) : false;
        public static bool operator ==(ActivityContext left, ActivityContext right) => left.Equals(right);
        public static bool operator !=(ActivityContext left, ActivityContext right) => !(left == right);
    }
}
