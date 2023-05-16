// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public class CCSE
{
    private static volatile uint s_source = 4;
    private static volatile uint s_sink1 = 0;
    private static volatile uint s_sink2 = 0;
    [Fact]
    public static int TestEntryPoint()
    {
        uint v1 = s_source;
        uint v2 = s_source;
        uint v3 = s_source;
        uint v4 = s_source;
        uint v5 = s_source;
        uint v6 = s_source;
        uint v7 = s_source;
        uint v8 = s_source;
        s_sink1 = ((v1 * v2) + (v2 * v3)) | (v5 * v6) + ((v1 + v4) * (v4 + v6)) * ((v4 + v5) + ((v1 * v3) | (v7 + v1)) & (v2 + v4));
        s_sink2 = (v6 * v5) | ((v2 * v1) + (v3 * v2)) + ((v4 + v2) & ((v1 + v7) | (v3 * v1)) + (v5 + v4)) * ((v6 + v4) * (v4 + v1));

        if (s_sink1 != s_sink2)
            return 1;

        s_sink1 = ((v1 * v3) + (v2 * v4)) | (v5 * v7) + ((v1 + v5) * (v4 + v7)) * ((v4 + v6) + ((v1 * v4) | (v7 + v2)) & (v2 + v5));
        s_sink2 = (v7 * v5) | ((v3 * v1) + (v4 * v2)) + ((v5 + v2) & ((v2 + v7) | (v4 * v1)) + (v6 + v4)) * ((v7 + v4) * (v5 + v1));

        if ((s_sink1 + s_sink2) != (((v1 * v3) + (v4 * v2)) | (v5 * v7) + ((v4 + v7) * (v1 + v5)) * ((v6 + v4) + ((v1 * v4) | (v7 + v2)) & (v2 + v5))) * 2)
            return 1;

        s_sink1 *= ((v1 + v2) * (v3 | v4)) & (((v2 & v6) + v7 * v8) + (v3 + v4 * v6));
        s_sink2 *= ((v6 * v4 + v3) + (v8 * v7 + (v6 & v2))) & ((v4 | v3) * (v2 + v1));

        if (s_sink1 == s_sink2)
            ;
        else
        {
            Console.WriteLine(s_sink1);
            Console.WriteLine(s_sink2);
            return 1;
        }

        s_sink1 = (((v1 + v3) * (v3 | v5)) & (((v3 & v6) + v3 * v8) + (v3 + v3 * v6))) + 18;
        s_sink2 = 18 + (((v6 * v3 + v3) + (v8 * v3 + (v6 & v3))) & ((v5 | v3) * (v3 + v1)));

        if (s_sink1 == s_sink2)
            Console.WriteLine(s_sink1 + s_sink2);
        if (s_sink1 != s_sink2)
            return 1;
        return 100;
    }
}

