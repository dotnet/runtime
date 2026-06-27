// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Synchronizes access to a shared resource or region of code in a multi-threaded
**             program.
**
**
=============================================================================*/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    public static partial class Monitor
    {
        #region Object->Lock mapping
        internal static Lock GetLockObject(object obj)
        {
            IntPtr lockHandle = GetLockHandleIfExists(obj);
            if (lockHandle != 0)
            {
                Lock lockObj = GCHandle<Lock>.FromIntPtr(lockHandle).Target;
                GC.KeepAlive(obj);
                return lockObj;
            }

            return GetLockObjectFallback(obj);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static Lock GetLockObjectFallback(object obj)
            {
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement.
                object lockObj = new Lock();
#pragma warning restore CS9216
                GetOrCreateLockObject(ObjectHandleOnStack.Create(ref obj), ObjectHandleOnStack.Create(ref lockObj));
                return (Lock)lockObj!;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetLockHandleIfExists(object obj);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Monitor_GetOrCreateLockObject")]
        private static partial void GetOrCreateLockObject(ObjectHandleOnStack obj, ObjectHandleOnStack lockObj);

        #endregion
    }
}
