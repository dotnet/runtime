// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Runtime.InteropServices
{
    public sealed class HandleCollector
    {
        // Used for increasing the threshold.
        private const int DeltaPercent = 10;
        private int _threshold;
        private int _handleCount;

        private readonly int[] _gcCounts = new int[3];
        private int _gcGeneration;

        public HandleCollector(string? name, int initialThreshold) :
            this(name, initialThreshold, int.MaxValue)
        {
        }

        public HandleCollector(string? name, int initialThreshold, int maximumThreshold)
        {
            if (initialThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialThreshold), initialThreshold, SR.Arg_NeedNonNegNumRequired);
            }
            if (maximumThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumThreshold), maximumThreshold, SR.Arg_NeedNonNegNumRequired);
            }
            if (initialThreshold > maximumThreshold)
            {
                throw new ArgumentException(SR.Arg_InvalidThreshold, nameof(initialThreshold));
            }

            Name = name ?? string.Empty;
            InitialThreshold = initialThreshold;
            MaximumThreshold = maximumThreshold;
            _threshold = initialThreshold;
            _handleCount = 0;
            _gcGeneration = 0;
        }

        public int Count => _handleCount;

        public int InitialThreshold { get; }

        public int MaximumThreshold { get; }

        public string Name { get; }

        public void Add()
        {
            int collectionGeneration = -1;
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
                    collectionGeneration = _gcGeneration;
                    if (_gcGeneration < 2)
                    {
                        _gcGeneration++;
                    }
                }
            }

            if ((collectionGeneration >= 0) &&
                    ((collectionGeneration == 0) ||
                    (_gcCounts[collectionGeneration] == GC.CollectionCount(collectionGeneration))))
            {
                GC.Collect(collectionGeneration);
                Thread.Sleep(10 * collectionGeneration);
            }

            // Don't bother with gen0.
            for (int i = 1; i < 3; i++)
            {
                _gcCounts[i] = GC.CollectionCount(i);
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
                    if (newThreshold > InitialThreshold)
                    {
                        _threshold = newThreshold;
                    }
                    else
                    {
                        _threshold = InitialThreshold;
                    }
                    _gcGeneration = 0;
                }
            }

            for (int i = 1; i < 3; i++)
            {
                _gcCounts[i] = GC.CollectionCount(i);
            }
        }
    }
}
