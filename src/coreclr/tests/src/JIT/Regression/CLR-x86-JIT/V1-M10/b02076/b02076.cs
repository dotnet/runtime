// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.InteropServices;

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


class MainClass
{
    public static int Main(string[] args)
    {
        CSwarm swarm = new CSwarm();
        return (100);
    }
};


