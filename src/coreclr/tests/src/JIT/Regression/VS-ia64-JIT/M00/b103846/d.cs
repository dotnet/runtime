// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

class Bug
{
    static short s1 = 8712, s2 = -973;
    public static int Main()
    {
        short s3 = (short)(s1 / s2);
        short s4 = (short)(s1 % s2);
        System.Console.WriteLine(s3);
        System.Console.WriteLine(s4);
        return 100;
    }
}