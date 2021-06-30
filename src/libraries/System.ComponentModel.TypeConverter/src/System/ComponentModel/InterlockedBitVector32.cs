// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a subset of the <see cref="System.Collections.Specialized.BitVector32"/> surface area, using volatile
    /// operations for reads and interlocked operations for writes.
    /// </summary>
    internal struct InterlockedBitVector32
    {
        private int _data;

        public bool this[int bit]
        {
            get => (Volatile.Read(ref _data) & bit) == bit;
            set
            {
                if (value)
                {
                    Interlocked.Or(ref _data, bit);
                }
                else
                {
                    Interlocked.And(ref _data, ~bit);
                }
            }
        }

        /// <summary>
        /// Sets or unsets the specified bit, without using interlocked operations.
        /// </summary>
        public void DangerousSet(int bit, bool value) => _data = value ? _data | bit : _data & ~bit;

        public static int CreateMask() => CreateMask(0);

        public static int CreateMask(int previous)
        {
            Debug.Assert(previous != unchecked((int)0x80000000));
            return previous == 0 ? 1 : previous << 1;
        }

        public override bool Equals([NotNullWhen(true)] object? o) => o is InterlockedBitVector32 vector && _data == vector._data;

        public override int GetHashCode() => base.GetHashCode();
    }
}
