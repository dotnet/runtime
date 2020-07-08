// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal class TEST
{
    public static int Main()
    {
        Test();
        return 100;
    }
    public static void Test()
    {
        int SSS;
        try
        {
            goto LB1;
        LB7:
            goto LB4;
        LB1:
            SSS = 0;
            goto LB9;
        LB3:
            goto LB4;
        LB4:
            goto LB13;
        LB9:
            switch (SSS)
            {
                case 0:
                    goto LB7;
                case 1:
                    goto LB3;
                case 2:
                    goto LB4;
            }
            goto LB13;
        }
        finally
        {
        }
    LB13:
        System.Console.WriteLine("TEST SUCCESS");
    }
}

