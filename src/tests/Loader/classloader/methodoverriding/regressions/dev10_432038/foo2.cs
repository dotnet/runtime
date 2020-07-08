// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class Foo2: Foo1
{
    public virtual new int A() { Console.WriteLine("\nin Foo2::A()"); return 2; }
}

public class Bar2<T> : Bar1<T>
{
    public virtual new int A<U>() { Console.WriteLine("\nin Foo2::A() with T=" + typeof(T) + " and U=" + typeof(U)); return 2; }
}
