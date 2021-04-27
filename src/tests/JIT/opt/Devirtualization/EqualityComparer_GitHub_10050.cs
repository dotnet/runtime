// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

class EqualityComparer_GitHub_10050
{
    // Would like to see just one call to Default per call to Hoist
    public static int Hoist()
    {
        int result = 0;
        string s0 = "s";
        string s1 = "s";

        for (int i = 0; i < 100; i++)
        {
            if (EqualityComparer<string>.Default.Equals(s0, s1))
            {
                result++;
            }
        }

        return result;
    }

    // Would like to see call to Default optimized away
    // and Equals devirtualized and inlined
    public static int Sink(int v0, int v1)
    {
        int result = 0;

        var c = EqualityComparer<int>.Default;

        for (int i = 0; i < 100; i++)
        {
            if (c.Equals(v0, v1))
            {
                result++;
            }
        }

        return result;
    }

    // Would like to see just one call to Default per call to Common
    public static int Common()
    {
        int result = 0;
        string s0 = "t";
        string s1 = "t";

        for (int i = 0; i < 50; i++)
        {
            if (EqualityComparer<string>.Default.Equals(s0, s1))
            {
                result++;
            }
        }

        for (int i = 0; i < 50; i++)
        {
            if (EqualityComparer<string>.Default.Equals(s0, s1))
            {
                result++;
            }
        }

        return result;
    }

    // Optimization pattern should vary here.
    //
    // If T is a ref type, Default will be hoisted and Equals will not devirtualize.
    // If T is a value type, Default will be removed and Equals will devirtualize.
    public static int GeneralHoist<T>(T v0, T v1)
    {
        int result = 0;

        for (int i = 0; i < 100; i++)
        {
            if (EqualityComparer<T>.Default.Equals(v0, v1))
            {
                result++;
            }
        }

        return result;

    }

    // Optimization pattern should vary here.
    //
    // If T is a ref type we will compile as is.
    // If T is a value type, Default will be removed and Equals will devirtualize.
    public static int GeneralSink<T>(T v0, T v1)
    {
        int result = 0;

        var c = EqualityComparer<T>.Default;

        for (int i = 0; i < 100; i++)
        {
            if (c.Equals(v0, v1))
            {
                result++;
            }
        }

        return result;

    }

    public static int Main()
    {
        int h = Hoist();
        int s = Sink(33, 33);
        int c = Common();
        int ghr = GeneralHoist<string>("u", "u");
        int ghv = GeneralHoist<int>(44, 44);
        int gsr = GeneralSink<string>("v", "v");
        int gsv = GeneralSink<int>(55, 55);

        return h + s + c + ghr + ghv + gsr + gsv - 600;
    }
}
