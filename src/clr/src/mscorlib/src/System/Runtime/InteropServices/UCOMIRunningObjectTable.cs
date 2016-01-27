// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: UCOMIRunningObjectTable interface definition.
**
**
=============================================================================*/

namespace System.Runtime.InteropServices
{
    using System;

    [Obsolete("Use System.Runtime.InteropServices.ComTypes.IRunningObjectTable instead. http://go.microsoft.com/fwlink/?linkid=14202", false)]
    [Guid("00000010-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface UCOMIRunningObjectTable 
    {
        void Register(int grfFlags, [MarshalAs(UnmanagedType.Interface)] Object punkObject, UCOMIMoniker pmkObjectName, out int pdwRegister);
        void Revoke(int dwRegister);
        void IsRunning(UCOMIMoniker pmkObjectName);
        void GetObject(UCOMIMoniker pmkObjectName, [MarshalAs(UnmanagedType.Interface)] out Object ppunkObject);
        void NoteChangeTime(int dwRegister, ref FILETIME pfiletime);
        void GetTimeOfLastChange(UCOMIMoniker pmkObjectName, out FILETIME pfiletime);
        void EnumRunning(out UCOMIEnumMoniker ppenumMoniker);
    }
}
