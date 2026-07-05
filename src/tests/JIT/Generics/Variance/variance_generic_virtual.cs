// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;

class Base
{

}

class Derived : Base
{

}

interface IInVariant<in T>
{
    string Func<U>(T t);
}

interface IOutVariant<out T>
{
    string Func<U>();
}

class ClassWithVariantGvms : IInVariant<object>, IInVariant<Base>, IOutVariant<Derived>, IOutVariant<Base>
{
    string IInVariant<object>.Func<U>(object t)
    {
        return "CallOnObject";
    }
    string IInVariant<Base>.Func<U>(Base t)
    {
        return "CallOnBase";
    }
    string IOutVariant<Derived>.Func<U>()
    {
        return "CallOnDerived";
    }
    string IOutVariant<Base>.Func<U>()
    {
        return "CallOnBase";
    }
}

public class VarianceGenericVirtual
{
    [Fact]
    public static void TestEntryPoint()
    {
        ClassWithVariantGvms testClass = new ClassWithVariantGvms();

        Assert.Equal("CallOnObject", ((IInVariant<object>)testClass).Func<object>(new Derived()));
        Assert.Equal("CallOnBase", ((IInVariant<Base>)testClass).Func<object>(new Derived()));
        Assert.Equal("CallOnObject", ((IInVariant<Derived>)testClass).Func<object>(new Derived()));

        Assert.Equal("CallOnDerived", ((IOutVariant<object>)testClass).Func<object>());
        Assert.Equal("CallOnBase", ((IOutVariant<Base>)testClass).Func<object>());
        Assert.Equal("CallOnDerived", ((IOutVariant<Derived>)testClass).Func<object>());
    }
}
