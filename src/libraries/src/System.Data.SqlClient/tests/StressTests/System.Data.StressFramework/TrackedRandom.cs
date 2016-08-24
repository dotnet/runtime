// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stress.Data
{
    /// <summary>
    /// Random number generator that tracks information necessary to reproduce a sequence of random numbers.
    /// </summary>
    /// <remarks>
    /// There are three items maintained by instances of this class
    /// that are used to assist in the reproduction of a sequence of generated numbers:
    /// 
    /// 1. The seed used for initialization.
    /// 2. The count of numbers generated.
    /// 3. Markers to indicate relevant points in the sequence.
    /// 
    /// For tests that use random numbers to control execution, 
    /// these tracked items can be used to help determine the specific code path that was executed.
    /// Here's an example:
    /// 
    /// A test starts to execute, and retrieves an instance of this class.  
    /// If an instance of this class has not been created beforehand, it is constructed and the *seed* is stored.
    /// The test inserts a *marker* to track the *count* of numbers generated before the test starts its work.
    /// As the test executes, it asks for a sequence of random numbers.  At some point, the test causes a crash.
    /// Using the resulting dump (or live debugging session if available), it is possible to examine an instance
    /// of this class to recreate the sequence of numbers used by the test.
    /// You can create an instance of a Random offline using the tracked *seed*, 
    /// and generate numbers up to the *marked* count to determine the starting point for the sequence of numbers used by the test.
    /// The length of the sequence is indicated by the last *count* of number generated.
    /// So for a failed test, you can use the numbers from Mark+1 to Count to retrace the code path taken by the test.
    /// 
    /// Instances of this class keep track of a finite number of multiple marks, 
    /// so it is possible to track the beginning and end of a series of tests,
    /// assuming they all mark at least the start of their execution.
    /// </remarks>
    public class TrackedRandom : Random
    {
        private readonly int _seed;

        /// <summary>
        /// Number of random numbers generated.
        /// </summary>
        private long _count;

        /// <summary>
        /// Circular buffer to track the most recent marks that indicate the count at the time a given mark was created.
        /// </summary>
        private readonly long[] _marks = new long[16];

        /// <summary>
        /// Index of where to place next mark in buffer.
        /// This index is incremented after each mark, and wraps around as necessary.
        /// </summary>
        private int _nextMark;

        private const int EmptyMark = -1;

        public TrackedRandom()
            : this(Environment.TickCount)
        {
        }

        public TrackedRandom(int seed)
            : base(seed)
        {
            _seed = seed;

            for (int i = 0; i < _marks.Length; i++)
            {
                _marks[i] = EmptyMark;
            }
        }

        public int Seed
        {
            get
            {
                return _seed;
            }
        }

        public long Count
        {
            get
            {
                return _count;
            }
        }

        public void Mark()
        {
            long mark = _count;

            // marking forward
            _marks[_nextMark++] = mark;

            // wrap when necessary
            if (_nextMark == _marks.Length)
            {
                _nextMark = 0;
            }
        }

        /// <summary>
        /// Return an enumerable that can be used to iterate over the most recent marks,
        /// starting from the most recent, and ending with the earliest mark still being tracked.
        /// </summary>
        public IEnumerable<long> Marks
        {
            get
            {
                // Iterate backwards through the mark array,
                // starting just before the index of the next mark,
                // and ending at the next mark.
                // Iteration stops earlier if an empty mark is found.
                int index;
                long mark;

                for (int i = 1; i <= _marks.Length; i++)
                {
                    // Index of current element determined by:
                    // ((L+n) - i) % L
                    // where
                    // L is the length of the array, 
                    // n is the index of where to insert the next mark, 0 <= n < L,
                    // i is the current iteration variable value, 0 < i <= L.
                    index = (_marks.Length + _nextMark - i) % _marks.Length;
                    mark = _marks[index];

                    if (mark == EmptyMark)
                    {
                        break;
                    }

                    yield return mark;
                }
            }
        }

        private void IncrementCount()
        {
            if (_count == long.MaxValue)
            {
                _count = -1;
            }

            ++_count;
        }

        public override int Next()
        {
            IncrementCount();
            return base.Next();
        }

        public override int Next(int minValue, int maxValue)
        {
            IncrementCount();
            return base.Next(minValue, maxValue);
        }

        public override int Next(int maxValue)
        {
            IncrementCount();
            return base.Next(maxValue);
        }

        public override void NextBytes(byte[] buffer)
        {
            IncrementCount();
            base.NextBytes(buffer);
        }

        public override double NextDouble()
        {
            IncrementCount();
            return base.NextDouble();
        }
    }
}