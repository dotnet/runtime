// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

class Test
{
    /// <summary>
    /// Another 64 bit optimization issue where we dont do the coversion correctly. The following output is seen when this program fails
    /// Error! expected, -4.54403989493052E+18, returned: -66876.654654
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static int Main(string[] args)
    {
        double expected = -4.54403989493052E+18;
        double value = -66876.654654;
        double result = (double)BitConverter.DoubleToInt64Bits(value);
        if (result > -4.5E18)
        {
            Console.WriteLine("Error! expected, {0}, returned: {1}", expected, result);
            return -1;
        }
        else
        {
            Console.WriteLine("Pass");
            return 100;
        }
    }
}
