// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

class A { }
class B { }
interface IFoo<T>
{
    void Foo(T t);
}

class C : IFoo<A>, IFoo<B>
{
    void IFoo<A>.Foo(A a)
    {
        System.Console.WriteLine("A");
    }

    void IFoo<B>.Foo(B b)
    {
        System.Console.WriteLine("B");
    }

    static int Main()
    {
        C c = new C();
        IFoo<A> i = c;
        i.Foo(null);
        System.Console.WriteLine("PASSED");
        return 100;
    }
}

