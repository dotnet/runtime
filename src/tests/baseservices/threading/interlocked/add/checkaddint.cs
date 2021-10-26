// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

class CheckAddInt
{
    [Theory]
    [InlineData(2147483647, 100)]
    [InlineData(0, 100)]
    public static void Run(int iStart, int iAdd)
    {
        int iNew = 0;
		int iNewExpected;
        int iTotal = iStart;
        for(int i=0;i<iAdd;i++)
        {
			iNewExpected = iTotal + (i * (i + 1));
			iNew = Interlocked.Add(ref iTotal, (i * (i + 1)));

			if ((iNew != iNewExpected) || (iNew != iTotal))
            {
				Console.WriteLine(iNew + " " + iNewExpected + " " + iTotal);
                Console.WriteLine("Test Failed");
                Assert.False((iNew != iNewExpected) || (iNew != iTotal));
            }
			Console.WriteLine(iNew);
        }

        Console.WriteLine("Test Passed");
    }
}
