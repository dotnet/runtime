// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;


delegate int Del(object p);

abstract class Base
{
    public abstract int Target<U>(object p);
}

class Middle : Base
{
    public override int Target<U>(object p) => 123;
}

class Top : Middle
{
    public override int Target<U>(object p) => 456;

    public Del TestA<U>()
    {
        return new Del(base.Target<U>);
    }

    public Del TestB<U>()
    {
        return new Del(Target<U>);
    }
}

class Test
{
    public static int Main() 
    {
        var del1 = new Top().TestA<object>();
        var del2 = new Top().TestB<object>();
        
        var x = del1(null);
        var y = del2(null);
        Console.WriteLine(x);
        Console.WriteLine(y);

        if (x == 123 && y == 456)
        {
            Console.WriteLine("Pass");
            return 100;
        }
        Console.WriteLine("FAIL");
        return -1;
    }
}
