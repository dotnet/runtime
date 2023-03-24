// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//Testing small for loops (less than 5)

using System;
using Xunit;

public class SmallLoop1
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool passed = true;
        int j;

        //i++
        j = 2;
        for (int i = 0; i == 4; i++) j++;
        if (j != 2)
        {
            Console.WriteLine("testcase 1.1 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 1; i < 5; i++) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 1.2 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 2; i <= 6; i++) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 1.3 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 3; -i > -7; i++) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 1.4 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 4; -i >= -8; i++) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 1.5 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 5; j != 6; i++) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 1.6 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        //i+=1
        j = 2;
        for (int i = 0; i == 4; i += 1) j++;
        if (j != 2)
        {
            Console.WriteLine("testcase 2.1 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 6; i < 10; i += 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 2.2 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 7; i <= 11; i += 1) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 2.3 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 8; -i > -12; i += 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 2.4 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 9; -i >= -13; i += 1) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 2.5 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 10; j != 6; i += 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 2.6 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        //i--
        j = 2;
        for (int i = 4; i == 0; i--) j++;
        if (j != 2)
        {
            Console.WriteLine("testcase 3.1 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 16; -i < -12; i--) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 3.2 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 15; -i <= -11; i--) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 3.3 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 14; i > 10; i--) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 3.4 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 13; i >= 9; i--) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 3.5 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 12; j != 8; i--) j++;
        if (j != 8)
        {
            Console.WriteLine("testcase 3.6 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        //i-=1
        j = 2;
        for (int i = 1; i == 0; i -= 1) j++;
        if (j != 2)
        {
            Console.WriteLine("testcase 4.1 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 2; -i < -2; i -= 1) j++;
        if (j != 2)
        {
            Console.WriteLine("testcase 4.2 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 3; -i <= -1; i -= 1) j++;
        if (j != 5)
        {
            Console.WriteLine("testcase 4.3 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 4; i > 0; i -= 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 4.4 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 5; i >= 1; i -= 1) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 4.5 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 6; j != 6; i -= 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 4.6 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        //i*=2
        j = 2;
        for (int i = 0; i == 4; i *= 2) j++;
        if (j != 2)
        {
            Console.WriteLine("testcase 5.1 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 1; i < 8; i *= 2) j++;
        if (j != 5)
        {
            Console.WriteLine("testcase 5.2 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 2; i <= 32; i *= 2) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 5.3 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 2; -i > -32; i *= 2) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 5.4 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 1; -i >= -8; i *= 2) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 5.5 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 6; j != 6; i *= 2) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 5.6 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        //i/=3
        j = 2;
        for (int i = 4; i == 4; i /= 3) j++;
        if (j != 3)
        {
            Console.WriteLine("testcase 6.1 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 81; -i < -1; i /= 3) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 6.2 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 81; -i <= -1; i /= 3) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 6.3 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 81; i > 1; i /= 3) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 6.4 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 81; i >= 1; i /= 3) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 6.5 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 6; j != 6; i /= 3) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 6.6 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        //i*=2
        j = 2;
        for (int i = 0; i == 4; i <<= 1) j++;
        if (j != 2)
        {
            Console.WriteLine("testcase 7.1 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 1; i < 8; i <<= 1) j++;
        if (j != 5)
        {
            Console.WriteLine("testcase 7.2 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 2; i <= 32; i <<= 1) j++;
        if (j != 7)
        {
            Console.WriteLine("testcase 7.3 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 2; -i > -32; i <<= 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 7.4 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 1; -i >= -8; i <<= 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 7.5 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 6; j != 6; i <<= 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 7.6 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        //i/=3
        j = 2;
        for (int i = 4; i == 4; i >>= 1) j++;
        if (j != 3)
        {
            Console.WriteLine("testcase 8.1 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 81; -i < -1; i >>= 1) j++;
        if (j != 8)
        {
            Console.WriteLine("testcase 8.2 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 81; -i <= -1; i >>= 1) j++;
        if (j != 9)
        {
            Console.WriteLine("testcase 8.3 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 81; i > 1; i >>= 1) j++;
        if (j != 8)
        {
            Console.WriteLine("testcase 8.4 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 81; i >= 1; i >>= 1) j++;
        if (j != 9)
        {
            Console.WriteLine("testcase 8.5 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }
        j = 2;
        for (int i = 6; j != 6; i >>= 1) j++;
        if (j != 6)
        {
            Console.WriteLine("testcase 8.6 failed");
            Console.WriteLine("j is {0}", j);
            passed = false;
        }

        if (!passed)
        {
            Console.WriteLine("FAILED");
            return 1;
        }
        else
        {
            Console.WriteLine("PASSED");
            return 100;
        }
    }
}




