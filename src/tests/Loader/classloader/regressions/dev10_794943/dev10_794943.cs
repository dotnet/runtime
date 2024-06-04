// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

struct A<T> { }
struct B<T> { }

interface Interface<T>
{ A<T> InterfaceFunc(); }

class Base<T>
{ public virtual B<T> Func() { return default(B<T>); }  }

class C<U,T> where U:Base<T>, Interface<T>
{ 
  public static void CallFunc(U u) { u.Func(); }
  public static void CallInterfaceFunc(U u) { u.InterfaceFunc(); }
}

class Problem : Base<object>, Interface<object>
{
    public A<object> InterfaceFunc() { return new A<object>(); }
    public override B<object> Func() { return new B<object>(); }
}

public class Test_dev10_794943
{
    [Fact]
    public static void TestEntryPoint()
    {
        C<Problem, object>.CallFunc(new Problem());
        C<Problem, object>.CallInterfaceFunc(new Problem());
    }
}
