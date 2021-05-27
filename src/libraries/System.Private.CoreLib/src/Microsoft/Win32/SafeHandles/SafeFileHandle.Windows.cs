// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Strategies;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private bool? _isAsync;

        public SafeFileHandle() : base(true)
        {
            _isAsync = null;
        }

        public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);

            _isAsync = null;
        }

        internal bool? IsAsync
        {
            get => _isAsync;
            set => _isAsync = value;
        }

        internal ThreadPoolBoundHandle? ThreadPoolBinding { get; set; }

        protected override bool ReleaseHandle() =>
            Interop.Kernel32.CloseHandle(handle);

        internal static unsafe SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            using (DisableMediaInsertionPrompt.Create())
            {
                Debug.Assert(fullPath != null);

                if (FileStreamHelpers.ShouldPreallocate(preallocationSize, access, mode))
                {
                    IntPtr fileHandle = NtCreateFile(fullPath, mode, access, share, options, preallocationSize);

                    return ValidateFileHandle(new SafeFileHandle(fileHandle, ownsHandle: true), fullPath, (options & FileOptions.Asynchronous) != 0);
                }

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

                SafeFileHandle safeFileHandle = ValidateFileHandle(
                    Interop.Kernel32.CreateFile(fullPath, fAccess, share, &secAttrs, mode, flagsAndAttributes, IntPtr.Zero),
                    fullPath,
                    (options & FileOptions.Asynchronous) != 0);

                return safeFileHandle;
            }
        }

        private static IntPtr NtCreateFile(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            uint ntStatus;
            IntPtr fileHandle;

            const string mandatoryNtPrefix = @"\??\";
            if (fullPath.StartsWith(mandatoryNtPrefix, StringComparison.Ordinal))
            {
                (ntStatus, fileHandle) = Interop.NtDll.CreateFile(fullPath, mode, access, share, options, preallocationSize);
            }
            else
            {
                var vsb = new ValueStringBuilder(stackalloc char[1024]);
                vsb.Append(mandatoryNtPrefix);

                if (fullPath.StartsWith(@"\\?\", StringComparison.Ordinal)) // NtCreateFile does not support "\\?\" prefix, only "\??\"
                {
                    vsb.Append(fullPath.AsSpan(4));
                }
                else
                {
                    vsb.Append(fullPath);
                }

                (ntStatus, fileHandle) = Interop.NtDll.CreateFile(vsb.AsSpan(), mode, access, share, options, preallocationSize);
                vsb.Dispose();
            }

            switch (ntStatus)
            {
                case 0:
                    return fileHandle;
                case Interop.NtDll.NT_ERROR_STATUS_DISK_FULL:
                    throw new IOException(SR.Format(SR.IO_DiskFull_Path_AllocationSize, fullPath, preallocationSize));
                // NtCreateFile has a bug and it reports STATUS_INVALID_PARAMETER for files
                // that are too big for the current file system. Example: creating a 4GB+1 file on a FAT32 drive.
                case Interop.NtDll.NT_STATUS_INVALID_PARAMETER:
                case Interop.NtDll.NT_ERROR_STATUS_FILE_TOO_LARGE:
                    throw new IOException(SR.Format(SR.IO_FileTooLarge_Path_AllocationSize, fullPath, preallocationSize));
                default:
                    int error = (int)Interop.NtDll.RtlNtStatusToDosError((int)ntStatus);
                    throw Win32Marshal.GetExceptionForWin32Error(error, fullPath);
            }
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
                int errorCode = Marshal.GetLastPInvokeError();

                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && path!.Length == PathInternal.GetRootLength(path))
                    errorCode = Interop.Errors.ERROR_ACCESS_DENIED;

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            }

            fileHandle.IsAsync = useAsyncIO;
            return fileHandle;
        }
    }
}
