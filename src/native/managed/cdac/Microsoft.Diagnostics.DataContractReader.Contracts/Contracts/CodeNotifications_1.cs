// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct CodeNotifications_1 : ICodeNotifications
{
    private readonly Target _target;

    internal CodeNotifications_1(Target target)
    {
        _target = target;
    }

    void ICodeNotifications.SetCodeNotification(TargetPointer module, uint methodToken, CodeNotificationKind flags)
    {
        TableView? view = PrepareTable(allocateIfMissing: flags != CodeNotificationKind.None);
        if (view is null)
            return;

        TableView v = view.Value;

        if (flags == CodeNotificationKind.None)
        {
            if (v.TryFindEntry(module, methodToken, out uint foundIndex))
            {
                Data.JITNotification entry = v.GetEntry(foundIndex);
                entry.Clear();

                // Trim all trailing free entries so Length stays the smallest value satisfying
                // "Length > index of every active entry". A single decrement is insufficient
                // because earlier clears can leave an interior hole adjacent to the just-cleared
                // tail entry (e.g., set A,B,C; clear B, then clear C should land at Length=1).
                uint newLength = v.Length;
                while (newLength > 0 && v.GetEntry(newLength - 1).IsFree)
                {
                    newLength--;
                }

                v.Length = newLength;
            }

            return;
        }

        if (v.TryFindEntry(module, methodToken, out uint existingIndex))
        {
            v.GetEntry(existingIndex).State = (ushort)flags;

            return;
        }

        uint firstFree = v.Length;
        for (uint i = 0; i < v.Length; i++)
        {
            if (v.GetEntry(i).IsFree)
            {
                firstFree = i;
                break;
            }
        }

        if (firstFree >= v.Capacity)
        {
            // Match legacy DAC (ClrDataMethodDefinition::SetCodeNotification path): when the
            // notification table is full, SetNotification returns FALSE which bubbles up as E_FAIL.
            const int E_FAIL = unchecked((int)0x80004005);
            throw new COMException("JIT notification table is full", E_FAIL);
        }

        v.GetEntry(firstFree).WriteEntry(module, methodToken, (ushort)flags);

        if (firstFree >= v.Length)
        {
            v.Length++;
        }
    }

    CodeNotificationKind ICodeNotifications.GetCodeNotification(TargetPointer module, uint methodToken)
    {
        TableView? view = PrepareTable(allocateIfMissing: false);
        if (view is null)
            return CodeNotificationKind.None;

        TableView v = view.Value;
        if (v.TryFindEntry(module, methodToken, out uint foundIndex))
        {
            return (CodeNotificationKind)v.GetEntry(foundIndex).State;
        }

        return CodeNotificationKind.None;
    }

    void ICodeNotifications.SetAllCodeNotifications(TargetPointer module, CodeNotificationKind flags)
    {
        // When the table has not been allocated there are no entries to update, so this is a
        // no-op. Matches native JITNotifications::SetAllNotifications (util.cpp:1112).
        TableView? maybeView = PrepareTable(allocateIfMissing: false);
        if (maybeView is null)
            return;

        TableView v = maybeView.Value;
        bool changed = false;
        for (uint i = 0; i < v.Length; i++)
        {
            Data.JITNotification entry = v.GetEntry(i);
            if (entry.IsFree)
                continue;

            if (module != TargetPointer.Null && entry.ClrModule.Value != module.Value)
                continue;

            if (flags == CodeNotificationKind.None)
            {
                entry.Clear();
            }
            else
            {
                entry.State = (ushort)flags;
            }

            changed = true;
        }

        if (changed && flags == CodeNotificationKind.None)
        {
            // Trim only trailing free entries. This deliberately diverges from native
            // JITNotifications::SetAllNotifications (src/coreclr/vm/util.cpp:1140-1149), which
            // decrements the stored length for every free slot in [0, Length), including holes.
            // That algorithm can trim Length below the index of still-active entries belonging
            // to other modules (e.g., when SetAllCodeNotifications filters by module), orphaning
            // those entries. Trimming only trailing free slots preserves the invariant
            // "Length > index of every active entry" which the lookup/iteration code relies on.
            uint newLength = v.Length;
            while (newLength > 0 && v.GetEntry(newLength - 1).IsFree)
            {
                newLength--;
            }

            v.Length = newLength;
        }
    }

    /// <summary>
    /// A live handle to the JIT notification table in the target process.
    /// <see cref="Length"/> reads and writes through the sentinel slot (index 0) via the
    /// <see cref="Data.JITNotification"/> IData; <see cref="Capacity"/> comes from the
    /// <c>JITNotificationTableSize</c> global. Per-entry access is via
    /// <see cref="GetEntry"/> and <see cref="TryFindEntry"/>.
    /// </summary>
    private readonly struct TableView
    {
        private readonly Target _target;
        private readonly Data.JITNotification _sentinel;
        public readonly ulong EntriesBase;
        public readonly uint EntrySize;

        public TableView(Target target, TargetPointer basePointer, uint entrySize)
        {
            _target = target;
            _sentinel = new Data.JITNotification(target, basePointer);
            EntrySize = entrySize;
            EntriesBase = basePointer + entrySize;
        }

        public uint Length
        {
            get => _sentinel.MethodToken;
            set => _sentinel.MethodToken = value;
        }

        public uint Capacity => _target.ReadGlobal<uint>(Constants.Globals.JITNotificationTableSize);

        public Data.JITNotification GetEntry(uint index)
            => new(_target, new TargetPointer(EntriesBase + (ulong)(index * EntrySize)));

        public bool TryFindEntry(TargetPointer module, uint methodToken, out uint index)
        {
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                Data.JITNotification entry = GetEntry(i);
                if (entry.IsFree)
                    continue;
                if (entry.ClrModule.Value != module.Value)
                    continue;
                if (entry.MethodToken != methodToken)
                    continue;

                index = i;

                return true;
            }

            index = 0;

            return false;
        }
    }

    /// <summary>
    /// Read (and optionally lazily allocate) the JIT notification table. Returns null if
    /// the table is not allocated and <paramref name="allocateIfMissing"/> is false.
    /// </summary>
    private TableView? PrepareTable(bool allocateIfMissing)
    {
        Target.TypeInfo jitNotifType = _target.GetTypeInfo(DataType.JITNotification);
        uint entrySize = (uint)(jitNotifType.Size
            ?? throw new InvalidOperationException("JITNotification has no declared size"));

        TargetPointer globalAddr = _target.ReadGlobalPointer(Constants.Globals.JITNotificationTable);
        TargetPointer tablePointer = _target.ReadPointer(globalAddr);

        if (tablePointer == TargetPointer.Null)
        {
            if (!allocateIfMissing)
                return null;
            tablePointer = AllocateTable(entrySize, globalAddr);
        }

        return new TableView(_target, tablePointer, entrySize);
    }

    /// <summary>
    /// Lazily allocate a JIT notification table in the target process using AllocateMemory,
    /// zero-fill it (slot 0's methodToken is the length, which starts at 0), and write the
    /// pointer back to <c>g_pNotificationTable</c>.
    /// </summary>
    private TargetPointer AllocateTable(uint entrySize, TargetPointer globalAddr)
    {
        uint capacity = _target.ReadGlobal<uint>(Constants.Globals.JITNotificationTableSize);
        // Table has capacity+1 entries: index 0 is bookkeeping
        uint tableByteSize = entrySize * (capacity + 1);
        TargetPointer tablePointer = _target.AllocateMemory(tableByteSize);

        byte[] zeros = new byte[checked((int)tableByteSize)];
        _target.WriteBuffer(tablePointer.Value, zeros);

        _target.WritePointer(globalAddr.Value, tablePointer);

        return tablePointer;
    }
}
