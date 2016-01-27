// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: UCOMIEnumConnectionPoints interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{   
    using System;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IEnumConnectionPoints instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("B196B285-BAB4-101A-B69C-00AA00341D07")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMIEnumConnectionPoints
    {       
        [PreserveSig]
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] UCOMIConnectionPoint[] rgelt, out int pceltFetched);
        [PreserveSig]
        int Skip(int celt);
        [PreserveSig]
        int Reset();
        void Clone(out UCOMIEnumConnectionPoints ppenum);
    }
}
