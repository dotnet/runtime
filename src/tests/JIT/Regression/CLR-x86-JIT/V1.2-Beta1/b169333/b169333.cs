// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
class A { }
class B { }
interface IFoo<T>
{
    void Foo(T t);
}

public class C : IFoo<A>, IFoo<B>
{
    void IFoo<A>.Foo(A a)
    {
        System.Console.WriteLine("A");
    }

    void IFoo<B>.Foo(B b)
    {
        System.Console.WriteLine("B");
    }

    [Fact]
    public static int TestEntryPoint()
    {
        C c = new C();
        IFoo<A> i = c;
        i.Foo(null);
        System.Console.WriteLine("PASSED");
        return 100;
    }
}

