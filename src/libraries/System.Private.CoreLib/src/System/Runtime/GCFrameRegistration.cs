// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GCFrameRegistration
    {
#if CORECLR
        private const uint GCFrameValueClassFlag = 0x80000000;
#endif

        private nuint _reserved1;
        private nuint _reserved2;
        private void** _pObjRefs;
        private uint _numObjRefs;
        private uint _gcFlags;
#if FEATURE_INTERPRETER
        private nuint _osStackLocation;
#endif
#if CORECLR
        private void* _pValueClassInfo;
#endif

        public GCFrameRegistration(void** allocation, uint elemCount, bool areByRefs = true)
        {
            _reserved1 = 0;
            _reserved2 = 0;
            _pObjRefs = allocation;
            _numObjRefs = elemCount;
            _gcFlags = areByRefs ? 1u : 0;
#if FEATURE_INTERPRETER
            _osStackLocation = 0;
#endif
#if CORECLR
            _pValueClassInfo = null;
#endif
        }

#if CORECLR
        public GCFrameRegistration(void* valueClassInfo)
        {
            _reserved1 = 0;
            _reserved2 = 0;
            _pObjRefs = null;
            _numObjRefs = 0;
            _gcFlags = GCFrameValueClassFlag;
#if FEATURE_INTERPRETER
            _osStackLocation = 0;
#endif
            _pValueClassInfo = valueClassInfo;
        }
#endif

#if CORECLR
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void RegisterForGCReporting(GCFrameRegistration* pRegistration);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void UnregisterForGCReporting(GCFrameRegistration* pRegistration);
#endif
    }
}
