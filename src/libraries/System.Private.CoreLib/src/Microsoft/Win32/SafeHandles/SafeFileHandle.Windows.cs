// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal const FileOptions NoBuffering = (FileOptions)0x20000000;
        private long _length = -1; // negative means that hasn't been fetched.
        private bool _lengthCanBeCached; // file has been opened for reading and not shared for writing.
        private volatile FileOptions _fileOptions = (FileOptions)(-1);
        private volatile int _fileType = -1;

        public SafeFileHandle() : base(true)
        {
        }

        public bool IsAsync => (GetFileOptions() & FileOptions.Asynchronous) != 0;

        internal bool IsNoBuffering => (GetFileOptions() & NoBuffering) != 0;

        internal bool CanSeek => !IsClosed && GetFileType() == Interop.Kernel32.FileTypes.FILE_TYPE_DISK;

        internal ThreadPoolBoundHandle? ThreadPoolBinding { get; set; }

        internal bool TryGetCachedLength(out long cachedLength)
        {
            cachedLength = _length;
            return _lengthCanBeCached && cachedLength >= 0;
        }

        internal static unsafe SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize, UnixFileMode? unixCreateMode = null)
        {
            Debug.Assert(!unixCreateMode.HasValue);

            using (DisableMediaInsertionPrompt.Create())
            {
                // we don't use NtCreateFile as there is no public and reliable way
                // of converting DOS to NT file paths (RtlDosPathNameToRelativeNtPathName_U_WithStatus is not documented)
                SafeFileHandle fileHandle = CreateFile(fullPath, mode, access, share, options);

                if (preallocationSize > 0)
                {
                    Preallocate(fullPath, preallocationSize, fileHandle);
                }

                if ((options & FileOptions.Asynchronous) != 0)
                {
                    // the handle has not been exposed yet, so we don't need to acquire a lock
                    fileHandle.InitThreadPoolBinding();
                }

                return fileHandle;
            }
        }

        private static unsafe SafeFileHandle CreateFile(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options)
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

            int fAccess =
                ((access & FileAccess.Read) == FileAccess.Read ? Interop.Kernel32.GenericOperations.GENERIC_READ : 0) |
                ((access & FileAccess.Write) == FileAccess.Write ? Interop.Kernel32.GenericOperations.GENERIC_WRITE : 0);

            // Our Inheritable bit was stolen from Windows, but should be set in
            // the security attributes class.  Don't leave this bit set.
            share &= ~FileShare.Inheritable;

            // Must use a valid Win32 constant here...
            if (mode == FileMode.Append)
            {
                mode = FileMode.OpenOrCreate;
            }

            int flagsAndAttributes = (int)options;

            // For mitigating local elevation of privilege attack through named pipes
            // make sure we always call CreateFile with SECURITY_ANONYMOUS so that the
            // named pipe server can't impersonate a high privileged client security context
            // (note that this is the effective default on CreateFile2)
            flagsAndAttributes |= (Interop.Kernel32.SecurityOptions.SECURITY_SQOS_PRESENT | Interop.Kernel32.SecurityOptions.SECURITY_ANONYMOUS);

            SafeFileHandle fileHandle = Interop.Kernel32.CreateFile(fullPath, fAccess, share, &secAttrs, mode, flagsAndAttributes, IntPtr.Zero);
            if (fileHandle.IsInvalid)
            {
                // Return a meaningful exception with the full path.

                // NT5 oddity - when trying to open "C:\" as a Win32FileStream,
                // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                // probably be consistent w/ every other directory.
                int errorCode = Marshal.GetLastPInvokeError();

                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && fullPath!.Length == PathInternal.GetRootLength(fullPath))
                {
                    errorCode = Interop.Errors.ERROR_ACCESS_DENIED;
                }

                fileHandle.Dispose();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }

            fileHandle._path = fullPath;
            fileHandle._fileOptions = options;
            fileHandle._lengthCanBeCached = (share & FileShare.Write) == 0 && (access & FileAccess.Write) == 0;
            return fileHandle;
        }

        private static unsafe void Preallocate(string fullPath, long preallocationSize, SafeFileHandle fileHandle)
        {
            var allocationInfo = new Interop.Kernel32.FILE_ALLOCATION_INFO
            {
                AllocationSize = preallocationSize
            };

            if (!Interop.Kernel32.SetFileInformationByHandle(
                fileHandle,
                Interop.Kernel32.FileAllocationInfo,
                &allocationInfo,
                (uint)sizeof(Interop.Kernel32.FILE_ALLOCATION_INFO)))
            {
                int errorCode = Marshal.GetLastPInvokeError();

                // Only throw for errors that indicate there is not enough space.
                if (errorCode == Interop.Errors.ERROR_DISK_FULL ||
                    errorCode == Interop.Errors.ERROR_FILE_TOO_LARGE)
                {
                    fileHandle.Dispose();

                    // Delete the file we've created.
                    Interop.Kernel32.DeleteFile(fullPath);

                    throw new IOException(SR.Format(errorCode == Interop.Errors.ERROR_DISK_FULL
                                                        ? SR.IO_DiskFull_Path_AllocationSize
                                                        : SR.IO_FileTooLarge_Path_AllocationSize,
                                            fullPath, preallocationSize));
                }
            }
        }

        internal void EnsureThreadPoolBindingInitialized()
        {
            if (IsAsync && ThreadPoolBinding == null)
            {
                Init();
            }

            void Init() // moved to a separate method so EnsureThreadPoolBindingInitialized can be inlined
            {
                lock (this)
                {
                    if (ThreadPoolBinding == null)
                    {
                        InitThreadPoolBinding();
                    }
                }
            }
        }

        private void InitThreadPoolBinding()
        {
            Debug.Assert(IsAsync);

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

        internal long GetFileLength()
        {
            if (!_lengthCanBeCached)
            {
                return GetFileLengthCore();
            }

            // On Windows, when the file is locked for writes we can cache file length
            // in memory and avoid subsequent native calls which are expensive.
            if (_length < 0)
            {
                _length = GetFileLengthCore();
            }

            return _length;

            unsafe long GetFileLengthCore()
            {
                Interop.Kernel32.FILE_STANDARD_INFO info;

                if (!Interop.Kernel32.GetFileInformationByHandleEx(this, Interop.Kernel32.FileStandardInfo, &info, (uint)sizeof(Interop.Kernel32.FILE_STANDARD_INFO)))
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error(Path);
                }

                return info.EndOfFile;
            }
        }
    }
}
