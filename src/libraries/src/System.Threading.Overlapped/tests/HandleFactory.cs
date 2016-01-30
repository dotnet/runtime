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

    public static Win32Handle CreateSyncFileHandleFoWrite()
    {
        return CreateHandle(async: false);
    }

    public static Win32Handle CreateAsyncFileHandleForWrite(string fileName = null)
    {
        return CreateHandle(async:true, fileName:fileName);
    }

    private static Win32Handle CreateHandle(bool async, string fileName = null)
    {
        // Assume the current directory is writable
        return DllImport.CreateFile(fileName ?? @"Overlapped.tmp", DllImport.FileAccess.GenericWrite, DllImport.FileShare.Write, IntPtr.Zero, DllImport.CreationDisposition.CreateAlways, async ? DllImport.FileAttributes.Overlapped : DllImport.FileAttributes.Normal, IntPtr.Zero);
    }
}
