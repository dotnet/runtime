// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public static class HelperExtensions
    {
        public static IEnumerable<object?[]> UnwrapItemsAsArrays(this IEnumerable<IEnumerable<object?>> enumerable)
            => enumerable.Select(e => e.ToArray());

        public static IEnumerable<object?[]> Dump(this IEnumerable<object?[]> enumerable)
        {
            foreach (var row in enumerable)
            {
                Console.WriteLine ("{");
                foreach (var param in row)
                    Console.WriteLine ($"\t{param}");
                Console.WriteLine ("}");
            }
            return enumerable;
        }

        /// <summary>
        /// Cartesian product
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
        /// <param name="rowsWithColumnArrays"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<object?>> Multiply(this IEnumerable<IEnumerable<object?>> data, params object?[][] rowsWithColumnArrays)
            => data.SelectMany(row =>
                        rowsWithColumnArrays.Select(new_cols => row.Concat(new_cols)));

        public static object?[] Enumerate(this RunHost host)
        {
            if (host == RunHost.None)
                return Array.Empty<object?>();

            var list = new List<object?>();
            foreach (var value in Enum.GetValues<RunHost>())
            {
                if (value == RunHost.None)
                    continue;

                if (value == RunHost.V8 && OperatingSystem.IsWindows())
                {
                    // Don't run tests with V8 on windows
                    continue;
                }

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
            if (hosts == RunHost.None)
                return data.Select(d => d.Append((object?) Path.GetRandomFileName()));

            return data.SelectMany(d =>
            {
                string runId = Path.GetRandomFileName();
                return hostsEnumerable.Select(o =>
                        d.Append((object?)o)
                         .Append((object?)runId));
            });
        }

        public static void UpdateTo(this IDictionary<string, (string fullPath, bool unchanged)> dict, bool unchanged, params string[] filenames)
        {
            IEnumerable<string> keys = filenames.Length == 0 ? dict.Keys.ToList() : filenames;

            foreach (var filename in keys)
            {
                if (!dict.TryGetValue(filename, out var oldValue))
                {
                    StringBuilder sb = new();
                    sb.AppendLine($"Cannot find key named {filename} in the dict. Existing ones:");
                    foreach (var kvp in dict)
                        sb.AppendLine($"[{kvp.Key}] = [{kvp.Value}]");

                    throw new KeyNotFoundException(sb.ToString());
                }

                dict[filename] = (oldValue.fullPath, unchanged);
            }
        }
    }
}
