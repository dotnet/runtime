// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public class StandardOleMarshalObject : MarshalByRefObject, IMarshal
    {
        private static readonly Guid CLSID_StdMarshal = new Guid("00000017-0000-0000-c000-000000000046");
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetMarshalSizeMax_Delegate(IntPtr _this, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize);
        
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int MarshalInterface_Delegate(IntPtr _this, IntPtr pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags);

        protected StandardOleMarshalObject() { }

        private IntPtr GetStdMarshaler(ref Guid riid, int dwDestContext, int mshlflags)
        {
            IntPtr pStandardMarshal = IntPtr.Zero;

            IntPtr pUnknown = Marshal.GetIUnknownForObject(this);
            if (pUnknown != IntPtr.Zero)
            {
                try
                {
                    if (HResults.S_OK == Interop.Ole32.CoGetStandardMarshal(ref riid, pUnknown, dwDestContext, IntPtr.Zero, mshlflags, out pStandardMarshal))
                    {
                        Debug.Assert(pStandardMarshal != IntPtr.Zero, "Failed to get marshaler for interface '" + riid.ToString() + "', CoGetStandardMarshal returned S_OK");
                        return pStandardMarshal;
                    }
                }
                finally
                {
                    Marshal.Release(pUnknown);
                }
            }
            throw new InvalidOperationException(string.Format(SR.StandardOleMarshalObjectGetMarshalerFailed, riid.ToString()));
        }

        int IMarshal.GetUnmarshalClass(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out Guid pCid)
        {
            pCid = CLSID_StdMarshal;
            return HResults.S_OK;
        }

        unsafe int IMarshal.GetMarshalSizeMax(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize)
        {
            IntPtr pStandardMarshal = GetStdMarshaler(ref riid, dwDestContext, mshlflags);

            try
            {
                // we must not wrap pStandardMarshal with an RCW because that would trigger QIs for random IIDs and the marshaler
                // (aka stub manager object) does not really handle these well and we would risk triggering an AppVerifier break
                IntPtr vtable = *(IntPtr*)pStandardMarshal.ToPointer();
                IntPtr method = *((IntPtr*)vtable.ToPointer() + 4); // GetMarshalSizeMax is 4th slot

                GetMarshalSizeMax_Delegate del = (GetMarshalSizeMax_Delegate)Marshal.GetDelegateForFunctionPointer(method, typeof(GetMarshalSizeMax_Delegate));
                return del(pStandardMarshal, ref riid, pv, dwDestContext, pvDestContext, mshlflags, out pSize);
            }
            finally
            {
                Marshal.Release(pStandardMarshal);
            }
        }

        unsafe int IMarshal.MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags)
        {
            IntPtr pStandardMarshal = GetStdMarshaler(ref riid, dwDestContext, mshlflags);

            try
            {
                // we must not wrap pStandardMarshal with an RCW because that would trigger QIs for random IIDs and the marshaler
                // (aka stub manager object) does not really handle these well and we would risk triggering an AppVerifier break
                IntPtr vtable = *(IntPtr*)pStandardMarshal.ToPointer();
                IntPtr method = *((IntPtr*)vtable.ToPointer() + 5); // MarshalInterface is 5th slot

                MarshalInterface_Delegate del = (MarshalInterface_Delegate)Marshal.GetDelegateForFunctionPointer(method, typeof(MarshalInterface_Delegate));
                return del(pStandardMarshal, pStm, ref riid, pv, dwDestContext, pvDestContext, mshlflags);
            }
            finally
            {
                Marshal.Release(pStandardMarshal);
            }
        }

        int IMarshal.UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv)
        {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::UnmarshalInterface should not be called.");
            ppv = IntPtr.Zero;
            return HResults.E_NOTIMPL;
        }

        int IMarshal.ReleaseMarshalData(IntPtr pStm)
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
}
