// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

public class Test
{
    public byte m_value;

    public static int Main()
    {
        Test a = new Test();
        Test b = new Test();

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
