// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;


public unsafe class T
{
    public static int Main()
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
