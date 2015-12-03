// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class foo
{
    public static int Main()
    {
        long lo = 0x01;
        lo = lo << 63;
        System.Console.WriteLine(lo >> 32);
        System.Console.WriteLine(lo >> 33);
        return 100;
    }
}