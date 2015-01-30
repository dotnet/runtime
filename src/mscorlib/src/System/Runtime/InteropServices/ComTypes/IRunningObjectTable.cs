// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
**
** Purpose: IRunningObjectTable interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    [Guid("00000010-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IRunningObjectTable 
    {
        int Register(int grfFlags, [MarshalAs(UnmanagedType.Interface)] Object punkObject, IMoniker pmkObjectName);
        void Revoke(int dwRegister);
        [PreserveSig]
        int IsRunning(IMoniker pmkObjectName);
        [PreserveSig]
        int GetObject(IMoniker pmkObjectName, [MarshalAs(UnmanagedType.Interface)] out Object ppunkObject);
        void NoteChangeTime(int dwRegister, ref FILETIME pfiletime);
        [PreserveSig]
        int GetTimeOfLastChange(IMoniker pmkObjectName, out FILETIME pfiletime);
        void EnumRunning(out IEnumMoniker ppenumMoniker);
    }
}
