// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

class B
{
    public virtual int F() => 33;
}

class D<T> : B
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int F() => typeof(T) == typeof(string) ? 44 : 55;
}

class E : D<string>
{

}

class G<T> : E
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int F() => typeof(T) == typeof(string) ? 66 : 77;
}

// All of the calls to F() in Main should devirtualize and inline

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        B b = new B();
        D<string> ds = new D<string>();
        E e = new E();
        G<string> gs = new G<string>();

        //        33 +     44 +    44 +     66 = 187
        return b.F() + ds.F() + e.F() + gs.F() - 87;
    }
}
