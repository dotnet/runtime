// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal const FileOptions NoBuffering = (FileOptions)0x20000000;
        private volatile FileOptions _fileOptions = (FileOptions)(-1);
        private volatile int _fileType = -1;

        public SafeFileHandle() : base(true)
        {
        }

        public SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);
        }

        private SafeFileHandle(IntPtr preexistingHandle, bool ownsHandle, FileOptions fileOptions) : base(ownsHandle)
        {
            SetHandle(preexistingHandle);

            _fileOptions = fileOptions;
        }

        public bool IsAsync => (GetFileOptions() & FileOptions.Asynchronous) != 0;

        internal bool CanSeek => !IsClosed && GetFileType() == Interop.Kernel32.FileTypes.FILE_TYPE_DISK;

        internal bool IsPipe => GetFileType() == Interop.Kernel32.FileTypes.FILE_TYPE_PIPE;

        internal ThreadPoolBoundHandle? ThreadPoolBinding { get; set; }

        internal static unsafe SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            using (DisableMediaInsertionPrompt.Create())
            {
                SafeFileHandle fileHandle = new SafeFileHandle(
                    NtCreateFile(fullPath, mode, access, share, options, preallocationSize),
                    ownsHandle: true,
                    options);

                fileHandle.InitThreadPoolBindingIfNeeded();

                return fileHandle;
            }
        }

        private static IntPtr NtCreateFile(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            uint ntStatus;
            IntPtr fileHandle;

            const string MandatoryNtPrefix = @"\??\";
            if (fullPath.StartsWith(MandatoryNtPrefix, StringComparison.Ordinal))
            {
                (ntStatus, fileHandle) = Interop.NtDll.NtCreateFile(fullPath, mode, access, share, options, preallocationSize);
            }
            else
            {
                var vsb = new ValueStringBuilder(stackalloc char[256]);
                vsb.Append(MandatoryNtPrefix);

                if (fullPath.StartsWith(@"\\?\", StringComparison.Ordinal)) // NtCreateFile does not support "\\?\" prefix, only "\??\"
                {
                    vsb.Append(fullPath.AsSpan(4));
                }
                else
                {
                    vsb.Append(fullPath);
                }

                (ntStatus, fileHandle) = Interop.NtDll.NtCreateFile(vsb.AsSpan(), mode, access, share, options, preallocationSize);
                vsb.Dispose();
            }

            switch (ntStatus)
            {
                case Interop.StatusOptions.STATUS_SUCCESS:
                    return fileHandle;
                case Interop.StatusOptions.STATUS_DISK_FULL:
                    throw new IOException(SR.Format(SR.IO_DiskFull_Path_AllocationSize, fullPath, preallocationSize));
                // NtCreateFile has a bug and it reports STATUS_INVALID_PARAMETER for files
                // that are too big for the current file system. Example: creating a 4GB+1 file on a FAT32 drive.
                case Interop.StatusOptions.STATUS_INVALID_PARAMETER when preallocationSize > 0:
                case Interop.StatusOptions.STATUS_FILE_TOO_LARGE:
                    throw new IOException(SR.Format(SR.IO_FileTooLarge_Path_AllocationSize, fullPath, preallocationSize));
                default:
                    int error = (int)Interop.NtDll.RtlNtStatusToDosError((int)ntStatus);
                    throw Win32Marshal.GetExceptionForWin32Error(error, fullPath);
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
                    if (OwnsHandle)
                    {
                        // We should close the handle so that the handle is not open until SafeFileHandle GC
                        Dispose();
                    }

                    throw new IOException(SR.IO_BindHandleFailed, ex);
                }
            }
        }

        internal unsafe FileOptions GetFileOptions()
        {
            FileOptions fileOptions = _fileOptions;
            if (fileOptions != (FileOptions)(-1))
            {
                return fileOptions;
            }

            Interop.NtDll.CreateOptions options;
            int ntStatus = Interop.NtDll.NtQueryInformationFile(
                FileHandle: this,
                IoStatusBlock: out _,
                FileInformation: &options,
                Length: sizeof(uint),
                FileInformationClass: Interop.NtDll.FileModeInformation);

            if (ntStatus != Interop.StatusOptions.STATUS_SUCCESS)
            {
                int error = (int)Interop.NtDll.RtlNtStatusToDosError(ntStatus);
                throw Win32Marshal.GetExceptionForWin32Error(error);
            }

            FileOptions result = FileOptions.None;

            if ((options & (Interop.NtDll.CreateOptions.FILE_SYNCHRONOUS_IO_ALERT | Interop.NtDll.CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT)) == 0)
            {
                result |= FileOptions.Asynchronous;
            }
            if ((options & Interop.NtDll.CreateOptions.FILE_WRITE_THROUGH) != 0)
            {
                result |= FileOptions.WriteThrough;
            }
            if ((options & Interop.NtDll.CreateOptions.FILE_RANDOM_ACCESS) != 0)
            {
                result |= FileOptions.RandomAccess;
            }
            if ((options & Interop.NtDll.CreateOptions.FILE_SEQUENTIAL_ONLY) != 0)
            {
                result |= FileOptions.SequentialScan;
            }
            if ((options & Interop.NtDll.CreateOptions.FILE_DELETE_ON_CLOSE) != 0)
            {
                result |= FileOptions.DeleteOnClose;
            }
            if ((options & Interop.NtDll.CreateOptions.FILE_NO_INTERMEDIATE_BUFFERING) != 0)
            {
                result |= NoBuffering;
            }

            return _fileOptions = result;
        }

        internal int GetFileType()
        {
            int fileType = _fileType;
            if (fileType == -1)
            {
                _fileType = fileType = Interop.Kernel32.GetFileType(this);

                Debug.Assert(fileType == Interop.Kernel32.FileTypes.FILE_TYPE_DISK
                    || fileType == Interop.Kernel32.FileTypes.FILE_TYPE_PIPE
                    || fileType == Interop.Kernel32.FileTypes.FILE_TYPE_CHAR,
                    $"Unknown file type: {fileType}");
            }

            return fileType;
        }
    }
}
