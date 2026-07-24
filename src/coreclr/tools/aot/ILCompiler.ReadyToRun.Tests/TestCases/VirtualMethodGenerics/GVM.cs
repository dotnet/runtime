// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Test1 -- Interface GVM call that resolves to method on a base type
interface ITest1
{
    T Test1Method<T>();
}

abstract class Test1A : ITest1
{
    public T Test1Method<T>() => default;
}

sealed class Test1B : Test1A
{
    public Test1B() { }
}

// Test2 -- Interface GVM call that resolves to override on intermediate type
interface ITest2
{
    int Test2Method<T>();
}

class Test2A : ITest2
{
    public virtual int Test2Method<T>() { return 0; }
}

class Test2B : Test2A
{
    public override int Test2Method<T>() { return 42; }
}

class Test2C : Test2B
{
}

// Test3 -- Explicit interface GVM implementation on a base type
interface ITest3<T>
{
    int Test3Method<U>();
}

class Test3A<T> : ITest3<T>
{
    int ITest3<T>.Test3Method<U>() => 42;
}

class Test3B<T> : Test3A<T>
{
    public Test3B() { }
}

// Test4 -- Interface GVM reimplementation with new slot
interface ITest4
{
    int Test4Method<T>();
}

class Test4A : ITest4
{
    public virtual int Test4Method<T>() => 1;
}

class Test4B : Test4A, ITest4
{
    public new virtual int Test4Method<T>() => 42;
}

class Test4C : Test4B
{
}

// Test5 -- Non-final default interface GVM (non-explicit DIM)
interface ITest5
{
    int Test5Method<T>() => 42;
}

class Test5 : ITest5
{
}

// Test6 -- Explicit DIM with generic method (explicit, virtual final)
interface ITest6Base
{
    T Test6Method<T>();
}

interface ITest6WithDim<U> : ITest6Base
{
    T ITest6Base.Test6Method<T>() => default;
}

class Test6 : ITest6WithDim<int>
{
}

// Test7 -- Static virtual generic method on interface (SVM)
interface ITest7Base
{
    static virtual T Test7Method<T>() => default;
}

interface ITest7<U> : ITest7Base
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static T ITest7Base.Test7Method<T>() => default;
}

struct Test7 : ITest7<int>
{
}

class GVMTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest1 CreateTest1() => new Test1B();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest2 CreateTest2() => new Test2C();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest3<T> CreateTest3<T>() => new Test3B<T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest4 CreateTest4() => new Test4C();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest5 CreateTest5() => new Test5();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ITest6Base CreateTest6() => new Test6();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CallTest7<T>() where T : ITest7Base
    {
        Console.WriteLine(T.Test7Method<int>());
    }

    static void Run()
    {
        ITest1 t1 = CreateTest1();
        Console.WriteLine(t1.Test1Method<int>());

        ITest2 t2 = CreateTest2();
        Console.WriteLine(t2.Test2Method<int>());

        ITest3<int> t3 = CreateTest3<int>();
        Console.WriteLine(t3.Test3Method<int>());

        ITest4 t4 = CreateTest4();
        Console.WriteLine(t4.Test4Method<int>());

        ITest5 t5 = CreateTest5();
        Console.WriteLine(t5.Test5Method<int>());

        ITest6Base t6 = CreateTest6();
        Console.WriteLine(t6.Test6Method<int>());

        CallTest7<Test7>();
    }
}
