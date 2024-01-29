// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime
{
    // A conservative GC already scans the stack looking for potential object-refs or by-refs.
    // Mono uses a conservative GC so there is no need for this API to be full implemented.
    internal unsafe struct GCFrameRegistration
    {
#pragma warning disable IDE0060
        public GCFrameRegistration(void* allocation, uint elemCount, bool areByRefs = true)
        {
        }
#pragma warning restore IDE0060

        [Conditional("unnecessary")]
        internal static unsafe void RegisterForGCReporting(GCFrameRegistration* pRegistration) { /* nop */ }
        [Conditional("unnecessary")]
        internal static unsafe void UnregisterForGCReporting(GCFrameRegistration* pRegistration) { /* nop */ }
    }
}
