// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using Xunit;


internal class NullableTest1
{
    private static bool BoxUnboxToNQ(IEmpty o)
    {
        try
        {
            return Helper.Compare((ImplementOneInterface)(ValueType)(object)o, Helper.Create(default(ImplementOneInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IEmpty o)
    {
        return Helper.Compare((ImplementOneInterface?)(ValueType)(object)o, Helper.Create(default(ImplementOneInterface)));
    }

    public static void Run()
    {
        ImplementOneInterface? s = Helper.Create(default(ImplementOneInterface));

        Console.WriteLine("--- ImplementOneInterface? s = Helper.Create(default(ImplementOneInterface)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- ImplementOneInterface? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        ImplementOneInterface u = Helper.Create(default(ImplementOneInterface));

        Console.WriteLine("--- ImplementOneInterface u = Helper.Create(default(ImplementOneInterface)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest2
{
    private static bool BoxUnboxToNQ(IEmpty o)
    {
        try
        {
            return Helper.Compare((ImplementTwoInterface)(ValueType)(object)o, Helper.Create(default(ImplementTwoInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IEmpty o)
    {
        return Helper.Compare((ImplementTwoInterface?)(ValueType)(object)o, Helper.Create(default(ImplementTwoInterface)));
    }

    public static void Run()
    {
        ImplementTwoInterface? s = Helper.Create(default(ImplementTwoInterface));

        Console.WriteLine("--- ImplementTwoInterface? s = Helper.Create(default(ImplementTwoInterface)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- ImplementTwoInterface? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        ImplementTwoInterface u = Helper.Create(default(ImplementTwoInterface));

        Console.WriteLine("--- ImplementTwoInterface u = Helper.Create(default(ImplementTwoInterface)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest3
{
    private static bool BoxUnboxToNQ(IEmptyGen<int> o)
    {
        try
        {
            return Helper.Compare((ImplementOneInterfaceGen<int>)(ValueType)(object)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IEmptyGen<int> o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>?)(ValueType)(object)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    public static void Run()
    {
        ImplementOneInterfaceGen<int>? s = Helper.Create(default(ImplementOneInterfaceGen<int>));

        Console.WriteLine("--- ImplementOneInterfaceGen<int>? s = Helper.Create(default(ImplementOneInterfaceGen<int>)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- ImplementOneInterfaceGen<int>? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        ImplementOneInterfaceGen<int> u = Helper.Create(default(ImplementOneInterfaceGen<int>));

        Console.WriteLine("--- ImplementOneInterfaceGen<int> u = Helper.Create(default(ImplementOneInterfaceGen<int>)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest4
{
    private static bool BoxUnboxToNQ(IEmptyGen<int> o)
    {
        try
        {
            return Helper.Compare((ImplementTwoInterfaceGen<int>)(ValueType)(object)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IEmptyGen<int> o)
    {
        return Helper.Compare((ImplementTwoInterfaceGen<int>?)(ValueType)(object)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
    }

    public static void Run()
    {
        ImplementTwoInterfaceGen<int>? s = Helper.Create(default(ImplementTwoInterfaceGen<int>));

        Console.WriteLine("--- ImplementTwoInterfaceGen<int>? s = Helper.Create(default(ImplementTwoInterfaceGen<int>)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- ImplementTwoInterfaceGen<int>? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        ImplementTwoInterfaceGen<int> u = Helper.Create(default(ImplementTwoInterfaceGen<int>));

        Console.WriteLine("--- ImplementTwoInterfaceGen<int> u = Helper.Create(default(ImplementTwoInterfaceGen<int>)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest5
{
    private static bool BoxUnboxToNQ(IEmpty o)
    {
        try
        {
            return Helper.Compare((ImplementAllInterface<int>)(ValueType)(object)o, Helper.Create(default(ImplementAllInterface<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IEmpty o)
    {
        return Helper.Compare((ImplementAllInterface<int>?)(ValueType)(object)o, Helper.Create(default(ImplementAllInterface<int>)));
    }

    public static void Run()
    {
        ImplementAllInterface<int>? s = Helper.Create(default(ImplementAllInterface<int>));

        Console.WriteLine("--- ImplementAllInterface<int>? s = Helper.Create(default(ImplementAllInterface<int>)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- ImplementAllInterface<int>? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        ImplementAllInterface<int> u = Helper.Create(default(ImplementAllInterface<int>));

        Console.WriteLine("--- ImplementAllInterface<int> u = Helper.Create(default(ImplementAllInterface<int>)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest6
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((char)(ValueType)(object)o, Helper.Create(default(char)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((char?)(ValueType)(object)o, Helper.Create(default(char)));
    }

    public static void Run()
    {
        char? s = Helper.Create(default(char));

        Console.WriteLine("--- char? s = Helper.Create(default(char)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- char? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        char u = Helper.Create(default(char));

        Console.WriteLine("--- char u = Helper.Create(default(char)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest7
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((bool)(ValueType)(object)o, Helper.Create(default(bool)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((bool?)(ValueType)(object)o, Helper.Create(default(bool)));
    }

    public static void Run()
    {
        bool? s = Helper.Create(default(bool));

        Console.WriteLine("--- bool? s = Helper.Create(default(bool)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- bool? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        bool u = Helper.Create(default(bool));

        Console.WriteLine("--- bool u = Helper.Create(default(bool)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest8
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((byte)(ValueType)(object)o, Helper.Create(default(byte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((byte?)(ValueType)(object)o, Helper.Create(default(byte)));
    }

    public static void Run()
    {
        byte? s = Helper.Create(default(byte));

        Console.WriteLine("--- byte? s = Helper.Create(default(byte)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- byte? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        byte u = Helper.Create(default(byte));

        Console.WriteLine("--- byte u = Helper.Create(default(byte)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest9
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((sbyte)(ValueType)(object)o, Helper.Create(default(sbyte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((sbyte?)(ValueType)(object)o, Helper.Create(default(sbyte)));
    }

    public static void Run()
    {
        sbyte? s = Helper.Create(default(sbyte));

        Console.WriteLine("--- sbyte? s = Helper.Create(default(sbyte)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- sbyte? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        sbyte u = Helper.Create(default(sbyte));

        Console.WriteLine("--- sbyte u = Helper.Create(default(sbyte)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest10
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((short)(ValueType)(object)o, Helper.Create(default(short)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((short?)(ValueType)(object)o, Helper.Create(default(short)));
    }

    public static void Run()
    {
        short? s = Helper.Create(default(short));

        Console.WriteLine("--- short? s = Helper.Create(default(short)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- short? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        short u = Helper.Create(default(short));

        Console.WriteLine("--- short u = Helper.Create(default(short)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest11
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((ushort)(ValueType)(object)o, Helper.Create(default(ushort)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((ushort?)(ValueType)(object)o, Helper.Create(default(ushort)));
    }

    public static void Run()
    {
        ushort? s = Helper.Create(default(ushort));

        Console.WriteLine("--- ushort? s = Helper.Create(default(ushort)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- ushort? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        ushort u = Helper.Create(default(ushort));

        Console.WriteLine("--- ushort u = Helper.Create(default(ushort)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest12
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((int)(ValueType)(object)o, Helper.Create(default(int)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((int?)(ValueType)(object)o, Helper.Create(default(int)));
    }

    public static void Run()
    {
        int? s = Helper.Create(default(int));

        Console.WriteLine("--- int? s = Helper.Create(default(int)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- int? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        int u = Helper.Create(default(int));

        Console.WriteLine("--- int u = Helper.Create(default(int)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest13
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((uint)(ValueType)(object)o, Helper.Create(default(uint)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((uint?)(ValueType)(object)o, Helper.Create(default(uint)));
    }

    public static void Run()
    {
        uint? s = Helper.Create(default(uint));

        Console.WriteLine("--- uint? s = Helper.Create(default(uint)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- uint? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        uint u = Helper.Create(default(uint));

        Console.WriteLine("--- uint u = Helper.Create(default(uint)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest14
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((long)(ValueType)(object)o, Helper.Create(default(long)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((long?)(ValueType)(object)o, Helper.Create(default(long)));
    }

    public static void Run()
    {
        long? s = Helper.Create(default(long));

        Console.WriteLine("--- long? s = Helper.Create(default(long)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- long? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        long u = Helper.Create(default(long));

        Console.WriteLine("--- long u = Helper.Create(default(long)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest15
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((ulong)(ValueType)(object)o, Helper.Create(default(ulong)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((ulong?)(ValueType)(object)o, Helper.Create(default(ulong)));
    }

    public static void Run()
    {
        ulong? s = Helper.Create(default(ulong));

        Console.WriteLine("--- ulong? s = Helper.Create(default(ulong)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- ulong? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        ulong u = Helper.Create(default(ulong));

        Console.WriteLine("--- ulong u = Helper.Create(default(ulong)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest16
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((float)(ValueType)(object)o, Helper.Create(default(float)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((float?)(ValueType)(object)o, Helper.Create(default(float)));
    }

    public static void Run()
    {
        float? s = Helper.Create(default(float));

        Console.WriteLine("--- float? s = Helper.Create(default(float)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- float? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        float u = Helper.Create(default(float));

        Console.WriteLine("--- float u = Helper.Create(default(float)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest17
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((double)(ValueType)(object)o, Helper.Create(default(double)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((double?)(ValueType)(object)o, Helper.Create(default(double)));
    }

    public static void Run()
    {
        double? s = Helper.Create(default(double));

        Console.WriteLine("--- double? s = Helper.Create(default(double)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- double? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        double u = Helper.Create(default(double));

        Console.WriteLine("--- double u = Helper.Create(default(double)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest18
{
    private static bool BoxUnboxToNQ(IComparable o)
    {
        try
        {
            return Helper.Compare((decimal)(ValueType)(object)o, Helper.Create(default(decimal)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(IComparable o)
    {
        return Helper.Compare((decimal?)(ValueType)(object)o, Helper.Create(default(decimal)));
    }

    public static void Run()
    {
        decimal? s = Helper.Create(default(decimal));

        Console.WriteLine("--- decimal? s = Helper.Create(default(decimal)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- decimal? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        decimal u = Helper.Create(default(decimal));

        Console.WriteLine("--- decimal u = Helper.Create(default(decimal)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



public class Test_castclassinterface
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

