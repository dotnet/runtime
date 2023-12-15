// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GCFrameRegistration
    {
        private nuint _reserved1;
        private nuint _reserved2;
        private void** _pObjRefs;
        private uint _numObjRefs;
        private int _maybeInterior;

        public GCFrameRegistration(void** allocation, uint elemCount, bool areByRefs = true)
        {
            _reserved1 = 0;
            _reserved2 = 0;
            _pObjRefs = allocation;
            _numObjRefs = elemCount;
            _maybeInterior = areByRefs ? 1 : 0;
        }

#if CORECLR
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RegisterForGCReporting(GCFrameRegistration* pRegistration);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void UnregisterForGCReporting(GCFrameRegistration* pRegistration);
#endif
    }
}
