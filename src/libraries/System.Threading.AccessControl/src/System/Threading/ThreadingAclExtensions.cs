// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from EventWaitHandle.cs, Mutex.cs, Semaphore.cs and made extension methods (or renamed statics, where necessary) to allow
// extending the class without System.Threading needing to rely on System.Security.AccessControl

using System;
using System.Security.AccessControl;

namespace System.Threading
{
    public static class ThreadingAclExtensions
    {
        public static EventWaitHandleSecurity GetAccessControl(this EventWaitHandle handle)
        {
            return new EventWaitHandleSecurity(handle.GetSafeWaitHandle(), AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public static void SetAccessControl(this EventWaitHandle handle, EventWaitHandleSecurity eventSecurity)
        {
            ArgumentNullException.ThrowIfNull(eventSecurity);

            eventSecurity.Persist(handle.GetSafeWaitHandle());
        }

        public static MutexSecurity GetAccessControl(this Mutex mutex)
        {
            return new MutexSecurity(mutex.GetSafeWaitHandle(), AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public static void SetAccessControl(this Mutex mutex, MutexSecurity mutexSecurity)
        {
            ArgumentNullException.ThrowIfNull(mutexSecurity);

            mutexSecurity.Persist(mutex.GetSafeWaitHandle());
        }

        public static SemaphoreSecurity GetAccessControl(this Semaphore semaphore)
        {
            return new SemaphoreSecurity(semaphore.GetSafeWaitHandle(), AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public static void SetAccessControl(this Semaphore semaphore, SemaphoreSecurity semaphoreSecurity)
        {
            ArgumentNullException.ThrowIfNull(semaphoreSecurity);

            semaphoreSecurity.Persist(semaphore.GetSafeWaitHandle());
        }
    }
}
