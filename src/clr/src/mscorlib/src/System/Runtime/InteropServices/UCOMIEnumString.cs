// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: UCOMIEnumString interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{
    using System;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumString instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("00000101-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMIEnumString 
    {
        [PreserveSig]
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 0), Out] String[] rgelt, out int pceltFetched);
        [PreserveSig]
        int Skip(int celt);
        [PreserveSig]
        int Reset();
        void Clone(out UCOMIEnumString ppenum);
    }
}
