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
        RunIt<string>(box, "abc");
        Console.WriteLine (box.Value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RunIt<H> (MyBox<H> dest, H input)
    {
        ref H boxWriter = ref AccessBox(dest);
        boxWriter = input;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name="_value")]
    private static extern ref W AccessBox<W>(MyBox<W> x);
}
