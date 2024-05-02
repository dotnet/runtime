// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

#if false

public class MyBox<T>
{
    private T _value;
    public T Value { get => _value; private set { _value = value; }}
    public MyBox(T inp) { Value = inp; }
}

public class SimpleBox
{
        private decimal _value;
        public decimal Value { get => _value; private set { _value = value;}}
        public SimpleBox(decimal inp) { Value = inp;}
}

public class Program
{
    public static void Main()
    {
        var sbox = new SimpleBox((decimal)-5);
        RunSimple(sbox, (decimal)20);
        Console.WriteLine ("20 = {0}", sbox.Value);
        var box = new MyBox<string>("xyz");
        RunItAgain<string>(box, "hjk");
        Console.WriteLine ("hjk = {0}", box.Value);
        var box2 = new MyBox<decimal>((decimal)-2);
        RunItAgain<decimal>(box2, (decimal)10.0);
        Console.WriteLine (box2.Value);
        RunIt<string>(box, "abc");
        Console.WriteLine ("abc = {0}", box.Value);
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
    private static void RunSimple(SimpleBox s, decimal input)
    {
        ref decimal boxWriter = ref SimpleHelper.AccessSimpleBox(s);
        boxWriter = input;
    }
}

public class AccessHelper<Q>
{
#if false
    [MethodImpl(MethodImplOptions.NoInlining)]
            public static /*extern*/ ref Q AccessBox2(MyBox<Q> q) => throw new NotImplementedException("exn");
#else
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name="_value")]
    public static extern ref Q AccessBox2(MyBox<Q> q);
#endif
}

public class SimpleHelper
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name="_value")]
    public static extern ref decimal AccessSimpleBox(SimpleBox b);
}

#else

public class MyService<T>
{
        public MyService() {}

        private static string Explain(T arg) => arg.ToString() + " : " + typeof(T).ToString(); //$"{arg} : {typeof(T)}";
}

public class Program
{
        public static void Main()
        {
                new Program().Runner<string>("abcd");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Runner<Q>(Q x)
        {
                Console.WriteLine (AccessHelper<Q>.CallExplain(default, x));
        }

}

public static class AccessHelper<W>
{
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name="Explain")]
        public static extern string CallExplain(MyService<W> target, W arg);

}

#endif
