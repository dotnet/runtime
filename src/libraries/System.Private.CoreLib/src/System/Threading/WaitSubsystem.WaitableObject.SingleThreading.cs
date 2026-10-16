// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading;

internal static partial class WaitSubsystem
{
    private static Dictionary<string, NamedMutex>? s_namedMutexes;

    private enum WaitableObjectType : byte
    {
        ManualResetEvent,
        AutoResetEvent,
        Semaphore,
        Mutex
    }

    private class WaitableObject
    {
        private readonly WaitableObjectType _type;
        // For mutexes, 1 is unowned; 0 is owned once; negative values count recursive reacquisitions.
        private int _signalCount;
        private readonly int _maximumSignalCount;

        public WaitableObject(WaitableObjectType type, int initialSignalCount, int maximumSignalCount = 1)
        {
            _type = type;
            _signalCount = initialSignalCount;
            _maximumSignalCount = maximumSignalCount;
        }

        public bool CanAcquire
        {
            get
            {
                if (_type == WaitableObjectType.Mutex)
                {
                    if (_signalCount == -int.MaxValue)
                    {
                        throw new OverflowException(SR.Overflow_MutexReacquireCount);
                    }
                    return true;
                }

                return _signalCount != 0;
            }
        }

        public void AcceptSignal()
        {
            Debug.Assert(CanAcquire);
            if (_type != WaitableObjectType.ManualResetEvent)
            {
                _signalCount--;
            }
        }

        public void SetEvent(bool signaled)
        {
            if (_type is not (WaitableObjectType.ManualResetEvent or WaitableObjectType.AutoResetEvent))
            {
                WaitHandle.ThrowInvalidHandleException();
            }

            _signalCount = signaled ? 1 : 0;
        }

        public int ReleaseSemaphore(int count)
        {
            Debug.Assert(count > 0);
            if (_type != WaitableObjectType.Semaphore)
            {
                WaitHandle.ThrowInvalidHandleException();
            }

            int previousCount = _signalCount;
            if (count > _maximumSignalCount - previousCount)
            {
                throw new SemaphoreFullException();
            }

            _signalCount = previousCount + count;
            return previousCount;
        }

        public void Signal()
        {
            switch (_type)
            {
                case WaitableObjectType.Mutex:
                    if (_signalCount > 0)
                    {
                        throw new ApplicationException(SR.Arg_SynchronizationLockException);
                    }
                    _signalCount++;
                    break;

                case WaitableObjectType.Semaphore:
                    ReleaseSemaphore(1);
                    break;

                default:
                    _signalCount = 1;
                    break;
            }
        }
    }

    private sealed class NamedMutex(string name) : WaitableObject(WaitableObjectType.Mutex, 1)
    {
        public readonly string Name = name;
        public int ReferenceCount = 1;
    }
}
