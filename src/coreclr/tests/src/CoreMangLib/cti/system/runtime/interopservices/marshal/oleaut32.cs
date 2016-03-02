// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;

namespace OleAut32
{
    [Guid("22F03340-547D-101B-8E65-08002B2BD119")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public unsafe interface ICreateErrorInfo
    {
        void QueryInterface(ref Guid riid, out IntPtr *ppvObject);

        long AddRef();

        long Release();

        void SetGUID(ref Guid rguid);

        void SetSource(string szSource);

        void SetDescription(string szDescription);

        void SetHelpFile(string szHelpFile);

        void SetHelpContext(int dwHelpContext);
    }

    [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IErrorInfo
    {
        void GetGUID(IntPtr pGUID);
        
        void GetSource(ref string pBstrSource);
        
        void GetDescription(ref string pBstrDescription);
        
        void GetHelpFile(ref string pBstrHelpFile);
        
        void GetHelpContext(ref int pdwHelpContext);
    }
}