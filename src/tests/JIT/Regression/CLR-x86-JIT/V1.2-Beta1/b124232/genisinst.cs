// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

interface I<T>
{
}
class C1<T> : I<T>
{
    public T a;
    public C1(T arg)
    {
        a = arg;
    }
}

class C2
{
    public static T GetMemberList<T>(ref object o)
    {
        C1<T> c2 = o as C1<T>;
        Console.WriteLine(c2.a);
        return ((C1<T>)o).a;
    }
}
public class Test_genisinst
{
    [Fact]
    public static int TestEntryPoint()
    {
        C1<int> c1 = new C1<int>(100);
        object o = c1;
        return C2.GetMemberList<int>(ref o);
    }
}


