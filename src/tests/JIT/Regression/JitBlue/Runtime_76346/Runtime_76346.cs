// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        int i = 0;

        try
        {
            Console.WriteLine("Running... {0}", i);
        }
        finally
        {
            // Don't want this to be cloned, so add more EH
            Console.WriteLine("In finally1");
            try
            {
                Console.WriteLine("try2... {0}", i);
            }
            finally
            {
                Console.WriteLine("finally2... {0}", i);
            }
        }
        do
        {
            do
            {
                ++i;
            } while (i % 19 != 0);
        } while (i < 40);

        return (i == 57) ? 100 : 101;
    }
}
