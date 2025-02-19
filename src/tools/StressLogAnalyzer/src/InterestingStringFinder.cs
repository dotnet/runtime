﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader;

namespace StressLogAnalyzer
{
    internal sealed class InterestingStringFinder(Target target, StressLogHeader.ModuleTable moduleTable, string[] userStrings, string[] userStringPrefixes, bool enableDefaultMessages) : IInterestingStringFinder
    {
        private static readonly Dictionary<string, WellKnownString> _knownStrings =
            new()
            {
                {"    GC Root %p RELOCATED %p -> %p  MT = %pT\n", WellKnownString.GCROOT },
                {"    IGCHeap::Promote: Promote GC Root *%p = %p MT = %pT\n", WellKnownString.GCROOT_PROMOTE },
                {"GC_HEAP RELOCATING Objects in heap within range [%p %p) by -0x%x bytes\n", WellKnownString.PLUG_MOVE },
                {"%d gc thread waiting...", WellKnownString.THREAD_WAIT },
                {"%d gc thread waiting... Done", WellKnownString.THREAD_WAIT_DONE },
                {"*GC* %d(gen0:%d)(%d)(alloc: %zd)(%s)(%d)(%d)", WellKnownString.GCSTART },
                {"*EGC* %zd(gen0:%zd)(%zd)(%d)(%s)(%s)(%s)(ml: %d->%d)\n", WellKnownString.GCEND },
                {"---- Mark Phase on heap %d condemning %d ----", WellKnownString.MARK_START },
                {"---- Plan Phase on heap %d ---- Condemned generation %d, promotion: %d", WellKnownString.PLAN_START },
                {"---- Relocate phase on heap %d -----", WellKnownString.RELOCATE_START },
                {"---- End of Relocate phase on heap %d ----", WellKnownString.RELOCATE_END },
                {"---- Compact Phase on heap %d: %zx(%zx)----", WellKnownString.COMPACT_START },
                {"---- End of Compact phase on heap %d ----", WellKnownString.COMPACT_END },
                {" mc: [%zx->%zx, %zx->%zx[", WellKnownString.GCMEMCOPY },
                {"(%zx)[%zx->%zx, NA: [%zx(%zd), %zx[: %zx(%d), x: %zx (%s)", WellKnownString.PLAN_PLUG },
                {"(%zx)PP: [%zx, %zx[%zx](m:%d)", WellKnownString.PLAN_PINNED_PLUG },
                {"h%d g%d surv: %zd current: %zd alloc: %zd (%d%%) f: %d%% new-size: %zd new-alloc: %zd", WellKnownString.DESIRED_NEW_ALLOCATION },
                {"Making unused array [%zx, %zx[", WellKnownString.MAKE_UNUSED_ARRAY },
                {"beginning of bgc on heap %d: gen2 FL: %d, FO: %d, frag: %d", WellKnownString.START_BGC_THREAD },
                {"Relocating reference *(%p) from %p to %p", WellKnownString.RELOCATE_REFERENCE },
                {"TraceGC is not turned on", WellKnownString.LOGGING_OFF },
            };

        private static readonly SearchValues<byte> _defaultInterestingMessages = SearchValues.Create([
            (byte)WellKnownString.LOGGING_OFF,
            (byte)WellKnownString.GCSTART,
            (byte)WellKnownString.GCEND,
            (byte)WellKnownString.MARK_START,
            (byte)WellKnownString.PLAN_START,
            (byte)WellKnownString.RELOCATE_START,
            (byte)WellKnownString.RELOCATE_END,
            (byte)WellKnownString.COMPACT_START,
            (byte)WellKnownString.COMPACT_END,
        ]);

        private static string InterpretEscapeSequences(string input)
        {
            return input.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
        }

        private readonly SearchValues<string> _userInterestingStrings = SearchValues.Create([.. userStrings.Select(InterpretEscapeSequences)], StringComparison.Ordinal);

        private readonly string[] _userStringPrefixes = userStringPrefixes?.Select(InterpretEscapeSequences).ToArray() ?? [];

        private readonly struct StringCacheEntry(bool isInteresting, WellKnownString stringKind)
        {
            // Set the 16th bit to indicate that the entry is valid.
            private readonly int _data = (1 << 16) | (byte)stringKind | (isInteresting ? 1 << 8 : 0);

            public bool IsValid => (_data & (1 << 16)) != 0;
            public bool IsInteresting => (_data & (1 << 8)) != 0;
            public WellKnownString StringKind => (WellKnownString)(_data & 0xFF);
        }

        private readonly StringCacheEntry[] _addressCache = new StringCacheEntry[StressLogHeader.ModuleImageDataSize];

        public bool IsInteresting(TargetPointer formatStringPointer, out WellKnownString wellKnownStringKind)
        {
            int cacheIndex = GetCacheIndex(formatStringPointer);
            if (_addressCache[cacheIndex].IsValid)
            {
                StringCacheEntry entry = _addressCache[cacheIndex];
                wellKnownStringKind = entry.StringKind;
                return entry.IsInteresting;
            }

            StringCacheEntry newEntry = CalculateEntryForString(formatStringPointer);
            _addressCache[cacheIndex] = newEntry;
            wellKnownStringKind = newEntry.StringKind;
            return newEntry.IsInteresting;

            StringCacheEntry CalculateEntryForString(TargetPointer address)
            {
                string formatString = target.ReadUtf8String(address);
                bool defaultInteresting = false;
                if (_knownStrings.TryGetValue(formatString, out WellKnownString wellKnown))
                {
                    if (enableDefaultMessages)
                    {
                        defaultInteresting = _defaultInterestingMessages.Contains((byte)wellKnown);
                    }
                }
                else
                {
                    wellKnown = WellKnownString.NOT_INTERESTING;
                }

                if (_userInterestingStrings.Contains(formatString))
                {
                    return new StringCacheEntry(true, wellKnown);
                }

                foreach (string prefix in _userStringPrefixes)
                {
                    if (formatString.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return new StringCacheEntry(true, wellKnown);
                    }
                }

                return new StringCacheEntry(defaultInteresting, wellKnown);
            }

            int GetCacheIndex(TargetPointer address)
            {
                ulong cumulativeSize = 0;
                foreach (StressLogHeader.ModuleDesc module in moduleTable)
                {
                    if (address >= module.baseAddr && address < module.baseAddr + module.size)
                    {
                        ulong moduleOffset = address - module.baseAddr;
                        return (int)(cumulativeSize + moduleOffset);
                    }
                    else
                    {
                        cumulativeSize += module.size;
                    }
                }
                throw new ArgumentOutOfRangeException(nameof(formatStringPointer));
            }
        }

        public bool IsWellKnown(TargetPointer formatStringPointer, out WellKnownString wellKnownString)
        {
            return _knownStrings.TryGetValue(target.ReadUtf8String(formatStringPointer), out wellKnownString);
        }
    }
}
