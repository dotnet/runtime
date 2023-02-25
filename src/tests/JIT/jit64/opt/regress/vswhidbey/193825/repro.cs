// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;


public unsafe class T
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (Bug() == "0") return 100;

        return 1;
    }
    public static string Bug()
    {
        int maxSize, sizeInt, sizeFract;

        sizeInt = return_int(false, 0);
        sizeFract = return_int(false, 1) - return_int(false, -2);

        maxSize = sizeInt + sizeFract + 4;

        char* pBuf = stackalloc char[maxSize];
        char* pch = pBuf;

        *pch++ = '0';
        return new string(pBuf, 0, (int)(pch - pBuf));
    }
    private static int return_int(bool verbose, int input)
    {
        int ans;

        try
        {
            ans = input;
        }
        finally
        {
            if (verbose)
            {
                Console.WriteLine("returning  : ans");
            }
        }
        return ans;
    }
}
