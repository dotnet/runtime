// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    public class StandardOleMarshalObject : MarshalByRefObject, IMarshal
    {
        protected StandardOleMarshalObject()
        {
        }

        int IMarshal.GetUnmarshalClass(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out Guid pCid)
        {
            pCid = Guid.Empty;
            return HResults.E_NOTIMPL;
        }

        int IMarshal.GetMarshalSizeMax(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize)
        {
            pSize = -1;
            return HResults.E_NOTIMPL;
        }

        int IMarshal.MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags)
        {
            return HResults.E_NOTIMPL;
        }

        int IMarshal.UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;
            return HResults.E_NOTIMPL;
        }

        int IMarshal.ReleaseMarshalData(IntPtr pStm)
        {
            return HResults.E_NOTIMPL;
        }

        int IMarshal.DisconnectObject(int dwReserved)
        {
            return HResults.E_NOTIMPL;
        }
    }
}
