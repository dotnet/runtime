// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Output;

internal sealed class GCThreadNameOutput(GCThreadMap threadMap) : IThreadNameOutput
{
    public string GetThreadName(ulong threadId)
    {
        if (!threadMap.ThreadHasHeap(threadId))
        {
            return $"{threadId:x4}";
        }
        (ulong heap, bool isBackground) = threadMap.GetThreadHeap(threadId);
        string threadName = isBackground ? "BG" : "GC";
        return $"{threadName}{heap:D2}";
    }
}
