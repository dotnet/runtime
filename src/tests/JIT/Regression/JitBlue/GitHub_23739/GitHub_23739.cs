// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_23739
{
    struct Struct1f
    {
        float a;
    }

    class Cls1f
    {
        public Struct1f sf;
    }

    struct Struct2f
    {
        float a, b;
    }

    class Cls2f
    {
        public Struct2f sf;
    }

    struct Struct3f
    {
        float a, b, c;
    }

    class Cls3f
    {
        public Struct3f sf;
    }

    struct Struct4f
    {
        float a, b, c, d;
    }

    class Cls4f
    {
        public Struct4f sf;
    }

    struct Struct5f
    {
        float a, b, c, d, e;
    }

    class Cls5f
    {
        public Struct5f sf;
    }

    struct Struct6f
    {
        float a, b, c, d, e, f;
    }

    class Cls6f
    {
        public Struct6f sf;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Sink<T>(ref T t)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test1f(Cls1f c)
    {
        Struct1f l1 = default;
        Struct1f l2 = default;
        Struct1f l3 = default;

        for (int i = 0; i < 10; i++)
        {
            l1 = c.sf;
            l2 = c.sf;
            l3 = c.sf;
        }

        Sink(ref l1);
        Sink(ref l2);
        Sink(ref l3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test2f(Cls2f c)
    {
        Struct2f l1 = default;
        Struct2f l2 = default;
        Struct2f l3 = default;

        for (int i = 0; i < 10; i++)
        {
            l1 = c.sf;
            l2 = c.sf;
            l3 = c.sf;
        }

        Sink(ref l1);
        Sink(ref l2);
        Sink(ref l3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test3f(Cls3f c)
    {
        Struct3f l1 = default;
        Struct3f l2 = default;
        Struct3f l3 = default;

        for (int i = 0; i < 10; i++)
        {
            l1 = c.sf;
            l2 = c.sf;
            l3 = c.sf;
        }

        Sink(ref l1);
        Sink(ref l2);
        Sink(ref l3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test4f(Cls4f c)
    {
        Struct4f l1 = default;
        Struct4f l2 = default;
        Struct4f l3 = default;

        for (int i = 0; i < 10; i++)
        {
            l1 = c.sf;
            l2 = c.sf;
            l3 = c.sf;
        }

        Sink(ref l1);
        Sink(ref l2);
        Sink(ref l3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test5f(Cls5f c)
    {
        Struct5f l1 = default;
        Struct5f l2 = default;
        Struct5f l3 = default;

        for (int i = 0; i < 10; i++)
        {
            l1 = c.sf;
            l2 = c.sf;
            l3 = c.sf;
        }

        Sink(ref l1);
        Sink(ref l2);
        Sink(ref l3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test6f(Cls6f c)
    {
        Struct6f l1 = default;
        Struct6f l2 = default;
        Struct6f l3 = default;

        for (int i = 0; i < 10; i++)
        {
            l1 = c.sf;
            l2 = c.sf;
            l3 = c.sf;
        }

        Sink(ref l1);
        Sink(ref l2);
        Sink(ref l3);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Cls1f cls1f = new Cls1f();
        Test1f(cls1f);

        Cls2f cls2f = new Cls2f();
        Test2f(cls2f);

        Cls3f cls3f = new Cls3f();
        Test3f(cls3f);

        Cls4f cls4f = new Cls4f();
        Test4f(cls4f);

        Cls5f cls5f = new Cls5f();
        Test5f(cls5f);

        Cls6f cls6f = new Cls6f();
        Test6f(cls6f);

        return 100;
    }
}
