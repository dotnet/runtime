// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_138342
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/134949", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsWasi), nameof(TestLibrary.PlatformDetection.IsReadyToRunCompiled))]
    [Fact]
    public static void Test()
    {
        int[] arr = { 1, 2, 3 };

        for (int i = 0; i < 200; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                Assert.Equal(6, Sum(arr));
            }

            Thread.Sleep(10);
        }

        Assert.Equal(6, Sum(arr));
    }

    // The enumerator is allocated inside a try, but is used after the try.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Sum(IEnumerable<int> e)
    {
        IEnumerator<int> en;
        try
        {
            en = e.GetEnumerator();
        }
        catch
        {
            return -1;
        }

        int sum = 0;
        while (en.MoveNext())
        {
            sum += en.Current;
        }

        return sum;
    }
}
