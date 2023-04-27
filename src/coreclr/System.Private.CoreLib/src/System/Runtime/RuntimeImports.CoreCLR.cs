// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime
{
    internal static class RuntimeImports
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhRegisterForGCReporting(GCFrameRegistration* pRegistration);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe void RhUnregisterForGCReporting(GCFrameRegistration* pRegistration);

#pragma warning disable 0414, IDE0044
        // Type that represents a managed view of the unmanaged GCFrame
        // data structure in coreclr. The type layouts between the two should match.
        internal unsafe struct GCFrameRegistration
        {
            private nuint m_reserved1;
            private nuint m_reserved2;
            private void** m_pObjRefs;
            private uint m_numObjRefs;
            private int m_MaybeInterior;

            public GCFrameRegistration(void** allocation, uint elemCount, bool areByRefs = true)
            {
                m_reserved1 = 0;
                m_reserved2 = 0;
                m_pObjRefs = allocation;
                m_numObjRefs = elemCount;
                m_MaybeInterior = areByRefs ? 1 : 0;
            }
        }
#pragma warning restore 0414, IDE0044
    }
}
