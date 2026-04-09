// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using Xunit;


internal class NullableTest1
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((char?)o) == null;
    }

    public static void Run()
    {
        char? s = null;

        Console.WriteLine("char");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest2
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((bool?)o) == null;
    }

    public static void Run()
    {
        bool? s = null;

        Console.WriteLine("bool");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest3
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((byte?)o) == null;
    }

    public static void Run()
    {
        byte? s = null;

        Console.WriteLine("byte");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest4
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((sbyte?)o) == null;
    }

    public static void Run()
    {
        sbyte? s = null;

        Console.WriteLine("sbyte");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest5
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((short?)o) == null;
    }

    public static void Run()
    {
        short? s = null;

        Console.WriteLine("short");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest6
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ushort?)o) == null;
    }

    public static void Run()
    {
        ushort? s = null;

        Console.WriteLine("ushort");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest7
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((int?)o) == null;
    }

    public static void Run()
    {
        int? s = null;

        Console.WriteLine("int");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest8
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((uint?)o) == null;
    }

    public static void Run()
    {
        uint? s = null;

        Console.WriteLine("uint");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest9
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((long?)o) == null;
    }

    public static void Run()
    {
        long? s = null;

        Console.WriteLine("long");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest10
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ulong?)o) == null;
    }

    public static void Run()
    {
        ulong? s = null;

        Console.WriteLine("ulong");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest11
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((float?)o) == null;
    }

    public static void Run()
    {
        float? s = null;

        Console.WriteLine("float");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest12
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((double?)o) == null;
    }

    public static void Run()
    {
        double? s = null;

        Console.WriteLine("double");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest13
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((decimal?)o) == null;
    }

    public static void Run()
    {
        decimal? s = null;

        Console.WriteLine("decimal");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest14
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((IntPtr?)o) == null;
    }

    public static void Run()
    {
        IntPtr? s = null;

        Console.WriteLine("IntPtr");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest15
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((UIntPtr?)o) == null;
    }

    public static void Run()
    {
        UIntPtr? s = null;

        Console.WriteLine("UIntPtr");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest16
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((Guid?)o) == null;
    }

    public static void Run()
    {
        Guid? s = null;

        Console.WriteLine("Guid");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest17
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((GCHandle?)o) == null;
    }

    public static void Run()
    {
        GCHandle? s = null;

        Console.WriteLine("GCHandle");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest18
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ByteE?)o) == null;
    }

    public static void Run()
    {
        ByteE? s = null;

        Console.WriteLine("ByteE");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest19
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((IntE?)o) == null;
    }

    public static void Run()
    {
        IntE? s = null;

        Console.WriteLine("IntE");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest20
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((LongE?)o) == null;
    }

    public static void Run()
    {
        LongE? s = null;

        Console.WriteLine("LongE");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest21
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((EmptyStruct?)o) == null;
    }

    public static void Run()
    {
        EmptyStruct? s = null;

        Console.WriteLine("EmptyStruct");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest22
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStruct?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStruct? s = null;

        Console.WriteLine("NotEmptyStruct");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest23
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStructQ?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStructQ? s = null;

        Console.WriteLine("NotEmptyStructQ");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest24
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStructA?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStructA? s = null;

        Console.WriteLine("NotEmptyStructA");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest25
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStructQA?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStructQA? s = null;

        Console.WriteLine("NotEmptyStructQA");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest26
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((EmptyStructGen<int>?)o) == null;
    }

    public static void Run()
    {
        EmptyStructGen<int>? s = null;

        Console.WriteLine("EmptyStructGen<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest27
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStructGen<int>?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStructGen<int>? s = null;

        Console.WriteLine("NotEmptyStructGen<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest28
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStructConstrainedGen<int>?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStructConstrainedGen<int>? s = null;

        Console.WriteLine("NotEmptyStructConstrainedGen<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest29
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStructConstrainedGenA<int>?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStructConstrainedGenA<int>? s = null;

        Console.WriteLine("NotEmptyStructConstrainedGenA<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest30
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStructConstrainedGenQ<int>?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStructConstrainedGenQ<int>? s = null;

        Console.WriteLine("NotEmptyStructConstrainedGenQ<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest31
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NotEmptyStructConstrainedGenQA<int>?)o) == null;
    }

    public static void Run()
    {
        NotEmptyStructConstrainedGenQA<int>? s = null;

        Console.WriteLine("NotEmptyStructConstrainedGenQA<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest32
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NestedStruct?)o) == null;
    }

    public static void Run()
    {
        NestedStruct? s = null;

        Console.WriteLine("NestedStruct");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest33
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((NestedStructGen<int>?)o) == null;
    }

    public static void Run()
    {
        NestedStructGen<int>? s = null;

        Console.WriteLine("NestedStructGen<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest34
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ExplicitFieldOffsetStruct?)o) == null;
    }

    public static void Run()
    {
        ExplicitFieldOffsetStruct? s = null;

        Console.WriteLine("ExplicitFieldOffsetStruct");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest37
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((MarshalAsStruct?)o) == null;
    }

    public static void Run()
    {
        MarshalAsStruct? s = null;

        Console.WriteLine("MarshalAsStruct");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest38
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ImplementOneInterface?)o) == null;
    }

    public static void Run()
    {
        ImplementOneInterface? s = null;

        Console.WriteLine("ImplementOneInterface");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest39
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ImplementTwoInterface?)o) == null;
    }

    public static void Run()
    {
        ImplementTwoInterface? s = null;

        Console.WriteLine("ImplementTwoInterface");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest40
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ImplementOneInterfaceGen<int>?)o) == null;
    }

    public static void Run()
    {
        ImplementOneInterfaceGen<int>? s = null;

        Console.WriteLine("ImplementOneInterfaceGen<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest41
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ImplementTwoInterfaceGen<int>?)o) == null;
    }

    public static void Run()
    {
        ImplementTwoInterfaceGen<int>? s = null;

        Console.WriteLine("ImplementTwoInterfaceGen<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest42
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((ImplementAllInterface<int>?)o) == null;
    }

    public static void Run()
    {
        ImplementAllInterface<int>? s = null;

        Console.WriteLine("ImplementAllInterface<int>");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest43
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((WithMultipleGCHandleStruct?)o) == null;
    }

    public static void Run()
    {
        WithMultipleGCHandleStruct? s = null;

        Console.WriteLine("WithMultipleGCHandleStruct");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest44
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((WithOnlyFXTypeStruct?)o) == null;
    }

    public static void Run()
    {
        WithOnlyFXTypeStruct? s = null;

        Console.WriteLine("WithOnlyFXTypeStruct");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



internal class NullableTest45
{
    private static bool BoxUnboxToNQGen<T>(T o)
    {
        return ((object)o) == null;
    }

    private static bool BoxUnboxToQGen<T>(T? o) where T : struct
    {
        return ((T?)o) == null;
    }

    private static bool BoxUnboxToNQ(object o)
    {
        return o == null;
    }

    private static bool BoxUnboxToQ(object o)
    {
        return ((MixedAllStruct?)o) == null;
    }

    public static void Run()
    {
        MixedAllStruct? s = null;

        Console.WriteLine("MixedAllStruct");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));
        Assert.IsTrue(BoxUnboxToNQGen(s));
        Assert.IsTrue(BoxUnboxToQGen(s));
    }
}



public class Test_nullcomparaison
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            NullableTest1.Run();
            NullableTest2.Run();
            NullableTest3.Run();
            NullableTest4.Run();
            NullableTest5.Run();
            NullableTest6.Run();
            NullableTest7.Run();
            NullableTest8.Run();
            NullableTest9.Run();
            NullableTest10.Run();
            NullableTest11.Run();
            NullableTest12.Run();
            NullableTest13.Run();
            NullableTest14.Run();
            NullableTest15.Run();
            NullableTest16.Run();
            NullableTest17.Run();
            NullableTest18.Run();
            NullableTest19.Run();
            NullableTest20.Run();
            NullableTest21.Run();
            NullableTest22.Run();
            NullableTest23.Run();
            NullableTest24.Run();
            NullableTest25.Run();
            NullableTest26.Run();
            NullableTest27.Run();
            NullableTest28.Run();
            NullableTest29.Run();
            NullableTest30.Run();
            NullableTest31.Run();
            NullableTest32.Run();
            NullableTest33.Run();
            NullableTest34.Run();
            NullableTest37.Run();
            NullableTest38.Run();
            NullableTest39.Run();
            NullableTest40.Run();
            NullableTest41.Run();
            NullableTest42.Run();
            NullableTest43.Run();
            NullableTest44.Run();
            NullableTest45.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test FAILED");
            Console.WriteLine(ex);
            return 666;
        }
        Console.WriteLine("Test SUCCESS");
        return 100;
    }
}

