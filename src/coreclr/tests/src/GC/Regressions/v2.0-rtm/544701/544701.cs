// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// originally a regression test for VSWhidbey 158720

using System;
internal class AllocBug
{
    public int ret = 0;
    public AllocBug()
    {
    }

    private static int Main(string[] args)
    {
        AllocBug ab = new AllocBug();

        ab.RunTest(41938869, 41943020);
        Console.WriteLine(100 == ab.ret ? "Test Passed" : "Test Failed");
        return ab.ret;
    }
    private void RunTest(int start, int stop)
    {
        for (int i = start; i <= stop; i++)
        {
            Allocate(i);
            if (0 != ret)
                break;
        }

        if (0 == ret)
            ret = 100;
    }

    private void Allocate(int bytesToAlloc)
    {
        try
        {
            Console.Write("Allocating ");
            Console.Write(bytesToAlloc);
            Console.Write(" bytes... ");

            byte[] buffer = new byte[bytesToAlloc];

            Console.WriteLine("Passed");
        }
        catch (Exception)
        {
            Console.WriteLine("Unexpected Exception: ");
            ret = -1;
        }
    }
}
