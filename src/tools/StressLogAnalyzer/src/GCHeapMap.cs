// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StressLogAnalyzer
{
    internal sealed class GCHeapMap
    {
        private ulong _seenHeapBitmap;
        private readonly ConcurrentDictionary<ulong, (ulong heap, bool background)> _heapMap = [];

        public void RememberHeapForThread(ulong threadId, ulong heap, bool background)
        {
            if (Interlocked.Or(ref _seenHeapBitmap, heap) == 0)
            {
                // we don't want to remember these associations for WKS GC,
                // which can execute on any thread - as soon as we see
                // a heap number != 0, we assume SVR GC and remember it
                return;
            }

            _heapMap.GetOrAdd(threadId, (heap, background));
        }

        public bool IncludeThread(ulong threadId, ThreadFilter filter)
        {
            if (filter.IncludeThread(threadId))
            {
                return true;
            }

            if (_heapMap.TryGetValue(threadId, out (ulong heap, bool background) heapInfo))
            {
                return filter.IncludeHeapThread(heapInfo.heap, heapInfo.background);
            }

            return false;
        }
    }
}
