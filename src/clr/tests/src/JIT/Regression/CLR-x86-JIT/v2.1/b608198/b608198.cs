// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
 QFE regression TC for AV while optimizing away basic blocks that 
 are not used which contain switch statements.
*/

class TEST
{
    public static int Main()
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
        System.Console.WriteLine("END");

        System.Console.WriteLine("!!!!!!!!!!!!! PASSED !!!!!!!!!!!!!");
        return 100;
    }
}

