// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal static partial class SyncTable
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe int AssignEntry(object obj, int* pHeader)
        {
            int slot = AssignEntryInternal(ObjectHandleOnStack.Create(ref obj));
#if DEBUG
            bool hasSlot = ObjectHeader.GetSyncEntryIndex(*pHeader, out int slotInHeader);
            Debug.Assert(hasSlot, "Expected the header to have a sync entry index.");
            Debug.Assert(slotInHeader == slot, $"Expected the slot in the header ({slotInHeader}) to match the assigned slot ({slot}).");
#endif
            return slot;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SyncTable_AssignEntry")]
        private static partial int AssignEntryInternal(ObjectHandleOnStack obj);

        internal static Lock GetLockObject(int index, object obj)
        {
            IntPtr lockHandle = GetLockHandleIfExists(index);
            if (lockHandle != 0)
            {
                return GCHandle<Lock>.FromIntPtr(lockHandle).Target;
            }

            return GetLockObjectFallback(index, obj);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static Lock GetLockObjectFallback(int index, object obj)
            {
                object? lockObj = null;
                GetLockObject(index, ObjectHandleOnStack.Create(ref obj), ObjectHandleOnStack.Create(ref lockObj));
                return (Lock)lockObj!;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetLockHandleIfExists(int index);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SyncTable_GetLockObject")]
        private static partial void GetLockObject(int index, ObjectHandleOnStack obj, ObjectHandleOnStack lockObj);
    }
}
