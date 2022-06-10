// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Transactions.Diagnostics;

namespace System.Transactions.Oletx
{
    static class HandleTable
    {
        private static Dictionary<int, object> handleTable = new Dictionary<int, object>(256);
        private static object syncRoot = new object();
        private static int currentHandle;

        public static IntPtr AllocHandle(object target)
        {
            lock (syncRoot)
            {
                int handle = FindAvailableHandle();
                handleTable.Add(handle, target);

                return new IntPtr(handle);
            }
        }

        public static bool FreeHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero, "handle is invalid");
            lock (syncRoot)
            {
                return handleTable.Remove(handle.ToInt32());
            }
        }

        public static object FindHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero, "handle is invalid");
            lock (syncRoot)
            {
                object target;
                if (!handleTable.TryGetValue(handle.ToInt32(), out target))
                {
                    return null;
                }

                return target;
            }
        }

        private static int FindAvailableHandle()
        {
            int handle = 0;
            do
            {
                handle = (++currentHandle != 0) ? currentHandle : ++currentHandle;
            } while (handleTable.ContainsKey(handle));

            Debug.Assert(handle != 0, "invalid handle selected");
            return handle;
        }
    }
}
