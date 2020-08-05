// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

class Test
{
    const int count = 100000;
    const int times = 10;

    public static int Main()
    {
        for (int i = 0; i < 10; i++)
        {
            var sw = Stopwatch.StartNew();
            var dic = new Dictionary<string, bool>(count * 3 / 2);
            TestDict(dic, i => "0x" + (count + i));
            Console.WriteLine(sw.Elapsed.ToString());
        }

        return 100;
    }

    static void TestDict<T>(IDictionary<T, bool> dic, Func<int, T> src)
    {
        var exists = Enumerable.Range(0, count).Select(i => src(i)).ToArray();
        var notExists = Enumerable.Range(count, count).Select(i => src(i)).ToArray();

        for (int i = 0; i < exists.Length; i++)
        {
            var item = exists[i];
            dic.Add(item, true);
        }

        for (int t = 0; t < times; t++)
        {
            for (int i = 0; i < exists.Length; i++)
            {
                var item = exists[i];
                dic.ContainsKey(item);
            }
        }
    }
}
