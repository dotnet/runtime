// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Filters;

internal sealed class FacilityMessageFilter(IMessageFilter inner, ulong ignoreFacility) : IMessageFilter
{
    public bool IncludeMessage(StressMsgData message)
    {
        // Filter out messages by log facility immediately.
        if (ignoreFacility != 0)
        {
            if ((message.Facility & ((uint)LogFacility.ALWAYS | 0xfffe | (uint)LogFacility.GC)) == ((uint)LogFacility.ALWAYS | (uint)LogFacility.GC))
            {
                // specially encoded GC message including dprintf level
                if ((ignoreFacility & (uint)LogFacility.GC) != 0)
                {
                    return false;
                }
            }
            else if ((ignoreFacility & message.Facility) != 0)
            {
                return false;
            }
        }

        return inner.IncludeMessage(message);
    }
}
