// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime
{
    internal static class RuntimeImports
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RegisterForGCReporting(GCFrameRegistration* pRegistration);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void UnregisterForGCReporting(GCFrameRegistration* pRegistration);
    }
}
