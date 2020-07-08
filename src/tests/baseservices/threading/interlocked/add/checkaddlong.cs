// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
                "/start:<long> /add:<int>");
            return -1;
        }

        // Get the args
        long iStart=0;
        int iAdd = 0;
        
        for(int i=0;i<args.Length;i++)
        {
            if(args[i].ToLower().StartsWith("/start:"))
            {
                iStart = Convert.ToInt64(args[i].Substring(7));
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

    private int Run(long iStart, int iAdd)
    {
        long iNew = 0;
		long iNewExpected;
        long iTotal = iStart;
        for(int i=0;i<iAdd;i++)
        {
			iNewExpected = iTotal + (i * (i + 1));
            iNew = Interlocked.Add(ref iTotal, (i * (i + 1)));
     
            if((iNew != iNewExpected) || (iNew != iTotal))
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
