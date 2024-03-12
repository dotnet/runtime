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
        public override int GetHashCode()
        {
            if (this == default)
                return 0;

            // HashCode.Combine would be the best but we need to compile for the full framework which require adding dependency
            // on the extensions package. Considering this simple type and hashing is not expected to be used much, we are implementing
            // the hashing manually.
            int hash = 5381;
            hash = ((hash << 5) + hash) + TraceId.GetHashCode();
            hash = ((hash << 5) + hash) + SpanId.GetHashCode();
            hash = ((hash << 5) + hash) + (int)TraceFlags;
            hash = ((hash << 5) + hash) + (TraceState == null ? 0 : TraceState.GetHashCode());

            return hash;
        }
    }
}
