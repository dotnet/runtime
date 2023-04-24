// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Test_b79642
{
    public byte m_value;

    [Fact]
    public static int TestEntryPoint()
    {
        Test_b79642 a = new Test_b79642();
        Test_b79642 b = new Test_b79642();

        a.m_value = 255;
        b.m_value = 1;

        byte b1 = a.m_value;
        byte b2 = b.m_value;

        Console.WriteLine(a.m_value < b.m_value);
        Console.WriteLine((byte)a.m_value < (byte)b.m_value);
        Console.WriteLine(b1 < b2);
        return 100;
    }
}
