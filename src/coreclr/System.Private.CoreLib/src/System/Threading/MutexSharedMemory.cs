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
    internal abstract class NamedMutexProcessDataBase(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> header)
    {
        private const byte SyncSystemVersion = 1;
        protected const int PollLoopMaximumSleepMilliseconds = 100;

        [FeatureSwitchDefinition("System.Threading.Mutex.NamedMutexUsePThreadMutex")]
        private static bool UsePThreadMutexes { get; } = AppContext.TryGetSwitch("System.Threading.Mutex.NamedMutexUsePThreadMutex", out bool value)
            ? value
            : false;

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
            using Lock.Scope creationDeletionProcessLock = SharedMemoryManager<NamedMutexProcessDataBase>.Instance.AcquireCreationDeletionProcessLock();

            SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>? processDataHeader = SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.CreateOrOpen(
                name,
                isUserScope,
                new SharedMemorySharedDataHeader(SharedMemoryType.Mutex, SyncSystemVersion),
                UsePThreadMutexes ? NamedMutexProcessDataWithPThreads.SharedData.Size : (nuint)sizeof(NamedMutexProcessDataNoPThreads.SharedData),
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
                    if (UsePThreadMutexes)
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
            if (UsePThreadMutexes)
            {
                NamedMutexProcessDataWithPThreads.SharedData* sharedData = (NamedMutexProcessDataWithPThreads.SharedData*)v;
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
                NamedMutexProcessDataNoPThreads.SharedData* sharedData = (NamedMutexProcessDataNoPThreads.SharedData*)v;
                sharedData->LockOwnerProcessId = SharedMemoryHelpers.InvalidProcessId;
                sharedData->LockOwnerThreadId = SharedMemoryHelpers.InvalidThreadId;
                sharedData->IsAbandoned = false;
                sharedData->TimedWaiterCount = 0;
            }
        }
    }

    [UnsupportedOSPlatform("windows")]
    internal sealed unsafe class NamedMutexProcessDataWithPThreads(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader) : NamedMutexProcessDataBase(processDataHeader)
    {
        private readonly SharedData* _sharedData = (SharedData*)SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.GetDataPointer(processDataHeader);

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

        [UnsupportedOSPlatform("windows")]
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe ref struct SharedData
        {
            public static nuint Size => (nuint)PThreadMutexSize + IsAbandonedOffset + sizeof(byte);
            private static readonly int PThreadMutexSize = Interop.Sys.PThreadMutex_Size();

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
    }

    [UnsupportedOSPlatform("windows")]
    internal sealed unsafe class NamedMutexProcessDataNoPThreads : NamedMutexProcessDataBase
    {
        private const string SharedMemoryLockFilesDirectoryName = "lockfiles";
        private readonly Lock _processLevelLock = new();
        private readonly SafeFileHandle _sharedLockFileHandle;

        private readonly SharedData* _sharedData;

        public NamedMutexProcessDataNoPThreads(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader, bool created) : base(processDataHeader)
        {
            _sharedData = (SharedData*)SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.GetDataPointer(processDataHeader);
            string lockFileDirectory = Path.Combine(
                SharedMemoryHelpers.SharedFilesPath,
                Id.GetRuntimeTempDirectoryName(),
                SharedMemoryLockFilesDirectoryName
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
                SharedMemoryHelpers.SharedFilesPath,
                Id.GetRuntimeTempDirectoryName(),
                SharedMemoryLockFilesDirectoryName,
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
        [UnsupportedOSPlatform("windows")]
        [StructLayout(LayoutKind.Sequential)]
        internal ref struct SharedData
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
    }

    internal enum MutexTryAcquireLockResult : byte
    {
        AcquiredLock,
        AcquiredLockButMutexWasAbandoned,
        TimedOut,
        AcquiredLockRecursively,
    }
}
