// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: IEnumConnectionPoints interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    [Guid("B196B285-BAB4-101A-B69C-00AA00341D07")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IEnumConnectionPoints
    {       
        [PreserveSig]
        int Next(int celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] IConnectionPoint[] rgelt, IntPtr pceltFetched);
        [PreserveSig]
        int Skip(int celt);
        void Reset();
        void Clone(out IEnumConnectionPoints ppenum);
    }
}
