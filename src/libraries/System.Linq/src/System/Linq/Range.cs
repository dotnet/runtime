// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<int> Range(int start, int count)
        {
            long max = ((long)start) + count - 1;
            if (count < 0 || max > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            if (count == 0)
            {
                return [];
            }

            return new RangeIterator(start, count);
        }

        /// <summary>
        /// An iterator that yields a range of consecutive integers.
        /// </summary>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class RangeIterator : Iterator<int>
        {
            private readonly int _start;
            private readonly int _end;

            public RangeIterator(int start, int count)
            {
                Debug.Assert(count > 0);
                _start = start;
                _end = start + count;
            }

            private int CountForDebugger => _end - _start;

            private protected override Iterator<int> Clone() => new RangeIterator(_start, _end - _start);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        Debug.Assert(_start != _end);
                        _current = _start;
                        _state = 2;
                        return true;
                    case 2:
                        if (++_current == _end)
                        {
                            break;
                        }

                        return true;
                }

                _state = -1;
                return false;
            }

            public override void Dispose()
            {
                _state = -1; // Don't reset current
            }
        }

        /// <summary>Fills the <paramref name="destination"/> with incrementing numbers, starting from <paramref name="value"/>.</summary>
        private static void FillIncrementing(Span<int> destination, int value)
        {
            ref int pos = ref MemoryMarshal.GetReference(destination);
            ref int end = ref Unsafe.Add(ref pos, destination.Length);

            if (Vector.IsHardwareAccelerated &&
                destination.Length >= Vector<int>.Count)
            {
                Vector<int> init = Vector<int>.Indices;
                Vector<int> current = new Vector<int>(value) + init;
                Vector<int> increment = new Vector<int>(Vector<int>.Count);

                ref int oneVectorFromEnd = ref Unsafe.Subtract(ref end, Vector<int>.Count);
                do
                {
                    current.StoreUnsafe(ref pos);
                    current += increment;
                    pos = ref Unsafe.Add(ref pos, Vector<int>.Count);
                }
                while (!Unsafe.IsAddressGreaterThan(ref pos, ref oneVectorFromEnd));

                value = current[0];
            }

            while (Unsafe.IsAddressLessThan(ref pos, ref end))
            {
                pos = value++;
                pos = ref Unsafe.Add(ref pos, 1);
            }
        }
    }
}
