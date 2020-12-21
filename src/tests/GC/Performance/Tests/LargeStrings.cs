// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Threading;

public class StringConcat
{
    // Objects used by test. init before Main is entered.

    const int NUM_ITERS_CONCAT = 10;
    const int NUM_ITERS = 5000;

    public static String s1 = "11234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s2 = "21234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s3 = "31234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s4 = "41234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s5 = "51234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s6 = "61234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s7 = "71234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s8 = "81234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s9 = "91234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static String s10 = "01234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public static void Main(string[] p_args)
    {
        string str = null;

        for (long i = 0; i < NUM_ITERS; i++)
        {
            for (int j = 0; j < NUM_ITERS_CONCAT; j++)
            {
                str += s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10
                    + s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8 + s9 + s10;
            }

            str = "";
        }
    }
}

