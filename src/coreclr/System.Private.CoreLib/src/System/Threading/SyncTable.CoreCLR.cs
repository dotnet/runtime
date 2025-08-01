// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal static partial class SyncTable
    {
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

        internal static Lock GetLockObject(int index)
        {
            return GCHandle<Lock>.FromIntPtr(GetLockHandleInternal(index)).Target;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SyncTable_GetLockHandle")]
        private static partial IntPtr GetLockHandleInternal(int index);
    }
}
