// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class HandleFactory
{
    public static SafeHandle CreateHandle(IntPtr handle)
    {
        return new Win32Handle(handle);
    }

    public static Win32Handle CreateSyncFileHandleForWrite(string fileName = null)
    {
        return CreateHandle(async:false, fileName:fileName);
    }

    public static Win32Handle CreateAsyncFileHandleForWrite(string fileName = null)
    {
        return CreateHandle(async:true, fileName:fileName);
    }

    private static unsafe Win32Handle CreateHandle(bool async, string fileName = null)
    {
#if !uap
        // Assume the current directory is writable
        return DllImport.CreateFile(fileName ?? @"Overlapped.tmp", DllImport.FileAccess.GenericWrite, DllImport.FileShare.Write, IntPtr.Zero, DllImport.CreationDisposition.CreateAlways, async ? DllImport.FileAttributes.Overlapped : DllImport.FileAttributes.Normal, IntPtr.Zero);
#else
        var p = new DllImport.CREATEFILE2_EXTENDED_PARAMETERS();
        p.dwSize = (uint)sizeof(DllImport.CREATEFILE2_EXTENDED_PARAMETERS);
        p.dwFileAttributes = DllImport.FileAttributes.Normal;
        p.dwFileFlags = async ? DllImport.FileAttributes.Overlapped : DllImport.FileAttributes.Normal;
        p.dwSecurityQosFlags = (uint)0;
        p.lpSecurityAttributes = IntPtr.Zero;
        p.hTemplateFile = IntPtr.Zero;
        return DllImport.CreateFile2(
            fileName ?? @"Overlapped.tmp",
            DllImport.FileAccess.GenericWrite,
            DllImport.FileShare.Write,
            DllImport.CreationDisposition.CreateAlways,
            &p);
#endif
    }
}
