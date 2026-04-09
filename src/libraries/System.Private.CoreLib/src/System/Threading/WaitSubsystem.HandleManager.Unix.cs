// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal static partial class WaitSubsystem
    {
        public interface IWaitableObject
        {
            void OnDeleteHandle();
            int Wait_Locked(ThreadWaitInfo waitInfo, int timeoutMilliseconds, bool interruptible, bool prioritize, ref LockHolder lockHolder);
            void Signal(int count, ref LockHolder lockHolder);
        }

        public static int Wait(this IWaitableObject waitable, ThreadWaitInfo waitInfo, int timeoutMilliseconds, bool interruptible, bool prioritize)
        {
            Debug.Assert(waitInfo.Thread == Thread.CurrentThread);

            Debug.Assert(timeoutMilliseconds >= -1);

            var lockHolder = new LockHolder(s_lock);
            try
            {
                if (interruptible && waitInfo.CheckAndResetPendingInterrupt)
                {
                    lockHolder.Dispose();
                    throw new ThreadInterruptedException();
                }

                return waitable.Wait_Locked(waitInfo, timeoutMilliseconds, interruptible, prioritize, ref lockHolder);
            }
            finally
            {
                lockHolder.Dispose();
            }
        }

        private static class HandleManager
        {
            public static IntPtr NewHandle(IWaitableObject waitableObject)
            {
                Debug.Assert(waitableObject != null);

                IntPtr handle = GCHandle.ToIntPtr(GCHandle.Alloc(waitableObject, GCHandleType.Normal));

                // SafeWaitHandle treats -1 and 0 as invalid, and the handle should not be these values anyway
                Debug.Assert(handle != IntPtr.Zero);
                Debug.Assert(handle != new IntPtr(-1));
                return handle;
            }

            public static IWaitableObject FromHandle(IntPtr handle)
            {
                if (handle == IntPtr.Zero || handle == new IntPtr(-1))
                {
                    WaitHandle.ThrowInvalidHandleException();
                }

                // We don't know if any other handles are invalid, and this may crash or otherwise do bad things, that is by
                // design, IntPtr is unsafe by nature.
                return (IWaitableObject)GCHandle.FromIntPtr(handle).Target!;
            }

            /// <summary>
            /// Unlike on Windows, a handle may not be deleted more than once with this implementation
            /// </summary>
            public static void DeleteHandle(IntPtr handle)
            {
                if (handle == IntPtr.Zero || handle == new IntPtr(-1))
                {
                    return;
                }

                // We don't know if any other handles are invalid, and this may crash or otherwise do bad things, that is by
                // design, IntPtr is unsafe by nature.
                FromHandle(handle).OnDeleteHandle();
                GCHandle.FromIntPtr(handle).Free();
            }
        }
    }
}
