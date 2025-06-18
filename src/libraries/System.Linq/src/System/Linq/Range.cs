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

            return new RangeIterator<int>(start, start + count);
        }

        /// <summary>
        /// An iterator that yields a range of consecutive integers.
        /// </summary>
        [DebuggerDisplay("Count = {CountForDebugger}")]
        private sealed partial class RangeIterator<T> : Iterator<T> where T : INumber<T>
        {
            private readonly T _start;
            private readonly T _endExclusive;

            public RangeIterator(T start, T endExclusive)
            {
                Debug.Assert(int.CreateChecked(endExclusive - start) >= 0);
                _start = start;
                _endExclusive = endExclusive;
            }

            private int CountForDebugger => int.CreateTruncating(_endExclusive - _start);

            private protected override Iterator<T> Clone() => new RangeIterator<T>(_start, _endExclusive);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        Debug.Assert(_start != _endExclusive);
                        _current = _start;
                        _state = 2;
                        return true;

                    case 2:
                        if (++_current == _endExclusive)
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
        private static void FillIncrementing<T>(Span<T> destination, T value) where T : INumber<T>
        {
            ref T pos = ref MemoryMarshal.GetReference(destination);
            ref T end = ref Unsafe.Add(ref pos, destination.Length);

            if (Vector.IsHardwareAccelerated &&
                Vector<T>.IsSupported &&
                destination.Length >= Vector<T>.Count)
            {
                Vector<T> init = Vector<T>.Indices;
                Vector<T> current = new Vector<T>(value) + init;
                Vector<T> increment = new Vector<T>(T.CreateTruncating(Vector<T>.Count));

                ref T oneVectorFromEnd = ref Unsafe.Subtract(ref end, Vector<T>.Count);
                do
                {
                    current.StoreUnsafe(ref pos);
                    current += increment;
                    pos = ref Unsafe.Add(ref pos, Vector<T>.Count);
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
