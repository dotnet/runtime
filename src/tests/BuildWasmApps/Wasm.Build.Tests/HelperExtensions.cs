// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Wasm.Build.Tests
{
    public static class HelperExtensions
    {
        public static IEnumerable<object?[]> UnwrapItemsAsArrays(this IEnumerable<IEnumerable<object?>> enumerable)
            => enumerable.Select(e => e.ToArray());

        /// <summary>
        /// Cartesian product
        ///
        /// Given:
        ///
        /// Say we want to provide test data for:
        ///     [MemberData(nameof(TestData))]
        ///     public void Test(string name, int num) { }
        ///
        /// And we want to test with `names = object[] { "Name0", "Name1" }`
        ///
        /// And for each of those names, we want to test with some numbers,
        ///   say `numbers = object[] { 1, 4 }`
        ///
        /// So, we want the final test data to be:
        ///
        ///     { "Name0", 1 }
        ///     { "Name0", 4 }
        ///     { "Name1", 1 }
        ///     { "Name1", 4 }
        ///
        /// Then we can use: names.Combine(numbers)
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<object?>> Multiply(this IEnumerable<IEnumerable<object?>> data, object?[] options)
            => data?.SelectMany(d => options.Select(o => d.Append(o)));

        public static object?[] Enumerate(this RunHost host)
        {
            var list = new List<object?>();
            foreach (var value in Enum.GetValues<RunHost>())
            {
                // Ignore any combos like RunHost.All from Enum.GetValues
                // by ignoring any @value that has more than 1 bit set
                if (((int)value & ((int)value - 1)) != 0)
                    continue;

                if ((host & value) == value)
                    list.Add(value);
            }
            return list.ToArray();
        }

        public static IEnumerable<IEnumerable<object?>> WithRunHosts(this IEnumerable<IEnumerable<object?>> data, RunHost hosts)
        {
            IEnumerable<object?> hostsEnumerable = hosts.Enumerate();
            return data?.SelectMany(d =>
            {
                string runId = Path.GetRandomFileName();
                return hostsEnumerable.Select(o =>
                        d.Append((object?)o)
                         .Append((object?)runId));
            });
        }
    }
}
