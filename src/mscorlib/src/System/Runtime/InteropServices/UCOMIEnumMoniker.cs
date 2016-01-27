// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: UCOMIEnumMoniker interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{
    using System;
    using DWORD = System.UInt32;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumMoniker instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("00000102-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMIEnumMoniker 
    {
        [PreserveSig]
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] UCOMIMoniker[] rgelt, out int pceltFetched);
        [PreserveSig]
        int Skip(int celt);
        [PreserveSig]
        int Reset();
        void Clone(out UCOMIEnumMoniker ppenum);
    }
}
