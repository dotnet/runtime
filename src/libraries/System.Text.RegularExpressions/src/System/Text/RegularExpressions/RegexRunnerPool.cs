// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexRunnerPool
    {
        private static readonly int s_maxCapacity = Math.Max(4, Environment.ProcessorCount);

        private ConcurrentStack<RegexRunner> _storage = new();
        private int _storedCount;

        public bool TryGet([NotNullWhen(true)] out RegexRunner? runner)
        {
            if (_storage.TryPop(out runner))
            {
                Interlocked.Decrement(ref _storedCount);
                return true;
            }

            return false;
        }

        public void Return(RegexRunner runner)
        {
            if (Interlocked.Increment(ref _storedCount) <= s_maxCapacity)
            {
                _storage.Push(runner);
            }
            else
            {
                Interlocked.Decrement(ref _storedCount);
            }
        }
    }
}
