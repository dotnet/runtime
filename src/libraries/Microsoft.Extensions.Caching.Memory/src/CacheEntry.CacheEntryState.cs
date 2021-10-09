// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Caching.Memory
{
    internal sealed partial class CacheEntry
    {
        // this type exists just to reduce CacheEntry size by replacing many enum & boolean fields with one of a size of Int32
        private struct CacheEntryState
        {
            private byte _flags;
            private byte _evictionReason;
            private byte _priority;

            internal CacheEntryState(CacheItemPriority priority) : this() => _priority = (byte)priority;

            internal bool IsDisposed
            {
                get => ((Flags)_flags & Flags.IsDisposed) != 0;
                set => SetFlag(Flags.IsDisposed, value);
            }

            internal bool IsExpired
            {
                get => ((Flags)_flags & Flags.IsExpired) != 0;
                set => SetFlag(Flags.IsExpired, value);
            }

            internal bool IsValueSet
            {
                get => ((Flags)_flags & Flags.IsValueSet) != 0;
                set => SetFlag(Flags.IsValueSet, value);
            }

            internal EvictionReason EvictionReason
            {
                get => (EvictionReason)_evictionReason;
                set => _evictionReason = (byte)value;
            }

            internal CacheItemPriority Priority
            {
                get => (CacheItemPriority)_priority;
                set => _priority = (byte)value;
            }

            private void SetFlag(Flags option, bool value) => _flags = (byte)(value ? (_flags | (byte)option) : (_flags & ~(byte)option));

            [Flags]
            private enum Flags : byte
            {
                Default = 0,
                IsValueSet = 1 << 0,
                IsExpired = 1 << 1,
                IsDisposed = 1 << 2,
            }
        }
    }
}
