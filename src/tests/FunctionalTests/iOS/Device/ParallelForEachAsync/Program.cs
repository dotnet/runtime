// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Linq;

public static class Program
{
    [DllImport("__Internal")]
    public static extern void mono_ios_set_summary (string value);

    private static async Task GitHubIssue_114262_Async()
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 5,
            CancellationToken = new CancellationTokenSource().Token
        };

        var range = Enumerable.Range(1, 1000);

        for (int i = 0; i < 100; i++)
        {
            await Parallel.ForEachAsync(range, options, async (data, token) =>
            {
                for (int i = 0; i < 5; i++)
                {
                    await Task.Yield();
                    var buffer = new byte[10_000];
                    await Task.Run(() => {var _ = buffer[0];} );
                    await Task.Yield();
                }
            });
        }
    }
    
    public static async Task<int> Main(string[] args)
    {
        mono_ios_set_summary($"Starting functional test");

        await GitHubIssue_114262_Async();

        Console.WriteLine("Done!");

        return 42;
    }
}
