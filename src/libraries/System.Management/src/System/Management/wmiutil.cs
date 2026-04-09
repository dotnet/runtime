// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

// We need to target netstandard2.0, so keep using ref for MemoryMarshal.Write
// CS9191: The 'ref' modifier for argument 2 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
#pragma warning disable CS9191

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
#pragma warning disable CS9191 // The 'ref' modifier for argument 1 corresponding to 'in' parameter is equivalent to 'in'. Consider using 'in' instead.
                Marshal.QueryInterface(pErrorInfo, ref IWbemClassObjectFreeThreaded.IID_IWbemClassObject, out pIWbemClassObject);
#pragma warning restore CS9191
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
