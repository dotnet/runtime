// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public class A
{
    public virtual int f0(int i)
    {
        return 1;
    }
}

public unsafe class B : A
{
    public override int f0(int i)
    {
        return i;
    }

    public static int f1(ref int i)
    {
        return i;
    }

    public int f(int i)
    {
        return f1(ref i);
    }
    public static int F1downBy1ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 4; i >= 1; i -= 1)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1downBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 1; i -= 2)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1upBy1le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 4; i += 1)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1upBy1lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 4; i += 1)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1downBy1gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i > 2; i -= 1)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1upBy2le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 5; i += 2)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1downBy2ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i >= 1; i -= 2)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1upBy2lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 5; i += 2)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1downBy2gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 10; i > 2; i -= 2)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1upBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 4; i += 1)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1downBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 2; i -= 1)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1upBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 5; i += 2)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1upBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 8; i += 3)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }
        return sum + i;
    }

    public static int F1downBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 8; i != 1; i -= 3)
        {
            Object c = new Object(); c = amount; sum += Convert.ToInt32(c);
        }

        return sum + i;
    }

    public static int F2downBy1ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 4; i >= 1; i -= 1)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2downBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 1; i -= 2)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2upBy1le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 4; i += 1)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2upBy1lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 4; i += 1)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2downBy1gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i > 2; i -= 1)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2upBy2le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 5; i += 2)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2downBy2ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i >= 1; i -= 2)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2upBy2lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 5; i += 2)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2downBy2gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 10; i > 2; i -= 2)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2upBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 4; i += 1)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2downBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 2; i -= 1)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2upBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 5; i += 2)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }
        return sum + i;
    }

    public static int F2upBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 8; i += 3)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }

        return sum + i;
    }

    public static int F2downBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 8; i != 1; i -= 3)
        {
            int* n = stackalloc int[1]; *n = amount; sum += amount;
        }

        return sum + i;
    }

    public static int F3downBy1ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 4; i >= 1; i -= 1)
        {
            int[] n = new int[i];
        }
        for (i = 4; i >= 1; i -= 1)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3downBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 1; i -= 2)
        {
            int[] n = new int[i];
        }
        for (i = 5; i != 1; i -= 2)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3upBy1le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 4; i += 1)
        {
            int[] n = new int[i];
        }
        for (i = 1; i <= 4; i += 1)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3upBy1lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 4; i += 1)
        {
            int[] n = new int[i];
        }
        for (i = 1; i < 4; i += 1)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3downBy1gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i > 2; i -= 1)
        {
            int[] n = new int[i];
        }
        for (i = 5; i > 2; i -= 1)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3upBy2le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 5; i += 2)
        {
            int[] n = new int[i];
        }
        for (i = 1; i <= 5; i += 2)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3downBy2ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i >= 1; i -= 2)
        {
            int[] n = new int[i];
        }
        for (i = 5; i >= 1; i -= 2)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3upBy2lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 5; i += 2)
        {
            int[] n = new int[i];
        }
        for (i = 1; i < 5; i += 2)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3downBy2gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 10; i > 2; i -= 2)
        {
            int[] n = new int[i];
        }
        for (i = 10; i > 2; i -= 2)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3upBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 4; i += 1)
        {
            int[] n = new int[i];
        }
        for (i = 1; i != 4; i += 1)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3downBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 2; i -= 1)
        {
            int[] n = new int[i];
        }
        for (i = 5; i != 2; i -= 1)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3upBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 5; i += 2)
        {
            int[] n = new int[i];
        }
        for (i = 1; i != 5; i += 2)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3upBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 10; i += 3)
        {
            int[] n = new int[i];
        }
        for (i = 1; i != 8; i += 3)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F3downBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 10; i != 1; i -= 3)
        {
            int[] n = new int[i];
        }
        for (i = 8; i != 1; i -= 3)
        {
            sum += amount;
        }
        return sum + i;
    }

    public static int F4downBy1ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 4; i >= 1; i -= 1)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4downBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 1; i -= 2)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4upBy1le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 4; i += 1)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4upBy1lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 4; i += 1)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4downBy1gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i > 2; i -= 1)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4upBy2le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 5; i += 2)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4downBy2ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i >= 1; i -= 2)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4upBy2lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 5; i += 2)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4downBy2gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 10; i > 2; i -= 2)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4upBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 4; i += 1)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4downBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 2; i -= 1)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4upBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 5; i += 2)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4upBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 8; i += 3)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F4downBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 8; i != 1; i -= 3)
        {
            TypedReference _ref = __makeref(sum); __refvalue(_ref, int) += amount;
        }
        return sum + i;
    }

    public static int F5downBy1ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 4; i >= 1; i -= 1)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5downBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 1; i -= 2)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5upBy1le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 4; i += 1)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5upBy1lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 4; i += 1)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5downBy1gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i > 2; i -= 1)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5upBy2le(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i <= 5; i += 2)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5downBy2ge(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i >= 1; i -= 2)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5upBy2lt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i < 5; i += 2)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5downBy2gt(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 10; i > 2; i -= 2)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5upBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 4; i += 1)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5downBy1ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 5; i != 2; i -= 1)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5upBy2ne(int amount)
    {
        int i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 5; i += 2)
        {
            try { sum += amount; } catch { }
        }
        return sum + i;
    }

    public static int F5upBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 1; i != 8; i += 3)
        {
            try { sum += amount; } catch { }
        }

        return sum + i;
    }

    public static int F5downBy3neWrap(int amount)
    {
        short i;
        int sum = 0;
        B b = new B();
        for (i = 8; i != 1; i -= 3)
        {
            try { sum += amount; } catch { }
        }

        return sum + i;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool failed = false;

        if (F1upBy1le(10) != 45)
        {
            Console.WriteLine("F1upBy1le failed");
            failed = true;
        }
        if (F1downBy1ge(10) != 40)
        {
            Console.WriteLine("F1downBy1ge failed");
            failed = true;
        }
        if (F1upBy1lt(10) != 34)
        {
            Console.WriteLine("F1upBy1lt failed");
            failed = true;
        }
        if (F1downBy1gt(10) != 32)
        {
            Console.WriteLine("F1downBy1gt failed");
            failed = true;
        }
        if (F1upBy2le(10) != 37)
        {
            Console.WriteLine("F1upBy2le failed");
            failed = true;
        }
        if (F1downBy2ge(10) != 29)
        {
            Console.WriteLine("F1downBy2ge failed");
            failed = true;
        }
        if (F1upBy2lt(10) != 25)
        {
            Console.WriteLine("F1upBy2lt failed");
            failed = true;
        }
        if (F1downBy2gt(10) != 42)
        {
            Console.WriteLine("F1downBy2gt failed");
            failed = true;
        }
        if (F1upBy1ne(10) != 34)
        {
            Console.WriteLine("F1upBy1ne failed");
            failed = true;
        }
        if (F1downBy1ne(10) != 32)
        {
            Console.WriteLine("F1downBy1ne failed");
            failed = true;
        }
        if (F1upBy2ne(10) != 25)
        {
            Console.WriteLine("F1upBy2ne failed");
            failed = true;
        }
        if (F1downBy2ne(10) != 21)
        {
            Console.WriteLine("F1downBy2ne failed");
            failed = true;
        }
        if (F1upBy3neWrap(1) != 43701)
        {
            Console.WriteLine("F1upBy3neWrap failed");
            failed = true;
        }
        if (F1downBy3neWrap(1) != 43694)
        {
            Console.WriteLine("F1downBy3neWrap failed");
            failed = true;
        }

        if (F2upBy1le(10) != 45)
        {
            Console.WriteLine("F2upBy1le failed");
            failed = true;
        }
        if (F2downBy1ge(10) != 40)
        {
            Console.WriteLine("F2downBy1ge failed");
            failed = true;
        }
        if (F2upBy1lt(10) != 34)
        {
            Console.WriteLine("F2upBy1lt failed");
            failed = true;
        }
        if (F2downBy1gt(10) != 32)
        {
            Console.WriteLine("F2downBy1gt failed");
            failed = true;
        }
        if (F2upBy2le(10) != 37)
        {
            Console.WriteLine("F2upBy2le failed");
            failed = true;
        }
        if (F2downBy2ge(10) != 29)
        {
            Console.WriteLine("F2downBy2ge failed");
            failed = true;
        }
        if (F2upBy2lt(10) != 25)
        {
            Console.WriteLine("F2upBy2lt failed");
            failed = true;
        }
        if (F2downBy2gt(10) != 42)
        {
            Console.WriteLine("F2downBy2gt failed");
            failed = true;
        }
        if (F2upBy1ne(10) != 34)
        {
            Console.WriteLine("F2upBy1ne failed");
            failed = true;
        }
        if (F2downBy1ne(10) != 32)
        {
            Console.WriteLine("F2downBy1ne failed");
            failed = true;
        }
        if (F2upBy2ne(10) != 25)
        {
            Console.WriteLine("F2upBy2ne failed");
            failed = true;
        }
        if (F2downBy2ne(10) != 21)
        {
            Console.WriteLine("F2downBy2ne failed");
            failed = true;
        }
        if (F2upBy3neWrap(1) != 43701)
        {
            Console.WriteLine("F2upBy3neWrap failed");
            failed = true;
        }
        if (F2downBy3neWrap(1) != 43694)
        {
            Console.WriteLine("F2downBy3neWrap failed");
            failed = true;
        }

        if (F3upBy1le(10) != 45)
        {
            Console.WriteLine("F3upBy1le failed");
            failed = true;
        }
        if (F3downBy1ge(10) != 40)
        {
            Console.WriteLine("F3downBy1ge failed");
            failed = true;
        }
        if (F3upBy1lt(10) != 34)
        {
            Console.WriteLine("F3upBy1lt failed");
            failed = true;
        }
        if (F3downBy1gt(10) != 32)
        {
            Console.WriteLine("F3downBy1gt failed");
            failed = true;
        }
        if (F3upBy2le(10) != 37)
        {
            Console.WriteLine("F3upBy2le failed");
            failed = true;
        }
        if (F3downBy2ge(10) != 29)
        {
            Console.WriteLine("F3downBy2ge failed");
            failed = true;
        }
        if (F3upBy2lt(10) != 25)
        {
            Console.WriteLine("F3upBy2lt failed");
            failed = true;
        }
        if (F3downBy2gt(10) != 42)
        {
            Console.WriteLine("F3downBy2gt failed");
            failed = true;
        }
        if (F3upBy1ne(10) != 34)
        {
            Console.WriteLine("F3upBy1ne failed");
            failed = true;
        }
        if (F3downBy1ne(10) != 32)
        {
            Console.WriteLine("F3downBy1ne failed");
            failed = true;
        }
        if (F3upBy2ne(10) != 25)
        {
            Console.WriteLine("F3upBy2ne failed");
            failed = true;
        }
        if (F3downBy2ne(10) != 21)
        {
            Console.WriteLine("F3downBy2ne failed");
            failed = true;
        }
        if (F3upBy3neWrap(1) != 43701)
        {
            Console.WriteLine("F3upBy3neWrap failed");
            failed = true;
        }
        if (F3downBy3neWrap(1) != 43694)
        {
            Console.WriteLine("F3downBy3neWrap failed");
            failed = true;
        }

        if (F4upBy1le(10) != 45)
        {
            Console.WriteLine("F4upBy1le failed");
            failed = true;
        }
        if (F4downBy1ge(10) != 40)
        {
            Console.WriteLine("F4downBy1ge failed");
            failed = true;
        }
        if (F4upBy1lt(10) != 34)
        {
            Console.WriteLine("F4upBy1lt failed");
            failed = true;
        }
        if (F4downBy1gt(10) != 32)
        {
            Console.WriteLine("F4downBy1gt failed");
            failed = true;
        }
        if (F4upBy2le(10) != 37)
        {
            Console.WriteLine("F4upBy2le failed");
            failed = true;
        }
        if (F4downBy2ge(10) != 29)
        {
            Console.WriteLine("F4downBy2ge failed");
            failed = true;
        }
        if (F4upBy2lt(10) != 25)
        {
            Console.WriteLine("F4upBy2lt failed");
            failed = true;
        }
        if (F4downBy2gt(10) != 42)
        {
            Console.WriteLine("F4downBy2gt failed");
            failed = true;
        }
        if (F4upBy1ne(10) != 34)
        {
            Console.WriteLine("F4upBy1ne failed");
            failed = true;
        }
        if (F4downBy1ne(10) != 32)
        {
            Console.WriteLine("F4downBy1ne failed");
            failed = true;
        }
        if (F4upBy2ne(10) != 25)
        {
            Console.WriteLine("F4upBy2ne failed");
            failed = true;
        }
        if (F4downBy2ne(10) != 21)
        {
            Console.WriteLine("F4downBy2ne failed");
            failed = true;
        }
        if (F4upBy3neWrap(1) != 43701)
        {
            Console.WriteLine("F4upBy3neWrap failed");
            failed = true;
        }
        if (F4downBy3neWrap(1) != 43694)
        {
            Console.WriteLine("F4downBy3neWrap failed");
            failed = true;
        }

        if (F5upBy1le(10) != 45)
        {
            Console.WriteLine("F5upBy1le failed");
            failed = true;
        }
        if (F5downBy1ge(10) != 40)
        {
            Console.WriteLine("F5downBy1ge failed");
            failed = true;
        }
        if (F5upBy1lt(10) != 34)
        {
            Console.WriteLine("F5upBy1lt failed");
            failed = true;
        }
        if (F5downBy1gt(10) != 32)
        {
            Console.WriteLine("F5downBy1gt failed");
            failed = true;
        }
        if (F5upBy2le(10) != 37)
        {
            Console.WriteLine("F5upBy2le failed");
            failed = true;
        }
        if (F5downBy2ge(10) != 29)
        {
            Console.WriteLine("F5downBy2ge failed");
            failed = true;
        }
        if (F5upBy2lt(10) != 25)
        {
            Console.WriteLine("F5upBy2lt failed");
            failed = true;
        }
        if (F5downBy2gt(10) != 42)
        {
            Console.WriteLine("F5downBy2gt failed");
            failed = true;
        }
        if (F5upBy1ne(10) != 34)
        {
            Console.WriteLine("F5upBy1ne failed");
            failed = true;
        }
        if (F5downBy1ne(10) != 32)
        {
            Console.WriteLine("F5downBy1ne failed");
            failed = true;
        }
        if (F5upBy2ne(10) != 25)
        {
            Console.WriteLine("F5upBy2ne failed");
            failed = true;
        }
        if (F5downBy2ne(10) != 21)
        {
            Console.WriteLine("F5downBy2ne failed");
            failed = true;
        }
        if (F5upBy3neWrap(1) != 43701)
        {
            Console.WriteLine("F5upBy3neWrap failed");
            failed = true;
        }
        if (F5downBy3neWrap(1) != 43694)
        {
            Console.WriteLine("F5downBy3neWrap failed");
            failed = true;
        }

        if (!failed)
        {
            Console.WriteLine();
            Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Failed");
            return 1;
        }
    }
}

