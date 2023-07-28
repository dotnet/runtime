// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Threading.Tasks.Dataflow.Tests
{
    internal static partial class DataflowTestHelpers
    {
        internal static Func<int, IAsyncEnumerable<int>> ToAsyncEnumerable = item => AsyncEnumerable.Repeat(item, 1);
    }

    internal static partial class AsyncEnumerable
    {
        internal static async IAsyncEnumerable<int> Repeat(int item, int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Yield();
                yield return item;
            }
        }

        internal static async IAsyncEnumerable<int> Range(int start, int count)
        {
            var end = start + count;
            for (int i = start; i < end; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }

        internal static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable)
            {
                await Task.Yield();
                yield return item;
            }
        }
    }
}
