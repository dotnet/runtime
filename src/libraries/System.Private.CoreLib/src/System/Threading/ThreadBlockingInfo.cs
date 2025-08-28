// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    // Tracks some kinds of blocking on the thread, like waiting on locks where it may be useful to know when debugging which
    // thread owns the lock.
    //
    // Notes:
    // - The type, some fields, and some other members may be used by debuggers (noted specifically below), so take care when
    //   renaming them
    //
    // Debuggers may use this info by evaluating expressions to enumerate the blocking infos for a thread. For example:
    // - Evaluate "System.Threading.ThreadBlockingInfo.t_first" to obtain the first pointer to a blocking info for the current
    //   thread
    // - While there is a non-null pointer to a blocking info:
    //   - Evaluate "(*(System.Threading.ThreadBlockingInfo*)ptr).fieldOrProperty", where "ptr" is the blocking info pointer
    //     value, to get the field and relevant property getter values below
    //   - Use the _objectKind field value to determine what kind of blocking is occurring
    //   - Get the LockOwnerManagedThreadId property getter value. If the blocking is waiting for a
    //     lock and the lock is currently owned by a thread, this property will return a nonzero value that can be
    //     used to identify the lock owner thread.
    //   - Use the _next field value to obtain the next pointer to a blocking info for the thread
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ThreadBlockingInfo
    {
        // Points to the first (most recent) blocking info for the thread. The _next field points to the next-most-recent
        // blocking info for the thread, or null if there are no more. Blocking can be reentrant in some cases, such as on UI
        // threads where reentrant waits are used, or if a SynchronizationContext wait override is set.
        [ThreadStatic]
        private static ThreadBlockingInfo* t_first; // may be used by debuggers

        // This pointer can be used to obtain the object relevant to the blocking.
        // It points to a stack location containing the managed object reference.
        private void* _objectPtr; // may be used by debuggers

        // Indicates the type of object relevant to the blocking
        private ObjectKind _objectKind; // may be used by debuggers

        // The timeout in milliseconds for the wait, -1 for infinite timeout
        private int _timeoutMs; // may be used by debuggers

        // Points to the next-most-recent blocking info for the thread
        private ThreadBlockingInfo* _next; // may be used by debuggers

        private void Push(void* objectPtr, ObjectKind objectKind, int timeoutMs)
        {
            Debug.Assert(objectPtr != null);

            _objectPtr = objectPtr;
            _objectKind = objectKind;
            _timeoutMs = timeoutMs;
            _next = t_first;
            t_first = (ThreadBlockingInfo*)Unsafe.AsPointer(ref this);
        }

        private void Pop()
        {
            Debug.Assert(_objectPtr != null);
            Debug.Assert(t_first != null);
            Debug.Assert(t_first->_next == _next);

            t_first = _next;
            _objectPtr = null;
        }

        // If the blocking is associated with a lock of some kind that has thread affinity and tracks the owner's managed thread
        // ID, returns the managed thread ID of the thread that currently owns the lock. Otherwise, returns 0. A return value of
        // 0 may indicate that the associated lock is currently not owned by a thread, or that the information could not be
        // determined.
        //
        // Calls to native helpers are avoided in the property getter such that it can be more easily evaluated by a debugger.
        public int LockOwnerManagedThreadId // the getter may be used by debuggers
        {
            get
            {
                Debug.Assert(_objectPtr != null);

                switch (_objectKind)
                {
                    case ObjectKind.Lock:
                        return ((Lock)Unsafe.AsRef<object>(_objectPtr)).OwningManagedThreadId;

#if !MONO
                    case ObjectKind.Condition:
                        Debug.Assert(_objectKind == ObjectKind.Condition);
                        return ((Condition)Unsafe.AsRef<object>(_objectPtr)).AssociatedLock.OwningManagedThreadId;
#endif

                    default:
                        throw new UnreachableException();
                }
            }
        }

        public ref struct Scope
        {
            private object? _object;
            private ThreadBlockingInfo _blockingInfo;

#pragma warning disable CS9216 // casting Lock to object
            public Scope(Lock lockObj, int timeoutMs) : this(lockObj, ObjectKind.Lock, timeoutMs) { }
#pragma warning restore CS9216

#if !MONO
            public Scope(Condition condition, int timeoutMs) : this(condition, ObjectKind.Condition, timeoutMs) { }
#endif

            private Scope(object obj, ObjectKind objectKind, int timeoutMs)
            {
                _object = obj;
                _blockingInfo.Push(Unsafe.AsPointer(ref _object), objectKind, timeoutMs);
            }

            public void Dispose()
            {
                if (_object is not null)
                {
                    _blockingInfo.Pop();
                    _object = null;
                }
            }
        }

        public enum ObjectKind // may be used by debuggers
        {
            [Obsolete("Represents native Monitor locks, which have been removed", error: true)]
            MonitorLock, // maps to DebugBlockingItemType::Legacy_DebugBlock_MonitorCriticalSection in coreclr
            [Obsolete("Represents native Monitor waits, which have been removed", error: true)]
            MonitorWait, // maps to DebugBlockingItemType::Legacy_DebugBlock_MonitorEvent in coreclr
            Lock,
            Condition
        }
    }
}
