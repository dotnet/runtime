// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Linq.Parallel.Tests
{
    internal static class Sources
    {
        public static IEnumerable<object[]> Ranges(int start, IEnumerable<int> counts)
        {
            foreach (object[] parms in UnorderedSources.Ranges(start, counts))
            {
                yield return new object[] { ((Labeled<ParallelQuery<int>>)parms[0]).Order(), parms[1] };
            }
        }

        // Wrapper for attribute calls
        public static IEnumerable<object[]> Ranges(int[] counts)
        {
            foreach (object[] parms in Ranges(counts.Cast<int>())) yield return parms;
        }

        public static IEnumerable<object[]> Ranges(IEnumerable<int> counts)
        {
            foreach (object[] parms in Ranges(0, counts)) yield return parms;
        }

        public static IEnumerable<object[]> Ranges<T>(IEnumerable<int> counts, params Func<int, IEnumerable<T>>[] modifiers)
        {
            foreach (object[] parms in UnorderedSources.Ranges(counts, modifiers))
            {
                parms[0] = ((Labeled<ParallelQuery<int>>)parms[0]).Order();
                yield return parms;
            }
        }
    }
}
