// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    [UnsupportedOSPlatform("windows")]
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

            if (name.ContainsAny(['\\', '/']))
            {
                throw new ArgumentException("Name cannot contain path separators after prefixes.", nameof(name));
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
                return $"{SharedMemoryManager.UserScopedRuntimeTempDirectoryName}{Uid}";
            }
            else
            {
                return SharedMemoryManager.UserUnscopedRuntimeTempDirectoryName;
            }
        }

        internal readonly string GetSessionDirectoryName()
        {
            if (IsSessionScope)
            {
                return $"{SharedMemoryManager.SharedMemorySessionDirectoryName}{SharedMemoryManager.SessionId}";
            }
            else
            {
                return SharedMemoryManager.SharedMemoryGlobalDirectoryName;
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
            public uint Version;
        }

        [FieldOffset(0)]
        private SharedMemoryAndVersion _data;

        [FieldOffset(0)]
        private ulong _raw;

        public readonly SharedMemoryType Type => _data.Type;
        public readonly uint Version => _data.Version;

        public SharedMemorySharedDataHeader(SharedMemoryType type, uint version)
        {
            _data = new SharedMemoryAndVersion
            {
                Type = type,
                Version = version
            };
        }
    }

    [UnsupportedOSPlatform("windows")]
    [StructLayout(LayoutKind.Sequential)]
    internal ref struct NamedMutexSharedDataNoPThread
    {
        private uint _timedWaiterCount;
        private uint _lockOwnerProcessId;
        private uint _lockOwnerThreadId;
        private byte _isAbandoned;

        public uint TimedWaiterCount { get => _timedWaiterCount; set => _timedWaiterCount = value; }
        public uint LockOwnerProcessId
        {
            get
            {
                return _lockOwnerProcessId;
            }
            set
            {
                _lockOwnerProcessId = value;
            }
        }

        public uint LockOwnerThreadId
        {
            get
            {
                return _lockOwnerThreadId;
            }
            set
            {
                _lockOwnerThreadId = value;
            }
        }

        public bool IsAbandoned
        {
            get
            {
                return _isAbandoned != 0;
            }
            set
            {
                _isAbandoned = value ? (byte)1 : (byte)0;
            }
        }
    }

    [UnsupportedOSPlatform("windows")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe ref struct NamedMutexSharedDataWithPThread
    {
        public static readonly nuint Size;
        private static readonly nuint PThreadMutexSize;

        private const uint LockOwnerProcessIdOffset = 0x0;
        private const uint LockOwnerThreadIdOffset = 0x4;
        private const uint IsAbandonedOffset = 0x8;

        [UnscopedRef]
        public ref uint LockOwnerProcessId => ref *(uint*)((byte*)Unsafe.AsPointer(ref this) + PThreadMutexSize + LockOwnerProcessIdOffset);
        [UnscopedRef]
        public ref uint LockOwnerThreadId => ref *(uint*)((byte*)Unsafe.AsPointer(ref this) + PThreadMutexSize + LockOwnerThreadIdOffset);

        public bool IsAbandoned
        {
            get
            {
                return *((byte*)Unsafe.AsPointer(ref this) + PThreadMutexSize + IsAbandonedOffset) != 0;
            }
            set
            {
                *((byte*)Unsafe.AsPointer(ref this) + PThreadMutexSize + IsAbandonedOffset) = value ? (byte)1 : (byte)0;
            }
        }

        public void* LockAddress => Unsafe.AsPointer(ref this);
    }

    [UnsupportedOSPlatform("windows")]
    internal sealed unsafe class SharedMemoryProcessDataHeader<TSharedMemoryProcessData>
        where TSharedMemoryProcessData : class
    {
        internal SharedMemoryId _id;
        internal TSharedMemoryProcessData? _processData;
        private readonly SafeFileHandle _fileHandle;
        private readonly SharedMemorySharedDataHeader* _sharedDataHeader;
        private readonly nuint _sharedDataTotalByteCount;

        public SharedMemoryProcessDataHeader(SharedMemoryId id, SafeFileHandle fileHandle, SharedMemorySharedDataHeader* sharedDataHeader, nuint sharedDataTotalByteCount)
        {
            _id = id;
            _fileHandle = fileHandle;
            _sharedDataHeader = sharedDataHeader;
            _sharedDataTotalByteCount = sharedDataTotalByteCount;
            _processData = null; // Will be initialized later
        }

        public static void* GetDataPointer(SharedMemoryProcessDataHeader<TSharedMemoryProcessData> processDataHeader)
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
            creationDeletionLockFileHandle = new AutoReleaseFileLock(new SafeFileHandle());
            SharedMemoryId id = new(name, isUserScope);

            nuint sharedDataUsedByteCount = (nuint)sizeof(SharedMemorySharedDataHeader) + sharedMemoryDataSize;
            nuint sharedDataTotalByteCount = AlignUp(sharedDataUsedByteCount, SharedMemoryHelpers.GetVirtualPageSize());

            SharedMemoryProcessDataHeader<TSharedMemoryProcessData>? processDataHeader = SharedMemoryManager.Instance.FindProcessDataHeader(id);

            if (processDataHeader is not null)
            {
                Debug.Assert(processDataHeader._sharedDataTotalByteCount == sharedDataTotalByteCount);
                return processDataHeader;
            }

            creationDeletionLockFileHandle = SharedMemoryManager.Instance.AcquireCreationDeletionLockForId(id);

            string sessionDirectory = Path.Combine(
                SharedMemoryManager.SharedFilesPath,
                id.GetRuntimeTempDirectoryName(),
                SharedMemoryManager.SharedMemorySharedMemoryDirectoryName,
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
                SetFileSize(fileHandle, sharedMemoryFilePath, (long)sharedDataTotalByteCount);
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
                    throw new InvalidOperationException("Header mismatch");
                }

                SetFileSize(fileHandle, sharedMemoryFilePath, (long)sharedDataTotalByteCount);
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
            if (createdFile && clearContents)
            {
                NativeMemory.Clear(memory.Pointer, sharedDataUsedByteCount);
                *sharedDataHeader = requiredSharedDataHeader;
            }
            else
            {
                if (sharedDataHeader->Type != requiredSharedDataHeader.Type ||
                    sharedDataHeader->Version != requiredSharedDataHeader.Version)
                {
                    throw new InvalidOperationException("Header mismatch");
                }
            }

            if (!createdFile)
            {
                creationDeletionLockFileHandle.Dispose();
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

            static void SetFileSize(SafeFileHandle fd, string path, long size)
            {
                if (Interop.Sys.FTruncate(fd, 0) < 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error != Interop.Error.EBADF && errorInfo.Error != Interop.Error.EINVAL)
                    {
                        // We know the file descriptor is valid and we know the size argument to FTruncate is correct,
                        // so if EBADF or EINVAL is returned, it means we're dealing with a special file that can't be
                        // truncated.  Ignore the error in such cases; in all others, throw.
                        throw Interop.GetExceptionForIoErrno(errorInfo, path);
                    }
                }
            }
        }
    }

    [UnsupportedOSPlatform("windows")]
    internal abstract class NamedMutexProcessDataBase(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> header)
    {
        private const byte SyncSystemVersion = 1;
        protected const int PollLoopMaximumSleepMilliseconds = 100;

        private SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> _processDataHeader = header;
        protected nuint _lockCount;
        private Thread? _lockOwnerThread;
        private NamedMutexProcessDataBase? _nextInThreadOwnedNamedMutexList;

        protected SharedMemoryId Id => _processDataHeader._id;

        public abstract bool IsLockOwnedByCurrentThread { get; }

        protected abstract void SetLockOwnerToCurrentThread();

        public MutexTryAcquireLockResult TryAcquireLock(int timeoutMilliseconds)
        {
            MutexTryAcquireLockResult result = AcquireLockCore(timeoutMilliseconds);

            if (result == MutexTryAcquireLockResult.AcquiredLockRecursively)
            {
                return MutexTryAcquireLockResult.AcquiredLock;
            }

            SetLockOwnerToCurrentThread();
            _lockCount = 1;
            _lockOwnerThread = Thread.CurrentThread;
            AddOwnedNamedMutex(Thread.CurrentThread, this);

            if (IsAbandoned)
            {
                IsAbandoned = false;
                result = MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned;
            }

            return result;
        }

        private static void AddOwnedNamedMutex(Thread currentThread, NamedMutexProcessDataBase namedMutexProcessDataBase)
        {
            namedMutexProcessDataBase._nextInThreadOwnedNamedMutexList = currentThread._ownedSharedNamedMutexes;
            currentThread._ownedSharedNamedMutexes = namedMutexProcessDataBase;
        }

        public void ReleaseLock()
        {
            if (!IsLockOwnedByCurrentThread)
            {
                throw new InvalidOperationException("Cannot release a lock that is not owned by the current thread.");
            }

            --_lockCount;
            if (_lockCount != 0)
            {
                return;
            }

            RemoveOwnedNamedMutex(Thread.CurrentThread, this);
            _lockOwnerThread = null;
            ReleaseLockCore();
        }

        private static void RemoveOwnedNamedMutex(Thread currentThread, NamedMutexProcessDataBase namedMutexProcessDataBase)
        {
            if (currentThread._ownedSharedNamedMutexes == namedMutexProcessDataBase)
            {
                currentThread._ownedSharedNamedMutexes = namedMutexProcessDataBase._nextInThreadOwnedNamedMutexList;
            }
            else
            {
                NamedMutexProcessDataBase? previous = currentThread._ownedSharedNamedMutexes;
                while (previous?._nextInThreadOwnedNamedMutexList != namedMutexProcessDataBase)
                {
                    previous = previous?._nextInThreadOwnedNamedMutexList;
                }

                if (previous is not null)
                {
                    previous._nextInThreadOwnedNamedMutexList = namedMutexProcessDataBase._nextInThreadOwnedNamedMutexList;
                }
            }

            namedMutexProcessDataBase._nextInThreadOwnedNamedMutexList = null;
        }

        public void Abandon()
        {
            IsAbandoned = true;
            _lockCount = 0;
            _lockOwnerThread = null;
            ReleaseLockCore();
        }

        protected abstract MutexTryAcquireLockResult AcquireLockCore(int timeoutMilliseconds);

        protected abstract bool IsAbandoned { get; set; }

        protected abstract void ReleaseLockCore();

        ~NamedMutexProcessDataBase()
        {
            if (_lockOwnerThread is not null)
            {
                Abandon();
            }
        }

        private static unsafe SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>? CreateOrOpen(string name, bool isUserScope, bool createIfNotExist, bool acquireLockIfCreated, out bool created)
        {
            using Lock.Scope creationDeletionProcessLock = SharedMemoryManager.Instance.AcquireCreationDeletionProcessLock();

            SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>? processDataHeader = SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.CreateOrOpen(
                name,
                isUserScope,
                new SharedMemorySharedDataHeader(SharedMemoryType.Mutex, SyncSystemVersion),
                SharedMemoryManager.UsePThreadMutexes ? NamedMutexSharedDataWithPThread.Size : (nuint)sizeof(NamedMutexSharedDataNoPThread),
                createIfNotExist,
                acquireLockIfCreated,
                out created,
                out AutoReleaseFileLock creationDeletionLockFileScope);

            if (processDataHeader is null)
            {
                return null;
            }

            using (creationDeletionLockFileScope)
            {
                if (created)
                {
                    InitializeSharedData(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.GetDataPointer(processDataHeader));
                }

                if (processDataHeader._processData is null)
                {
                    if (SharedMemoryManager.UsePThreadMutexes)
                    {
                        processDataHeader._processData = new NamedMutexProcessDataWithPThreads(processDataHeader);
                    }
                    else
                    {
                        processDataHeader._processData = new NamedMutexProcessDataNoPThreads(processDataHeader, created);
                    }

                    if (created && acquireLockIfCreated)
                    {
                        MutexTryAcquireLockResult acquireResult = processDataHeader._processData.TryAcquireLock(timeoutMilliseconds: 0);
                        Debug.Assert(acquireResult != MutexTryAcquireLockResult.AcquiredLock);
                    }
                }

                return processDataHeader;
            }
        }

        private static unsafe void InitializeSharedData(void* v)
        {
            if (SharedMemoryManager.UsePThreadMutexes)
            {
                NamedMutexSharedDataWithPThread* sharedData = (NamedMutexSharedDataWithPThread*)v;
                if (Interop.Sys.PThreadMutex_Init(sharedData->LockAddress) != 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    throw Interop.GetExceptionForIoErrno(errorInfo, "Failed to initialize pthread mutex");
                }
                sharedData->LockOwnerProcessId = SharedMemoryHelpers.InvalidProcessId;
                sharedData->LockOwnerThreadId = SharedMemoryHelpers.InvalidThreadId;
                sharedData->IsAbandoned = false;
            }
            else
            {
                NamedMutexSharedDataNoPThread* sharedData = (NamedMutexSharedDataNoPThread*)v;
                sharedData->LockOwnerProcessId = SharedMemoryHelpers.InvalidProcessId;
                sharedData->LockOwnerThreadId = SharedMemoryHelpers.InvalidThreadId;
                sharedData->IsAbandoned = false;
                sharedData->TimedWaiterCount = 0;
            }
        }
    }

    [UnsupportedOSPlatform("windows")]
    internal unsafe class NamedMutexProcessDataWithPThreads(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader) : NamedMutexProcessDataBase(processDataHeader)
    {
        private readonly NamedMutexSharedDataWithPThread* _sharedData = (NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.GetDataPointer(processDataHeader);

        public override bool IsLockOwnedByCurrentThread
        {
            get
            {
                return _sharedData->LockOwnerProcessId == (uint)Environment.ProcessId &&
                       _sharedData->LockOwnerThreadId == (uint)Thread.CurrentThread.ManagedThreadId;
            }
        }

        protected override void SetLockOwnerToCurrentThread()
        {
            _sharedData->LockOwnerProcessId = (uint)Environment.ProcessId;
            _sharedData->LockOwnerThreadId = (uint)Thread.CurrentThread.ManagedThreadId;
        }

        protected override bool IsAbandoned
        {
            get => _sharedData->IsAbandoned;
            set => _sharedData->IsAbandoned = value;
        }

        protected override MutexTryAcquireLockResult AcquireLockCore(int timeoutMilliseconds)
        {
            Interop.Error lockResult = (Interop.Error)Interop.Sys.PThreadMutex_Acquire(_sharedData->LockAddress, timeoutMilliseconds);

            MutexTryAcquireLockResult result = lockResult switch
            {
                Interop.Error.SUCCESS => MutexTryAcquireLockResult.AcquiredLock,
                Interop.Error.EBUSY => MutexTryAcquireLockResult.TimedOut,
                Interop.Error.ETIMEDOUT => MutexTryAcquireLockResult.TimedOut,
                Interop.Error.EOWNERDEAD => MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned,
                Interop.Error.EAGAIN => throw new OutOfMemoryException(),
                _ => throw new Win32Exception((int)lockResult)
            };

            if (result == MutexTryAcquireLockResult.TimedOut)
            {
                return MutexTryAcquireLockResult.TimedOut;
            }

            if (_lockCount != 0)
            {
                Debug.Assert(IsLockOwnedByCurrentThread);
                Debug.Assert(result != MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned);
                Debug.Assert(!IsAbandoned);
                try
                {
                    checked
                    {
                        _lockCount++;
                    }
                    return MutexTryAcquireLockResult.AcquiredLockRecursively;
                }
                finally
                {
                    // The lock is released upon acquiring a recursive lock from the thread that already owns the lock
                    Interop.Sys.PThreadMutex_Release(_sharedData->LockAddress);
                }
            }

            return result;
        }

        protected override void ReleaseLockCore()
        {
            Debug.Assert(IsLockOwnedByCurrentThread);
            Debug.Assert(_lockCount == 0);
            _sharedData->LockOwnerProcessId = 0;
            _sharedData->LockOwnerThreadId = 0;

            Interop.Sys.PThreadMutex_Release(_sharedData->LockAddress);
        }
    }

    [UnsupportedOSPlatform("windows")]
    internal unsafe class NamedMutexProcessDataNoPThreads : NamedMutexProcessDataBase
    {
        private readonly Lock _processLevelLock = new();
        private readonly SafeFileHandle _sharedLockFileHandle;

        private readonly NamedMutexSharedDataNoPThread* _sharedData;

        public NamedMutexProcessDataNoPThreads(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader, bool created) : base(processDataHeader)
        {
            _sharedData = (NamedMutexSharedDataNoPThread*)SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.GetDataPointer(processDataHeader);
            string lockFileDirectory = Path.Combine(
                SharedMemoryManager.SharedFilesPath,
                Id.GetRuntimeTempDirectoryName(),
                SharedMemoryManager.SharedMemoryLockFilesDirectoryName
            );

            if (created)
            {
                SharedMemoryHelpers.EnsureDirectoryExists(lockFileDirectory, Id, isGlobalLockAcquired: true);
            }

            string sessionDirectory = Path.Combine(lockFileDirectory, Id.GetSessionDirectoryName());

            if (created)
            {
                SharedMemoryHelpers.EnsureDirectoryExists(sessionDirectory, Id, isGlobalLockAcquired: true);
            }

            string lockFilePath = Path.Combine(sessionDirectory, Id.Name);
            _sharedLockFileHandle = SharedMemoryHelpers.CreateOrOpenFile(lockFilePath, Id, created, out _);
        }

        public override bool IsLockOwnedByCurrentThread
        {
            get
            {
                return _sharedData->LockOwnerProcessId == (uint)Environment.ProcessId &&
                       _sharedData->LockOwnerThreadId == (uint)Thread.CurrentThread.ManagedThreadId;
            }
        }

        protected override void SetLockOwnerToCurrentThread()
        {
            _sharedData->LockOwnerProcessId = (uint)Environment.ProcessId;
            _sharedData->LockOwnerThreadId = (uint)Thread.CurrentThread.ManagedThreadId;
        }

        private bool IsLockOwnedByAnyThread
        {
            get
            {
                return _sharedData->LockOwnerProcessId != SharedMemoryHelpers.InvalidProcessId &&
                       _sharedData->LockOwnerThreadId != SharedMemoryHelpers.InvalidThreadId;
            }
        }

        protected override void ReleaseLockCore()
        {
            Debug.Assert(IsLockOwnedByCurrentThread);
            Debug.Assert(_lockCount == 0);
            _sharedData->LockOwnerProcessId = 0;
            _sharedData->LockOwnerThreadId = 0;

            Interop.Sys.FLock(_sharedLockFileHandle, Interop.Sys.LockOperations.LOCK_UN);
            _processLevelLock.Exit();
        }

        protected override bool IsAbandoned
        {
            get => _sharedData->IsAbandoned;
            set => _sharedData->IsAbandoned = value;
        }

        ~NamedMutexProcessDataNoPThreads()
        {
            string sessionDirectory = Path.Combine(
                SharedMemoryManager.SharedFilesPath,
                Id.GetRuntimeTempDirectoryName(),
                SharedMemoryManager.SharedMemoryLockFilesDirectoryName,
                Id.GetSessionDirectoryName()
            );

            try
            {
                // Delete the lock file.
                File.Delete(Path.Combine(sessionDirectory, Id.Name));
                // Delete the session directory if it's empty.
                Directory.Delete(sessionDirectory);
            }
            catch (Exception)
            {
                // Ignore the error, just don't release the shared data.
            }
        }

        protected override unsafe MutexTryAcquireLockResult AcquireLockCore(int timeoutMilliseconds)
        {
            int startTime = 0;
            if (timeoutMilliseconds > 0)
            {
                startTime = Environment.TickCount;
            }

            // Acquire the process lock. A file lock can only be acquired once per file descriptor, so to synchronize the threads of
            // this process, the process lock is used.
            using Lock.Scope scope = _processLevelLock.EnterScope();

            if (_lockCount > 0)
            {
                Debug.Assert(IsLockOwnedByCurrentThread);
                // The lock is already owned by the current thread.
                checked
                {
                    _lockCount++;
                }
                return MutexTryAcquireLockResult.AcquiredLockRecursively;
            }

            switch (timeoutMilliseconds)
            {
                case -1:
                    bool acquiredLock = false;
                    while (_sharedData->TimedWaiterCount > 0)
                    {
                        if (SharedMemoryHelpers.TryAcquireFileLock(_sharedLockFileHandle, nonBlocking: true))
                        {
                            acquiredLock = true;
                            break;
                        }
                        Thread.Sleep(PollLoopMaximumSleepMilliseconds);
                    }

                    if (acquiredLock)
                    {
                        break;
                    }

                    acquiredLock = SharedMemoryHelpers.TryAcquireFileLock(_sharedLockFileHandle, nonBlocking: false);
                    Debug.Assert(acquiredLock);
                    break;
                case 0:
                    if (!SharedMemoryHelpers.TryAcquireFileLock(_sharedLockFileHandle, nonBlocking: true))
                    {
                        return MutexTryAcquireLockResult.TimedOut;
                    }
                    break;
                default:
                {
                    if (SharedMemoryHelpers.TryAcquireFileLock(_sharedLockFileHandle, nonBlocking: true))
                    {
                        break;
                    }

                    _sharedData->TimedWaiterCount++;

                    do
                    {
                        int elapsedMilliseconds = Environment.TickCount - startTime;
                        if (elapsedMilliseconds >= timeoutMilliseconds)
                        {
                            _sharedData->TimedWaiterCount--;
                            return MutexTryAcquireLockResult.TimedOut;
                        }

                        int remainingTimeoutMilliseconds = timeoutMilliseconds - elapsedMilliseconds;
                        int sleepMilliseconds = Math.Min(PollLoopMaximumSleepMilliseconds, remainingTimeoutMilliseconds);
                        Thread.Sleep(sleepMilliseconds);
                    } while (!SharedMemoryHelpers.TryAcquireFileLock(_sharedLockFileHandle, nonBlocking: true));
                    _sharedData->TimedWaiterCount--;
                    break;
                }
            }

            // We've now acquired the lock.
            // We're going to release the scoped process lock we took above,
            // so recursively acquire it to maintain the lock ownership.
            _processLevelLock.Enter();

            // Detect abandoned lock that isn't marked as abandoned.
            if (IsLockOwnedByAnyThread)
                return MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned;

            return MutexTryAcquireLockResult.AcquiredLock;
        }
    }

    [UnsupportedOSPlatform("windows")]
    internal static class SharedMemoryHelpers
    {
        public const uint InvalidProcessId = unchecked((uint)-1);
        public const uint InvalidThreadId = unchecked((uint)-1);

        private const UnixFileMode PermissionsMask_OwnerUser_ReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        private const UnixFileMode PermissionsMask_OwnerUser_ReadWriteExecute = PermissionsMask_OwnerUser_ReadWrite | UnixFileMode.UserExecute;
        private const UnixFileMode PermissionsMask_NonOwnerUsers_Write = UnixFileMode.GroupWrite | UnixFileMode.OtherWrite;
        private const UnixFileMode PermissionsMask_AllUsers_ReadWrite = UnixFileMode.UserRead | UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;
        private const UnixFileMode PermissionsMask_AllUsers_ReadWriteExecute = PermissionsMask_AllUsers_ReadWrite | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        private const UnixFileMode PermissionsMask_Sticky = UnixFileMode.StickyBit;

        internal static SafeFileHandle CreateOrOpenFile(string sharedMemoryFilePath, SharedMemoryId id, bool createIfNotExist, out bool createdFile)
        {
            SafeFileHandle fd = Interop.Sys.Open(sharedMemoryFilePath, Interop.Sys.OpenFlags.O_RDWR, 0);
            if (!fd.IsInvalid)
            {
                if (id.IsUserScope)
                {
                    if (Interop.Sys.FStat(fd, out Interop.Sys.FileStatus fileStatus) != 0)
                    {
                        Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                        fd.Dispose();
                        throw Interop.GetExceptionForIoErrno(error, sharedMemoryFilePath);
                    }

                    if (fileStatus.Uid != id.Uid)
                    {
                        fd.Dispose();
                        throw new IOException($"The file '{sharedMemoryFilePath}' is not owned by the current user with UID {id.Uid}.");
                    }

                    if ((fileStatus.Mode & (int)PermissionsMask_AllUsers_ReadWriteExecute) != (int)PermissionsMask_OwnerUser_ReadWrite)
                    {
                        fd.Dispose();
                        throw new IOException($"The file '{sharedMemoryFilePath}' does not have the expected permissions for user scope: {PermissionsMask_OwnerUser_ReadWrite}.");
                    }
                }
                createdFile = false;
                return fd;
            }

            Debug.Assert(Interop.Sys.GetLastError() == Interop.Error.ENOENT);
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
                Interop.Sys.OpenFlags.O_RDWR | Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_EXCL,
                (int)permissionsMask);

            int result = Interop.Sys.FChMod(fd, (int)permissionsMask);

            if (result != 0)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
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
                    Directory.CreateDirectory(directoryPath, permissionsMask);

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

                string tempPath = Path.Combine(SharedMemoryManager.SharedFilesPath, SharedMemoryManager.SharedMemoryUniqueTempNameTemplate);

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
                    throw new IOException($"The path '{directoryPath}' exists but is not a directory.");
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

                throw new IOException($"The directory '{directoryPath}' does not have the expected owner or permissions for system scope: {fileStatus.Uid}, {Convert.ToString(fileStatus.Mode, 8)}.");
            }

            // For non-system directories (such as SharedFilesPath/UserUnscopedRuntimeTempDirectoryName),
            // require the sufficient permissions and try to update them if requested to create the directory, so that
            // shared memory files may be shared according to its scope.

            // For user-scoped directories, verify the owner UID
            if (id.IsUserScope && fileStatus.Uid != id.Uid)
            {
                throw new IOException($"The directory '{directoryPath}' is not owned by the current user with UID {id.Uid}.");
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
                throw new IOException($"The directory '{directoryPath}' does not have the expected permissions for user scope: {Convert.ToString(fileStatus.Mode, 8)}.");
            }


            // For user-unscoped directories, as a last resort, check that at least the owner user has full access.
            permissionsMask = PermissionsMask_OwnerUser_ReadWriteExecute;
            if ((fileStatus.Mode & (int)permissionsMask) != (int)permissionsMask)
            {
                throw new IOException($"The directory '{directoryPath}' does not have the expected owner permissions: {Convert.ToString(fileStatus.Mode, 8)}.");
            }

            return true;
        }

        internal static nuint GetVirtualPageSize() => throw new NotImplementedException();
        internal static MemoryMappedFileHolder MemoryMapFile(SafeFileHandle fileHandle, nuint sharedDataTotalByteCount)
        {
            nint addr = Interop.Sys.MMap(
                0,
                sharedDataTotalByteCount,
                Interop.Sys.MemoryMappedProtections.PROT_READ | Interop.Sys.MemoryMappedProtections.PROT_WRITE,
                Interop.Sys.MemoryMappedFlags.MAP_SHARED,
                fileHandle,
                (long)sharedDataTotalByteCount);

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

    internal enum MutexTryAcquireLockResult : byte
    {
        AcquiredLock,
        AcquiredLockButMutexWasAbandoned,
        TimedOut,
        AcquiredLockRecursively,
    }

    [UnsupportedOSPlatform("windows")]
    internal sealed class SharedMemoryManager
    {
        [FeatureSwitchDefinition("System.Threading.NamedMutexUsePThreadMutex")]
        internal static bool UsePThreadMutexes { get; } = AppContext.TryGetSwitch("System.Threading.NamedMutexUsePThreadMutex", out bool value)
            ? value
            : false;

        internal static SharedMemoryManager Instance { get; } = new SharedMemoryManager();
        public static string SharedFilesPath { get; } = InitalizeSharedFilesPath();
        public static int SessionId { get; } = Interop.Sys.GetSid(Environment.ProcessId);

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

            if (OperatingSystem.IsAndroid())
            {
                return "/data/local/tmp/";
            }
            else
            {
                return "/tmp/";
            }
        }

        private const string ApplicationContainerBasePathSuffix = "/Library/Group Containers/";

        internal const string SharedMemoryLockFilesDirectoryName = "lockfiles";

        internal const string SharedMemorySharedMemoryDirectoryName = "shm";

        internal const string UserUnscopedRuntimeTempDirectoryName = ".dotnet";

        internal const string UserScopedRuntimeTempDirectoryName = ".dotnet-uid";

        internal const string SharedMemoryGlobalDirectoryName = "global";

        internal const string SharedMemorySessionDirectoryName = "session";

        internal const string SharedMemoryUniqueTempNameTemplate = ".dotnet.XXXXXX";

        private Lock _creationDeletionProcessLock = new Lock();
        private SafeFileHandle? _creationDeletionLockFileHandle;
        private Dictionary<uint, SafeFileHandle> _uidToFileHandleMap = [];

        public Lock.Scope AcquireCreationDeletionProcessLock()
        {
            return _creationDeletionProcessLock.EnterScope();
        }

        public AutoReleaseFileLock AcquireCreationDeletionLockForId(SharedMemoryId id)
        {
            Debug.Assert(_creationDeletionProcessLock.IsHeldByCurrentThread);
            SafeFileHandle? fd = id.IsUserScope ? GetUserScopeCreationDeletionLockFileHandle(id.Uid) : _creationDeletionLockFileHandle;
            if (fd is null)
            {
                if (!SharedMemoryHelpers.EnsureDirectoryExists(SharedFilesPath, id, isGlobalLockAcquired: false, createIfNotExist: false, isSystemDirectory: true))
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    throw Interop.GetExceptionForIoErrno(error, SharedFilesPath);
                }
                string runtimeTempDirectory = Path.Combine(
                    SharedFilesPath,
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
            }

            if (id.IsUserScope)
            {
                _uidToFileHandleMap.Add(id.Uid, fd);
            }

            _creationDeletionLockFileHandle = fd;

            bool acquired = SharedMemoryHelpers.TryAcquireFileLock(fd, nonBlocking: true, exclusive: true);
            Debug.Assert(acquired);
            return new AutoReleaseFileLock(fd);
        }

        public void AddUserScopeCreationDeletionLockFileHandle(uint uid, SafeFileHandle fileHandle)
        {
            _uidToFileHandleMap[uid] = fileHandle;
        }

        public SafeFileHandle? GetUserScopeCreationDeletionLockFileHandle(uint uid)
        {
            _uidToFileHandleMap.TryGetValue(uid, out SafeFileHandle? fileHandle);
            return fileHandle;
        }

        private Dictionary<SharedMemoryId, SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>> _processDataHeaders = [];

        public void AddProcessDataHeader(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader)
        {
            _processDataHeaders[processDataHeader._id] = processDataHeader;
        }

        public void RemoveProcessDataHeader(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader)
        {
            _processDataHeaders.Remove(processDataHeader._id);
        }

        public SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>? FindProcessDataHeader(SharedMemoryId id)
        {
            _processDataHeaders.TryGetValue(id, out SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>? header);
            return header;
        }
    }
}
