// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Filters;

internal sealed class ValueRangeFilter(IMessageFilter inner, IInterestingStringFinder stringFinder, IntegerRange[] valueRanges) : IMessageFilter
{
    public bool IncludeMessage(StressMsgData message)
    {
        _ = stringFinder.IsInteresting(message.FormatString, out WellKnownString? wellKnownString);
        if (wellKnownString is null)
        {
            return inner.IncludeMessage(message);
        }

        IReadOnlyList<TargetPointer> args = message.Args;

        switch (wellKnownString)
        {
            case WellKnownString.PLAN_PLUG:
            case WellKnownString.PLAN_PINNED_PLUG:
            {
                ulong gapSize = args[0];
                ulong plugStart = args[1];
                ulong gapStart = plugStart - gapSize;
                ulong plugEnd = args[2];
                return RangeIsInteresting(gapStart, plugEnd);
            }
            case WellKnownString.GCMEMCOPY:
                return RangeIsInteresting(args[0], args[2]) || RangeIsInteresting(args[1], args[3]);
            case WellKnownString.MAKE_UNUSED_ARRAY:
                return RangeIsInteresting(args[0], args[1]);
            case WellKnownString.RELOCATE_REFERENCE:
            {
                ulong src = args[0];
                ulong destFrom = args[1];
                ulong destTo = args[2];

                foreach (IntegerRange filter in valueRanges)
                {
                    if ((filter.End < src || src > filter.Start)
                        && (filter.End < destFrom || destFrom > filter.Start)
                        && (filter.End < destTo || destTo > filter.Start))
                    {
                        continue;
                    }
                    return true;
                }

                return false;
            }
        }
        return false;
    }

    private bool RangeIsInteresting(ulong start, ulong end)
    {
        foreach (IntegerRange filter in valueRanges)
        {
            if (filter.End < start || end > filter.Start)
            {
                continue;
            }
            return true;
        }

        return false;
    }
}
