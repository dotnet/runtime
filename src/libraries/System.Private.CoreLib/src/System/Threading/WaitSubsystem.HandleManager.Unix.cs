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
        private static class HandleManager
        {
            public static nint NewHandle(WaitableObject waitableObject)
            {
                Debug.Assert(waitableObject != null);

                nint handle = GCHandle.ToIntPtr(GCHandle.Alloc(waitableObject, GCHandleType.Normal));

                // SafeWaitHandle treats -1 and 0 as invalid, and the handle should not be these values anyway
                Debug.Assert(handle != 0);
                Debug.Assert(handle != -1);
                return handle;
            }

            public static WaitableObject FromHandle(nint handle)
            {
                if (handle == 0 || handle == -1)
                {
                    WaitHandle.ThrowInvalidHandleException();
                }

                // We don't know if any other handles are invalid, and this may crash or otherwise do bad things, that is by
                // design, nint is unsafe by nature.
                return (WaitableObject)GCHandle.FromIntPtr(handle).Target!;
            }

            /// <summary>
            /// Unlike on Windows, a handle may not be deleted more than once with this implementation
            /// </summary>
            public static void DeleteHandle(nint handle)
            {
                if (handle == 0 || handle == -1)
                {
                    return;
                }

                // We don't know if any other handles are invalid, and this may crash or otherwise do bad things, that is by
                // design, nint is unsafe by nature.
                FromHandle(handle).OnDeleteHandle();
                GCHandle.FromIntPtr(handle).Free();
            }
        }
    }
}
