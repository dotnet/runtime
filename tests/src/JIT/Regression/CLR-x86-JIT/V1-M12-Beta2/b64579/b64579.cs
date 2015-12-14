// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
