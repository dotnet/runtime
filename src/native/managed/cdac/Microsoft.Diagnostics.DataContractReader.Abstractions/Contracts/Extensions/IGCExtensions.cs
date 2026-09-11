// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;

public static class IGCExtensions
{
    public static IEnumerable<(GCHeapSegmentInfo Segment, GCHeapData Heap)> EnumerateAllSegments(this IGC gc)
    {
        string[] gcIdentifiers = gc.GetGCIdentifiers();
        bool isWorkstation = gcIdentifiers.Contains(GCIdentifiers.Workstation);
        foreach (GCHeapData heap in EnumerateHeaps(gc, isWorkstation))
        {
            foreach (GCHeapSegmentInfo seg in gc.EnumerateHeapSegments(heap))
            {
                yield return (seg, heap);
            }
        }
    }

    private static IEnumerable<GCHeapData> EnumerateHeaps(IGC gc, bool isWorkstation)
    {
        if (isWorkstation)
        {
            yield return gc.GetHeapData();
        }
        else
        {
            foreach (TargetPointer heapAddress in gc.GetGCHeaps())
                yield return gc.GetHeapData(heapAddress);
        }
    }
}
