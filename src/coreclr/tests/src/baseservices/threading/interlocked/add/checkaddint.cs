// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class CheckAddInt
{
    public static int Main(string[] args)
    {
        // Check number of args
        if(args.Length != 2)
        {
            Console.WriteLine("USAGE:  CheckAddInt " +
                "/start:<int> /add:<int>");
            return -1;
        }

        // Get the args
        int iStart=0;
        int iAdd = 0;
        
        for(int i=0;i<args.Length;i++)
        {
            if(args[i].ToLower().StartsWith("/start:"))
            {
                iStart = Convert.ToInt32(args[i].Substring(7));
                continue;
            }

            if(args[i].ToLower().StartsWith("/add:"))
            {
                iAdd = Convert.ToInt32(args[i].Substring(5));
                continue;
            }
        }

        CheckAddInt cai = new CheckAddInt();
        return cai.Run(iStart, iAdd);
    }

    private int Run(int iStart, int iAdd)
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
                return -1;
            }
			Console.WriteLine(iNew);
        }

        Console.WriteLine("Test Passed");
        return 100;
    }
}