// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#nullable disable

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Scalable 32bit counter that can be used from multiple threads.
    /// </summary>
    internal sealed class Counter32: CounterBase
    {
        private class Cell
        {
            [StructLayout(LayoutKind.Explicit, Size = CACHE_LINE * 2 - OBJ_HEADER_SIZE)]
            public struct SpacedCounter
            {
                [FieldOffset(CACHE_LINE - OBJ_HEADER_SIZE)]
                public int count;
            }

            public SpacedCounter counter;
        }

        // spaced out counters
        private Cell[] cells;

        // default counter
        private int count;

        // delayed estimated count
        private int lastCount;

        /// <summary>
        /// Initializes a new instance of the <see
        /// cref="Counter32"/>
        /// </summary>
        public Counter32()
        {
        }

        /// <summary>
        /// Returns the value of the counter at the time of the call.
        /// </summary>
        /// <remarks>
        /// The value may miss in-progress updates if the counter is being concurrently modified.
        /// </remarks>
        public int Value
        {
            get
            {
                var count = this.count;
                var cells = this.cells;

                if (cells != null)
                {
                    for (int i = 0; i < cells.Length; i++)
                    {
                        var cell = cells[i];
                        if (cell != null)
                        {
                            count += cell.counter.count;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Returns the approximate value of the counter at the time of the call.
        /// </summary>
        /// <remarks>
        /// EstimatedValue could be significantly cheaper to obtain, but may be slightly delayed.
        /// </remarks>
        public int EstimatedValue
        {
            get
            {
                if (this.cells == null)
                {
                    return this.count;
                }

                var curTicks = (uint)Environment.TickCount;
                // more than a millisecond passed?
                if (curTicks != lastCountTicks)
                {
                    lastCountTicks = curTicks;
                    lastCount = Value;
                }

                return lastCount;
            }
        }

        /// <summary>
        /// Increments the counter by 1.
        /// </summary>
        public void Increment()
        {
            int curCellCount = this.cellCount;
            var drift = increment(ref GetCountRef(curCellCount));

            if (drift != 0)
            {
                TryAddCell(curCellCount);
            }
        }

        /// <summary>
        /// Decrements the counter by 1.
        /// </summary>
        public void Decrement()
        {
            int curCellCount = this.cellCount;
            var drift = decrement(ref GetCountRef(curCellCount));

            if (drift != 0)
            {
                TryAddCell(curCellCount);
            }
        }

        /// <summary>
        /// Increments the counter by 'value'.
        /// </summary>
        public void Add(int value)
        {
            int curCellCount = this.cellCount;
            var drift = add(ref GetCountRef(curCellCount), value);

            if (drift != 0)
            {
                TryAddCell(curCellCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetCountRef(int curCellCount)
        {
            ref var countRef = ref count;

            Cell[] cells;
            if ((cells = this.cells) != null && curCellCount > 1)
            {
                var cell = cells[GetIndex((uint)curCellCount)];
                if (cell != null)
                {
                    countRef = ref cell.counter.count;
                }
            }

            return ref countRef;
        }

        private static int increment(ref int val)
        {
            return -val - 1 + Interlocked.Increment(ref val);
        }

        private static int add(ref int val, int inc)
        {
            return -val - inc + Interlocked.Add(ref val, inc);
        }

        private static int decrement(ref int val)
        {
            return val - 1 - Interlocked.Decrement(ref val);
        }

        private void TryAddCell(int curCellCount)
        {
            if (curCellCount < s_MaxCellCount)
            {
                TryAddCellCore(curCellCount);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void TryAddCellCore(int curCellCount)
        {
            var cells = this.cells;
            if (cells == null)
            {
                var newCells = new Cell[s_MaxCellCount];
                cells = Interlocked.CompareExchange(ref this.cells, newCells, null) ?? newCells;
            }

            if (cells[curCellCount] == null)
            {
                Interlocked.CompareExchange(ref cells[curCellCount], new Cell(), null);
            }

            if (this.cellCount == curCellCount)
            {
                Interlocked.CompareExchange(ref this.cellCount, curCellCount + 1, curCellCount);
                //if (Interlocked.CompareExchange(ref this.cellCount, curCellCount + 1, curCellCount) == curCellCount)
                //{
                //    System.Console.WriteLine(curCellCount + 1);
                //}
            }
        }
    }
}
