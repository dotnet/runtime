// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Runtime.InteropServices
{
    public sealed class HandleCollector
    {
        private const int DeltaPercent = 10; // this is used for increasing the threshold. 
        private int _initialThreshold;
        private int _threshold;
        private int _handleCount;

        private int[] _gc_counts = new int[3];
        private int _gc_gen = 0;

        public HandleCollector(string name, int initialThreshold) :
            this(name, initialThreshold, int.MaxValue)
        {
        }

        public HandleCollector(string name, int initialThreshold, int maximumThreshold)
        {
            if (initialThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialThreshold), SR.Arg_NeedNonNegNumRequired);
            }

            if (maximumThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumThreshold), SR.Arg_NeedNonNegNumRequired);
            }

            if (initialThreshold > maximumThreshold)
            {
                throw new ArgumentException(SR.Arg_InvalidThreshold);
            }

            Name = name ?? string.Empty;
            _initialThreshold = initialThreshold;
            MaximumThreshold = maximumThreshold;
            _threshold = initialThreshold;
            _handleCount = 0;
        }

        public int Count => _handleCount;

        public int InitialThreshold => _initialThreshold;

        public int MaximumThreshold { get; }

        public string Name { get; }

        public void Add()
        {
            int gen_collect = -1;
            Interlocked.Increment(ref _handleCount);
            if (_handleCount < 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_HCCountOverflow);
            }

            if (_handleCount > _threshold)
            {
                lock (this)
                {
                    _threshold = _handleCount + (_handleCount / DeltaPercent);
                    gen_collect = _gc_gen;
                    if (_gc_gen < 2)
                    {
                        _gc_gen++;
                    }
                }
            }

            if ((gen_collect >= 0) &&
                    ((gen_collect == 0) ||
                    (_gc_counts[gen_collect] == GC.CollectionCount(gen_collect))))
            {
                GC.Collect(gen_collect);
                Internal.Runtime.Augments.RuntimeThread.Sleep(10 * gen_collect);
            }

            //don't bother with gen0. 
            for (int i = 1; i < 3; i++)
            {
                _gc_counts[i] = GC.CollectionCount(i);
            }
        }

        public void Remove()
        {
            Interlocked.Decrement(ref _handleCount);
            if (_handleCount < 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_HCCountOverflow);
            }

            int newThreshold = _handleCount + _handleCount / DeltaPercent;
            if (newThreshold < (_threshold - _threshold / DeltaPercent))
            {
                lock (this)
                {
                    if (newThreshold > _initialThreshold)
                    {
                        _threshold = newThreshold;
                    }
                    else
                    {
                        _threshold = _initialThreshold;
                    }
                    _gc_gen = 0;
                }
            }

            for (int i = 1; i < 3; i++)
            {
                _gc_counts[i] = GC.CollectionCount(i);
            }
        }
    }
}
