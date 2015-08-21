// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    // Dummy psuedo-set for verifying we've seen all of a range of integers, and only once.
    internal class IntegerRangeSet : IEnumerable<KeyValuePair<int, bool>>
    {
        private BitArray _seen;
        private int _start;
        private object _locker;

        public IntegerRangeSet(int start, int count)
        {
            _start = start;
            _seen = new BitArray(count);
            _locker = new object();
        }

        public bool Add(int entry)
        {
            Assert.InRange(entry, _start, _start + _seen.Length - 1);

            lock (_locker)
            {
                Assert.False(_seen[entry - _start]);
                return _seen[entry - _start] = true;
            }
        }

        public void AssertComplete()
        {
            Assert.All(this, kv => Assert.True(kv.Value));
        }

        public IEnumerator<KeyValuePair<int, bool>> GetEnumerator()
        {
            return new BitArrayEnumerator(_start, _seen);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class BitArrayEnumerator : IEnumerator<KeyValuePair<int, bool>>
        {
            private int _start;
            private BitArray _values;

            private int _current = -1;

            public BitArrayEnumerator(int start, BitArray values)
            {
                _start = start;
                _values = values;
            }

            public KeyValuePair<int, bool> Current
            {
                get
                {
                    return new KeyValuePair<int, bool>(_start + _current, _values[_current]);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
                Reset();
            }

            public bool MoveNext()
            {
                return (_current + 1 < _values.Length) ? ++_current >= 0 : false;
            }

            public void Reset()
            {
                _current = -1;
            }
        }
    }

    // Simple class for counting the number of times an integer in a range has occurred.
    internal class IntegerRangeCounter : IEnumerable<KeyValuePair<int, int>>
    {
        private int[] _seen;
        private int _start;

        public IntegerRangeCounter(int start, int count)
        {
            _start = start;
            _seen = new int[count];
        }

        public int Add(int entry)
        {
            Assert.InRange(entry, _start, _start + _seen.Length - 1);

            return Interlocked.Increment(ref _seen[entry - _start]);
        }

        public void AssertEncountered(int count)
        {
            Assert.All(this, kv => Assert.Equal(count, kv.Value));
        }

        public IEnumerator<KeyValuePair<int, int>> GetEnumerator()
        {
            return _seen.Select((v, index) => new KeyValuePair<int, int>(index + _start, v)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
