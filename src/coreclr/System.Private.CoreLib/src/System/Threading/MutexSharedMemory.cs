// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal unsafe struct SharedMemoryId
    {
        private byte* _name;
        private nint _nameCharLength;
        private byte _isSessionScope;

        private byte _isUserScope;
        private uint _uid;


        public bool IsSessionScope => _isSessionScope != 0;
        public bool IsUserScope => _isUserScope != 0;
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

        public SharedMemoryType Type => _data.Type;
        public uint Version => _data.Version;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct NamedMutexSharedDataNoPThread
    {
        uint _timedWaiterCount;
        uint _lockOwnerProcessId;
        uint _lockOwnerThreadId;
        byte _isAbandoned;

        public uint TimedWaiterCount {get => _timedWaiterCount; set => _timedWaiterCount = value; }
        public uint LockOwnerProcessId => _lockOwnerProcessId;
        public uint LockOwnerThreadId => _lockOwnerThreadId;

        public bool IsAbandoned => _isAbandoned != 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct NamedMutexSharedDataWithPThread
    {
        public static readonly nuint Size;
        private static readonly nuint PThreadMutexSize;

        private const uint LockOwnerProcessIdOffset = 0x0;
        private const uint LockOwnerThreadIdOffset = 0x4;
        private const uint IsAbandonedOffset = 0x8;

        public uint LockOwnerProcessId => *(uint*)(Unsafe.AsPointer(ref this) + PThreadMutexSize + LockOwnerProcessIdOffset);
        public uint LockOwnerThreadId => *(uint*)(Unsafe.AsPointer(ref this) + PThreadMutexSize + LockOwnerThreadIdOffset);
        public bool IsAbandoned => *(byte*)(Unsafe.AsPointer(ref this) + PThreadMutexSize + IsAbandonedOffset) != 0;

        public void* LockAddress => Unsafe.AsPointer(ref this);
    }

    internal unsafe class SharedMemoryProcessDataHeader
    {
        private nuint _refCount;
        private SharedMemoryId _id;
        internal ISharedMemoryProcessData _data;
        private SafeFileHandle _fileHandle;
        internal SharedMemorySharedDataHeader* _sharedDataHeader;
        private nuint _sharedDataTotalByteCount;

        public static void* GetDataPointer(SharedMemorySharedDataHeader processDataHeader)
        {
            if (processDataHeader == null)
            {
                return null;
            }

            return processDataHeader._sharedDataHeader + 1;
        }
    }

    internal interface ISharedMemoryProcessData
    {
        bool CanClose { get; }
        bool HasImplicitRef { get; set; }
        void Close(bool isAbruptShutdown, bool releaseSharedData);
    }

    abstract class NamedMutexProcessDataBase(SharedMemoryProcessDataHeader header) : ISharedMemoryProcessData
    {
        private const byte SyncSystemVersion = 1;
        private const int PollLoopMaximumSleepMilliseconds = 100;

        private SharedMemoryProcessDataHeader _processDataHeader = header;
        protected nuint _lockCount;
        Thread? _lockOwnerThread;
        NamedMutexProcessDataBase? _nextInThreadOwnedNamedMutexList;
        bool _hasRefFromLockOwnerThread;

        public abstract bool IsLockOwnedByCurrentThread { get; }

        protected abstract void SetLockOwnerToCurrentThread();

        public MutexTryAcquireResult TryAcquireLock(int timeoutMilliseconds)
        {
            MutexTryAcquireResult result = AcquireLockCore(timeoutMilliseconds);

            if (result == MutexTryAcquireResult.AcquiredLockRecursively)
            {
                return MutexTryAcquireResult.AcquiredLock;
            }

            SetLockOwnerToCurrentThread();
            _lockCount = 1;
            _lockOwnerThread = Thread.CurrentThread;
            AddOwnedNamedMutex(Thread.CurrentThread, this);

            if (IsAbandoned)
            {
                IsAbandoned = false;
                result = MutexTryAcquireResult.AcquiredLockButMutexWasAbandoned;
            }

            return result;
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

        public void Abandon()
        {
            IsAbandoned = true;
            _lockCount = 0;
            _lockOwnerThread = null;
            ReleaseLockCore();

            if (_hasRefFromLockOwnerThread)
            {
                _hasRefFromLockOwnerThread = false;
                _processDataHeader.DecRefCount();
            }
        }

        protected abstract MutexTryAcquireLockResult AcquireLockCore(int timeoutMilliseconds);

        protected abstract bool IsAbandoned { get; set; }

        protected abstract void ReleaseLockCore();

        public bool CanClose => _lockOwnerThread == null || _lockOwnerThread == Thread.CurrentThread;

        public bool HasImplicitRef
        {
            get => _hasRefFromLockOwnerThread;
            set => _hasRefFromLockOwnerThread = value;
        }

        public virtual void Close(bool isAbruptShutdown, bool releaseSharedData)
        {
            if (!isAbruptShutdown)
            {
                Debug.Assert(CanClose);
                Debug.Assert(!_hasRefFromLockOwnerThread);

                if (_lockOwnerThread == Thread.CurrentThread)
                {
                    RemoveOwnedNamedMutex(Thread.CurrentThread, this);
                    Abandon();
                }
                else
                {
                    Debug.Assert(_lockOwnerThread == null);
                }

                if (releaseSharedData)
                {
                    ReleaseSharedData();
                }
            }
        }

        protected abstract void ReleaseSharedData();

        private static unsafe SharedMemoryProcessDataHeader CreateOrOpen(string name, bool isUserScope, bool createIfNotExist, bool acquireLockIfCreated, out bool created)
        {
            using var creationDeletionProcessLock = SharedMemoryManager.Instance.AcquireCreationDeletionLock();

            SharedMemoryProcessDataHeader processDataHeader = SharedMemoryProcessDataHeader.CreateOrOpen(name, isUserScope, new SharedMemorySharedDataHeader
            {
                Type = SharedMemoryType.Mutex,
                Version = SyncSystemVersion,
            },
            SharedMemoryManager.UsePThreadMutexes ? NamedMutexSharedDataWithPThread.Size : sizeof(NamedMutexSharedDataNoPThread),
            createIfNotExist, acquireLockIfCreated, out created, out IDisposable? creationDeletionLockFileHandle);

            if (processDataHeader == null)
            {
                return null;
            }

            using IDisposable? creationDeletionLockFileHandleScope = created ? creationDeletionLockFileHandle : null;

            if (created)
            {
                InitializeSharedData(SharedMemoryProcessDataHeader.GetDataPointer(processDataHeader._sharedDataHeader));
            }

            if (processDataHeader._data is null)
            {
                if (SharedMemoryManager.UsePThreadMutexes)
                {
                    processDataHeader._data = new NamedMutexProcessDataWithPThread(processDataHeader);
                }
                else
                {
                    processDataHeader._data = new NamedMutexProcessDataNoPThreads(processDataHeader, created);
                }

                if (created && acquireLockIfCreated)
                {
                    MutexTryAcquireResult acquireResult = processDataHeader._data.TryAcquireLock();
                    Debug.Assert(acquireResult != MutexTryAcquireResult.AcquiredLock);
                }
            }

            return processDataHeader;
        }
    }

    class NamedMutexProcessDataWithPThreads : NamedMutexProcessDataBase
    {
        public NamedMutexProcessDataWithPThreads(SharedMemoryProcessDataHeader processDataHeader) : base(processDataHeader)
        {
            // Initialize the shared data header for pthread mutexes.
            NamedMutexSharedDataWithPThread* sharedData = (NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
        }

        public override bool IsLockOwnedByCurrentThread
        {
            get
            {
                NamedMutexSharedDataWithPThread* sharedData = (NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
                return sharedData->LockOwnerProcessId == (uint)Environment.ProcessId &&
                       sharedData->LockOwnerThreadId == (uint)Thread.CurrentThread.ManagedThreadId;
            }
        }

        protected override void SetLockOwnerToCurrentThread()
        {
            NamedMutexSharedDataWithPThread* sharedData = (NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
            sharedData->LockOwnerProcessId = (uint)Environment.ProcessId;
            sharedData->LockOwnerThreadId = (uint)Thread.CurrentThread.ManagedThreadId;
        }

        private bool IsLockOwnedByAnyThread
        {
            get
            {
                NamedMutexSharedDataWithPThread* sharedData = (NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
                return sharedData->LockOwnerProcessId != SharedMemoryHelpers.InvalidProcessId &&
                       sharedData->LockOwnerThreadId != SharedMemoryHelpers.InvalidThreadId;
            }
        }

        protected override bool IsAbandoned
        {
            get => ((NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader))->IsAbandoned;
            set => ((NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader))->IsAbandoned = value ? 1 : 0;
        }

        protected override void ReleaseSharedData()
        {
        }

        protected override MutexTryAcquireLockResult AcquireLockCore(System.Int32 timeoutMilliseconds)
        {
            var sharedData = (NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
            MutexTryAcquireResult result = Interop.Sys.PThreadMutex_TryAcquireLock(sharedData->LockAddress, timeoutMilliseconds);

            if (result == MutexTryAcquireResult.TimedOut)
            {
                return MutexTryAcquireLockResult.TimedOut;
            }

            if (_lockCount != 0)
            {
                Debug.Assert(IsLockOwnedByCurrentThread);
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
                    Interop.Sys.PThreadMutex_ReleaseLock(sharedData->LockAddress);
                }
                Debug.Assert(result != MutexTryAcquireResult.AcquiredLockButMutexWasAbandoned);
                Debug.Assert(!IsAbandoned);
            }

            return result;
        }

        protected override void ReleaseLockCore()
        {
            Debug.Assert(IsLockOwnedByCurrentThread);
            Debug.Assert(_lockCount == 0);
            NamedMutexSharedDataWithPThread* sharedData = (NamedMutexSharedDataWithPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
            sharedData->LockOwnerProcessId = 0;
            sharedData->LockOwnerThreadId = 0;

            Interop.Sys.PThreadMutex_ReleaseLock(sharedData->LockAddress);
        }
    }

    class NamedMutexProcessDataNoPThreads : NamedMutexProcessDataBase
    {
        private Lock _processLockHandle = new();
        private SafeFileHandle _sharedLockFileHandle;
        public NamedMutexProcessDataNoPThreads(SharedMemoryProcessDataHeader processDataHeader, bool created) : base(processDataHeader)
        {
            SharedMemoryId id = processDataHeader._id;
            string lockFileDirectory = Path.Combine(
                SharedMemoryManager.SharedFilesPath,
                id.GetRuntimeTempDirectoryName(),
                SharedMemoryManager.SharedMemoryLockFilesDirectoryName
            );

            if (created)
            {
                SharedMemoryHelpers.EnsureDirectoryExists(lockFileDirectory);
            }

            string sessionDirectory = Path.Combine(lockFileDirectory, id.GetSessionDirectoryName());

            if (created)
            {
                SharedMemoryHelpers.EnsureDirectoryExists(sessionDirectory);
            }

            string lockFilePath = Path.Combine(sessionDirectory, id.GetName());
            _sharedLockFileHandle = SharedMemoryHelpers.CreateOrOpenLockFile(lockFilePath, id, out bool createdNew);
        }

        public override bool IsLockOwnedByCurrentThread
        {
            get
            {
                NamedMutexSharedDataNoPThread* sharedData = (NamedMutexSharedDataNoPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
                return sharedData->LockOwnerProcessId == (uint)Environment.ProcessId &&
                       sharedData->LockOwnerThreadId == (uint)Thread.CurrentThread.ManagedThreadId;
            }
        }

        protected override void SetLockOwnerToCurrentThread()
        {
            NamedMutexSharedDataNoPThread* sharedData = (NamedMutexSharedDataNoPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
            sharedData->LockOwnerProcessId = (uint)Environment.ProcessId;
            sharedData->LockOwnerThreadId = (uint)Thread.CurrentThread.ManagedThreadId;
        }

        private bool IsLockOwnedByAnyThread
        {
            get
            {
                NamedMutexSharedDataNoPThread* sharedData = (NamedMutexSharedDataNoPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
                return sharedData->LockOwnerProcessId != SharedMemoryHelpers.InvalidProcessId &&
                       sharedData->LockOwnerThreadId != SharedMemoryHelpers.InvalidThreadId;
            }
        }

        protected override void ReleaseLockCore()
        {
            Debug.Assert(IsLockOwnedByCurrentThread);
            Debug.Assert(_lockCount == 0);
            NamedMutexSharedDataNoPThread* sharedData = (NamedMutexSharedDataNoPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);
            sharedData->LockOwnerProcessId = 0;
            sharedData->LockOwnerThreadId = 0;

            SharedMemoryHelpers.ReleaseFileLock(_sharedLockFileHandle);
            _processLockHandle.Release();
        }

        protected override bool IsAbandoned
        {
            get => ((NamedMutexSharedDataNoPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader))->IsAbandoned;
            set => ((NamedMutexSharedDataNoPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader))->IsAbandoned = value ? 1 : 0;
        }

        public override void Close(System.Boolean isAbruptShutdown, System.Boolean releaseSharedData)
        {
            base.Close(isAbruptShutdown, releaseSharedData);

            if (!releaseSharedData)
            {
                return;
            }

            string sessionDirectory = Path.Combine(
                SharedMemoryManager.SharedFilesPath,
                _processDataHeader._id.GetRuntimeTempDirectoryName(),
                SharedMemoryManager.SharedMemoryLockFilesDirectoryName,
                _processDataHeader._id.GetSessionDirectoryName()
            );

            try
            {
                // Delete the lock file.
                File.Delete(Path.Combine(sessionDirectory, _processDataHeader._id.GetName()));
                // Delete the session directory if it's empty.
                Directory.Delete(sessionDirectory);
            }
            catch (Exception)
            {
                // Ignore the error, just don't release the shared data.
            }
        }

        protected override void ReleaseSharedData()
        {
            _sharedLockFileHandle.Dispose();
            _sharedLockFileHandle = null;
        }

        protected override MutexTryAcquireLockResult AcquireLockCore(int timeoutMilliseconds)
        {
            int startTime = 0;
            if (timeoutMilliseconds > 0)
            {
                startTime = Environment.TickCount;
            }

            using var lockScope = _processLockHandle.Acquire();

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

            NamedMutexSharedDataNoPThread* sharedData = (NamedMutexSharedDataNoPThread*)SharedMemoryProcessDataHeader.GetDataPointer(_processDataHeader._sharedDataHeader);

            switch (timeoutMilliseconds)
            {
                case -1:
                    bool acquiredLock = false;
                    while (sharedData->TimedWaiterCount > 0)
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

                        sharedData->TimedWaiterCount++;

                        do
                        {
                            int elapsedMilliseconds = Environment.TickCount - startTime;
                            if (elapsedMilliseconds >= timeoutMilliseconds)
                            {
                                sharedData->TimedWaiterCount--;
                                return MutexTryAcquireLockResult.TimedOut;
                            }

                            int remainingTimeoutMilliseconds = timeoutMilliseconds - elapsedMilliseconds;
                            int sleepMilliseconds = Math.Min(PollLoopMaximumSleepMilliseconds, remainingTimeoutMilliseconds);
                            Thread.Sleep(sleepMilliseconds);
                        } while (!SharedMemoryHelpers.TryAcquireFileLock(_sharedLockFileHandle, nonBlocking: true));
                        sharedData->TimedWaiterCount--;
                    }
            }

            // Detect abandoned lock that isn't marked as abandoned.
            if (IsLockOwnedByAnyThread)
                return MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned;

            return MutexTryAcquireLockResult.AcquiredLock;
        }
    }

    enum MutexTryAcquireResult : byte
    {
        AcquiredLock,
        AcquiredLockButMutexWasAbandoned,
        TimedOut,
        AcquiredLockRecursively,
    }

    class SharedMemoryManager
    {
        [FeatureSwitchDefinition("System.Threading.NamedMutexUsePThreadMutex", "Use shared pthread mutexes for named mutexes")]
        internal static bool UsePThreadMutexes { get; } = AppContext.TryGetSwitch("System.Threading.NamedMutexUsePThreadMutex", out bool value)
            ? value
            : false;

        internal const string SharedMemoryLockFilesDirectoryName = "lockfiles";

        private Lock _creationDeletionProcessLock = new Lock();
        private SafeFileHandle _creationDeletionLockFileHandle;
        private Dictionary<uint, SafeFileHandle> _uidToFileHandleMap = [];

        public Lock.Scope AcquireCreationDeletionLock()
        {
            return _creationDeletionProcessLock.Acquire();
        }

        public IDisposable AcquireCreationDeletionLockFileHandle(SharedMemoryId id)
        {
            throw new NotImplementedException();
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

        private Dictionary<SharedMemoryId, SharedMemoryProcessDataHeader> _processDataHeaders = new Dictionary<SharedMemoryId, SharedMemoryProcessDataHeader>();

        public void AddProcessDataHeader(SharedMemoryProcessDataHeader processDataHeader)
        {
            _processDataHeaders[processDataHeader._id] = processDataHeader;
        }

        public void RemoveProcessDataHeader(SharedMemoryProcessDataHeader processDataHeader)
        {
            _processDataHeaders.Remove(processDataHeader._id);
        }

        public SharedMemoryProcessDataHeader? FindProcessDataHeader(SharedMemoryId id)
        {
            _processDataHeaders.TryGetValue(id, out SharedMemoryProcessDataHeader? header);
            return header;
        }
    }
}