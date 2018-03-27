// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;

public class COMLib
{
    [Guid("00020404-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IEnumVARIANT
    {
        [PreserveSig]
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] object[] rgVar, IntPtr pceltFetched);

        [PreserveSig]
        int Skip(int celt);

        [PreserveSig]
        int Reset();

        IEnumVARIANT Clone();
    }

    [ComImport]
    [Guid("78A51822-51F4-11D0-8F20-00805F2CD064")]
    public class ProcessDebugManager
    {
    }
}
