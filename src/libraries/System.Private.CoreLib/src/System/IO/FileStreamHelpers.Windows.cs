// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // this type defines a set of stateless FileStream/FileStreamStrategy helper methods
    internal static class FileStreamHelpers
    {
        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
            => new LegacyFileStreamStrategy(fileStream, handle, access, bufferSize, isAsync);

        internal static FileStreamStrategy ChooseStrategy(FileStream fileStream, string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => new LegacyFileStreamStrategy(fileStream, path, mode, access, share, bufferSize, options);

        internal static SafeFileHandle OpenHandle(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
            => CreateFileOpenHandle(path, mode, access, share, options);

        private static unsafe SafeFileHandle CreateFileOpenHandle(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(share);

            int fAccess =
                ((access & FileAccess.Read) == FileAccess.Read ? Interop.Kernel32.GenericOperations.GENERIC_READ : 0) |
                ((access & FileAccess.Write) == FileAccess.Write ? Interop.Kernel32.GenericOperations.GENERIC_WRITE : 0);

            // Our Inheritable bit was stolen from Windows, but should be set in
            // the security attributes class.  Don't leave this bit set.
            share &= ~FileShare.Inheritable;

            // Must use a valid Win32 constant here...
            if (mode == FileMode.Append)
                mode = FileMode.OpenOrCreate;

            int flagsAndAttributes = (int)options;

            // For mitigating local elevation of privilege attack through named pipes
            // make sure we always call CreateFile with SECURITY_ANONYMOUS so that the
            // named pipe server can't impersonate a high privileged client security context
            // (note that this is the effective default on CreateFile2)
            flagsAndAttributes |= (Interop.Kernel32.SecurityOptions.SECURITY_SQOS_PRESENT | Interop.Kernel32.SecurityOptions.SECURITY_ANONYMOUS);

            using (DisableMediaInsertionPrompt.Create())
            {
                Debug.Assert(path != null);
                return ValidateFileHandle(
                    Interop.Kernel32.CreateFile(path, fAccess, share, &secAttrs, mode, flagsAndAttributes, IntPtr.Zero),
                    path,
                    (options & FileOptions.Asynchronous) != 0);
            }
        }

        internal static bool GetDefaultIsAsync(SafeFileHandle handle, bool defaultIsAsync)
        {
            return handle.IsAsync ?? !IsHandleSynchronous(handle, ignoreInvalid: true) ?? defaultIsAsync;
        }

        private static unsafe bool? IsHandleSynchronous(SafeFileHandle fileHandle, bool ignoreInvalid)
        {
            if (fileHandle.IsInvalid)
                return null;

            uint fileMode;

            int status = Interop.NtDll.NtQueryInformationFile(
                FileHandle: fileHandle,
                IoStatusBlock: out _,
                FileInformation: &fileMode,
                Length: sizeof(uint),
                FileInformationClass: Interop.NtDll.FileModeInformation);

            switch (status)
            {
                case 0:
                    // We were successful
                    break;
                case Interop.NtDll.STATUS_INVALID_HANDLE:
                    if (!ignoreInvalid)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_INVALID_HANDLE);
                    }
                    else
                    {
                        return null;
                    }
                default:
                    // Something else is preventing access
                    Debug.Fail("Unable to get the file mode information, status was" + status.ToString());
                    return null;
            }

            // If either of these two flags are set, the file handle is synchronous (not overlapped)
            return (fileMode & (Interop.NtDll.FILE_SYNCHRONOUS_IO_ALERT | Interop.NtDll.FILE_SYNCHRONOUS_IO_NONALERT)) > 0;
        }

        internal static void VerifyHandleIsSync(SafeFileHandle handle)
        {
            // As we can accurately check the handle type when we have access to NtQueryInformationFile we don't need to skip for
            // any particular file handle type.

            // If the handle was passed in without an explicit async setting, we already looked it up in GetDefaultIsAsync
            if (!handle.IsAsync.HasValue)
                return;

            // If we can't check the handle, just assume it is ok.
            if (!(IsHandleSynchronous(handle, ignoreInvalid: false) ?? true))
                throw new ArgumentException(SR.Arg_HandleNotSync, nameof(handle));
        }

        private static unsafe Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(FileShare share)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default;
            if ((share & FileShare.Inheritable) != 0)
            {
                secAttrs = new Interop.Kernel32.SECURITY_ATTRIBUTES
                {
                    nLength = (uint)sizeof(Interop.Kernel32.SECURITY_ATTRIBUTES),
                    bInheritHandle = Interop.BOOL.TRUE
                };
            }
            return secAttrs;
        }

        private static SafeFileHandle ValidateFileHandle(SafeFileHandle fileHandle, string path, bool useAsyncIO)
        {
            if (fileHandle.IsInvalid)
            {
                // Return a meaningful exception with the full path.

                // NT5 oddity - when trying to open "C:\" as a Win32FileStream,
                // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                // probably be consistent w/ every other directory.
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && path!.Length == PathInternal.GetRootLength(path))
                    errorCode = Interop.Errors.ERROR_ACCESS_DENIED;

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            }

            fileHandle.IsAsync = useAsyncIO;
            return fileHandle;
        }

        internal static unsafe long GetFileLength(SafeFileHandle handle, string? path)
        {
            Interop.Kernel32.FILE_STANDARD_INFO info;

            if (!Interop.Kernel32.GetFileInformationByHandleEx(handle, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(path);
            }

            return info.EndOfFile;
        }

        internal static void FlushToDisk(SafeFileHandle handle, string? path)
        {
            if (!Interop.Kernel32.FlushFileBuffers(handle))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(path);
            }
        }

        internal static long Seek(SafeFileHandle handle, string? path, long offset, SeekOrigin origin, bool closeInvalidHandle = false)
        {
            Debug.Assert(origin >= SeekOrigin.Begin && origin <= SeekOrigin.End, "origin >= SeekOrigin.Begin && origin <= SeekOrigin.End");

            if (!Interop.Kernel32.SetFilePointerEx(handle, offset, out long ret, (uint)origin))
            {
                if (closeInvalidHandle)
                {
                    throw Win32Marshal.GetExceptionForWin32Error(GetLastWin32ErrorAndDisposeHandleIfInvalid(handle), path);
                }
                else
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error(path);
                }
            }

            return ret;
        }

        private static int GetLastWin32ErrorAndDisposeHandleIfInvalid(SafeFileHandle handle)
        {
            int errorCode = Marshal.GetLastWin32Error();

            // If ERROR_INVALID_HANDLE is returned, it doesn't suffice to set
            // the handle as invalid; the handle must also be closed.
            //
            // Marking the handle as invalid but not closing the handle
            // resulted in exceptions during finalization and locked column
            // values (due to invalid but unclosed handle) in SQL Win32FileStream
            // scenarios.
            //
            // A more mainstream scenario involves accessing a file on a
            // network share. ERROR_INVALID_HANDLE may occur because the network
            // connection was dropped and the server closed the handle. However,
            // the client side handle is still open and even valid for certain
            // operations.
            //
            // Note that _parent.Dispose doesn't throw so we don't need to special case.
            // SetHandleAsInvalid only sets _closed field to true (without
            // actually closing handle) so we don't need to call that as well.
            if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
            {
                handle.Dispose();
            }

            return errorCode;
        }

        internal static void Lock(SafeFileHandle handle, string? path, long position, long length)
        {
            int positionLow = unchecked((int)(position));
            int positionHigh = unchecked((int)(position >> 32));
            int lengthLow = unchecked((int)(length));
            int lengthHigh = unchecked((int)(length >> 32));

            if (!Interop.Kernel32.LockFile(handle, positionLow, positionHigh, lengthLow, lengthHigh))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(path);
            }
        }

        internal static void Unlock(SafeFileHandle handle, string? path, long position, long length)
        {
            int positionLow = unchecked((int)(position));
            int positionHigh = unchecked((int)(position >> 32));
            int lengthLow = unchecked((int)(length));
            int lengthHigh = unchecked((int)(length >> 32));

            if (!Interop.Kernel32.UnlockFile(handle, positionLow, positionHigh, lengthLow, lengthHigh))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error(path);
            }
        }

        internal static void ValidateFileTypeForNonExtendedPaths(SafeFileHandle handle, string originalPath)
        {
            if (!PathInternal.IsExtended(originalPath))
            {
                // To help avoid stumbling into opening COM/LPT ports by accident, we will block on non file handles unless
                // we were explicitly passed a path that has \\?\. GetFullPath() will turn paths like C:\foo\con.txt into
                // \\.\CON, so we'll only allow the \\?\ syntax.

                int fileType = Interop.Kernel32.GetFileType(handle);
                if (fileType != Interop.Kernel32.FileTypes.FILE_TYPE_DISK)
                {
                    int errorCode = fileType == Interop.Kernel32.FileTypes.FILE_TYPE_UNKNOWN
                        ? Marshal.GetLastWin32Error()
                        : Interop.Errors.ERROR_SUCCESS;

                    handle.Dispose();

                    if (errorCode != Interop.Errors.ERROR_SUCCESS)
                    {
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                    }
                    throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
                }
            }
        }

        internal static void GetFileTypeSpecificInformation(SafeFileHandle handle, out bool canSeek, out bool isPipe)
        {
            int handleType = Interop.Kernel32.GetFileType(handle);
            Debug.Assert(handleType == Interop.Kernel32.FileTypes.FILE_TYPE_DISK
                || handleType == Interop.Kernel32.FileTypes.FILE_TYPE_PIPE
                || handleType == Interop.Kernel32.FileTypes.FILE_TYPE_CHAR,
                "FileStream was passed an unknown file type!");

            canSeek = handleType == Interop.Kernel32.FileTypes.FILE_TYPE_DISK;
            isPipe = handleType == Interop.Kernel32.FileTypes.FILE_TYPE_PIPE;
        }

        internal static unsafe void SetLength(SafeFileHandle handle, string? path, long length)
        {
            var eofInfo = new Interop.Kernel32.FILE_END_OF_FILE_INFO
            {
                EndOfFile = length
            };

            if (!Interop.Kernel32.SetFileInformationByHandle(
                handle,
                Interop.Kernel32.FileEndOfFileInfo,
                &eofInfo,
                (uint)sizeof(Interop.Kernel32.FILE_END_OF_FILE_INFO)))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                    throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_FileLengthTooBig);
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            }
        }

        // __ConsoleStream also uses this code.
        internal static unsafe int ReadFileNative(SafeFileHandle handle, Span<byte> bytes, NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert(handle != null, "handle != null");

            int r;
            int numBytesRead = 0;

            fixed (byte* p = &MemoryMarshal.GetReference(bytes))
            {
                r = overlapped != null ?
                    Interop.Kernel32.ReadFile(handle, p, bytes.Length, IntPtr.Zero, overlapped) :
                    Interop.Kernel32.ReadFile(handle, p, bytes.Length, out numBytesRead, IntPtr.Zero);
            }

            if (r == 0)
            {
                errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                return -1;
            }
            else
            {
                errorCode = 0;
                return numBytesRead;
            }
        }

        internal static unsafe int WriteFileNative(SafeFileHandle handle, ReadOnlySpan<byte> buffer, NativeOverlapped* overlapped, out int errorCode)
        {
            Debug.Assert(handle != null, "handle != null");

            int numBytesWritten = 0;
            int r;

            fixed (byte* p = &MemoryMarshal.GetReference(buffer))
            {
                r = overlapped != null ?
                    Interop.Kernel32.WriteFile(handle, p, buffer.Length, IntPtr.Zero, overlapped) :
                    Interop.Kernel32.WriteFile(handle, p, buffer.Length, out numBytesWritten, IntPtr.Zero);
            }

            if (r == 0)
            {
                errorCode = GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
                return -1;
            }
            else
            {
                errorCode = 0;
                return numBytesWritten;
            }
        }
    }
}
