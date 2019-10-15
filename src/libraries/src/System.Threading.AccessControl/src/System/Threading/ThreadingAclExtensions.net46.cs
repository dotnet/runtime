// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.AccessControl;

namespace System.Threading
{
    public static class ThreadingAclExtensions
    {
        public static EventWaitHandleSecurity GetAccessControl(this EventWaitHandle handle)
        {
            return handle.GetAccessControl();
        }

        public static void SetAccessControl(this EventWaitHandle handle, EventWaitHandleSecurity eventSecurity)
        {
            handle.SetAccessControl(eventSecurity);
        }

        public static MutexSecurity GetAccessControl(this Mutex mutex)
        {
            return mutex.GetAccessControl();
        }

        public static void SetAccessControl(this Mutex mutex, MutexSecurity mutexSecurity)
        {
            mutex.SetAccessControl(mutexSecurity);
        }

        public static SemaphoreSecurity GetAccessControl(this Semaphore semaphore)
        {
            return semaphore.GetAccessControl();
        }

        public static void SetAccessControl(this Semaphore semaphore, SemaphoreSecurity semaphoreSecurity)
        {
            semaphore.SetAccessControl(semaphoreSecurity);
        }
    }
}
