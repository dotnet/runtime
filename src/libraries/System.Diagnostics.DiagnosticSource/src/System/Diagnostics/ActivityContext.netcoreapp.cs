// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityContext representation conforms to the w3c TraceContext specification. It contains two identifiers
    /// a TraceId and a SpanId - along with a set of common TraceFlags and system-specific TraceState values.
    /// </summary>
    public readonly partial struct ActivityContext : IEquatable<ActivityContext>
    {
        public override int GetHashCode() => HashCode.Combine(TraceId, SpanId, TraceFlags, TraceState);
    }
}
