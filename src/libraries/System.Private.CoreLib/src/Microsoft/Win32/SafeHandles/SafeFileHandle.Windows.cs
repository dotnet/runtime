// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
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

        private SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle, bool isAsync) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);

            _isAsync = isAsync;
        }

        public bool IsAsync
        {
            get
            {
                if (IsInvalid)
                {
                    throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_INVALID_HANDLE);
                }

                if (_isAsync == null)
                {
                    _isAsync = !IsHandleSynchronous();
                }

                return _isAsync.Value;
            }
        }

        internal ThreadPoolBoundHandle? ThreadPoolBinding { get; set; }

        protected override bool ReleaseHandle() => Interop.Kernel32.CloseHandle(handle);

        internal static unsafe SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            using (DisableMediaInsertionPrompt.Create())
            {
                SafeFileHandle fileHandle = new SafeFileHandle(
                    NtCreateFile(fullPath, mode, access, share, options, preallocationSize),
                    ownsHandle: true,
                    isAsync: (options & FileOptions.Asynchronous) != 0);

                fileHandle.Validate(fullPath);

                fileHandle.InitThreadPoolBindingIfNeeded();

                return fileHandle;
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
                case Interop.NtDll.NT_STATUS_INVALID_PARAMETER when preallocationSize > 0:
                case Interop.NtDll.NT_ERROR_STATUS_FILE_TOO_LARGE:
                    throw new IOException(SR.Format(SR.IO_FileTooLarge_Path_AllocationSize, fullPath, preallocationSize));
                default:
                    int error = (int)Interop.NtDll.RtlNtStatusToDosError((int)ntStatus);
                    throw Win32Marshal.GetExceptionForWin32Error(error, fullPath);
            }
        }

        private void Validate(string path)
        {
            if (IsInvalid)
            {
                // Return a meaningful exception with the full path.

                // NT5 oddity - when trying to open "C:\" as a Win32FileStream,
                // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                // probably be consistent w/ every other directory.
                int errorCode = Marshal.GetLastPInvokeError();

                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && path.Length == PathInternal.GetRootLength(path))
                {
                    errorCode = Interop.Errors.ERROR_ACCESS_DENIED;
                }

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, path);
            }
        }

        internal void InitThreadPoolBindingIfNeeded()
        {
            if (IsAsync == true && ThreadPoolBinding == null)
            {
                // This is necessary for async IO using IO Completion ports via our
                // managed Threadpool API's.  This (theoretically) calls the OS's
                // BindIoCompletionCallback method, and passes in a stub for the
                // LPOVERLAPPED_COMPLETION_ROUTINE.  This stub looks at the Overlapped
                // struct for this request and gets a delegate to a managed callback
                // from there, which it then calls on a threadpool thread.  (We allocate
                // our native OVERLAPPED structs 2 pointers too large and store EE state
                // & GC handles there, one to an IAsyncResult, the other to a delegate.)
                try
                {
                    ThreadPoolBinding = ThreadPoolBoundHandle.BindHandle(this);
                }
                catch (ArgumentException ex)
                {
                    throw new IOException(SR.IO_BindHandleFailed, ex);
                }
                finally
                {
                    if (ThreadPoolBinding == null)
                    {
                        // We should close the handle so that the handle is not open until SafeFileHandle GC
                        Dispose();
                    }
                }
            }
        }

        private unsafe bool IsHandleSynchronous()
        {
            uint fileMode;
            int status = Interop.NtDll.NtQueryInformationFile(
                FileHandle: this,
                IoStatusBlock: out _,
                FileInformation: &fileMode,
                Length: sizeof(uint),
                FileInformationClass: Interop.NtDll.FileModeInformation);

            if (status != 0)
            {
                throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_INVALID_HANDLE);
            }

            // If either of these two flags are set, the file handle is synchronous (not overlapped)
            return (fileMode & (uint)(Interop.NtDll.CreateOptions.FILE_SYNCHRONOUS_IO_ALERT | Interop.NtDll.CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT)) > 0;
        }
    }
}
