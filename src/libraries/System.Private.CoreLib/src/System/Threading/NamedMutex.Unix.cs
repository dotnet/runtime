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
    internal abstract class NamedMutexProcessDataBase(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> header) : ISharedMemoryProcessData
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
        public bool IsLockOwnedByAnyThreadInThisProcess => _lockOwnerThread is not null;

        protected abstract void SetLockOwnerToCurrentThread();

        public MutexTryAcquireLockResult TryAcquireLock(WaitSubsystem.ThreadWaitInfo waitInfo, int timeoutMilliseconds)
        {
            MutexTryAcquireLockResult result = AcquireLockCore(timeoutMilliseconds);

            if (result == MutexTryAcquireLockResult.AcquiredLockRecursively)
            {
                return MutexTryAcquireLockResult.AcquiredLock;
            }

            SetLockOwnerToCurrentThread();
            _lockCount = 1;
            _lockOwnerThread = waitInfo.Thread;
            AddOwnedNamedMutex(waitInfo.Thread, this);

            if (IsAbandoned)
            {
                IsAbandoned = false;
                result = MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned;
            }

            return result;
        }

        private static void AddOwnedNamedMutex(Thread thread, NamedMutexProcessDataBase namedMutexProcessDataBase)
        {
            namedMutexProcessDataBase._nextInThreadOwnedNamedMutexList = thread.WaitInfo.LockedNamedMutexesHead;
            thread.WaitInfo.LockedNamedMutexesHead = namedMutexProcessDataBase;
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
            if (currentThread.WaitInfo.LockedNamedMutexesHead == namedMutexProcessDataBase)
            {
                currentThread.WaitInfo.LockedNamedMutexesHead = namedMutexProcessDataBase._nextInThreadOwnedNamedMutexList;
            }
            else
            {
                NamedMutexProcessDataBase? previous = currentThread.WaitInfo.LockedNamedMutexesHead;
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

        internal static unsafe SharedMemoryProcessDataHeader<NamedMutexProcessDataBase>? CreateOrOpen(string name, bool isUserScope, bool createIfNotExist, bool acquireLockIfCreated, out bool created)
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
                        MutexTryAcquireLockResult acquireResult = processDataHeader._processData.TryAcquireLock(Thread.CurrentThread.WaitInfo, timeoutMilliseconds: 0);
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

        public virtual void Close(bool releaseSharedData)
        {
            if (IsLockOwnedByCurrentThread)
            {
                RemoveOwnedNamedMutex(Thread.CurrentThreadAssumedInitialized, this);
            }
        }
    }

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

        public override void Close(bool releaseSharedData)
        {
            base.Close(releaseSharedData);

            if (releaseSharedData)
            {
                // Release the pthread mutex.
                Interop.Sys.PThreadMutex_Destroy(_sharedData->LockAddress);
            }
        }

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

        public override void Close(bool releaseSharedData)
        {
            base.Close(releaseSharedData);

            _sharedLockFileHandle.Dispose();
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
        private sealed class NamedMutex(SharedMemoryProcessDataHeader<NamedMutexProcessDataBase> processDataHeader) : IWaitableObject
        {
            /// <summary>
            /// Dictionary to look up named mutexes.
            /// </summary>
            private static Dictionary<string, NamedMutex>? s_namedObjects;
            private int _referenceCount = 1;

            public void IncrementRefCount()
            {
                Debug.Assert(_referenceCount > 0, "Ref count should not be negative.");
                _referenceCount++;
            }

            public int Wait_Locked(ThreadWaitInfo waitInfo, int timeoutMilliseconds, bool interruptible, bool prioritize, ref LockHolder lockHolder)
            {
                // We're done touching the wait subsystem data structures, so we can release the lock on them.
                lockHolder.Dispose();
                MutexTryAcquireLockResult result = processDataHeader._processData!.TryAcquireLock(waitInfo, timeoutMilliseconds);
                return result switch
                {
                    MutexTryAcquireLockResult.AcquiredLock => WaitHandle.WaitSuccess,
                    MutexTryAcquireLockResult.AcquiredLockButMutexWasAbandoned => WaitHandle.WaitAbandoned,
                    MutexTryAcquireLockResult.TimedOut => WaitHandle.WaitTimeout,
                    _ => throw new InvalidOperationException("Unexpected result from TryAcquireLock")
                };
            }

            public void Signal(int count, ref LockHolder lockHolder)
            {
                lockHolder.Dispose();
                processDataHeader._processData!.ReleaseLock();
            }

            public void OnDeleteHandle()
            {
                s_lock.Acquire();
                try
                {
                    // Multiple handles may refer to the same named object.  Make sure the object
                    // is only abandoned once the last handle to it is deleted.  Also, remove the
                    // object from the named objects dictionary at this point.
                    _referenceCount--;
                    if (_referenceCount > 0)
                    {
                        return;
                    }

                    processDataHeader.Close();
                }
                finally
                {
                    s_lock.Release();
                }
            }

            public static NamedMutex? CreateNamedMutex_Locked(string name, bool isUserScope, bool initiallyOwned, out bool createdNew)
            {
                s_lock.VerifyIsLocked();

                s_namedObjects ??= [];

                if (s_namedObjects.TryGetValue(name, out NamedMutex? result))
                {
                    createdNew = false;
                    result._referenceCount++;
                }
                else
                {
                    var namedMutexProcessData = NamedMutexProcessDataBase.CreateOrOpen(name, isUserScope, createIfNotExist: true, acquireLockIfCreated: initiallyOwned, out createdNew);
                    if (namedMutexProcessData is null)
                    {
                        createdNew = false;
                        return null;
                    }
                    result = new NamedMutex(namedMutexProcessData);
                    s_namedObjects.Add(name, result);
                    return result;
                }

                return result;
            }

            public static OpenExistingResult OpenNamedMutex(string name, bool isUserScope, out NamedMutex? result)
            {
                s_lock.VerifyIsLocked();

                if (s_namedObjects is null || !s_namedObjects.TryGetValue(name, out NamedMutex? namedMutex))
                {
                    // We haven't seen this named mutex before in this process.
                    // Try seeing if it exists on the system.
                    var namedMutexProcessData = NamedMutexProcessDataBase.CreateOrOpen(name, isUserScope, createIfNotExist: false, acquireLockIfCreated: false, out _);
                    if (namedMutexProcessData is null)
                    {
                        // This mutex does not exist.
                        result = null;
                        return OpenExistingResult.NameNotFound;
                    }

                    // We found the mutex on the system, so we can open it.
                    s_namedObjects ??= [];
                    namedMutex = new NamedMutex(namedMutexProcessData);
                    s_namedObjects.Add(name, namedMutex);
                }

                namedMutex.IncrementRefCount();
                result = namedMutex;
                return OpenExistingResult.Success;
            }
        }
    }
}
