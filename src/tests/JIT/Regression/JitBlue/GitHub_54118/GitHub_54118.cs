// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class Program
{
    public static int Main()
    {
        int[] arr = new int[1];
        ref int r = ref arr[0];
        for (int i = 0; i < 2; i++)
        {
            r = 1;
            if (r != 1)
                return 101;
        }

        return 100;
    }
}
