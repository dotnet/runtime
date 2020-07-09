// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

namespace TestStackOverflow3
{
    class Program
    {
        private const int MAX_RECURSIVE_CALLS = 1000000;
        static int ctr = 0;

        public static void Main()
        {
            Program ex = new Program();
            ex.Execute();
        }

        private unsafe void Execute(string arg1 = "")
        {
            long* bar = stackalloc long [1000];
            ctr++;
            if (ctr % 50 == 0)
                Console.WriteLine("Call number {0} to the Execute method", ctr);

            if (ctr <= MAX_RECURSIVE_CALLS)
                Execute(string.Format("{0}", (IntPtr)bar));

            ctr--;
        }
    }
}

