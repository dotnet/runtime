// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class MyBox<T>
{
    private T _value;
    public T Value { get => _value; private set { _value = value; }}
    public MyBox(T inp) { Value = inp; }
}

public class Program
{
    public static void Main()
    {
        var box = new MyBox<string>("xyz");
        //Thinger<string>(box);
        //RunIt<string>(box, "abc");
        //Console.WriteLine (box.Value);
        RunItAgain<string>(box, "hjk");
        Console.WriteLine (box.Value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RunIt<H> (MyBox<H> dest, H input)
    {
        ref H boxWriter = ref AccessBox(dest);
        boxWriter = input;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RunItAgain<S> (MyBox<S> dest, S input)
    {
        ref S boxWriter = ref AccessHelper<S>.AccessBox2(dest);
        boxWriter = input;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name="_value")]
    private static extern ref W AccessBox<W>(MyBox<W> x);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref W Thinger<W>(MyBox<W> x) => throw new InvalidOperationException("oops");
}

public class AccessHelper<Q>
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name="_value")]
    public static extern ref Q AccessBox2(MyBox<Q> q);
}
