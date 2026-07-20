// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Test1 -- Interface call that resolves to method on a generic base type
interface ITest1<T>
{
    T Test1Method();
}

abstract class Test1A<T> : ITest1<T>
{
    protected T _value;
    public Test1A(T value) => _value = value;

    public T Test1Method() => _value;
}

sealed class Test1B<T> : Test1A<T>
{
    public Test1B(T value) : base(value) { }
}

// Test2 -- Virtual call that resolves to an override on a generic base type
internal class Test2A<T>
{
    public Test2A() { }

    public virtual int Test2Method()
    {
        return 1;
    }
}

internal class Test2B<T> : Test2A<T>
{
    public Test2B() { }
}

internal class Test2C<T> : Test2B<T>
{
    public Test2C() { }

    public override int Test2Method()
    {
        return 42;
    }
}

internal class Test2D<T> : Test2C<T>
{
    public Test2D() { }
}

// Test3 -- Interface call that resolves to a DIM (explicit, virtual final)
interface ITest3Base
{
    int Test3Method();
}

interface ITest3WithDim<T>: ITest3Base
{
    int ITest3Base.Test3Method()
    {
        return 42;
    }
}

class Test3 : ITest3WithDim<int>
{
}

// Test4 -- Explicit interface implementation on a generic base type
interface ITest4<T>
{
    T Test4Method();
}

class Test4A<T>: ITest4<T>
{
    private T _value;
    public Test4A(T value) => _value = value;
    T ITest4<T>.Test4Method() => _value;
}

class Test4B<T> : Test4A<T>
{
    public Test4B(T value) : base(value) { }
}

// Test5 -- Interface dispatch resolves to override on intermediate generic type
interface ITest5<T>
{
    int Test5Method();
}

class Test5A<T> : ITest5<T>
{
    public virtual int Test5Method() => 1;
}

class Test5B<T> : Test5A<T>
{
    public override int Test5Method() => 42;
}

class Test5C : Test5B<int>
{
}

// Test6 -- Interface reimplementation with new slot
interface ITest6<T>
{
    int Test6Method();
}

class Test6A<T> : ITest6<T>
{
    public virtual int Test6Method() => 1;
}

class Test6B<T> : Test6A<T>, ITest6<T>
{
    public new virtual int Test6Method() => 42;
}

class Test6C : Test6B<int>
{
}

// Test7 -- Non-final default interface method (non-explicit DIM)
interface ITest7<T>
{
    int Test7Method() => 42;
}

class Test7A<T> : ITest7<T>
{
}

// Entry points that drive dependency analysis
static class NonGVMTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest1<T> CreateTest1<T>(T value) => new Test1B<T>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Test2A<T> CreateTest2A<T>() => new Test2D<T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest3Base CreateTest3() => new Test3();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest4<T> CreateTest4<T>(T value) => new Test4B<T>(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest5<int> CreateTest5() => new Test5C();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest6<int> CreateTest6() => new Test6C();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest7<T> CreateTest7<T>() => new Test7A<T>();

    static void Run()
    {
        ITest1<int> t1 = CreateTest1(42);
        Console.WriteLine(t1.Test1Method());

        Test2A<int> t2 = CreateTest2A<int>();
        Console.WriteLine(t2.Test2Method());

        ITest3Base t3 = CreateTest3();
        Console.WriteLine(t3.Test3Method());

        ITest4<int> t4 = CreateTest4(42);
        Console.WriteLine(t4.Test4Method());

        ITest5<int> t5 = CreateTest5();
        Console.WriteLine(t5.Test5Method());

        ITest6<int> t6 = CreateTest6();
        Console.WriteLine(t6.Test6Method());

        ITest7<int> t7 = CreateTest7<int>();
        Console.WriteLine(t7.Test7Method());
    }
}
