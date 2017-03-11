// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

//Takes a command line parameter to sleep a specified time in milliseconds
public class Sleep
{
    public static void Main(string[] args)
    {
        try
        {
            if (args.Length == 1)
            {
                int sleeptime = Int32.Parse(args[0]);
                Thread.Sleep(sleeptime);
            }
            else
            {
                throw new ArgumentException();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Usage: sleep.exe [timeout] where [timeout] is in milliseconds");
            Console.WriteLine(e.Message);
        }
    }
}
