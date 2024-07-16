// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader;

namespace StressLogAnalyzer
{
    internal sealed class GCThreadMap
    {
        private ulong _seenHeapBitmap;
        private readonly ConcurrentDictionary<ulong, (ulong heap, bool background)> _heapMap = [];

        public void ProcessInterestingMessage(ulong threadId, WellKnownString wellKnownString, IReadOnlyList<TargetPointer> args)
        {
            switch (wellKnownString)
            {
                case WellKnownString.THREAD_WAIT:
                case WellKnownString.THREAD_WAIT_DONE:
                case WellKnownString.MARK_START:
                case WellKnownString.PLAN_START:
                case WellKnownString.RELOCATE_START:
                case WellKnownString.RELOCATE_END:
                case WellKnownString.COMPACT_START:
                case WellKnownString.COMPACT_END:
                    RememberHeapForThread(threadId, (ulong)args[0], false);
                    break;

                case WellKnownString.DESIRED_NEW_ALLOCATION:
                    if (args[1] <= 1)
                    {
                        // do this only for gen 0 and 1, because otherwise it
                        // may be background GC
                        RememberHeapForThread(threadId, (ulong)args[0], false);
                    }
                    break;

                case WellKnownString.START_BGC_THREAD:
                    RememberHeapForThread(threadId, (ulong)args[0], true);
                    break;
            }
        }

        private void RememberHeapForThread(ulong threadId, ulong heap, bool background)
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

        public bool ThreadHasHeap(ulong threadId) => _heapMap.ContainsKey(threadId);

        public (ulong heap, bool background) GetThreadHeap(ulong threadId) => _heapMap[threadId];

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
