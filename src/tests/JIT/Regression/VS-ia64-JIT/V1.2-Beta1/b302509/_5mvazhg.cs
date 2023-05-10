// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Xunit;

public struct AA
{
    public static byte Static2()
    {
        do
        {
            TypedReference local19 = __makeref(App.m_xFwd6);
            do
            {
            }
            while (App.m_bFwd5);
        }
        while (App.m_bFwd5);
        return App.m_byFwd9;
    }
}

[StructLayout(LayoutKind.Sequential)]
public class App
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Console.WriteLine("Testing AA::Static2");
            AA.Static2();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
    public static bool m_bFwd5;
    public static AA m_xFwd6;
    public static byte m_byFwd9;
}
