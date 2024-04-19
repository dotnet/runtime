// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

public abstract class Base<T>
{
   public virtual T Get() => throw new NotImplementedException();
}

public sealed class CovariantReturn : Base<object>
{
   public override string Get() => throw new NotImplementedException();
}

public abstract class ABase
{
    public abstract object this[int index] { get; }
}

public sealed class Concrete<T> : ABase
    where T : class
{
    public override T this[int index]
    {
        get
        {
            throw null;
        }
    }
}

class Parent
{
    public virtual object Value { get; }
}

class Child<T> : Parent where T : class
{
    public override T Value { get => (T)base.Value; }
}

class Foo { }

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        Type[] t = Assembly.GetExecutingAssembly().GetTypes();
        new Child<Foo>();
        new CovariantReturn();
    }
}
