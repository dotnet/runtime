// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal readonly unsafe struct SharedMemoryId
    {
        public SharedMemoryId(string name, bool isUserScope)
        {
            if (name.StartsWith("Global\\", StringComparison.Ordinal))
            {
                IsSessionScope = false;
                name = name.Substring("Global\\".Length);
            }
            else
            {
                IsSessionScope = true;
                if (name.StartsWith("Local\\", StringComparison.Ordinal))
                {
                    name = name.Substring("Local\\".Length);
                }
            }

            Name = name;

            if (name.Contains(Path.DirectorySeparatorChar))
            {
                throw new IOException(SR.Argument_DirectorySeparatorInvalid);
            }

            IsUserScope = isUserScope;
            Uid = IsUserScope ? Interop.Sys.GetEUid() : 0;
        }

        public string Name { get; }
        public bool IsSessionScope { get; }
        public bool IsUserScope { get; }
        public uint Uid { get; }
    }

    internal enum SharedMemoryType : byte
    {
        Mutex
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct SharedMemorySharedDataHeader
    {
        private struct SharedMemoryAndVersion
        {
            public SharedMemoryType Type;
            public byte Version;
        }

        [FieldOffset(0)]
        private SharedMemoryAndVersion _data;

        [FieldOffset(0)]
        private ulong _raw;

        public readonly SharedMemoryType Type => _data.Type;
        public readonly byte Version => _data.Version;

        public SharedMemorySharedDataHeader(SharedMemoryType type, byte version)
        {
            _data = new SharedMemoryAndVersion
            {
                Type = type,
                Version = version
            };
        }
    }

    internal interface ISharedMemoryProcessData
    {
        void Close(bool releaseSharedData);
    }

    internal sealed unsafe class SharedMemoryProcessDataHeader<TSharedMemoryProcessData>
        where TSharedMemoryProcessData : class, ISharedMemoryProcessData
    {
        internal readonly SharedMemoryId _id;
        internal TSharedMemoryProcessData? _processData;
        private readonly SafeFileHandle _fileHandle;
        private readonly SharedMemorySharedDataHeader* _sharedDataHeader;
        private readonly nuint _sharedDataTotalByteCount;
        private int _referenceCount = 1;

        public SharedMemoryProcessDataHeader(SharedMemoryId id, SafeFileHandle fileHandle, SharedMemorySharedDataHeader* sharedDataHeader, nuint sharedDataTotalByteCount)
        {
            _id = id;
            _fileHandle = fileHandle;
            _sharedDataHeader = sharedDataHeader;
            _sharedDataTotalByteCount = sharedDataTotalByteCount;
            _processData = null; // Will be initialized later
            SharedMemoryManager<TSharedMemoryProcessData>.Instance.AddProcessDataHeader(this);
        }

        public static void* GetDataPointer(SharedMemoryProcessDataHeader<TSharedMemoryProcessData>? processDataHeader)
        {
            return processDataHeader is null
                ? null
                : (void*)((byte*)processDataHeader._sharedDataHeader + sizeof(SharedMemorySharedDataHeader));
        }

        internal static SharedMemoryProcessDataHeader<TSharedMemoryProcessData>? CreateOrOpen(
            string name,
            bool isUserScope,
            SharedMemorySharedDataHeader requiredSharedDataHeader,
            nuint sharedMemoryDataSize,
            bool createIfNotExist,
            bool acquireLockIfCreated,
            out bool created,
            out AutoReleaseFileLock creationDeletionLockFileHandle,
            out SafeFileHandle runtimeDirectoryHandle)
        {
            created = false;
            runtimeDirectoryHandle = new SafeFileHandle();

            // If we don't create the shared memory file, the caller won't need to hold the creation/deletion lock file handle.
            // Return a placeholder to simplify the caller's logic and allow them to always dispose of the handle
            // when the return value is non-null.
            creationDeletionLockFileHandle = new AutoReleaseFileLock(new SafeFileHandle());
            SharedMemoryId id = new(name, isUserScope);

            nuint sharedDataUsedByteCount = (nuint)sizeof(SharedMemorySharedDataHeader) + sharedMemoryDataSize;
            nuint sharedDataTotalByteCount = AlignUp(sharedDataUsedByteCount, (nuint)Environment.SystemPageSize);

            SharedMemoryProcessDataHeader<TSharedMemoryProcessData>? processDataHeader = SharedMemoryManager<TSharedMemoryProcessData>.Instance.FindProcessDataHeader(id);

            if (processDataHeader is not null)
            {
                Debug.Assert(processDataHeader._sharedDataTotalByteCount == sharedDataTotalByteCount);
                processDataHeader.IncrementRefCount();
                return processDataHeader;
            }

            using AutoReleaseFileLock creationDeletionLock =
                SharedMemoryManager<TSharedMemoryProcessData>.Instance.AcquireCreationDeletionLockForId(id, out runtimeDirectoryHandle);

            string sessionDirectoryName = SharedMemoryHelpers.GetSessionDirectoryName(id);
            string sessionDirectoryPath = Path.Combine(
                SharedMemoryHelpers.GetSharedFilesSystemPath(id),
                SharedMemoryHelpers.GetRuntimeDirectoryName(id),
                SharedMemoryHelpers.SharedMemorySharedMemoryDirectoryName,
                sessionDirectoryName);
            using SafeFileHandle sessionDirectoryHandle = SharedMemoryHelpers.EnsureDirectoryExists(
                creationDeletionLock.FileHandle,
                sessionDirectoryName,
                sessionDirectoryPath,
                id,
                createIfNotExist);

            if (sessionDirectoryHandle.IsInvalid)
            {
                Debug.Assert(!createIfNotExist);
                return null;
            }

            string sharedMemoryFilePath = Path.Combine(sessionDirectoryPath, id.Name);

            SafeFileHandle fileHandle = SharedMemoryHelpers.CreateOrOpenFile(
                sessionDirectoryHandle,
                id.Name,
                sharedMemoryFilePath,
                id,
                createIfNotExist,
                out bool createdFile);
            if (fileHandle.IsInvalid)
            {
                return null;
            }

            bool clearContents = false;
            if (!createdFile)
            {
                // A shared file lock on the shared memory file would be held by any process that has opened the same file. Try to take
                // an exclusive lock on the file. Successfully acquiring an exclusive lock indicates that no process has a reference to
                // the shared memory file, and this process can reinitialize its contents.
                if (SharedMemoryHelpers.TryAcquireFileLock(fileHandle, nonBlocking: true))
                {
                    // The shared memory file is not being used, flag it as created so that its contents will be reinitialized
                    Interop.Sys.FLock(fileHandle, Interop.Sys.LockOperations.LOCK_UN);
                    if (!createIfNotExist)
                    {
                        return null;
                    }
                    createdFile = true;
                    clearContents = true;
                }
            }

            if (createdFile)
            {
                if (Interop.Sys.FTruncate(fileHandle, (long)sharedDataTotalByteCount) < 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    throw Interop.GetExceptionForIoErrno(error, sharedMemoryFilePath);
                }
            }
            else
            {
                if (Interop.Sys.FStat(fileHandle, out Interop.Sys.FileStatus fileStatus) != 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    throw Interop.GetExceptionForIoErrno(error, sharedMemoryFilePath);
                }

                if (fileStatus.Size < (long)sharedDataUsedByteCount)
                {
                    throw new InvalidDataException(SR.Format(SR.IO_SharedMemory_InvalidHeader, sharedMemoryFilePath));
                }

                if (Interop.Sys.FTruncate(fileHandle, (long)sharedDataTotalByteCount) < 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    throw Interop.GetExceptionForIoErrno(error, sharedMemoryFilePath);
                }
            }

            // Acquire and hold a shared file lock on the shared memory file as long as it is open, to indicate that this process is
            // using the file. An exclusive file lock is attempted above to detect whether the file contents are valid, for the case
            // where a process crashes or is killed after the file is created. Since we already hold the creation/deletion locks, a
            // non-blocking file lock should succeed.

            if (!SharedMemoryHelpers.TryAcquireFileLock(fileHandle, nonBlocking: true, exclusive: false))
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                throw Interop.GetExceptionForIoErrno(errorInfo, sharedMemoryFilePath);
            }

            using AutoReleaseFileLock autoReleaseFileLock = new(fileHandle);

            using MemoryMappedFileHolder memory = SharedMemoryHelpers.MemoryMapFile(fileHandle, sharedDataTotalByteCount);

            SharedMemorySharedDataHeader* sharedDataHeader = (SharedMemorySharedDataHeader*)memory.Pointer;
            if (createdFile)
            {
                if (clearContents)
                {
                    NativeMemory.Clear(memory.Pointer, sharedDataTotalByteCount);
                }
                *sharedDataHeader = requiredSharedDataHeader;
            }
            else
            {
                if (sharedDataHeader->Type != requiredSharedDataHeader.Type ||
                    sharedDataHeader->Version != requiredSharedDataHeader.Version)
                {
                    throw new InvalidDataException(SR.Format(SR.IO_SharedMemory_InvalidHeader, sharedMemoryFilePath));
                }
            }

            if (createdFile)
            {
                // If we created the file, then the caller still has more work to do to initialize the shared memory data.
                // Transfer the creation/deletion lock file handle to the caller to hold while they do that work.
                creationDeletionLock.SuppressRelease();
                creationDeletionLockFileHandle = new AutoReleaseFileLock(creationDeletionLock.FileHandle);
            }

            processDataHeader = new SharedMemoryProcessDataHeader<TSharedMemoryProcessData>(
                id,
                fileHandle,
                sharedDataHeader,
                sharedDataTotalByteCount
            );

            autoReleaseFileLock.SuppressRelease();
            memory.SuppressRelease();

            if (createdFile)
            {
                created = true;
            }

            return processDataHeader;

            static nuint AlignUp(nuint value, nuint alignment)
            {
                nuint alignMask = alignment - 1;
                return (nuint)((value + alignMask) & ~alignMask);
            }
        }

        public void IncrementRefCount()
        {
            Debug.Assert(_referenceCount > 0, "Ref count should not be negative.");
            _referenceCount++;
        }

        public void DecrementRefCount()
        {
            Debug.Assert(_referenceCount > 0, "Ref count should not be negative.");
            _referenceCount--;
            if (_referenceCount == 0)
            {
                Close();
            }
        }

        private void Close()
        {
            SharedMemoryManager<TSharedMemoryProcessData>.Instance.VerifyCreationDeletionProcessLockIsLocked();
            SharedMemoryManager<TSharedMemoryProcessData>.Instance.RemoveProcessDataHeader(this);

            using AutoReleaseFileLock autoReleaseFileLock =
                SharedMemoryManager<TSharedMemoryProcessData>.Instance.AcquireCreationDeletionLockForId(_id, out _);

            bool releaseSharedData = false;

            try
            {
                Interop.Sys.FLock(_fileHandle, Interop.Sys.LockOperations.LOCK_UN);
                if (SharedMemoryHelpers.TryAcquireFileLock(_fileHandle, nonBlocking: true, exclusive: true))
                {
                    // There's no one else using this mutex.
                    // We can delete our shared data.
                    releaseSharedData = true;
                }
            }
            catch
            {
                // Ignore the error, just don't release shared data.
            }

            _processData?.Close(releaseSharedData);
            _processData = null;
            Interop.Sys.MUnmap((nint)_sharedDataHeader, _sharedDataTotalByteCount);
            _fileHandle.Dispose();

            if (releaseSharedData)
            {
                // Directly call the underlying functions here as this is best-effort.
                // If we fail to delete, we don't want an exception.
                string sessionDirectoryName = SharedMemoryHelpers.GetSessionDirectoryName(_id);
                string sessionDirectoryPath = Path.Combine(
                    SharedMemoryHelpers.GetSharedFilesSystemPath(_id),
                    SharedMemoryHelpers.GetRuntimeDirectoryName(_id),
                    SharedMemoryHelpers.SharedMemorySharedMemoryDirectoryName,
                    sessionDirectoryName);
                using (SafeFileHandle sessionDirectoryHandle = SharedMemoryHelpers.EnsureDirectoryExists(
                    autoReleaseFileLock.FileHandle,
                    sessionDirectoryName,
                    sessionDirectoryPath,
                    _id,
                    createIfNotExist: false))
                {
                    if (!sessionDirectoryHandle.IsInvalid)
                    {
                        Interop.Sys.UnlinkAt(sessionDirectoryHandle, _id.Name, Interop.Sys.UnlinkAtFlags.None);
                    }
                }

                Interop.Sys.UnlinkAt(
                    autoReleaseFileLock.FileHandle,
                    sessionDirectoryName,
                    Interop.Sys.UnlinkAtFlags.AT_REMOVEDIR);
            }
        }
    }

    internal static class SharedMemoryHelpers
    {
        private const UnixFileMode PermissionsMask_OwnerUser_ReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        private const UnixFileMode PermissionsMask_OwnerUser_ReadWriteExecute = PermissionsMask_OwnerUser_ReadWrite | UnixFileMode.UserExecute;
        private const UnixFileMode PermissionsMask_AllUsers_ReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;
        private const UnixFileMode PermissionsMask_AllUsers_ReadWriteExecute = PermissionsMask_AllUsers_ReadWrite | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

        private const int TransientRetryCount = 10;

        internal const string SharedMemorySharedMemoryDirectoryName = "shm";

        private const string RuntimeTempDirectoryName = ".dotnet";

        private const string UserRuntimeTempDirectoryName = ".dotnet-uid";

        private const string SharedMemoryGlobalDirectoryName = "global";

        private const string SharedMemorySessionDirectoryName = "session";

        // See https://developer.apple.com/documentation/Foundation/FileManager/containerURL(forSecurityApplicationGroupIdentifier:)#App-Groups-in-macOS for details on this path.
        private const string ApplicationContainerBasePathSuffix = "Library/Group Containers/";

        private static string? ApplicationContainerPath { get; } =
            OperatingSystem.IsApplePlatform() ? GetApplicationContainerPath() : null;

        private static string SharedFilesPath { get; } = InitalizeSharedFilesPath();
        private static string InitalizeSharedFilesPath()
        {
            if (OperatingSystem.IsApplePlatform())
            {
                return ApplicationContainerPath ?? "/tmp/";
            }

            return "/tmp/";
        }

        private static string? GetApplicationContainerPath()
        {
            string? applicationGroupId = Environment.GetEnvironmentVariable("DOTNET_SANDBOX_APPLICATION_GROUP_ID");
            if (!string.IsNullOrEmpty(applicationGroupId))
            {
                string sharedFilesPath = Path.Combine(
                    PersistedFiles.GetHomeDirectoryFromPasswd(),
                    ApplicationContainerBasePathSuffix,
                    applicationGroupId
                );

                if (File.Exists(sharedFilesPath))
                {
                    // If the path exists and is a file, throw an exception.
                    // If it's a directory, or does not exist, callers can correctly handle it.
                    throw new DirectoryNotFoundException();
                }

                return sharedFilesPath;
            }
            return null;
        }

        private static string UserSpecificSharedFilesPath { get; } = InitalizeUserSpecificSharedFilesPath();

        private static string InitalizeUserSpecificSharedFilesPath()
        {
            if (OperatingSystem.IsApplePlatform())
            {
                // On apple platforms, TMPDIR points to the user-specific temporary directory.
                // For compat, we will still check the application container path first as that path
                // is also user-specific.
                string? path = ApplicationContainerPath ?? Environment.GetEnvironmentVariable("TMPDIR");
                if (path is not null)
                {
                    if (ApplicationContainerPath is not null)
                    {
                        // An Apple application-group container is already user-specific, so both global-scoped and
                        // user-scoped mutexes would otherwise use the same .dotnet directory and conflict. Give
                        // user-scoped mutexes a parallel directory to preserve the distinction between the scopes.
#pragma warning disable CA1416 // Validate platform compatibility. This file is only included on Unix platforms.
                        // The application-group container is owned by the current user, so this directory can be
                        // created directly without validating each component.
                        Directory.CreateDirectory(
                            Path.Combine(ApplicationContainerPath, UserRuntimeTempDirectoryName),
                            PermissionsMask_OwnerUser_ReadWriteExecute);
#pragma warning restore CA1416 // Validate platform compatibility
                    }

                    return path;
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                // XDG_RUNTIME_DIR is the user-specific, cleared on reboot, runtime files directory for systems
                // that follow the XDG Base Directory Specification
                // https://specifications.freedesktop.org/basedir
                // For Linux platforms that follow the XDG specification, we will use it.
                string? xdgRuntimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
                if (!string.IsNullOrEmpty(xdgRuntimeDir))
                {
                    return xdgRuntimeDir;
                }
            }

            // Fallback to storing data in the home directory.
            // The home directory is expected to be owned by the user and only accessible by the user.
            // If it is not, then that was a choice by the user.
            string userFeatureDirectory = PersistedFiles.GetUserFeatureDirectory("Runtime", "SharedMemory");

#pragma warning disable CA1416 // Validate platform compatibility. This file is only included on Unix platforms.
            // This is not a system-created directory, so we need to guarantee that it exists.
            // Default permissions are fine as we are under the user's home directory.
            Directory.CreateDirectory(userFeatureDirectory, PermissionsMask_OwnerUser_ReadWriteExecute);

            return userFeatureDirectory;
#pragma warning restore CA1416 // Validate platform compatibility
        }

        public static SafeFileHandle OpenSharedFilesSystemDirectory(SharedMemoryId id)
        {
            string directoryPath = GetSharedFilesSystemPath(id);
            int transientRetryCount = 0;

            while (true)
            {
                SafeFileHandle directoryHandle = Interop.Sys.Open(
                    directoryPath,
                    Interop.Sys.OpenFlags.O_RDONLY | Interop.Sys.OpenFlags.O_CLOEXEC | Interop.Sys.OpenFlags.O_NOFOLLOW | Interop.Sys.OpenFlags.O_DIRECTORY,
                    0);

                if (!directoryHandle.IsInvalid)
                {
                    return directoryHandle;
                }

                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                directoryHandle.Dispose();
                if ((error.Error == Interop.Error.ENOENT || error.Error == Interop.Error.EACCES) &&
                    RetryOnTransientPermissionFailure(ref transientRetryCount))
                {
                    continue;
                }

                throw Interop.GetExceptionForIoErrno(error, directoryPath);
            }
        }

        internal static string GetSharedFilesSystemPath(SharedMemoryId id)
        {
            return id.IsUserScope ? UserSpecificSharedFilesPath : SharedFilesPath;
        }

        internal static string GetRuntimeDirectoryName(SharedMemoryId id)
        {
            return id.IsUserScope && ApplicationContainerPath is not null
                ? UserRuntimeTempDirectoryName
                : RuntimeTempDirectoryName;
        }

        private static int SessionId { get; } = Interop.Sys.GetSid(Environment.ProcessId);

        public static string GetSessionDirectoryName(SharedMemoryId id)
        {
            return id.IsSessionScope ? $"{SharedMemorySessionDirectoryName}{SessionId}" : SharedMemoryGlobalDirectoryName;
        }

        internal static SafeFileHandle CreateOrOpenFile(
            SafeFileHandle directoryHandle,
            string fileName,
            string filePathForError,
            SharedMemoryId id,
            bool createIfNotExist,
            out bool createdFile)
        {
            int transientRetryCount = 0;

            while (true)
            {
                SafeFileHandle fd = Interop.Sys.OpenAt(directoryHandle, fileName, Interop.Sys.OpenFlags.O_RDWR | Interop.Sys.OpenFlags.O_NOFOLLOW | Interop.Sys.OpenFlags.O_CLOEXEC, 0);
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                if (!fd.IsInvalid)
                {
                    if (id.IsUserScope)
                    {
                        if (Interop.Sys.FStat(fd, out Interop.Sys.FileStatus fileStatus) != 0)
                        {
                            error = Interop.Sys.GetLastErrorInfo();
                            fd.Dispose();
                            throw Interop.GetExceptionForIoErrno(error, filePathForError);
                        }

                        if (fileStatus.Uid != id.Uid)
                        {
                            fd.Dispose();
                            throw new IOException(SR.Format(SR.IO_SharedMemory_FileNotOwnedByUid, filePathForError, id.Uid));
                        }

                        if ((fileStatus.Mode & (int)PermissionsMask_AllUsers_ReadWriteExecute) != (int)PermissionsMask_OwnerUser_ReadWrite)
                        {
                            fd.Dispose();

                            // Another process may have created this file and not yet applied the final mode via fchmod.
                            // Retry briefly in case this open races with that transient window.
                            if (transientRetryCount < TransientRetryCount)
                            {
                                transientRetryCount++;
                                Thread.Sleep(1);
                                continue;
                            }

                            throw new IOException(SR.Format(SR.IO_SharedMemory_FilePermissionsIncorrect, filePathForError, PermissionsMask_OwnerUser_ReadWrite));
                        }
                    }
                    createdFile = false;
                    return fd;
                }

                if (error.Error == Interop.Error.EACCES && transientRetryCount < TransientRetryCount)
                {
                    // During creation, the mode passed to O_CREAT is filtered by process umask. Retry briefly in case
                    // another process is still in the window between creating the file and fixing its permissions.
                    transientRetryCount++;
                    Thread.Sleep(1);
                    continue;
                }

                if (error.Error != Interop.Error.ENOENT)
                {
                    throw Interop.GetExceptionForIoErrno(error, filePathForError);
                }

                if (!createIfNotExist)
                {
                    createdFile = false;
                    return fd;
                }

                fd.Dispose();

                UnixFileMode permissionsMask = id.IsUserScope
                    ? PermissionsMask_OwnerUser_ReadWrite
                    : PermissionsMask_AllUsers_ReadWrite;

                fd = Interop.Sys.OpenAt(
                    directoryHandle,
                    fileName,
                    Interop.Sys.OpenFlags.O_RDWR | Interop.Sys.OpenFlags.O_NOFOLLOW | Interop.Sys.OpenFlags.O_CLOEXEC | Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_EXCL,
                    (int)permissionsMask);

                if (fd.IsInvalid)
                {
                    error = Interop.Sys.GetLastErrorInfo();
                    fd.Dispose();
                    if (error.Error == Interop.Error.EEXIST && transientRetryCount < TransientRetryCount)
                    {
                        // Another process created the file between our ENOENT check and our O_CREAT|O_EXCL attempt.
                        // Retry opening the existing file.
                        transientRetryCount++;
                        continue;
                    }
                    throw Interop.GetExceptionForIoErrno(error, filePathForError);
                }

                // The mode passed to open(..., O_CREAT|O_EXCL, permissionsMask) is filtered by process umask, so the
                // created file may not have the requested access. Set the exact mode explicitly.
                int result = Interop.Sys.FChMod(fd, (int)permissionsMask);

                if (result != 0)
                {
                    error = Interop.Sys.GetLastErrorInfo();
                    fd.Dispose();
                    Interop.Sys.UnlinkAt(directoryHandle, fileName, Interop.Sys.UnlinkAtFlags.None);
                    throw Interop.GetExceptionForIoErrno(error, filePathForError);
                }

                createdFile = true;
                return fd;
            }
        }

        internal static SafeFileHandle EnsureDirectoryExists(
            SafeFileHandle parentDirectoryHandle,
            string directoryName,
            string directoryPathForError,
            SharedMemoryId id,
            bool createIfNotExist = true)
        {
            int transientRetryCount = 0;
            while (true)
            {
                UnixFileMode permissionsMask = id.IsUserScope
                    ? PermissionsMask_OwnerUser_ReadWriteExecute
                    : PermissionsMask_AllUsers_ReadWriteExecute;

                SafeFileHandle directoryHandle = Interop.Sys.OpenAt(
                    parentDirectoryHandle,
                    directoryName,
                    Interop.Sys.OpenFlags.O_RDONLY | Interop.Sys.OpenFlags.O_CLOEXEC | Interop.Sys.OpenFlags.O_NOFOLLOW | Interop.Sys.OpenFlags.O_DIRECTORY,
                    0);

                if (directoryHandle.IsInvalid)
                {
                    Interop.ErrorInfo openError = Interop.Sys.GetLastErrorInfo();
                    directoryHandle.Dispose();

                    if (openError.Error == Interop.Error.ENOENT)
                    {
                        if (!createIfNotExist)
                        {
                            // The directory does not exist and we are not allowed to create it.
                            return new SafeFileHandle();
                        }

                        if (Interop.Sys.MkDirAt(parentDirectoryHandle, directoryName, (int)permissionsMask) == 0)
                        {
                            directoryHandle = Interop.Sys.OpenAt(
                                parentDirectoryHandle,
                                directoryName,
                                Interop.Sys.OpenFlags.O_RDONLY | Interop.Sys.OpenFlags.O_CLOEXEC | Interop.Sys.OpenFlags.O_NOFOLLOW | Interop.Sys.OpenFlags.O_DIRECTORY,
                                0);

                            if (directoryHandle.IsInvalid)
                            {
                                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                                directoryHandle.Dispose();
                                Interop.Sys.UnlinkAt(parentDirectoryHandle, directoryName, Interop.Sys.UnlinkAtFlags.AT_REMOVEDIR);
                                throw Interop.GetExceptionForIoErrno(error, directoryPathForError);
                            }

                            if (Interop.Sys.FChMod(directoryHandle, (int)permissionsMask) != 0)
                            {
                                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                                directoryHandle.Dispose();
                                Interop.Sys.UnlinkAt(parentDirectoryHandle, directoryName, Interop.Sys.UnlinkAtFlags.AT_REMOVEDIR);
                                throw Interop.GetExceptionForIoErrno(error, directoryPathForError);
                            }

                            return directoryHandle;
                        }

                        Interop.ErrorInfo mkdirError = Interop.Sys.GetLastErrorInfo();
                        if (mkdirError.Error != Interop.Error.EEXIST)
                        {
                            throw Interop.GetExceptionForIoErrno(mkdirError, directoryPathForError);
                        }

                        if (RetryOnTransientPermissionFailure(ref transientRetryCount))
                        {
                            // If we have two threads or processes racing to create this directory,
                            // the first one may have created it but not yet set the correct permissions.
                            // Retry briefly to let the other process finish setting permissions.
                            // We will not set permissions when we are not the creator of the directory.
                            continue;
                        }

                        throw Interop.GetExceptionForIoErrno(mkdirError, directoryPathForError);
                    }

                    if ((openError.Error == Interop.Error.ENOENT || openError.Error == Interop.Error.EACCES) &&
                        RetryOnTransientPermissionFailure(ref transientRetryCount))
                    {
                        // While another process is creating this path, openat() can temporarily fail with ENOENT before the
                        // directory appears, or with EACCES before the creating process has finished normalizing directory
                        // permissions for cross-user access.
                        continue;
                    }

                    if (openError.Error is Interop.Error.ELOOP or Interop.Error.ENOTDIR)
                    {
                        // If the path exists, check that it's a directory.
                        throw new IOException(SR.Format(SR.IO_SharedMemory_PathExistsButNotDirectory, directoryPathForError));
                    }

                    throw Interop.GetExceptionForIoErrno(openError, directoryPathForError);
                }

                return directoryHandle;
            }
        }

        private static bool RetryOnTransientPermissionFailure(ref int transientRetryCount)
        {
            if (transientRetryCount >= TransientRetryCount)
            {
                return false;
            }

            transientRetryCount++;
            Thread.Sleep(1);
            return true;
        }

        internal static MemoryMappedFileHolder MemoryMapFile(SafeFileHandle fileHandle, nuint sharedDataTotalByteCount)
        {
            nint addr = Interop.Sys.MMap(
                0,
                sharedDataTotalByteCount,
                Interop.Sys.MemoryMappedProtections.PROT_READ | Interop.Sys.MemoryMappedProtections.PROT_WRITE,
                Interop.Sys.MemoryMappedFlags.MAP_SHARED,
                fileHandle,
                0);

            if (addr == -1)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                throw Interop.GetExceptionForIoErrno(errorInfo, "Failed to memory map the file");
            }

            return new MemoryMappedFileHolder(addr, sharedDataTotalByteCount);
        }

        internal static bool TryAcquireFileLock(SafeFileHandle sharedLockFileHandle, bool nonBlocking, bool exclusive = true)
        {
            Interop.Sys.LockOperations lockOperation = exclusive ? Interop.Sys.LockOperations.LOCK_EX : Interop.Sys.LockOperations.LOCK_SH;
            if (nonBlocking)
            {
                lockOperation |= Interop.Sys.LockOperations.LOCK_NB;
            }
            int result = Interop.Sys.FLock(sharedLockFileHandle, lockOperation);

            if (result == 0)
            {
                return true;
            }

            Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
            if (errorInfo.Error == Interop.Error.EWOULDBLOCK)
            {
                return false;
            }

            throw Interop.GetExceptionForIoErrno(errorInfo);
        }
    }

    internal unsafe ref struct MemoryMappedFileHolder(nint addr, nuint length)
    {
        private bool _suppressed;

        public void SuppressRelease()
        {
            _suppressed = true;
        }

        public void Dispose()
        {
            if (!_suppressed)
            {
                Interop.Sys.MUnmap(addr, length);
            }
        }

        public void* Pointer => (void*)addr;
    }

    internal unsafe ref struct AutoReleaseFileLock(SafeFileHandle fd)
    {
        private bool _suppressed;

        public readonly SafeFileHandle FileHandle = fd;

        public void SuppressRelease()
        {
            _suppressed = true;
        }

        public void Dispose()
        {
            if (!_suppressed && !FileHandle.IsInvalid)
            {
                Interop.Sys.FLock(FileHandle, Interop.Sys.LockOperations.LOCK_UN);
            }
        }
    }

    internal sealed class SharedMemoryManager<TSharedMemoryProcessData>
        where TSharedMemoryProcessData : class, ISharedMemoryProcessData
    {
        internal static SharedMemoryManager<TSharedMemoryProcessData> Instance { get; } = new SharedMemoryManager<TSharedMemoryProcessData>();


        private readonly LowLevelLock _creationDeletionProcessLock = new();
        private (SafeFileHandle RuntimeDirectoryHandle, SafeFileHandle SharedMemoryDirectoryHandle)? _creationDeletionDirectoryHandles;
        private readonly Dictionary<uint, (SafeFileHandle RuntimeDirectoryHandle, SafeFileHandle SharedMemoryDirectoryHandle)> _uidToDirectoryHandlesMap = [];

        public WaitSubsystem.LockHolder AcquireCreationDeletionProcessLock()
        {
            return new WaitSubsystem.LockHolder(_creationDeletionProcessLock);
        }

        public void VerifyCreationDeletionProcessLockIsLocked()
        {
            _creationDeletionProcessLock.VerifyIsLocked();
        }

        public void VerifyCreationDeletionProcessLockIsNotLocked()
        {
            _creationDeletionProcessLock.VerifyIsNotLocked();
        }

        public AutoReleaseFileLock AcquireCreationDeletionLockForId(
            SharedMemoryId id,
            out SafeFileHandle runtimeDirectoryHandle)
        {
            _creationDeletionProcessLock.VerifyIsLocked();
            (SafeFileHandle RuntimeDirectoryHandle, SafeFileHandle SharedMemoryDirectoryHandle)? directoryHandles =
                id.IsUserScope ? GetUserScopeDirectoryHandles(id.Uid) : _creationDeletionDirectoryHandles;
            if (directoryHandles is null)
            {
                using SafeFileHandle systemDirectoryHandle = SharedMemoryHelpers.OpenSharedFilesSystemDirectory(id);
                string runtimeDirectoryName = SharedMemoryHelpers.GetRuntimeDirectoryName(id);
                string runtimeDirectoryPath = Path.Combine(
                    SharedMemoryHelpers.GetSharedFilesSystemPath(id),
                    runtimeDirectoryName);
                SafeFileHandle newRuntimeDirectoryHandle = SharedMemoryHelpers.EnsureDirectoryExists(
                    systemDirectoryHandle,
                    runtimeDirectoryName,
                    runtimeDirectoryPath,
                    id);

                SafeFileHandle sharedMemoryDirectoryHandle;
                try
                {
                    sharedMemoryDirectoryHandle = SharedMemoryHelpers.EnsureDirectoryExists(
                        newRuntimeDirectoryHandle,
                        SharedMemoryHelpers.SharedMemorySharedMemoryDirectoryName,
                        Path.Combine(runtimeDirectoryPath, SharedMemoryHelpers.SharedMemorySharedMemoryDirectoryName),
                        id);
                }
                catch
                {
                    newRuntimeDirectoryHandle.Dispose();
                    throw;
                }

                (SafeFileHandle RuntimeDirectoryHandle, SafeFileHandle SharedMemoryDirectoryHandle) newDirectoryHandles =
                    (newRuntimeDirectoryHandle, sharedMemoryDirectoryHandle);
                directoryHandles = newDirectoryHandles;

                if (id.IsUserScope)
                {
                    _uidToDirectoryHandlesMap.Add(id.Uid, newDirectoryHandles);
                }
                else
                {
                    _creationDeletionDirectoryHandles = newDirectoryHandles;
                }
            }

            (SafeFileHandle RuntimeDirectoryHandle, SafeFileHandle SharedMemoryDirectoryHandle) handles = directoryHandles.Value;
            bool acquired = SharedMemoryHelpers.TryAcquireFileLock(
                handles.SharedMemoryDirectoryHandle,
                nonBlocking: false,
                exclusive: true);
            Debug.Assert(acquired);
            runtimeDirectoryHandle = handles.RuntimeDirectoryHandle;
            return new AutoReleaseFileLock(handles.SharedMemoryDirectoryHandle);

            (SafeFileHandle RuntimeDirectoryHandle, SafeFileHandle SharedMemoryDirectoryHandle)? GetUserScopeDirectoryHandles(uint uid)
            {
                return _uidToDirectoryHandlesMap.TryGetValue(
                    uid,
                    out (SafeFileHandle RuntimeDirectoryHandle, SafeFileHandle SharedMemoryDirectoryHandle) handles)
                    ? handles
                    : null;
            }
        }

        private Dictionary<SharedMemoryId, SharedMemoryProcessDataHeader<TSharedMemoryProcessData>> _processDataHeaders = [];

        public void AddProcessDataHeader(SharedMemoryProcessDataHeader<TSharedMemoryProcessData> processDataHeader)
        {
            VerifyCreationDeletionProcessLockIsLocked();
            _processDataHeaders[processDataHeader._id] = processDataHeader;
        }

        public void RemoveProcessDataHeader(SharedMemoryProcessDataHeader<TSharedMemoryProcessData> processDataHeader)
        {
            VerifyCreationDeletionProcessLockIsLocked();
            _processDataHeaders.Remove(processDataHeader._id);
        }

        public SharedMemoryProcessDataHeader<TSharedMemoryProcessData>? FindProcessDataHeader(SharedMemoryId id)
        {
            _processDataHeaders.TryGetValue(id, out SharedMemoryProcessDataHeader<TSharedMemoryProcessData>? header);
            return header;
        }
    }
}
