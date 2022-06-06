// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public class StandardOleMarshalObject : MarshalByRefObject, IMarshal
    {
        private static readonly Guid CLSID_StdMarshal = new Guid("00000017-0000-0000-c000-000000000046");

        protected StandardOleMarshalObject()
        {
        }

        private nint GetStdMarshaler(ref Guid riid, int dwDestContext, int mshlflags)
        {
            Debug.Assert(OperatingSystem.IsWindows());
            nint pUnknown = Marshal.GetIUnknownForObject(this);
            if (pUnknown != 0)
            {
                try
                {
                    nint pStandardMarshal = 0;
                    int hr = Interop.Ole32.CoGetStandardMarshal(ref riid, pUnknown, dwDestContext, 0, mshlflags, out pStandardMarshal);
                    if (hr == HResults.S_OK)
                    {
                        Debug.Assert(pStandardMarshal != 0, $"Failed to get marshaler for interface '{riid}', CoGetStandardMarshal returned S_OK");
                        return pStandardMarshal;
                    }
                }
                finally
                {
                    Marshal.Release(pUnknown);
                }
            }

            throw new InvalidOperationException(SR.Format(SR.StandardOleMarshalObjectGetMarshalerFailed, riid));
        }

        int IMarshal.GetUnmarshalClass(ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags, out Guid pCid)
        {
            pCid = CLSID_StdMarshal;
            return HResults.S_OK;
        }

        unsafe int IMarshal.GetMarshalSizeMax(ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags, out int pSize)
        {
            nint pStandardMarshal = GetStdMarshaler(ref riid, dwDestContext, mshlflags);

            try
            {
                // We must not wrap pStandardMarshal with an RCW because that
                // would trigger QIs for random IIDs and the marshaler (aka stub
                // manager object) does not really handle these well and we would
                // risk triggering an AppVerifier break
                fixed (Guid* riidPtr = &riid)
                fixed (int* pSizePtr = &pSize)
                {
                    // GetMarshalSizeMax is 5th slot (zero-based indexing)
                    return ((delegate* unmanaged[Stdcall]<nint, Guid*, nint, int, nint, int, int*, int>)(*(nint**)pStandardMarshal)[4])(pStandardMarshal, riidPtr, pv, dwDestContext, pvDestContext, mshlflags, pSizePtr);
                }
            }
            finally
            {
                Debug.Assert(OperatingSystem.IsWindows());
                Marshal.Release(pStandardMarshal);
            }
        }

        unsafe int IMarshal.MarshalInterface(nint pStm, ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags)
        {
            nint pStandardMarshal = GetStdMarshaler(ref riid, dwDestContext, mshlflags);

            try
            {
                // We must not wrap pStandardMarshal with an RCW because that
                // would trigger QIs for random IIDs and the marshaler (aka stub
                // manager object) does not really handle these well and we would
                // risk triggering an AppVerifier break
                fixed (Guid* riidPtr = &riid)
                {
                    // MarshalInterface is 6th slot (zero-based indexing)
                    return ((delegate* unmanaged[Stdcall]<nint, nint, Guid*, nint, int, nint, int, int>)(*(nint**)pStandardMarshal)[5])(pStandardMarshal, pStm, riidPtr, pv, dwDestContext, pvDestContext, mshlflags);
                }
            }
            finally
            {
                Debug.Assert(OperatingSystem.IsWindows());
                Marshal.Release(pStandardMarshal);
            }
        }

        int IMarshal.UnmarshalInterface(nint pStm, ref Guid riid, out nint ppv)
        {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::UnmarshalInterface should not be called.");
            ppv = 0;
            return HResults.E_NOTIMPL;
        }

        int IMarshal.ReleaseMarshalData(nint pStm)
        {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::ReleaseMarshalData should not be called.");
            return HResults.E_NOTIMPL;
        }

        int IMarshal.DisconnectObject(int dwReserved)
        {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::DisconnectObject should not be called.");
            return HResults.E_NOTIMPL;
        }
    }

    [ComImport]
    [Guid("00000003-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMarshal
    {
        [PreserveSig]
        int GetUnmarshalClass(ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags, out Guid pCid);
        [PreserveSig]
        int GetMarshalSizeMax(ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags, out int pSize);
        [PreserveSig]
        int MarshalInterface(nint pStm, ref Guid riid, nint pv, int dwDestContext, nint pvDestContext, int mshlflags);
        [PreserveSig]
        int UnmarshalInterface(nint pStm, ref Guid riid, out nint ppv);
        [PreserveSig]
        int ReleaseMarshalData(nint pStm);
        [PreserveSig]
        int DisconnectObject(int dwReserved);
    }
}
