// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartChar
{
    char c = 'c';

    public static int Main(string[] args)
    {
        // check args
        if(args.Length != 1)
        {
            Console.WriteLine("USAGE: ThreadStartChar <char>|int\n");
            return -1;
        }

        char cToPass = 'c';
        // check for max or min
        if(args[0].ToLower() == "max")
            cToPass = Char.MaxValue;
        else if(args[0].ToLower() == "min")
            cToPass = Char.MinValue;
        // check if it is a char or an int
        else if(args[0].Length > 1)
            cToPass = Convert.ToChar(Convert.ToInt32(args[0]));
        else
            cToPass = Convert.ToChar(args[0]);
        ThreadStartChar tss = new ThreadStartChar();
        return tss.Run(cToPass);
    }

    private int Run(char cPass)
    {
        Thread t = new Thread(new ParameterizedThreadStart(ThreadWorker));
        t.Start(cPass);
        t.Join();
        Console.WriteLine(c == cPass ? "Test Passed" : "Test Failed");
        return (c == cPass ? 100 : -1);
    }

    private void ThreadWorker(Object o)
    {
        c = (char)o;
        Console.WriteLine(c);
    }
}