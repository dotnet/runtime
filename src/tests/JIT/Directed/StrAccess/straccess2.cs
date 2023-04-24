// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//Similar to StrAccess1, but instead of using constants, different expression is used as the index to access the string

using System;
using Xunit;

internal struct VT
{
    public String str;
    public char b0, b1, b2, b3, b4, b5, b6;
    public int i;
    public int[][] idxja;
}

internal class CL
{
    public String str = "test string";
    public char b0, b1, b2, b3, b4, b5, b6;
    public static int i = 10;
    public int[,] idx2darr = { { 5, 6 } };
}

public unsafe class StrAccess2
{
    public static String str1 = "test string";
    public static int idx1 = 2;
    public static String[,] str2darr = { { "test string" } };
    public static int[,,] idx3darr = { { { 8 } } };
    public static char sb0, sb1, sb2, sb3, sb4, sb5, sb6;
    public static String f(ref String arg)
    {
        return arg;
    }
    public static int f1(ref int arg)
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
        int* p = stackalloc int[11];
        for (int m = 0; m < 11; m++) p[m] = m;
        String str2 = "test string";
        String[] str1darr = { "string access", "test string" };
        Char[,] c2darr = { { '0', '1', '2', '3', '4', '5', '6' }, { 'a', 'b', 'c', 'd', 'e', 'f', 'g' } };
        CL cl1 = new CL();
        VT vt1;
        vt1.i = 0;
        vt1.str = "test string";
        vt1.idxja = new int[2][];
        vt1.idxja[1] = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int idx2 = 4;
        int[] idx1darr = { 3, 9, 4, 2, 6, 1, 8, 10, 5, 7, 0 };
        char b0, b1, b2, b3, b4, b5, b6;
        //accessing the strings at different indices. assign to local char
        b0 = str2[vt1.i];
        b1 = str1[vt1.i];
        b2 = cl1.str[vt1.i];
        b3 = vt1.str[vt1.i];
        b4 = str1darr[1][vt1.i];
        b5 = str2darr[0, 0][vt1.i];
        b6 = f(ref str2)[vt1.i];
        if ((b0 != b1) || (b1 != b2) || (b2 != b3) || (b3 != b4) || (b4 != b5) || (b5 != b6))
            passed = false;
        if ((str2[idx2] != str1[idx2]) || (str1[idx2] != cl1.str[idx2]) || (cl1.str[idx2] != vt1.str[idx2]) || (vt1.str[idx2] != str1darr[1][idx2]) || (str1darr[1][idx2] != str2darr[0, 0][idx2]) || (str2darr[0, 0][idx2] != f(ref str2)[idx2]))
            passed = false;
        b0 = str2[CL.i];
        b1 = str1[CL.i];
        b2 = cl1.str[CL.i];
        b3 = vt1.str[CL.i];
        b4 = str1darr[1][CL.i];
        b5 = str2darr[0, 0][CL.i];
        b6 = f(ref str2)[CL.i];
        if ((b0 != b1) || (b1 != b2) || (b2 != b3) || (b3 != b4) || (b4 != b5) || (b5 != b6))
            passed = false;
        int j = rand.Next(0, 10);
        b0 = str2[idx1darr[j]];
        b1 = str1[idx1darr[j]];
        b2 = cl1.str[idx1darr[j]];
        b3 = vt1.str[idx1darr[j]];
        b4 = str1darr[1][idx1darr[j]];
        b5 = str2darr[0, 0][idx1darr[j]];
        b6 = f(ref str2)[idx1darr[j]];
        if ((b0 != b1) || (b1 != b2) || (b2 != b3) || (b3 != b4) || (b4 != b5) || (b5 != b6))
            passed = false;

        //accessing the strings at different indices, assign to static char
        sb0 = str2[idx1 - 1];
        sb1 = str1[idx1 - 1];
        sb2 = cl1.str[idx1 - 1];
        sb3 = vt1.str[idx1 - 1];
        sb4 = str1darr[idx1 - 1][idx1 - 1];
        sb5 = str2darr[0, 0][idx1 - 1];
        sb6 = f(ref str2)[idx1 - 1];
        if ((sb0 != sb1) || (sb1 != sb2) || (sb2 != sb3) || (sb3 != sb4) || (sb4 != sb5) || (sb5 != sb6) || (sb6 != str2[1]))
            passed = false;
        if ((str2[5] != str1[5]) || (str1[5] != cl1.str[5]) || (cl1.str[5] != vt1.str[5]) || (vt1.str[5] != str1darr[1][5]) || (str1darr[1][5] != str2darr[0, 0][5]) || (str2darr[0, 0][5] != f(ref str2)[5]))
            passed = false;
        sb0 = str2[idx3darr[0, 0, 0] + 1];
        sb1 = str1[idx3darr[0, 0, 0] + 1];
        sb2 = cl1.str[idx3darr[0, 0, 0] + 1];
        sb3 = vt1.str[idx3darr[0, 0, 0] + 1];
        sb4 = str1darr[1][idx3darr[0, 0, 0] + 1];
        sb5 = str2darr[0, 0][idx3darr[0, 0, 0] + 1];
        sb6 = f(ref str2)[idx3darr[0, 0, 0] + 1];
        if ((sb0 != sb1) || (sb1 != sb2) || (sb2 != sb3) || (sb3 != sb4) || (sb4 != sb5) || (sb5 != sb6))
            passed = false;
        j = rand.Next(0, 10);
        sb0 = str2[vt1.idxja[1][j]];
        sb1 = str1[vt1.idxja[1][j]];
        sb2 = cl1.str[vt1.idxja[1][j]];
        sb3 = vt1.str[vt1.idxja[1][j]];
        sb4 = str1darr[1][vt1.idxja[1][j]];
        sb5 = str2darr[0, 0][vt1.idxja[1][j]];
        sb6 = f(ref str2)[vt1.idxja[1][j]];
        if ((sb0 != sb1) || (sb1 != sb2) || (sb2 != sb3) || (sb3 != sb4) || (sb4 != sb5) || (sb5 != sb6))
            passed = false;

        //accessing the strings at different indices, assign to VT char
        vt1.b0 = str2[idx2 - idx1];
        vt1.b1 = str1[idx2 - idx1];
        vt1.b2 = cl1.str[idx2 - idx1];
        vt1.b3 = vt1.str[idx2 - idx1];
        vt1.b4 = str1darr[1][idx2 - idx1];
        vt1.b5 = str2darr[0, 0][idx2 - idx1];
        vt1.b6 = f(ref str2)[idx2 - idx1];
        if ((vt1.b0 != vt1.b1) || (vt1.b1 != vt1.b2) || (vt1.b2 != vt1.b3) || (vt1.b3 != vt1.b4) || (vt1.b4 != vt1.b5) || (vt1.b5 != vt1.b6))
            passed = false;
        if ((str2[cl1.idx2darr[0, 1]] != str1[cl1.idx2darr[0, 1]]) || (str1[cl1.idx2darr[0, 1]] != cl1.str[cl1.idx2darr[0, 1]]) || (cl1.str[cl1.idx2darr[0, 1]] != vt1.str[cl1.idx2darr[0, 1]]) || (vt1.str[cl1.idx2darr[0, 1]] != str1darr[1][cl1.idx2darr[0, 1]]) || (str1darr[1][cl1.idx2darr[0, 1]] != str2darr[0, 0][cl1.idx2darr[0, 1]]) || (str2darr[0, 0][cl1.idx2darr[0, 1]] != f(ref str2)[cl1.idx2darr[0, 1]]))
            passed = false;
        vt1.b0 = str2[idx3darr[0, 0, 0]];
        vt1.b1 = str1[idx3darr[0, 0, 0]];
        vt1.b2 = cl1.str[idx3darr[0, 0, 0]];
        vt1.b3 = vt1.str[idx3darr[0, 0, 0]];
        vt1.b4 = str1darr[1][idx3darr[0, 0, 0]];
        vt1.b5 = str2darr[0, 0][idx3darr[0, 0, 0]];
        vt1.b6 = f(ref str2)[idx3darr[0, 0, 0]];
        if ((vt1.b0 != vt1.b1) || (vt1.b1 != vt1.b2) || (vt1.b2 != vt1.b3) || (vt1.b3 != vt1.b4) || (vt1.b4 != vt1.b5) || (vt1.b5 != vt1.b6))
            passed = false;
        j = rand.Next(0, 10);
        vt1.b0 = str2[p[j]];
        vt1.b1 = str1[p[j]];
        vt1.b2 = cl1.str[p[j]];
        vt1.b3 = vt1.str[p[j]];
        vt1.b4 = str1darr[1][p[j]];
        vt1.b5 = str2darr[0, 0][p[j]];
        vt1.b6 = f(ref str2)[p[j]];
        if ((vt1.b0 != vt1.b1) || (vt1.b1 != vt1.b2) || (vt1.b2 != vt1.b3) || (vt1.b3 != vt1.b4) || (vt1.b4 != vt1.b5) || (vt1.b5 != vt1.b6))
            passed = false;

        //accessing the strings at different indices, assign to CL char
        cl1.b0 = str2[CL.i % idx1darr[0]];
        cl1.b1 = str1[CL.i % idx1darr[0]];
        cl1.b2 = cl1.str[CL.i % idx1darr[0]];
        cl1.b3 = vt1.str[CL.i % idx1darr[0]];
        cl1.b4 = str1darr[1][CL.i % idx1darr[0]];
        cl1.b5 = str2darr[0, 0][CL.i % idx1darr[0]];
        cl1.b6 = f(ref str2)[CL.i % idx1darr[0]];
        if ((cl1.b0 != cl1.b1) || (cl1.b1 != cl1.b2) || (cl1.b2 != cl1.b3) || (cl1.b3 != cl1.b4) || (cl1.b4 != cl1.b5) || (cl1.b5 != cl1.b6))
            passed = false;
        if ((str2[0] != str1[0]) || (str1[0] != cl1.str[0]) || (cl1.str[0] != vt1.str[0]) || (vt1.str[0] != str1darr[1][0]) || (str1darr[1][0] != str2darr[0, 0][0]) || (str2darr[0, 0][0] != f(ref str2)[0]))
            passed = false;
        cl1.b0 = str2[Convert.ToInt32(c2darr[0, 4]) - Convert.ToInt32(c2darr[0, 0])];
        cl1.b1 = str1[Convert.ToInt32(c2darr[0, 4]) - Convert.ToInt32(c2darr[0, 0])];
        cl1.b2 = cl1.str[Convert.ToInt32(c2darr[0, 4]) - Convert.ToInt32(c2darr[0, 0])];
        cl1.b3 = vt1.str[Convert.ToInt32(c2darr[0, 4]) - Convert.ToInt32(c2darr[0, 0])];
        cl1.b4 = str1darr[1][Convert.ToInt32(c2darr[0, 4]) - Convert.ToInt32(c2darr[0, 0])];
        cl1.b5 = str2darr[0, 0][Convert.ToInt32(c2darr[0, 4]) - Convert.ToInt32(c2darr[0, 0])];
        cl1.b6 = f(ref str2)[Convert.ToInt32(c2darr[0, 4]) - Convert.ToInt32(c2darr[0, 0])];
        if ((cl1.b0 != cl1.b1) || (cl1.b1 != cl1.b2) || (cl1.b2 != cl1.b3) || (cl1.b3 != cl1.b4) || (cl1.b4 != cl1.b5) || (cl1.b5 != cl1.b6) || (cl1.b6 != str1[4]))
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
        c2darr[1, 0] = str2[idx1darr[0] * idx1];
        c2darr[1, 1] = str1[idx1darr[0] * idx1];
        c2darr[1, 2] = cl1.str[idx1darr[0] * idx1];
        c2darr[1, 3] = vt1.str[idx1darr[0] * idx1];
        c2darr[1, 4] = str1darr[1][idx1darr[0] * idx1];
        c2darr[1, 5] = str2darr[0, 0][idx1darr[0] * idx1];
        c2darr[1, 6] = f(ref str2)[idx1darr[0] * idx1];
        if ((c2darr[1, 0] != c2darr[1, 1]) || (c2darr[1, 1] != c2darr[1, 2]) || (c2darr[1, 2] != c2darr[1, 3]) || (c2darr[1, 3] != c2darr[1, 4]) || (c2darr[1, 4] != c2darr[1, 5]) || (c2darr[1, 5] != c2darr[1, 6]) || (str2[6] != c2darr[1, 6]))
            passed = false;
        if ((str2[vt1.i] != str1[vt1.i]) || (str1[vt1.i] != cl1.str[vt1.i]) || (cl1.str[vt1.i] != vt1.str[vt1.i]) || (vt1.str[vt1.i] != str1darr[1][vt1.i]) || (str1darr[1][vt1.i] != str2darr[0, 0][vt1.i]) || (str2darr[0, 0][vt1.i] != f(ref str2)[vt1.i]))
            passed = false;
        c2darr[1, 0] = str2[idx1darr[1] - idx1darr[0]];
        c2darr[1, 1] = str1[idx1darr[1] - idx1darr[0]];
        c2darr[1, 2] = cl1.str[idx1darr[1] - idx1darr[0]];
        c2darr[1, 3] = vt1.str[idx1darr[1] - idx1darr[0]];
        c2darr[1, 4] = str1darr[1][idx1darr[1] - idx1darr[0]];
        c2darr[1, 5] = str2darr[0, 0][idx1darr[1] - idx1darr[0]];
        c2darr[1, 6] = f(ref str2)[idx1darr[1] - idx1darr[0]];
        if ((c2darr[1, 0] != c2darr[1, 1]) || (c2darr[1, 1] != c2darr[1, 2]) || (c2darr[1, 2] != c2darr[1, 3]) || (c2darr[1, 3] != c2darr[1, 4]) || (c2darr[1, 4] != c2darr[1, 5]) || (c2darr[1, 5] != c2darr[1, 6]) || (str2[6] != c2darr[1, 6]))
            passed = false;
        j = rand.Next(0, 10);
        c2darr[1, 0] = str2[f1(ref j)];
        c2darr[1, 1] = str1[f1(ref j)];
        c2darr[1, 2] = cl1.str[f1(ref j)];
        c2darr[1, 3] = vt1.str[f1(ref j)];
        c2darr[1, 4] = str1darr[1][f1(ref j)];
        c2darr[1, 5] = str2darr[0, 0][f1(ref j)];
        c2darr[1, 6] = f(ref str2)[f1(ref j)];
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




