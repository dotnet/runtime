// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class HandleFactory
{
    public static SafeHandle CreateHandle(IntPtr handle)
    {
        return new Win32Handle(handle);
    }

    public static Win32Handle CreateSyncFileHandleForWrite(string fileName)
    {
        return CreateHandle(async:false, fileName:fileName);
    }

    public static Win32Handle CreateAsyncFileHandleForWrite(string fileName)
    {
        return CreateHandle(async:true, fileName:fileName);
    }

    private static unsafe Win32Handle CreateHandle(bool async, string fileName)
    {
        Win32Handle handle = DllImport.CreateFile(
            fileName,
            DllImport.FileAccess.GenericWrite,
            DllImport.FileShare.Write,
            IntPtr.Zero,
            DllImport.CreationDisposition.CreateAlways,
            async ? DllImport.FileAttributes.Overlapped : DllImport.FileAttributes.Normal,
            IntPtr.Zero);

        if (!handle.IsInvalid)
        {
            return handle;
        }

        int errorCode = Marshal.GetLastWin32Error();
        string filePath = Path.GetFullPath(fileName);
        string message =
            $"CreateFile or CreateFile2 failed (error code {errorCode}): {new Win32Exception(errorCode).Message}{Environment.NewLine}" +
            $"    File name: {fileName}{Environment.NewLine}" +
            $"    File path: {filePath}{Environment.NewLine}";
        if (Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            try
            {
                File.WriteAllText(filePath, string.Empty);
                message += "    Successfully wrote to the file.";
            }
            catch (Exception ex)
            {
                message += $"    Failed to write to the file: {ex}";
            }
        }
        else
        {
            message += $"    Directory does not exist.";
        }

        throw new IOException(message);
    }
}
