// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private static IEnumerable<TSource[]> ChunkIteratorOptimized<TSource>(IEnumerable<TSource> source, int maxSize, int totalCount)
        {
            if (totalCount == 0)
                yield break;

            var index = 0;
            TSource[]? chunk = null;
            var chunkIndex = 0;
            foreach (var element in source)
            {
                chunk ??= new TSource[Math.Clamp(totalCount - index, 0, maxSize)];
                chunk[chunkIndex] = element;
                index++;

                chunkIndex = index % maxSize;
                if (chunkIndex == 0)
                {
                    yield return chunk;
                    chunk = null;
                }
            }

            if (chunkIndex > 0)
            {
                yield return chunk!;
            }
        }
    }
}