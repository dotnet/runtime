// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Management
{

    [ComImport, Guid("87A5AD68-A38A-43ef-ACA9-EFE910E5D24C"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IWmiEventSource
    {
        void Indicate(IntPtr pIWbemClassObject);

        void SetStatus(
            int lFlags,
            int hResult,
            [MarshalAs(UnmanagedType.BStr)] string strParam,
            IntPtr pObjParam
        );
    }

    //Class for calling GetErrorInfo from managed code
    internal static class WbemErrorInfo
    {
        public static IWbemClassObjectFreeThreaded GetErrorInfo()
        {
            IntPtr pErrorInfo = WmiNetUtilsHelper.GetErrorInfo_f();
            if (IntPtr.Zero != pErrorInfo && new IntPtr(-1) != pErrorInfo)
            {
                IntPtr pIWbemClassObject;
                Marshal.QueryInterface(pErrorInfo, ref IWbemClassObjectFreeThreaded.IID_IWbemClassObject, out pIWbemClassObject);
                Marshal.Release(pErrorInfo);

                // The IWbemClassObjectFreeThreaded instance will own reference count on pIWbemClassObject
                if (pIWbemClassObject != IntPtr.Zero)
                    return new IWbemClassObjectFreeThreaded(pIWbemClassObject);
            }
            return null;
        }
    }

    //RCW for IErrorInfo
    [ComImport]
    [Guid("1CF2B120-547D-101B-8E65-08002B2BD119")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IErrorInfo
    {
        Guid GetGUID();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetSource();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetDescription();

        [return: MarshalAs(UnmanagedType.BStr)]
        string GetHelpFile();

        uint GetHelpContext();
    }

}
