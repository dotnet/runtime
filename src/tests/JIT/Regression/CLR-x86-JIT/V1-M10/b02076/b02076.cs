// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Sequential)]
class RECT
{
    public int left;
};

class MyInt
{
    public int i;
};

class CSwarm
{
    public CSwarm()
    {

        i = new MyInt();
        m_rScreen = new RECT();

        i.i = 99;
        m_rScreen.left = 99;
        Console.WriteLine(m_rScreen.left);
        Console.WriteLine(i.i);

        Console.WriteLine("---");

        Console.WriteLine(m_rScreen.left.ToString());
        Console.WriteLine(i.i.ToString());
    }
    RECT m_rScreen;
    MyInt i;
};


public class MainClass
{
    [Fact]
    public static int TestEntryPoint()
    {
        CSwarm swarm = new CSwarm();
        return (100);
    }
};


