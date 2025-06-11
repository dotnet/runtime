// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;

internal static class BinaryThenLinearSearch
{
    private const uint BinarySearchCountThreshold = 10;

    public static bool Search(
        uint start,
        uint end,
        Func<uint, bool> compare,
        Func<uint, bool> match,
        out uint index)
    {
        // Binary search until we get to a fewer than the threshold number of items.
        while (end - start > BinarySearchCountThreshold)
        {
            uint middle = start + (end - start) / 2;
            if (compare(middle))
            {
                end = middle - 1;
            }
            else
            {
                start = middle;
            }
        }

        // Linear search over the remaining items
        for (uint i = start; i <= end; ++i)
        {
            if (!match(i))
                continue;

            index = i;
            return true;
        }

        index = ~0u;
        return false;
    }
}
