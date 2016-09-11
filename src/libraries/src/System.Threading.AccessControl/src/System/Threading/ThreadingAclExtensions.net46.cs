// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.AccessControl;

namespace System.Threading
{
    public static class ThreadingAclExtensions
    {
        public static EventWaitHandleSecurity GetAccessControl(EventWaitHandle handle)
        {
            return handle.GetAccessControl();
        }

        public static void SetAccessControl(EventWaitHandle handle, EventWaitHandleSecurity eventSecurity)
        {
            handle.SetAccessControl(eventSecurity);
        }

        public static MutexSecurity GetAccessControl(Mutex mutex)
        {
            return mutex.GetAccessControl();
        }

        public static void SetAccessControl(Mutex mutex, MutexSecurity mutexSecurity)
        {
            mutex.SetAccessControl(mutexSecurity);
        }

        public static SemaphoreSecurity GetAccessControl(Semaphore semaphore)
        {
            return semaphore.GetAccessControl();
        }

        public static void SetAccessControl(Semaphore semaphore, SemaphoreSecurity semaphoreSecurity)
        {
            semaphore.SetAccessControl(semaphoreSecurity);
        }
    }
}
