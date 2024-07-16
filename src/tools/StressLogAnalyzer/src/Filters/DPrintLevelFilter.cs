// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Filters;

internal sealed class DPrintLevelFilter(IMessageFilter innerFilter, IReadOnlyList<IntegerRange> levels) : IMessageFilter
{
    private static uint GcLogLevel(uint facility)
    {
        if ((facility & ((uint)LogFacility.LF_ALWAYS | 0xfffeu | (uint)LogFacility.LF_GC)) == (uint)(LogFacility.LF_ALWAYS | LogFacility.LF_GC))
        {
            return (facility >> 16) & 0x7fff;
        }
        return 0;
    }

    public bool IncludeMessage(StressMsgData message)
    {
        uint level = GcLogLevel(message.Facility);
        return levels.Any(filter => filter.Start <= level && level <= filter.End)
            || innerFilter.IncludeMessage(message);
    }
}
