// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class IA { }
public class IB { }

public abstract class Base
{
    public abstract IA Key { get; }
    public abstract IB Value { get; }
}
public sealed class Derived : Base<IB>
{
    public class A : IA { }
    public sealed override A Key => default;
}
public abstract class Base<B> : Base where B : IB
{
    public sealed override B Value => null;
}

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        new Derived();
    }
}
