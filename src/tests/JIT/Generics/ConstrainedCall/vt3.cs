// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
interface IncrDecr
{
    int Val();
}
struct MyInt : IncrDecr
{
    int x;
    public int Val() { return x + 1; }
    public override int GetHashCode() { return Val(); }
}
class MyCounter<T> where T : IncrDecr
{
    T counter;
    T[] counters = new T[1];
    public int Val1A()
    {
        return counter.GetHashCode();
    }
    public int Val2A()
    {
        return counters[0].GetHashCode();
    }
    public int Val3A(T cnter)
    {
        counter = cnter;
        return counter.GetHashCode();
    }
    public int Val1B()
    {
        return counter.GetHashCode();
    }
    public int Val2B()
    {
        return counters[0].GetHashCode();
    }
    public int Val3B(T cnter)
    {
        counter = cnter;
        return counter.GetHashCode();
    }
}
public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        MyCounter<MyInt> mc = new MyCounter<MyInt>();
        if (mc.Val1A() != mc.Val1B())
        {
            Console.WriteLine("FAILED 1");
            Console.WriteLine("mc.Val1A()={0}, mc.Val1B()={0}", mc.Val1A(), mc.Val1B());
            return 1;
        }
        if (mc.Val2A() != mc.Val2B())
        {
            Console.WriteLine("FAILED 2");
            Console.WriteLine("mc.Val1A()={0}, mc.Val1B()={0}", mc.Val2A(), mc.Val2B());
            return 2;
        }
        MyInt mi = new MyInt();
        if (mc.Val3A(mi) != mc.Val3B(mi))
        {
            Console.WriteLine("FAILED 3");
            Console.WriteLine("mc.Val1A()={0}, mc.Val1B()={0}", mc.Val3A(mi), mc.Val3B(mi));
            return 3;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}

