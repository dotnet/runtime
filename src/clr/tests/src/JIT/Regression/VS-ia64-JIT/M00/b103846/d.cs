// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
