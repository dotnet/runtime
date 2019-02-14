// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
                while (true)
                {
                    int oldValue = _data;
                    int newValue = value ? oldValue | bit : oldValue &= ~bit;
                    if (Interlocked.CompareExchange(ref _data, newValue, oldValue) == oldValue)
                    {
                        break;
                    }
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

        public override bool Equals(object o) => o is InterlockedBitVector32 vector && _data == vector._data;

        public override int GetHashCode() => base.GetHashCode();
    }
}
