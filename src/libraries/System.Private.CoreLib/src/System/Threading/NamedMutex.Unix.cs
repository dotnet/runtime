// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    internal sealed class NamedMutexOwnershipChain(Thread thread)
    {
        private readonly Thread _thread = thread;
        private NamedMutexProcessDataBase? _head;

        public void Add(NamedMutexProcessDataBase namedMutex)
        {
            SharedMemoryManager<NamedMutexProcessDataBase>.Instance.VerifyCreationDeletionProcessLockIsLocked();
            namedMutex.NextOwnedNamedMutex = _head;
            _head = namedMutex;
        }

        public void Remove(NamedMutexProcessDataBase namedMutex)
        {
            SharedMemoryManager<NamedMutexProcessDataBase>.Instance.VerifyCreationDeletionProcessLockIsLocked();
            if (_head == namedMutex)
            {
                _head = namedMutex.NextOwnedNamedMutex;
            }
            else
            {
                NamedMutexProcessDataBase? previous = _head;
                while (previous?.NextOwnedNamedMutex != namedMutex)
                {
                    previous = previous?.NextOwnedNamedMutex;
                }

                if (previous is not null)
                {
                    previous.NextOwnedNamedMutex = namedMutex.NextOwnedNamedMutex;
                }
            }

            namedMutex.NextOwnedNamedMutex = null;
        }

        public void Abandon()
        {
            WaitSubsystem.LockHolder scope = SharedMemoryManager<NamedMutexProcessDataBase>.Instance.AcquireCreationDeletionProcessLock();
            try
            {
                while (true)
                {
                    NamedMutexProcessDataBase? namedMutex = _head;
                    if (namedMutex == null)
                    {
                        break;
                    }

                    namedMutex.Abandon(this, _thread);
                    Debug.Assert(_head != namedMutex);
                }
            }
            finally
            {
                scope.Dispose();
            }
        }
    }

    internal abstract class NamedMutexProcessDataBase(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> header) : ISharedMemoryProcessData
    {
        private const byte SyncSystemVersion = 1;
        protected const int PollLoopMaximumSleepMilliseconds = 100;
        protected const uint InvalidProcessId = unchecked((uint)-1);
        protected const uint InvalidThreadId = unchecked((uint)-1);

        // Use PThread mutex-backed named mutexes if possible.
        // macOS has support for the features we need in the pthread mutexes on arm64
        // but not in the Rosetta 2 x64 emulation layer.
        // Until Rosetta 2 is removed, we need to use the non-PThread mutexes on Apple platforms.
        // On FreeBSD, pthread process-shared robust mutexes cannot be placed in shared memory mapped
        // independently by the processes involved. See https://github.com/dotnet/runtime/issues/10519.
        private static bool UsePThreadMutexes => !OperatingSystem.IsApplePlatform() && !OperatingSystem.IsFreeBSD();

        private readonly SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> _processDataHeader = header;
        protected nuint _lockCount;
        private Thread? _lockOwnerThread;

        public SharedMemoryId Id => _processDataHeader._id;

        public NamedMutexProcessDataBase? NextOwnedNamedMutex { get; set; }

        public bool IsLockOwnedByCurrentThread => IsLockOwnedByThreadInThisProcess(Thread.CurrentThread);
        protected abstract bool IsLockOwnedByThreadInThisProcess(Thread thread);
        public bool IsLockOwnedByAnyThreadInThisProcess => _lockOwnerThread is not null;

        protected abstract void SetLockOwnerToCurrentThread();

        public MutexTryAcquireLockResult TryAcquireLock(WaitSubsystem.ThreadWaitInfo waitInfo, int timeoutMilliseconds, ref WaitSubsystem.LockHolder holder)
        {
            SharedMemoryManager<NamedMutexProcessDataBase>.Instance.VerifyCreationDeletionProcessLockIsLocked();
            holder.Dispose();
            MutexTryAcquireLockResult result = AcquireLockCore(timeoutMilliseconds);

            if (result == MutexTryAcquireLockResult.AcquiredLockRecursively)
            {
                return MutexTryAcquireLockResult.AcquiredLock;
            }

            if (result == MutexTryAcquireLockResult.TimedOut)
            {
                // If the lock was not acquired, we don't have any more work to do.
                return result;
            }

            holder = SharedMemoryManager<NamedMutexProcessDataBase>.Instance.AcquireCreationDeletionProcessLock();
            SetLockOwnerToCurrentThread();
            _lockCount = 1;
            _lockOwnerThread = waitInfo.Thread;
            // Add the ref count for the thread's wait info.
            _processDataHeader.IncrementRefCount();
            waitInfo.NamedMutexOwnershipChain.Add(this);

            if (IsAbandoned)
            {
                IsAbandoned = false;
                result = MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned;
            }

            return result;
        }

        public void ReleaseLock()
        {
            if (!IsLockOwnedByCurrentThread)
            {
                throw new InvalidOperationException(SR.Arg_InvalidOperationException_CannotReleaseUnownedMutex);
            }

            --_lockCount;
            if (_lockCount != 0)
            {
                return;
            }

            WaitSubsystem.LockHolder scope = SharedMemoryManager<NamedMutexProcessDataBase>.Instance.AcquireCreationDeletionProcessLock();
            try
            {
                Thread.CurrentThread.WaitInfo.NamedMutexOwnershipChain.Remove(this);
                _lockOwnerThread = null;
                ReleaseLockCore(abandoning: false);
                // Remove the refcount from the thread's wait info.
                _processDataHeader.DecrementRefCount();
            }
            finally
            {
                scope.Dispose();
            }
        }

        public void Abandon(NamedMutexOwnershipChain chain, Thread abandonedThread)
        {
            SharedMemoryManager<NamedMutexProcessDataBase>.Instance.VerifyCreationDeletionProcessLockIsLocked();

            // This method can be called from one of two threads:
            // 1. The dead thread that owns the mutex.
            //   In the first case, we know that no one else can acquire the lock, so we can safely
            //   set the lock count to 0 and abandon the mutex.
            // 2. The finalizer thread after the owning thread has died.
            //   In this case, its possible for the mutex to have been acquired by another thread
            //   after the owning thread died if it is backed by a pthread mutex.
            //   A pthread mutex doesn't need our Abandon() call to be abandoned, it will be automatically abandoned
            //   when the owning thread dies.
            //   In this case, we don't want to do anything. Our named mutex implementation will handle the abandonment
            //   as part of acquisition.
            Debug.Assert(IsLockOwnedByCurrentThread || (!abandonedThread.IsAlive && Thread.CurrentThreadIsFinalizerThread()),
                "Abandon can only be called from the thread that owns the lock or from the finalizer thread when the owning thread is dead.");

            if (!IsLockOwnedByThreadInThisProcess(abandonedThread))
            {
                Debug.Assert(Thread.CurrentThreadIsFinalizerThread());
                // Lock is owned by a different thread already, so we don't need to do anything.
                // Just remove it from the list of owned named mutexes.
                chain.Remove(this);
                return;
            }

            IsAbandoned = true;

            _lockCount = 0;

            Debug.Assert(_lockOwnerThread is not null);

            ReleaseLockCore(abandoning: true);

            chain.Remove(this);
            _lockOwnerThread = null;
            // Remove the refcount from the thread's wait info.
            _processDataHeader.DecrementRefCount();
        }

        protected abstract MutexTryAcquireLockResult AcquireLockCore(int timeoutMilliseconds);

        protected abstract bool IsAbandoned { get; set; }

        protected abstract void ReleaseLockCore(bool abandoning);

        internal static unsafe SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>? CreateOrOpen(string name, bool isUserScope, bool createIfNotExist, bool acquireLockIfCreated, out bool created)
        {
            WaitSubsystem.LockHolder creationDeletionProcessLock = SharedMemoryManager<NamedMutexProcessDataBase>.Instance.AcquireCreationDeletionProcessLock();
            try
            {

                SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>? processDataHeader = SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.CreateOrOpen(
                    name,
                    isUserScope,
                    new SharedMemorySharedDataHeader(SharedMemoryType.Mutex, SyncSystemVersion),
                    UsePThreadMutexes ? NamedMutexProcessDataWithPThreads.SharedDataSize : (nuint)sizeof(NamedMutexProcessDataNoPThreads.SharedData),
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
                            MutexTryAcquireLockResult acquireResult = processDataHeader._processData.TryAcquireLock(Thread.CurrentThread.WaitInfo, timeoutMilliseconds: 0, ref creationDeletionProcessLock);
                            Debug.Assert(acquireResult == MutexTryAcquireLockResult.AcquiredLock);
                        }
                    }

                    return processDataHeader;
                }
            }
            finally
            {
                creationDeletionProcessLock.Dispose();
            }
        }

        private static unsafe void InitializeSharedData(void* v)
        {
            if (UsePThreadMutexes)
            {
                if (Interop.Sys.LowLevelCrossProcessMutex_Init(v) != 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    throw Interop.GetExceptionForIoErrno(errorInfo, SR.Arg_FailedToInitializePThreadMutex);
                }
            }
            else
            {
                NamedMutexProcessDataNoPThreads.SharedData* sharedData = (NamedMutexProcessDataNoPThreads.SharedData*)v;
                sharedData->LockOwnerProcessId = InvalidProcessId;
                sharedData->LockOwnerThreadId = InvalidThreadId;
                sharedData->IsAbandoned = false;
                sharedData->TimedWaiterCount = 0;
            }
        }

        public virtual void Close(bool releaseSharedData)
        {
            if (IsLockOwnedByCurrentThread)
            {
                Thread.CurrentThread.WaitInfo.NamedMutexOwnershipChain.Remove(this);
            }
        }
    }

    internal sealed unsafe class NamedMutexProcessDataWithPThreads(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader) : NamedMutexProcessDataBase(processDataHeader)
    {
        public static nuint SharedDataSize { get; } = (nuint)Interop.Sys.LowLevelCrossProcessMutex_Size();
        private readonly void* _sharedData = SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>.GetDataPointer(processDataHeader);

        protected override bool IsLockOwnedByThreadInThisProcess(Thread thread)
        {
            Interop.Sys.LowLevelCrossProcessMutex_GetOwnerProcessAndThreadId(_sharedData, out uint ownerProcessId, out uint ownerThreadId);
            return ownerProcessId == (uint)Environment.ProcessId &&
                   ownerThreadId == (uint)thread.ManagedThreadId;
        }

        protected override void SetLockOwnerToCurrentThread()
        {
            Interop.Sys.LowLevelCrossProcessMutex_SetOwnerProcessAndThreadId(
                _sharedData,
                (uint)Environment.ProcessId,
                (uint)Thread.CurrentThread.ManagedThreadId);
        }

        protected override bool IsAbandoned
        {
            get => Interop.Sys.LowLevelCrossProcessMutex_IsAbandoned(_sharedData);
            set => Interop.Sys.LowLevelCrossProcessMutex_SetAbandoned(_sharedData, value);
        }

        protected override MutexTryAcquireLockResult AcquireLockCore(int timeoutMilliseconds)
        {
            Interop.Error lockResult = (Interop.Error)Interop.Sys.LowLevelCrossProcessMutex_Acquire(_sharedData, timeoutMilliseconds);

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

            if (result == MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned)
            {
                // If the underlying pthread mutex was abandoned, treat this as an abandoned mutex.
                // Its possible that the underlying pthread mutex was abandoned, but the shared data and process data
                // is out of sync. This can happen if the dead thread did not call Abandon() before it exited
                // and is relying on the finalizer to clean it up.
                return result;
            }

            if (_lockCount != 0)
            {
                Debug.Assert(IsLockOwnedByCurrentThread);
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
                    Interop.Sys.LowLevelCrossProcessMutex_Release(_sharedData);
                }
            }

            return result;
        }

        protected override void ReleaseLockCore(bool abandoning)
        {
            Debug.Assert(_lockCount == 0);
            Interop.Sys.LowLevelCrossProcessMutex_SetOwnerProcessAndThreadId(
                _sharedData,
                InvalidProcessId,
                InvalidThreadId);

            Interop.Sys.LowLevelCrossProcessMutex_Release(_sharedData);
        }

        public override void Close(bool releaseSharedData)
        {
            base.Close(releaseSharedData);

            if (releaseSharedData)
            {
                // Release the pthread mutex.
                Interop.Sys.LowLevelCrossProcessMutex_Destroy(_sharedData);
            }
        }
    }

    internal sealed unsafe class NamedMutexProcessDataNoPThreads : NamedMutexProcessDataBase
    {
        private const string SharedMemoryLockFilesDirectoryName = "lockfiles";
        private readonly Mutex _processLevelMutex = new Mutex(initiallyOwned: false);
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

        protected override bool IsLockOwnedByThreadInThisProcess(Thread thread)
        {
            return _sharedData->LockOwnerProcessId == (uint)Environment.ProcessId &&
                   _sharedData->LockOwnerThreadId == (uint)thread.ManagedThreadId;
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
                return _sharedData->LockOwnerProcessId != InvalidProcessId &&
                       _sharedData->LockOwnerThreadId != InvalidThreadId;
            }
        }

        protected override void ReleaseLockCore(bool abandoning)
        {
            Debug.Assert(_lockCount == 0);
            _sharedData->LockOwnerProcessId = InvalidProcessId;
            _sharedData->LockOwnerThreadId = InvalidThreadId;

            Interop.Sys.FLock(_sharedLockFileHandle, Interop.Sys.LockOperations.LOCK_UN);
            if (!abandoning)
            {
                // We're going to abandon this mutex, so don't release it.
                // We might not be on the owning thread, so we can't be sure we can release it.
                _processLevelMutex.ReleaseMutex();
            }
        }

        protected override bool IsAbandoned
        {
            get => _sharedData->IsAbandoned;
            set => _sharedData->IsAbandoned = value;
        }

        public override void Close(bool releaseSharedData)
        {
            base.Close(releaseSharedData);

            _sharedLockFileHandle.Dispose();
            _processLevelMutex.Dispose();
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
            bool releaseProcessLock = true;
            try
            {
                if (!_processLevelMutex.WaitOne(timeoutMilliseconds))
                {
                    return MutexTryAcquireLockResult.TimedOut;
                }
            }
            catch (AbandonedMutexException)
            {
                // If the process-level lock was abandoned, the shared mutex will also have been abandoned.
                // We don't need to do anything here. We'll acquire the shared lock below and throw for abandonment as expected.
            }

            try
            {
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
                releaseProcessLock = false;

                // Detect abandoned lock that isn't marked as abandoned.
                if (IsLockOwnedByAnyThread)
                    return MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned;

                return MutexTryAcquireLockResult.AcquiredLock;
            }
            finally
            {
                if (releaseProcessLock)
                {
                    _processLevelMutex.ReleaseMutex();
                }
            }
        }

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

    internal static partial class WaitSubsystem
    {
        private sealed class NamedMutex : IWaitableObject
        {
            private readonly SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> _processDataHeader;

            public NamedMutex(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader)
            {
                _processDataHeader = processDataHeader;
            }

            public int Wait_Locked(ThreadWaitInfo waitInfo, int timeoutMilliseconds, bool interruptible, bool prioritize, ref LockHolder lockHolder)
            {
                LockHolder scope = SharedMemoryManager<NamedMutexProcessDataBase>.Instance.AcquireCreationDeletionProcessLock();
                try
                {
                    lockHolder.Dispose();
                    MutexTryAcquireLockResult result = _processDataHeader._processData!.TryAcquireLock(waitInfo, timeoutMilliseconds, ref scope);
                    return result switch
                    {
                        MutexTryAcquireLockResult.AcquiredLock => WaitHandle.WaitSuccess,
                        MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned => WaitHandle.WaitAbandoned,
                        MutexTryAcquireLockResult.TimedOut => WaitHandle.WaitTimeout,
                        _ => throw new UnreachableException()
                    };
                }
                finally
                {
                    scope.Dispose();
                }
            }

            public void Signal(int count, ref LockHolder lockHolder)
            {
                lockHolder.Dispose();
                _processDataHeader._processData!.ReleaseLock();
            }

            public void OnDeleteHandle()
            {
                LockHolder scope = SharedMemoryManager<NamedMutexProcessDataBase>.Instance.AcquireCreationDeletionProcessLock();
                try
                {
                    _processDataHeader.DecrementRefCount();
                }
                finally
                {
                    scope.Dispose();
                }
            }

            public static NamedMutex? CreateNamedMutex(string name, bool isUserScope, bool initiallyOwned, out bool createdNew)
            {
                var namedMutexProcessData = NamedMutexProcessDataBase.CreateOrOpen(name, isUserScope, createIfNotExist: true, acquireLockIfCreated: initiallyOwned, out createdNew);
                if (namedMutexProcessData is null)
                {
                    createdNew = false;
                    return null;
                }

                Debug.Assert(namedMutexProcessData._processData is not null);
                return new NamedMutex(namedMutexProcessData);
            }

            public static OpenExistingResult OpenNamedMutex(string name, bool isUserScope, out NamedMutex? result)
            {
                var namedMutexProcessData = NamedMutexProcessDataBase.CreateOrOpen(name, isUserScope, createIfNotExist: false, acquireLockIfCreated: false, out _);
                if (namedMutexProcessData is null)
                {
                    result = null;
                    return OpenExistingResult.NameNotFound;
                }

                Debug.Assert(namedMutexProcessData._processData is not null);
                result = new NamedMutex(namedMutexProcessData);
                return OpenExistingResult.Success;
            }
        }
    }
}
