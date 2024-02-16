// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

[AttributeUsage(AttributeTargets.Property, Inherited = true)]
class FooAttribute : Attribute{ }
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
class BarAttribute : Attribute { }
[AttributeUsage(AttributeTargets.Property, Inherited = true)]
class BazAttribute : Attribute { }

class Base
{
    [Foo]
    public virtual int X { get; }
}

class Mid : Base
{
    [Bar]
    public override int X => base.X;
}

class Derived : Mid
{
    [Baz]
    public override int X => base.X;
}

class Program
{
    static int Main(string[] args)
    {
        int numAttributes = Attribute.GetCustomAttributes(typeof(Derived).GetProperty("X"), inherit: true).Length;
        if (numAttributes == 3)
        {
            return 100;
        }

        return -1;
    }
}
