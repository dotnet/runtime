// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

internal struct VT
{
    public String str;
    public char b0, b1, b2, b3, b4, b5, b6;
}

internal class CL
{
    public String str = "test string";
    public char b0, b1, b2, b3, b4, b5, b6;
}

public class StrAccess1
{
    public static String str1 = "test string";
    public static String[,] str2darr = { { "test string" } };
    public static char sb0, sb1, sb2, sb3, sb4, sb5, sb6;
    public static String f(ref String arg)
    {
        return arg;
    }

    public const int DefaultSeed = 20010415;
    public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    public static Random rand = new Random(Seed);
    [Fact]
    public static int TestEntryPoint()
    {
        bool passed = true;
        String str2 = "test string";
        String[] str1darr = { "string access", "test string" };
        Char[,] c2darr = { { '0', '1', '2', '3', '4', '5', '6' }, { 'a', 'b', 'c', 'd', 'e', 'f', 'g' } };
        CL cl1 = new CL();
        VT vt1;
        vt1.str = "test string";
        char b0, b1, b2, b3, b4, b5, b6;
        //accessing the strings at different indices. assign to local char
        b0 = str2[0];
        b1 = str1[0];
        b2 = cl1.str[0];
        b3 = vt1.str[0];
        b4 = str1darr[1][0];
        b5 = str2darr[0, 0][0];
        b6 = f(ref str2)[0];
        if ((b0 != b1) || (b1 != b2) || (b2 != b3) || (b3 != b4) || (b4 != b5) || (b5 != b6))
            passed = false;
        if ((str2[4] != str1[4]) || (str1[4] != cl1.str[4]) || (cl1.str[4] != vt1.str[4]) || (vt1.str[4] != str1darr[1][4]) || (str1darr[1][4] != str2darr[0, 0][4]) || (str2darr[0, 0][4] != f(ref str2)[4]))
            passed = false;
        b0 = str2[10];
        b1 = str1[10];
        b2 = cl1.str[10];
        b3 = vt1.str[10];
        b4 = str1darr[1][10];
        b5 = str2darr[0, 0][10];
        b6 = f(ref str2)[10];
        if ((b0 != b1) || (b1 != b2) || (b2 != b3) || (b3 != b4) || (b4 != b5) || (b5 != b6))
            passed = false;
        int j = rand.Next(0, 10);
        b0 = str2[j];
        b1 = str1[j];
        b2 = cl1.str[j];
        b3 = vt1.str[j];
        b4 = str1darr[1][j];
        b5 = str2darr[0, 0][j];
        b6 = f(ref str2)[j];
        if ((b0 != b1) || (b1 != b2) || (b2 != b3) || (b3 != b4) || (b4 != b5) || (b5 != b6))
            passed = false;

        //accessing the strings at different indices, assign to static char
        sb0 = str2[1];
        sb1 = str1[1];
        sb2 = cl1.str[1];
        sb3 = vt1.str[1];
        sb4 = str1darr[1][1];
        sb5 = str2darr[0, 0][1];
        sb6 = f(ref str2)[1];
        if ((sb0 != sb1) || (sb1 != sb2) || (sb2 != sb3) || (sb3 != sb4) || (sb4 != sb5) || (sb5 != sb6))
            passed = false;
        if ((str2[5] != str1[5]) || (str1[5] != cl1.str[5]) || (cl1.str[5] != vt1.str[5]) || (vt1.str[5] != str1darr[1][5]) || (str1darr[1][5] != str2darr[0, 0][5]) || (str2darr[0, 0][5] != f(ref str2)[5]))
            passed = false;
        sb0 = str2[9];
        sb1 = str1[9];
        sb2 = cl1.str[9];
        sb3 = vt1.str[9];
        sb4 = str1darr[1][9];
        sb5 = str2darr[0, 0][9];
        sb6 = f(ref str2)[9];
        if ((sb0 != sb1) || (sb1 != sb2) || (sb2 != sb3) || (sb3 != sb4) || (sb4 != sb5) || (sb5 != sb6))
            passed = false;
        j = rand.Next(0, 10);
        sb0 = str2[j];
        sb1 = str1[j];
        sb2 = cl1.str[j];
        sb3 = vt1.str[j];
        sb4 = str1darr[1][j];
        sb5 = str2darr[0, 0][j];
        sb6 = f(ref str2)[j];
        if ((sb0 != sb1) || (sb1 != sb2) || (sb2 != sb3) || (sb3 != sb4) || (sb4 != sb5) || (sb5 != sb6))
            passed = false;

        //accessing the strings at different indices, assign to VT char
        vt1.b0 = str2[2];
        vt1.b1 = str1[2];
        vt1.b2 = cl1.str[2];
        vt1.b3 = vt1.str[2];
        vt1.b4 = str1darr[1][2];
        vt1.b5 = str2darr[0, 0][2];
        vt1.b6 = f(ref str2)[2];
        if ((vt1.b0 != vt1.b1) || (vt1.b1 != vt1.b2) || (vt1.b2 != vt1.b3) || (vt1.b3 != vt1.b4) || (vt1.b4 != vt1.b5) || (vt1.b5 != vt1.b6))
            passed = false;
        if ((str2[6] != str1[6]) || (str1[6] != cl1.str[6]) || (cl1.str[6] != vt1.str[6]) || (vt1.str[6] != str1darr[1][6]) || (str1darr[1][6] != str2darr[0, 0][6]) || (str2darr[0, 0][6] != f(ref str2)[6]))
            passed = false;
        vt1.b0 = str2[8];
        vt1.b1 = str1[8];
        vt1.b2 = cl1.str[8];
        vt1.b3 = vt1.str[8];
        vt1.b4 = str1darr[1][8];
        vt1.b5 = str2darr[0, 0][8];
        vt1.b6 = f(ref str2)[8];
        if ((vt1.b0 != vt1.b1) || (vt1.b1 != vt1.b2) || (vt1.b2 != vt1.b3) || (vt1.b3 != vt1.b4) || (vt1.b4 != vt1.b5) || (vt1.b5 != vt1.b6))
            passed = false;
        j = rand.Next(0, 10);
        vt1.b0 = str2[j];
        vt1.b1 = str1[j];
        vt1.b2 = cl1.str[j];
        vt1.b3 = vt1.str[j];
        vt1.b4 = str1darr[1][j];
        vt1.b5 = str2darr[0, 0][j];
        vt1.b6 = f(ref str2)[j];
        if ((vt1.b0 != vt1.b1) || (vt1.b1 != vt1.b2) || (vt1.b2 != vt1.b3) || (vt1.b3 != vt1.b4) || (vt1.b4 != vt1.b5) || (vt1.b5 != vt1.b6))
            passed = false;

        //accessing the strings at different indices, assign to CL char
        cl1.b0 = str2[7];
        cl1.b1 = str1[7];
        cl1.b2 = cl1.str[7];
        cl1.b3 = vt1.str[7];
        cl1.b4 = str1darr[1][7];
        cl1.b5 = str2darr[0, 0][7];
        cl1.b6 = f(ref str2)[7];
        if ((cl1.b0 != cl1.b1) || (cl1.b1 != cl1.b2) || (cl1.b2 != cl1.b3) || (cl1.b3 != cl1.b4) || (cl1.b4 != cl1.b5) || (cl1.b5 != cl1.b6))
            passed = false;
        if ((str2[0] != str1[0]) || (str1[0] != cl1.str[0]) || (cl1.str[0] != vt1.str[0]) || (vt1.str[0] != str1darr[1][0]) || (str1darr[1][0] != str2darr[0, 0][0]) || (str2darr[0, 0][0] != f(ref str2)[0]))
            passed = false;
        cl1.b0 = str2[4];
        cl1.b1 = str1[4];
        cl1.b2 = cl1.str[4];
        cl1.b3 = vt1.str[4];
        cl1.b4 = str1darr[1][4];
        cl1.b5 = str2darr[0, 0][4];
        cl1.b6 = f(ref str2)[4];
        if ((cl1.b0 != cl1.b1) || (cl1.b1 != cl1.b2) || (cl1.b2 != cl1.b3) || (cl1.b3 != cl1.b4) || (cl1.b4 != cl1.b5) || (cl1.b5 != cl1.b6))
            passed = false;
        j = rand.Next(0, 10);
        cl1.b0 = str2[j];
        cl1.b1 = str1[j];
        cl1.b2 = cl1.str[j];
        cl1.b3 = vt1.str[j];
        cl1.b4 = str1darr[1][j];
        cl1.b5 = str2darr[0, 0][j];
        cl1.b6 = f(ref str2)[j];
        if ((cl1.b0 != cl1.b1) || (cl1.b1 != cl1.b2) || (cl1.b2 != cl1.b3) || (cl1.b3 != cl1.b4) || (cl1.b4 != cl1.b5) || (cl1.b5 != cl1.b6))
            passed = false;

        //accessing the strings at different indices, assign to 2d array char
        c2darr[1, 0] = str2[6];
        c2darr[1, 1] = str1[6];
        c2darr[1, 2] = cl1.str[6];
        c2darr[1, 3] = vt1.str[6];
        c2darr[1, 4] = str1darr[1][6];
        c2darr[1, 5] = str2darr[0, 0][6];
        c2darr[1, 6] = f(ref str2)[6];
        if ((c2darr[1, 0] != c2darr[1, 1]) || (c2darr[1, 1] != c2darr[1, 2]) || (c2darr[1, 2] != c2darr[1, 3]) || (c2darr[1, 3] != c2darr[1, 4]) || (c2darr[1, 4] != c2darr[1, 5]) || (c2darr[1, 5] != c2darr[1, 6]))
            passed = false;
        if ((str2[0] != str1[0]) || (str1[0] != cl1.str[0]) || (cl1.str[0] != vt1.str[0]) || (vt1.str[0] != str1darr[1][0]) || (str1darr[1][0] != str2darr[0, 0][0]) || (str2darr[0, 0][0] != f(ref str2)[0]))
            passed = false;
        c2darr[1, 0] = str2[6];
        c2darr[1, 1] = str1[6];
        c2darr[1, 2] = cl1.str[6];
        c2darr[1, 3] = vt1.str[6];
        c2darr[1, 4] = str1darr[1][6];
        c2darr[1, 5] = str2darr[0, 0][6];
        c2darr[1, 6] = f(ref str2)[6];
        if ((c2darr[1, 0] != c2darr[1, 1]) || (c2darr[1, 1] != c2darr[1, 2]) || (c2darr[1, 2] != c2darr[1, 3]) || (c2darr[1, 3] != c2darr[1, 4]) || (c2darr[1, 4] != c2darr[1, 5]) || (c2darr[1, 5] != c2darr[1, 6]))
            passed = false;
        j = rand.Next(0, 10);
        c2darr[1, 0] = str2[j];
        c2darr[1, 1] = str1[j];
        c2darr[1, 2] = cl1.str[j];
        c2darr[1, 3] = vt1.str[j];
        c2darr[1, 4] = str1darr[1][j];
        c2darr[1, 5] = str2darr[0, 0][j];
        c2darr[1, 6] = f(ref str2)[j];
        if ((c2darr[1, 0] != c2darr[1, 1]) || (c2darr[1, 1] != c2darr[1, 2]) || (c2darr[1, 2] != c2darr[1, 3]) || (c2darr[1, 3] != c2darr[1, 4]) || (c2darr[1, 4] != c2darr[1, 5]) || (c2darr[1, 5] != c2darr[1, 6]))
            passed = false;

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




