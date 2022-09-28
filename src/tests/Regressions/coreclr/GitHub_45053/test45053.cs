// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

public abstract class T<TR>
{
    public abstract TR GetA();
}

// This abstract causes the error - not overriding the base method cause the runtime to crash
public abstract class TA : T<A> { }

// This works
// public abstract class TA : T<A>
// {
//  // Overriding in between fixes the problem
//    public override A GetA() => new ();
// }

    // Overridden here, in the grandson
public class TB : TA
{
    public override B GetA() => new ();
}
public class A { }

public class B : A { }

class Program
{
    static int Main()
    {
        System.Console.WriteLine((new TB() as T<A>).GetA().GetType().FullName);

        return 100;
    }
}
