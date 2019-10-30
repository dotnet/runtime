// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Runtime.InteropServices;

public enum TestEnum
{
    red = 1,
    green = 2,
    blue = 4,
}

public struct AA
{
    public Array[] m_axField1;
    public char[,,] m_achField2;
    public bool[,][][,] m_abField3;
    public static ushort m_ushStatic1;
    public TestEnum Method4()
    {
        return TestEnum.blue;
    }
}


public class App
{
    public static AA m_xStatic1 = new AA();
    public static AA m_xStatic2 = new AA();
    private static int Main()
    {
        try
        {
            Console.WriteLine("Testing AA::Method4");
            (m_xStatic1 = m_xStatic1).Method4();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
            return 1;
        }
        Console.WriteLine("Passed.");
        return 100;
    }
}
