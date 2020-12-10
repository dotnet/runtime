// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Extensions.Caching.Memory
{
    internal partial class CacheEntry
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct State
        {
            [FieldOffset(0)]
            private int _state;

            [FieldOffset(0)]
            private byte _flags;
            [FieldOffset(1)]
            private byte _evictionReason;
            [FieldOffset(2)]
            private byte _priority;
            [FieldOffset(3)]
            private byte _reserved; // for future use

            internal State(CacheItemPriority priority) : this() => _priority = (byte)priority;

            internal bool IsDisposed { get => ((Flags)_flags).HasFlag(Flags.IsDisposed); set => SetFlag(Flags.IsDisposed, value); }

            internal bool IsExpired { get => ((Flags)_flags).HasFlag(Flags.IsExpired); set => SetFlag(Flags.IsExpired, value); }

            internal bool IsValueSet { get => ((Flags)_flags).HasFlag(Flags.IsValueSet); set => SetFlag(Flags.IsValueSet, value); }

            internal EvictionReason EvictionReason
            {
                get => (EvictionReason)_evictionReason;
                set
                {
                    State before, after;
                    do
                    {
                        before = this;
                        after = this;
                        after._evictionReason = (byte)value;
                    } while (Interlocked.CompareExchange(ref _state, after._state, before._state) != before._state);
                }
            }

            internal CacheItemPriority Priority
            {
                get => (CacheItemPriority)_priority;
                set
                {
                    State before, after;
                    do
                    {
                        before = this;
                        after = this;
                        after._priority = (byte)value;
                    } while (Interlocked.CompareExchange(ref _state, after._state, before._state) != before._state);
                }
            }

            private void SetFlag(Flags option, bool value)
            {
                State before, after;

                do
                {
                    before = this;
                    after = this;
                    after._flags = (byte)(value ? (after._flags | (byte)option) : (after._flags & ~(byte)option));
                } while (Interlocked.CompareExchange(ref _state, after._state, before._state) != before._state);
            }

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
