// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;


namespace ReproMAUI6811;

public static class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        Leaf l = new Leaf();

        if (l.getI().ToString() != "Leaf")
                return 1;
        if (((Intermediate)l).getI().ToString() != "Leaf")
                return 2;
        if (((PseudoBase)l).getI().ToString() != "Leaf")
                return 3;
        if (((Base)l).getI().ToString() != "Leaf")
                return 4;
        return 100;
    }
}

public abstract class Base {
    public abstract I getI();
}

public class PseudoBase : Base {
    public override I getI() => new C ("PseudoBase");
}

public abstract class Intermediate : PseudoBase {
    public override abstract I getI();
}

public class Leaf : Intermediate {
    public Leaf() {}
    public override C getI() { return new C ("Leaf"); }
}

public interface I {}

public class C : I {
    private readonly string _repr;
    public C(string s) { _repr = s; }
    public override string ToString() => _repr;
}


