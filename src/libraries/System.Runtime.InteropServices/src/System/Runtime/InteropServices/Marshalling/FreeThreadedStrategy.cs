// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

// Implementations of the COM strategy interfaces defined in Com.cs that we would want to ship (can be internal only if we don't want to allow users to provide their own implementations in v1).
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    internal sealed unsafe class FreeThreadedStrategy : IIUnknownStrategy
    {
        public static readonly IIUnknownStrategy Instance = new FreeThreadedStrategy();

        void* IIUnknownStrategy.CreateInstancePointer(void* unknown)
        {
            AssertFreeThreaded(unknown);
            Marshal.AddRef((nint)unknown);
            return unknown;
        }

        unsafe int IIUnknownStrategy.QueryInterface(void* thisPtr, in Guid handle, out void* ppObj)
        {
            int hr = Marshal.QueryInterface((nint)thisPtr, handle, out nint ppv);
            if (hr < 0)
            {
                ppObj = null;
            }
            else
            {
                ppObj = (void*)ppv;
            }
            return hr;
        }

        unsafe int IIUnknownStrategy.Release(void* thisPtr)
        {
            // Avoid checking if the instance is free-threaded here,
            // since this method can be called from the GC finalizer thread
            // and we need to QI, which may not be safe if the object is not free-threaded.
            return Marshal.Release((nint)thisPtr);
        }

        // This strategy assumes every COM object it is given is free threaded (agile), so its
        // IUnknown methods can be called from any thread; including Release on the GC finalizer
        // thread.
        [Conditional("DEBUG")]
        private static void AssertFreeThreaded(void* thisPtr)
        {
#if DEBUG
            if (OperatingSystem.IsWindows())
            {
                Debug.Assert(
                    IsFreeThreaded(thisPtr),
                    "A COM object used through FreeThreadedStrategy is not free threaded (agile).");
            }
#endif
        }

#if DEBUG
        // Mirrors the built-in RCW's IUnkEntry::IsComponentFreeThreaded (src/coreclr/vm/comcache.cpp).
        private static bool IsFreeThreaded(void* thisPtr)
        {
            // IID_IAgileObject {94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90}
            Guid iidAgileObject = new(0x94ea2b94, 0xe9cc, 0x49e0, 0xc0, 0xff, 0xee, 0x64, 0xca, 0x8f, 0x5b, 0x90);
            if (Marshal.QueryInterface((nint)thisPtr, iidAgileObject, out nint agile) >= 0)
            {
                Marshal.Release(agile);
                return true;
            }

            // IID_IMarshal {00000003-0000-0000-C000-000000000046}
            Guid iidMarshal = new(0x00000003, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
            if (Marshal.QueryInterface((nint)thisPtr, iidMarshal, out nint marshalUnk) >= 0)
            {
                try
                {
                    void* pMarshal = (void*)marshalUnk;

                    // IMarshal::GetUnmarshalClass is the first IMarshal method (that is, 4th zero-indexed slot).
                    // HRESULT GetUnmarshalClass(REFIID riid, void* pv, DWORD dwDestContext,
                    //                           void* pvDestContext, DWORD mshlflags, CLSID* pCid)
                    var getUnmarshalClass =
                        (delegate* unmanaged[MemberFunction]<void*, Guid*, void*, uint, void*, uint, Guid*, int>)((*(void***)pMarshal)[3]);

                    // IID_IUnknown {00000000-0000-0000-C000-000000000046}
                    Guid iidUnknown = new(0x00000000, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
                    const uint MSHCTX_INPROC = 3;
                    const uint MSHLFLAGS_NORMAL = 0;

                    Guid unmarshalClass;
                    int hr = getUnmarshalClass(pMarshal, &iidUnknown, null, MSHCTX_INPROC, null, MSHLFLAGS_NORMAL, &unmarshalClass);

                    // CLSID_InProcFreeMarshaler {0000033A-0000-0000-C000-000000000046}
                    Guid clsidFreeMarshaler = new(0x0000033a, 0x0000, 0x0000, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46);
                    if (hr >= 0 && unmarshalClass == clsidFreeMarshaler)
                    {
                        return true;
                    }
                }
                finally
                {
                    Marshal.Release(marshalUnk);
                }
            }

            return false;
        }
#endif
    }
}
