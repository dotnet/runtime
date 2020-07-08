// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
public class Padre
{
    private double _x = 10;
    public virtual void Incrementa(double a)
    {
        _x = _x + a;
    }
    public void print() { Console.WriteLine(_x); }
}

public class Hijo : Padre
{
    public override void Incrementa(double a)
    {
        double b = a + (a * 0.1);

        Console.WriteLine(b);
        base.Incrementa(b);
    }
}
internal class Test
{
    public static int Main()
    {
        Hijo h = new Hijo();
        h.Incrementa(1.0);
        h.print();
        return 100;
    }
}
