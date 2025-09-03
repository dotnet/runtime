// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    internal readonly unsafe struct SharedMemoryId
    {
        private const string UserUnscopedRuntimeTempDirectoryName = ".dotnet";

        private const string UserScopedRuntimeTempDirectoryName = ".dotnet-uid";

        private const string SharedMemoryGlobalDirectoryName = "global";

        private const string SharedMemorySessionDirectoryName = "session";

        private static int SessionId { get; } = Interop.Sys.GetSid(Environment.ProcessId);

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

        internal readonly string GetRuntimeTempDirectoryName()
        {
            if (IsUserScope)
            {
                return $"{UserScopedRuntimeTempDirectoryName}{Uid}";
            }
            else
            {
                return UserUnscopedRuntimeTempDirectoryName;
            }
        }

        internal readonly string GetSessionDirectoryName()
        {
            if (IsSessionScope)
            {
                return $"{SharedMemorySessionDirectoryName}{SessionId}";
            }
            else
            {
                return SharedMemoryGlobalDirectoryName;
            }
        }
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
            out AutoReleaseFileLock creationDeletionLockFileHandle)
        {
            created = false;

            AutoReleaseFileLock placeholderAutoReleaseLock = new AutoReleaseFileLock(new SafeFileHandle());

            creationDeletionLockFileHandle = placeholderAutoReleaseLock;
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

            creationDeletionLockFileHandle = SharedMemoryManager<TSharedMemoryProcessData>.Instance.AcquireCreationDeletionLockForId(id);

            string sessionDirectory = Path.Combine(
                SharedMemoryHelpers.SharedFilesPath,
                id.GetRuntimeTempDirectoryName(),
                SharedMemoryManager<TSharedMemoryProcessData>.SharedMemorySharedMemoryDirectoryName,
                id.GetSessionDirectoryName()
            );

            if (!SharedMemoryHelpers.EnsureDirectoryExists(sessionDirectory, id, isGlobalLockAcquired: true, createIfNotExist))
            {
                Debug.Assert(!createIfNotExist);
                return null;
            }

            string sharedMemoryFilePath = Path.Combine(sessionDirectory, id.Name);

            SafeFileHandle fileHandle = SharedMemoryHelpers.CreateOrOpenFile(sharedMemoryFilePath, id, createIfNotExist, out bool createdFile);
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

            if (!createdFile)
            {
                creationDeletionLockFileHandle.Dispose();
                // Reset to the placeholder value to avoid returning a pre-disposed lock.
                creationDeletionLockFileHandle = placeholderAutoReleaseLock;
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
            SharedMemoryManager<NamedMutexProcessDataBase>.Instance.VerifyCreationDeletionProcessLockIsLocked();
            SharedMemoryManager<TSharedMemoryProcessData>.Instance.RemoveProcessDataHeader(this);

            using AutoReleaseFileLock autoReleaseFileLock = SharedMemoryManager<TSharedMemoryProcessData>.Instance.AcquireCreationDeletionLockForId(_id);

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
            catch (Exception)
            {
                // Ignore the error, just don't release shared data.
            }

            _processData?.Close(releaseSharedData);
            _processData = null;
            Interop.Sys.MUnmap((nint)_sharedDataHeader, _sharedDataTotalByteCount);
            _fileHandle.Dispose();

            if (releaseSharedData)
            {
                string sessionDirectoryPath = Path.Combine(
                    SharedMemoryHelpers.SharedFilesPath,
                    _id.GetRuntimeTempDirectoryName(),
                    SharedMemoryManager<TSharedMemoryProcessData>.SharedMemorySharedMemoryDirectoryName,
                    _id.GetSessionDirectoryName()
                );

                string sharedMemoryFilePath = Path.Combine(sessionDirectoryPath, _id.Name);

                // Directly call the underlying functions here as this is best-effort.
                // If we fail to delete, we don't want an exception.
                Interop.Sys.Unlink(sharedMemoryFilePath);

                Interop.Sys.RmDir(sessionDirectoryPath);
            }
        }
    }

    internal static class SharedMemoryHelpers
    {
        private const UnixFileMode PermissionsMask_OwnerUser_ReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        private const UnixFileMode PermissionsMask_OwnerUser_ReadWriteExecute = PermissionsMask_OwnerUser_ReadWrite | UnixFileMode.UserExecute;
        private const UnixFileMode PermissionsMask_NonOwnerUsers_Write = UnixFileMode.GroupWrite | UnixFileMode.OtherWrite;
        private const UnixFileMode PermissionsMask_AllUsers_ReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;
        private const UnixFileMode PermissionsMask_AllUsers_ReadWriteExecute = PermissionsMask_AllUsers_ReadWrite | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        private const UnixFileMode PermissionsMask_Sticky = UnixFileMode.StickyBit;

        private const string SharedMemoryUniqueTempNameTemplate = ".dotnet.XXXXXX";

        // See https://developer.apple.com/documentation/Foundation/FileManager/containerURL(forSecurityApplicationGroupIdentifier:)#App-Groups-in-macOS for details on this path.
        private const string ApplicationContainerBasePathSuffix = "/Library/Group Containers/";

        public static string SharedFilesPath { get; } = InitalizeSharedFilesPath();
        private static string InitalizeSharedFilesPath()
        {
            if (OperatingSystem.IsApplePlatform())
            {
                string? applicationGroupId = Environment.GetEnvironmentVariable("DOTNET_SHARED_MEMORY_APPLICATION_GROUP_ID");
                if (applicationGroupId is not null)
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
            }

            return "/tmp/";
        }

        internal static SafeFileHandle CreateOrOpenFile(string sharedMemoryFilePath, SharedMemoryId id, bool createIfNotExist, out bool createdFile)
        {
            SafeFileHandle fd = Interop.Sys.Open(sharedMemoryFilePath, Interop.Sys.OpenFlags.O_RDWR | Interop.Sys.OpenFlags.O_CLOEXEC, 0);
            Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
            if (!fd.IsInvalid)
            {
                if (id.IsUserScope)
                {
                    if (Interop.Sys.FStat(fd, out Interop.Sys.FileStatus fileStatus) != 0)
                    {
                        error = Interop.Sys.GetLastErrorInfo();
                        fd.Dispose();
                        throw Interop.GetExceptionForIoErrno(error, sharedMemoryFilePath);
                    }

                    if (fileStatus.Uid != id.Uid)
                    {
                        fd.Dispose();
                        throw new IOException(SR.Format(SR.IO_SharedMemory_FileNotOwnedByUid, sharedMemoryFilePath, id.Uid));
                    }

                    if ((fileStatus.Mode & (int)PermissionsMask_AllUsers_ReadWriteExecute) != (int)PermissionsMask_OwnerUser_ReadWrite)
                    {
                        fd.Dispose();
                        throw new IOException(SR.Format(SR.IO_SharedMemory_FilePermissionsIncorrect, sharedMemoryFilePath, PermissionsMask_OwnerUser_ReadWrite));
                    }
                }
                createdFile = false;
                return fd;
            }

            if (error.Error != Interop.Error.ENOENT)
            {
                throw Interop.GetExceptionForIoErrno(error, sharedMemoryFilePath);
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

            fd = Interop.Sys.Open(
                sharedMemoryFilePath,
                Interop.Sys.OpenFlags.O_RDWR | Interop.Sys.OpenFlags.O_CLOEXEC | Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_EXCL,
                (int)permissionsMask);

            if (fd.IsInvalid)
            {
                error = Interop.Sys.GetLastErrorInfo();
                throw Interop.GetExceptionForIoErrno(error, sharedMemoryFilePath);
            }

            int result = Interop.Sys.FChMod(fd, (int)permissionsMask);

            if (result != 0)
            {
                error = Interop.Sys.GetLastErrorInfo();
                fd.Dispose();
                Interop.Sys.Unlink(sharedMemoryFilePath);
                throw Interop.GetExceptionForIoErrno(error, sharedMemoryFilePath);
            }

            createdFile = true;
            return fd;
        }

        internal static bool EnsureDirectoryExists(string directoryPath, SharedMemoryId id, bool isGlobalLockAcquired, bool createIfNotExist = true, bool isSystemDirectory = false)
        {
            UnixFileMode permissionsMask = id.IsUserScope
                ? PermissionsMask_OwnerUser_ReadWriteExecute
                : PermissionsMask_AllUsers_ReadWriteExecute;

            int statResult = Interop.Sys.Stat(directoryPath, out Interop.Sys.FileStatus fileStatus);

            if (statResult != 0 && Interop.Sys.GetLastError() == Interop.Error.ENOENT)
            {
                if (!createIfNotExist)
                {
                    // The directory does not exist and we are not allowed to create it.
                    return false;
                }

                // The path does not exist, create the directory. The permissions mask passed to mkdir() is filtered by the process'
                // permissions umask, so mkdir() may not set all of the requested permissions. We need to use chmod() to set the proper
                // permissions. That creates a race when there is no global lock acquired when creating the directory. Another user's
                // process may create the directory and this user's process may try to use it before the other process sets the full
                // permissions. In that case, create a temporary directory first, set the permissions, and rename it to the actual
                // directory name.

                if (isGlobalLockAcquired)
                {
#pragma warning disable CA1416 // Validate platform compatibility. This file is only included on Unix platforms.
                    Directory.CreateDirectory(directoryPath, permissionsMask);
#pragma warning restore CA1416 // Validate platform compatibility

                    try
                    {
                        FileSystem.SetUnixFileMode(directoryPath, permissionsMask);
                    }
                    catch (Exception)
                    {
                        Directory.Delete(directoryPath);
                        throw;
                    }

                    return true;
                }

                string tempPath = Path.Combine(SharedFilesPath, SharedMemoryUniqueTempNameTemplate);

                unsafe
                {
                    byte* tempPathPtr = Utf8StringMarshaller.ConvertToUnmanaged(tempPath);
                    if (Interop.Sys.MkdTemp(tempPathPtr) == null)
                    {
                        Utf8StringMarshaller.Free(tempPathPtr);
                        Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                        throw Interop.GetExceptionForIoErrno(error, tempPath);
                    }
                    // Convert the path back to get the substituted path.
                    tempPath = Utf8StringMarshaller.ConvertToManaged(tempPathPtr)!;
                    Utf8StringMarshaller.Free(tempPathPtr);
                }

                try
                {
                    FileSystem.SetUnixFileMode(tempPath, permissionsMask);
                }
                catch (Exception)
                {
                    Directory.Delete(tempPath);
                    throw;
                }

                if (Interop.Sys.Rename(tempPath, directoryPath) == 0)
                {
                    return true;
                }

                // Another process may have beaten us to it. Delete the temp directory and continue to check the requested directory to
                // see if it meets our needs.
                Directory.Delete(tempPath);
                statResult = Interop.Sys.Stat(directoryPath, out fileStatus);
            }

            // If the path exists, check that it's a directory
            if (statResult != 0 || (fileStatus.Mode & Interop.Sys.FileTypes.S_IFDIR) == 0)
            {
                if (statResult != 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    if (error.Error != Interop.Error.ENOENT)
                    {
                        throw Interop.GetExceptionForIoErrno(error, directoryPath);
                    }
                }
                else
                {
                    throw new IOException(SR.Format(SR.IO_SharedMemory_PathExistsButNotDirectory, directoryPath));
                }
            }

            if (isSystemDirectory)
            {
                // For system directories (such as TEMP_DIRECTORY_PATH), require sufficient permissions only for the
                // owner user. For instance, "docker run --mount ..." to mount /tmp to some directory on the host mounts the
                // destination directory with the same permissions as the source directory, which may not include some permissions for
                // other users. In the docker container, other user permissions are typically not relevant and relaxing the permissions
                // requirement allows for that scenario to work without having to work around it by first giving sufficient permissions
                // for all users.
                //
                // If the directory is being used for user-scoped shared memory data, also ensure that either it has the sticky bit or
                // it's owned by the current user and without write access for other users.

                permissionsMask = PermissionsMask_OwnerUser_ReadWriteExecute;
                if ((fileStatus.Mode & (int)permissionsMask) == (int)permissionsMask
                    && (
                        !id.IsUserScope ||
                        (fileStatus.Mode & (int)PermissionsMask_Sticky) == (int)PermissionsMask_Sticky ||
                        (fileStatus.Uid == id.Uid && (fileStatus.Mode & (int)PermissionsMask_NonOwnerUsers_Write) == 0)
                    ))
                {
                    return true;
                }

                throw new IOException(SR.Format(SR.IO_SharedMemory_DirectoryPermissionsIncorrect, directoryPath, fileStatus.Uid, Convert.ToString(fileStatus.Mode, 8)));
            }

            // For non-system directories (such as SharedFilesPath/UserUnscopedRuntimeTempDirectoryName),
            // require the sufficient permissions and try to update them if requested to create the directory, so that
            // shared memory files may be shared according to its scope.

            // For user-scoped directories, verify the owner UID
            if (id.IsUserScope && fileStatus.Uid != id.Uid)
            {
                throw new IOException(SR.Format(SR.IO_SharedMemory_DirectoryNotOwnedByUid, directoryPath, id.Uid));
            }

            // Verify the permissions, or try to change them if possible
            if ((fileStatus.Mode & (int)PermissionsMask_AllUsers_ReadWriteExecute) == (int)permissionsMask
                || (createIfNotExist && Interop.Sys.ChMod(directoryPath, (int)permissionsMask) == 0))
            {
                return true;
            }

            // We were not able to verify or set the necessary permissions. For user-scoped directories, this is treated as a failure
            // since other users aren't sufficiently restricted in permissions.
            if (id.IsUserScope)
            {
                throw new IOException(SR.Format(SR.IO_SharedMemory_DirectoryPermissionsIncorrectUserScope, directoryPath, Convert.ToString(fileStatus.Mode, 8)));
            }


            // For user-unscoped directories, as a last resort, check that at least the owner user has full access.
            permissionsMask = PermissionsMask_OwnerUser_ReadWriteExecute;
            if ((fileStatus.Mode & (int)permissionsMask) != (int)permissionsMask)
            {
                throw new IOException(SR.Format(SR.IO_SharedMemory_DirectoryOwnerPermissionsIncorrect, directoryPath, Convert.ToString(fileStatus.Mode, 8)));
            }

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

        public void SuppressRelease()
        {
            _suppressed = true;
        }

        public void Dispose()
        {
            if (!_suppressed && !fd.IsInvalid)
            {
                Interop.Sys.FLock(fd, Interop.Sys.LockOperations.LOCK_UN);
            }
        }
    }

    internal sealed class SharedMemoryManager<TSharedMemoryProcessData>
        where TSharedMemoryProcessData : class, ISharedMemoryProcessData
    {
        internal static SharedMemoryManager<TSharedMemoryProcessData> Instance { get; } = new SharedMemoryManager<TSharedMemoryProcessData>();

        internal const string SharedMemorySharedMemoryDirectoryName = "shm";

        private readonly LowLevelLock _creationDeletionProcessLock = new();
        private SafeFileHandle? _creationDeletionLockFileHandle;
        private readonly Dictionary<uint, SafeFileHandle> _uidToFileHandleMap = [];

        public WaitSubsystem.LockHolder AcquireCreationDeletionProcessLock()
        {
            return new WaitSubsystem.LockHolder(_creationDeletionProcessLock);
        }

        public void VerifyCreationDeletionProcessLockIsLocked()
        {
            _creationDeletionProcessLock.VerifyIsLocked();
        }

        public AutoReleaseFileLock AcquireCreationDeletionLockForId(SharedMemoryId id)
        {
            _creationDeletionProcessLock.VerifyIsLocked();
            SafeFileHandle? fd = id.IsUserScope ? GetUserScopeCreationDeletionLockFileHandle(id.Uid) : _creationDeletionLockFileHandle;
            if (fd is null)
            {
                if (!SharedMemoryHelpers.EnsureDirectoryExists(SharedMemoryHelpers.SharedFilesPath, id, isGlobalLockAcquired: false, createIfNotExist: false, isSystemDirectory: true))
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    throw Interop.GetExceptionForIoErrno(error, SharedMemoryHelpers.SharedFilesPath);
                }
                string runtimeTempDirectory = Path.Combine(
                    SharedMemoryHelpers.SharedFilesPath,
                    id.GetRuntimeTempDirectoryName());

                SharedMemoryHelpers.EnsureDirectoryExists(runtimeTempDirectory, id, isGlobalLockAcquired: false);

                string sharedMemoryDirectory = Path.Combine(
                    runtimeTempDirectory,
                    SharedMemorySharedMemoryDirectoryName);

                SharedMemoryHelpers.EnsureDirectoryExists(sharedMemoryDirectory, id, isGlobalLockAcquired: false);

                fd = Interop.Sys.Open(sharedMemoryDirectory, Interop.Sys.OpenFlags.O_RDONLY, 0);
                if (fd.IsInvalid)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    fd.Dispose();
                    throw Interop.GetExceptionForIoErrno(error, sharedMemoryDirectory);
                }

                if (id.IsUserScope)
                {
                    _uidToFileHandleMap.Add(id.Uid, fd);
                }
                else
                {
                    _creationDeletionLockFileHandle = fd;
                }
            }

            bool acquired = SharedMemoryHelpers.TryAcquireFileLock(fd, nonBlocking: true, exclusive: true);
            Debug.Assert(acquired);
            return new AutoReleaseFileLock(fd);

            SafeFileHandle? GetUserScopeCreationDeletionLockFileHandle(uint uid)
            {
                _uidToFileHandleMap.TryGetValue(uid, out SafeFileHandle? fileHandle);
                return fileHandle;
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
