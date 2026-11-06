// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests;

public class TypeMapFactoryAttributeTests
{
    [Theory]
    [InlineData(typeof(FirstRegistration), typeof(FirstImplementation), 17)]
    [InlineData(typeof(SecondRegistration), typeof(SecondImplementation), 42)]
    [InlineData(typeof(SelfImplementation), typeof(SelfImplementation), 23)]
    [InlineData(typeof(FileLocalImplementation), typeof(FileLocalImplementation), 31)]
    public void AttributeLookupCreatesFreshTypedInstances(Type mappedType, Type implementationType, int expected)
    {
        TypeMapFactoryAttribute<IValue> factory = mappedType.GetCustomAttribute<TypeMapFactoryAttribute<IValue>>(inherit: false)!;
        IValue first = factory.CreateInstance();
        IValue second = factory.CreateInstance();
        Assert.NotSame(first, second);
        Assert.Equal(expected, first.Value);
        Assert.Equal(expected, second.Value);
        Assert.Equal(implementationType, first.GetType());
        Assert.Null(mappedType.GetCustomAttribute<TypeMapFactoryAttribute<IDisposable>>(inherit: false));
    }

    [Fact]
    public void FactoryAttributeIsNotInherited()
    {
        Assert.Null(typeof(DerivedRegistration).GetCustomAttribute<TypeMapFactoryAttribute<IValue>>(inherit: true));
    }

    public interface IValue
    {
        int Value { get; }
    }

    [TypeMapFactory<IValue, FirstImplementation>]
    public class FirstRegistration;

    [TypeMapFactory<IValue, SecondImplementation>]
    public class SecondRegistration;

    public class FirstImplementation : IValue
    {
        public int Value => 17;
    }

    public class SecondImplementation : IValue
    {
        public int Value => 42;
    }

    [TypeMapFactory<IValue, SelfImplementation>]
    public class SelfImplementation : IValue
    {
        public int Value => 23;
    }

    public class DerivedRegistration : FirstRegistration;
}

[TypeMapFactory<TypeMapFactoryAttributeTests.IValue, FileLocalImplementation>]
file class FileLocalImplementation : TypeMapFactoryAttributeTests.IValue
{
    public int Value => 31;
}
