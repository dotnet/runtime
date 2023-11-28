// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;
interface IncrDecr
{
    void Incr(int a);
    void Decr(int a);
    int Val();
}
class MyInt : IncrDecr
{
    int x;
    public void Incr(int a) { x += a; }
    public void Decr(int a) { x -= a; }
    public int Val() { return x; }
}
class MyCounter<T> where T : IncrDecr, new()
{
    T counter = new T();
    T[] counters = new T[1];
    public void Increment<T2>() where T2 : IncrDecr, new()
    {
        T2 cnter = new T2();
        cnter.Incr(100);
        counter = (T)(IncrDecr)cnter;
    }
    public void Decrement<T2>() where T2 : IncrDecr, new()
    {
        T2 cnter = (T2)(IncrDecr)counter;
        cnter.Decr(100);
        counter = (T)(IncrDecr)cnter;
    }
    public void Increment<T2>(int index) where T2 : IncrDecr, new()
    {
        T2[] cnters = new T2[1];
        cnters[index] = new T2();
        cnters[index].Incr(100);
        counters[index] = (T)(IncrDecr)cnters[index];
    }
    public void Decrement<T2>(int index) where T2 : IncrDecr, new()
    {
        T2[] cnters = new T2[1];
        cnters[index] = (T2)(IncrDecr)counters[index];
        cnters[index].Decr(100);
        counters[index] = (T)(IncrDecr)cnters[index];
    }
    public virtual void Increment2<T2>(T2 cnter) where T2 : IncrDecr, new()
    {
        cnter.Incr(100);
        counter = (T)(IncrDecr)cnter;
    }
    public virtual void Decrement2<T2>(T2 cnter) where T2 : IncrDecr, new()
    {
        cnter.Decr(100);
        counter = (T)(IncrDecr)cnter;
    }
    public int Val()
    {
        return counter.Val();
    }
    public int Val(int index)
    {
        return counters[index].Val();
    }
}
public class test
{
    [Fact]
    public static int TestEntryPoint()
    {
        MyCounter<MyInt> mc = new MyCounter<MyInt>();
        mc.Increment<MyInt>();
        if (mc.Val() != 100)
        {
            Console.WriteLine("FAILED 1");
            Console.WriteLine("Expected: 100, Actual: {0}", mc.Val());
            return 1;
        }
        mc.Decrement<MyInt>();
        if (mc.Val() != 0)
        {
            Console.WriteLine("FAILED 2");
            Console.WriteLine("Expected: 0, Actual: {0}", mc.Val());
            return 2;
        }
        mc.Increment<MyInt>(0);
        if (mc.Val(0) != 100)
        {
            Console.WriteLine("FAILED 3");
            Console.WriteLine("Expected: 100, Actual: {0}", mc.Val(0));
            return 3;
        }
        mc.Decrement<MyInt>(0);
        if (mc.Val(0) != 0)
        {
            Console.WriteLine("FAILED 4");
            Console.WriteLine("Expected: 0, Actual: {0}", mc.Val(0));
            return 4;
        }
        MyInt mi = new MyInt();
        mc.Increment2(mi);
        if (mc.Val() != 100)
        {
            Console.WriteLine("FAILED 5");
            Console.WriteLine("Expected: 100, Actual: {0}", mc.Val());
            return 5;
        }
        mc.Decrement2(mi);
        if (mc.Val() != 0)
        {
            Console.WriteLine("FAILED 6");
            Console.WriteLine("Expected: 0, Actual: {0}", mc.Val());
            return 6;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}

