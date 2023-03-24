// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System;
using Xunit;


internal class NullableTest1
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((char)o, Helper.Create(default(char)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((char?)o, Helper.Create(default(char)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((char)o, Helper.Create(default(char)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((char?)o, Helper.Create(default(char)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((char)(object)o, Helper.Create(default(char)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((char?)(object)o, Helper.Create(default(char)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((char)(object)o, Helper.Create(default(char)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((char?)(object)o, Helper.Create(default(char)));
    }

    public static void Run()
    {
        char? s = Helper.Create(default(char));

        Console.WriteLine("--- char? s = Helper.Create(default(char)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- char? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- char u = Helper.Create(default(char)) ---");
        char u = Helper.Create(default(char));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<char>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<char>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest2
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((bool)o, Helper.Create(default(bool)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((bool?)o, Helper.Create(default(bool)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((bool)o, Helper.Create(default(bool)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((bool?)o, Helper.Create(default(bool)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((bool)(object)o, Helper.Create(default(bool)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((bool?)(object)o, Helper.Create(default(bool)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((bool)(object)o, Helper.Create(default(bool)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((bool?)(object)o, Helper.Create(default(bool)));
    }

    public static void Run()
    {
        bool? s = Helper.Create(default(bool));

        Console.WriteLine("--- bool? s = Helper.Create(default(bool)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- bool? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- bool u = Helper.Create(default(bool)) ---");
        bool u = Helper.Create(default(bool));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<bool>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<bool>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest3
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((byte)o, Helper.Create(default(byte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((byte?)o, Helper.Create(default(byte)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((byte)o, Helper.Create(default(byte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((byte?)o, Helper.Create(default(byte)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((byte)(object)o, Helper.Create(default(byte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((byte?)(object)o, Helper.Create(default(byte)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((byte)(object)o, Helper.Create(default(byte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((byte?)(object)o, Helper.Create(default(byte)));
    }

    public static void Run()
    {
        byte? s = Helper.Create(default(byte));

        Console.WriteLine("--- byte? s = Helper.Create(default(byte)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- byte? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- byte u = Helper.Create(default(byte)) ---");
        byte u = Helper.Create(default(byte));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<byte>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<byte>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest4
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((sbyte)o, Helper.Create(default(sbyte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((sbyte?)o, Helper.Create(default(sbyte)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((sbyte)o, Helper.Create(default(sbyte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((sbyte?)o, Helper.Create(default(sbyte)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((sbyte)(object)o, Helper.Create(default(sbyte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((sbyte?)(object)o, Helper.Create(default(sbyte)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((sbyte)(object)o, Helper.Create(default(sbyte)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((sbyte?)(object)o, Helper.Create(default(sbyte)));
    }

    public static void Run()
    {
        sbyte? s = Helper.Create(default(sbyte));

        Console.WriteLine("--- sbyte? s = Helper.Create(default(sbyte)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- sbyte? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- sbyte u = Helper.Create(default(sbyte)) ---");
        sbyte u = Helper.Create(default(sbyte));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<sbyte>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<sbyte>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest5
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((short)o, Helper.Create(default(short)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((short?)o, Helper.Create(default(short)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((short)o, Helper.Create(default(short)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((short?)o, Helper.Create(default(short)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((short)(object)o, Helper.Create(default(short)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((short?)(object)o, Helper.Create(default(short)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((short)(object)o, Helper.Create(default(short)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((short?)(object)o, Helper.Create(default(short)));
    }

    public static void Run()
    {
        short? s = Helper.Create(default(short));

        Console.WriteLine("--- short? s = Helper.Create(default(short)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- short? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- short u = Helper.Create(default(short)) ---");
        short u = Helper.Create(default(short));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<short>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<short>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest6
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ushort)o, Helper.Create(default(ushort)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ushort?)o, Helper.Create(default(ushort)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ushort)o, Helper.Create(default(ushort)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ushort?)o, Helper.Create(default(ushort)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ushort)(object)o, Helper.Create(default(ushort)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ushort?)(object)o, Helper.Create(default(ushort)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ushort)(object)o, Helper.Create(default(ushort)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ushort?)(object)o, Helper.Create(default(ushort)));
    }

    public static void Run()
    {
        ushort? s = Helper.Create(default(ushort));

        Console.WriteLine("--- ushort? s = Helper.Create(default(ushort)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ushort? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ushort u = Helper.Create(default(ushort)) ---");
        ushort u = Helper.Create(default(ushort));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ushort>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ushort>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest7
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((int)o, Helper.Create(default(int)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((int?)o, Helper.Create(default(int)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((int)o, Helper.Create(default(int)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((int?)o, Helper.Create(default(int)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((int)(object)o, Helper.Create(default(int)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((int?)(object)o, Helper.Create(default(int)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((int)(object)o, Helper.Create(default(int)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((int?)(object)o, Helper.Create(default(int)));
    }

    public static void Run()
    {
        int? s = Helper.Create(default(int));

        Console.WriteLine("--- int? s = Helper.Create(default(int)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- int? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- int u = Helper.Create(default(int)) ---");
        int u = Helper.Create(default(int));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<int>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<int>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest8
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((uint)o, Helper.Create(default(uint)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((uint?)o, Helper.Create(default(uint)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((uint)o, Helper.Create(default(uint)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((uint?)o, Helper.Create(default(uint)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((uint)(object)o, Helper.Create(default(uint)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((uint?)(object)o, Helper.Create(default(uint)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((uint)(object)o, Helper.Create(default(uint)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((uint?)(object)o, Helper.Create(default(uint)));
    }

    public static void Run()
    {
        uint? s = Helper.Create(default(uint));

        Console.WriteLine("--- uint? s = Helper.Create(default(uint)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- uint? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- uint u = Helper.Create(default(uint)) ---");
        uint u = Helper.Create(default(uint));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<uint>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<uint>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest9
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((long)o, Helper.Create(default(long)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((long?)o, Helper.Create(default(long)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((long)o, Helper.Create(default(long)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((long?)o, Helper.Create(default(long)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((long)(object)o, Helper.Create(default(long)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((long?)(object)o, Helper.Create(default(long)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((long)(object)o, Helper.Create(default(long)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((long?)(object)o, Helper.Create(default(long)));
    }

    public static void Run()
    {
        long? s = Helper.Create(default(long));

        Console.WriteLine("--- long? s = Helper.Create(default(long)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- long? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- long u = Helper.Create(default(long)) ---");
        long u = Helper.Create(default(long));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<long>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<long>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest10
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ulong)o, Helper.Create(default(ulong)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ulong?)o, Helper.Create(default(ulong)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ulong)o, Helper.Create(default(ulong)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ulong?)o, Helper.Create(default(ulong)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ulong)(object)o, Helper.Create(default(ulong)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ulong?)(object)o, Helper.Create(default(ulong)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ulong)(object)o, Helper.Create(default(ulong)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ulong?)(object)o, Helper.Create(default(ulong)));
    }

    public static void Run()
    {
        ulong? s = Helper.Create(default(ulong));

        Console.WriteLine("--- ulong? s = Helper.Create(default(ulong)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ulong? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ulong u = Helper.Create(default(ulong)) ---");
        ulong u = Helper.Create(default(ulong));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ulong>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ulong>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest11
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((float)o, Helper.Create(default(float)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((float?)o, Helper.Create(default(float)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((float)o, Helper.Create(default(float)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((float?)o, Helper.Create(default(float)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((float)(object)o, Helper.Create(default(float)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((float?)(object)o, Helper.Create(default(float)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((float)(object)o, Helper.Create(default(float)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((float?)(object)o, Helper.Create(default(float)));
    }

    public static void Run()
    {
        float? s = Helper.Create(default(float));

        Console.WriteLine("--- float? s = Helper.Create(default(float)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- float? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- float u = Helper.Create(default(float)) ---");
        float u = Helper.Create(default(float));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<float>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<float>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest12
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((double)o, Helper.Create(default(double)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((double?)o, Helper.Create(default(double)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((double)o, Helper.Create(default(double)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((double?)o, Helper.Create(default(double)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((double)(object)o, Helper.Create(default(double)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((double?)(object)o, Helper.Create(default(double)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((double)(object)o, Helper.Create(default(double)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((double?)(object)o, Helper.Create(default(double)));
    }

    public static void Run()
    {
        double? s = Helper.Create(default(double));

        Console.WriteLine("--- double? s = Helper.Create(default(double)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- double? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- double u = Helper.Create(default(double)) ---");
        double u = Helper.Create(default(double));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<double>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<double>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest13
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((decimal)o, Helper.Create(default(decimal)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((decimal?)o, Helper.Create(default(decimal)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((decimal)o, Helper.Create(default(decimal)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((decimal?)o, Helper.Create(default(decimal)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((decimal)(object)o, Helper.Create(default(decimal)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((decimal?)(object)o, Helper.Create(default(decimal)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((decimal)(object)o, Helper.Create(default(decimal)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((decimal?)(object)o, Helper.Create(default(decimal)));
    }

    public static void Run()
    {
        decimal? s = Helper.Create(default(decimal));

        Console.WriteLine("--- decimal? s = Helper.Create(default(decimal)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- decimal? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- decimal u = Helper.Create(default(decimal)) ---");
        decimal u = Helper.Create(default(decimal));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<decimal>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<decimal>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest14
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((IntPtr)o, Helper.Create(default(IntPtr)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((IntPtr?)o, Helper.Create(default(IntPtr)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((IntPtr)o, Helper.Create(default(IntPtr)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((IntPtr?)o, Helper.Create(default(IntPtr)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((IntPtr)(object)o, Helper.Create(default(IntPtr)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((IntPtr?)(object)o, Helper.Create(default(IntPtr)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((IntPtr)(object)o, Helper.Create(default(IntPtr)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((IntPtr?)(object)o, Helper.Create(default(IntPtr)));
    }

    public static void Run()
    {
        IntPtr? s = Helper.Create(default(IntPtr));

        Console.WriteLine("--- IntPtr? s = Helper.Create(default(IntPtr)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- IntPtr? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- IntPtr u = Helper.Create(default(IntPtr)) ---");
        IntPtr u = Helper.Create(default(IntPtr));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<IntPtr>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<IntPtr>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest15
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((UIntPtr)o, Helper.Create(default(UIntPtr)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((UIntPtr?)o, Helper.Create(default(UIntPtr)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((UIntPtr)o, Helper.Create(default(UIntPtr)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((UIntPtr?)o, Helper.Create(default(UIntPtr)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((UIntPtr)(object)o, Helper.Create(default(UIntPtr)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((UIntPtr?)(object)o, Helper.Create(default(UIntPtr)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((UIntPtr)(object)o, Helper.Create(default(UIntPtr)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((UIntPtr?)(object)o, Helper.Create(default(UIntPtr)));
    }

    public static void Run()
    {
        UIntPtr? s = Helper.Create(default(UIntPtr));

        Console.WriteLine("--- UIntPtr? s = Helper.Create(default(UIntPtr)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- UIntPtr? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- UIntPtr u = Helper.Create(default(UIntPtr)) ---");
        UIntPtr u = Helper.Create(default(UIntPtr));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<UIntPtr>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<UIntPtr>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest16
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((Guid)o, Helper.Create(default(Guid)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((Guid?)o, Helper.Create(default(Guid)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((Guid)o, Helper.Create(default(Guid)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((Guid?)o, Helper.Create(default(Guid)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((Guid)(object)o, Helper.Create(default(Guid)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((Guid?)(object)o, Helper.Create(default(Guid)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((Guid)(object)o, Helper.Create(default(Guid)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((Guid?)(object)o, Helper.Create(default(Guid)));
    }

    public static void Run()
    {
        Guid? s = Helper.Create(default(Guid));

        Console.WriteLine("--- Guid? s = Helper.Create(default(Guid)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- Guid? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- Guid u = Helper.Create(default(Guid)) ---");
        Guid u = Helper.Create(default(Guid));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<Guid>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<Guid>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest17
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((GCHandle)o, Helper.Create(default(GCHandle)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((GCHandle?)o, Helper.Create(default(GCHandle)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((GCHandle)o, Helper.Create(default(GCHandle)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((GCHandle?)o, Helper.Create(default(GCHandle)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((GCHandle)(object)o, Helper.Create(default(GCHandle)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((GCHandle?)(object)o, Helper.Create(default(GCHandle)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((GCHandle)(object)o, Helper.Create(default(GCHandle)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((GCHandle?)(object)o, Helper.Create(default(GCHandle)));
    }

    public static void Run()
    {
        GCHandle? s = Helper.Create(default(GCHandle));

        Console.WriteLine("--- GCHandle? s = Helper.Create(default(GCHandle)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- GCHandle? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- GCHandle u = Helper.Create(default(GCHandle)) ---");
        GCHandle u = Helper.Create(default(GCHandle));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<GCHandle>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<GCHandle>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest18
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ByteE)o, Helper.Create(default(ByteE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ByteE?)o, Helper.Create(default(ByteE)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ByteE)o, Helper.Create(default(ByteE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ByteE?)o, Helper.Create(default(ByteE)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ByteE)(object)o, Helper.Create(default(ByteE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ByteE?)(object)o, Helper.Create(default(ByteE)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ByteE)(object)o, Helper.Create(default(ByteE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ByteE?)(object)o, Helper.Create(default(ByteE)));
    }

    public static void Run()
    {
        ByteE? s = Helper.Create(default(ByteE));

        Console.WriteLine("--- ByteE? s = Helper.Create(default(ByteE)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ByteE? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ByteE u = Helper.Create(default(ByteE)) ---");
        ByteE u = Helper.Create(default(ByteE));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ByteE>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ByteE>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest19
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((IntE)o, Helper.Create(default(IntE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((IntE?)o, Helper.Create(default(IntE)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((IntE)o, Helper.Create(default(IntE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((IntE?)o, Helper.Create(default(IntE)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((IntE)(object)o, Helper.Create(default(IntE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((IntE?)(object)o, Helper.Create(default(IntE)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((IntE)(object)o, Helper.Create(default(IntE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((IntE?)(object)o, Helper.Create(default(IntE)));
    }

    public static void Run()
    {
        IntE? s = Helper.Create(default(IntE));

        Console.WriteLine("--- IntE? s = Helper.Create(default(IntE)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- IntE? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- IntE u = Helper.Create(default(IntE)) ---");
        IntE u = Helper.Create(default(IntE));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<IntE>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<IntE>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest20
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((LongE)o, Helper.Create(default(LongE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((LongE?)o, Helper.Create(default(LongE)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((LongE)o, Helper.Create(default(LongE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((LongE?)o, Helper.Create(default(LongE)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((LongE)(object)o, Helper.Create(default(LongE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((LongE?)(object)o, Helper.Create(default(LongE)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((LongE)(object)o, Helper.Create(default(LongE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((LongE?)(object)o, Helper.Create(default(LongE)));
    }

    public static void Run()
    {
        LongE? s = Helper.Create(default(LongE));

        Console.WriteLine("--- LongE? s = Helper.Create(default(LongE)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- LongE? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- LongE u = Helper.Create(default(LongE)) ---");
        LongE u = Helper.Create(default(LongE));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<LongE>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<LongE>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest21
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((EmptyStruct)o, Helper.Create(default(EmptyStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((EmptyStruct?)o, Helper.Create(default(EmptyStruct)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((EmptyStruct)o, Helper.Create(default(EmptyStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((EmptyStruct?)o, Helper.Create(default(EmptyStruct)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((EmptyStruct)(object)o, Helper.Create(default(EmptyStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((EmptyStruct?)(object)o, Helper.Create(default(EmptyStruct)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((EmptyStruct)(object)o, Helper.Create(default(EmptyStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((EmptyStruct?)(object)o, Helper.Create(default(EmptyStruct)));
    }

    public static void Run()
    {
        EmptyStruct? s = Helper.Create(default(EmptyStruct));

        Console.WriteLine("--- EmptyStruct? s = Helper.Create(default(EmptyStruct)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- EmptyStruct? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- EmptyStruct u = Helper.Create(default(EmptyStruct)) ---");
        EmptyStruct u = Helper.Create(default(EmptyStruct));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<EmptyStruct>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<EmptyStruct>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest22
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStruct)o, Helper.Create(default(NotEmptyStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStruct?)o, Helper.Create(default(NotEmptyStruct)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStruct)o, Helper.Create(default(NotEmptyStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStruct?)o, Helper.Create(default(NotEmptyStruct)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStruct)(object)o, Helper.Create(default(NotEmptyStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStruct?)(object)o, Helper.Create(default(NotEmptyStruct)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStruct)(object)o, Helper.Create(default(NotEmptyStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStruct?)(object)o, Helper.Create(default(NotEmptyStruct)));
    }

    public static void Run()
    {
        NotEmptyStruct? s = Helper.Create(default(NotEmptyStruct));

        Console.WriteLine("--- NotEmptyStruct? s = Helper.Create(default(NotEmptyStruct)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStruct? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStruct u = Helper.Create(default(NotEmptyStruct)) ---");
        NotEmptyStruct u = Helper.Create(default(NotEmptyStruct));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStruct>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStruct>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest23
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructQ)o, Helper.Create(default(NotEmptyStructQ)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructQ?)o, Helper.Create(default(NotEmptyStructQ)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructQ)o, Helper.Create(default(NotEmptyStructQ)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStructQ?)o, Helper.Create(default(NotEmptyStructQ)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructQ)(object)o, Helper.Create(default(NotEmptyStructQ)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStructQ?)(object)o, Helper.Create(default(NotEmptyStructQ)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStructQ)(object)o, Helper.Create(default(NotEmptyStructQ)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStructQ?)(object)o, Helper.Create(default(NotEmptyStructQ)));
    }

    public static void Run()
    {
        NotEmptyStructQ? s = Helper.Create(default(NotEmptyStructQ));

        Console.WriteLine("--- NotEmptyStructQ? s = Helper.Create(default(NotEmptyStructQ)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructQ? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructQ u = Helper.Create(default(NotEmptyStructQ)) ---");
        NotEmptyStructQ u = Helper.Create(default(NotEmptyStructQ));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStructQ>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStructQ>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest24
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructA)o, Helper.Create(default(NotEmptyStructA)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructA?)o, Helper.Create(default(NotEmptyStructA)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructA)o, Helper.Create(default(NotEmptyStructA)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStructA?)o, Helper.Create(default(NotEmptyStructA)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructA)(object)o, Helper.Create(default(NotEmptyStructA)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStructA?)(object)o, Helper.Create(default(NotEmptyStructA)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStructA)(object)o, Helper.Create(default(NotEmptyStructA)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStructA?)(object)o, Helper.Create(default(NotEmptyStructA)));
    }

    public static void Run()
    {
        NotEmptyStructA? s = Helper.Create(default(NotEmptyStructA));

        Console.WriteLine("--- NotEmptyStructA? s = Helper.Create(default(NotEmptyStructA)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructA? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructA u = Helper.Create(default(NotEmptyStructA)) ---");
        NotEmptyStructA u = Helper.Create(default(NotEmptyStructA));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStructA>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStructA>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest25
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructQA)o, Helper.Create(default(NotEmptyStructQA)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructQA?)o, Helper.Create(default(NotEmptyStructQA)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructQA)o, Helper.Create(default(NotEmptyStructQA)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStructQA?)o, Helper.Create(default(NotEmptyStructQA)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructQA)(object)o, Helper.Create(default(NotEmptyStructQA)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStructQA?)(object)o, Helper.Create(default(NotEmptyStructQA)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStructQA)(object)o, Helper.Create(default(NotEmptyStructQA)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStructQA?)(object)o, Helper.Create(default(NotEmptyStructQA)));
    }

    public static void Run()
    {
        NotEmptyStructQA? s = Helper.Create(default(NotEmptyStructQA));

        Console.WriteLine("--- NotEmptyStructQA? s = Helper.Create(default(NotEmptyStructQA)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructQA? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructQA u = Helper.Create(default(NotEmptyStructQA)) ---");
        NotEmptyStructQA u = Helper.Create(default(NotEmptyStructQA));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStructQA>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStructQA>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest26
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((EmptyStructGen<int>)o, Helper.Create(default(EmptyStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((EmptyStructGen<int>?)o, Helper.Create(default(EmptyStructGen<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((EmptyStructGen<int>)o, Helper.Create(default(EmptyStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((EmptyStructGen<int>?)o, Helper.Create(default(EmptyStructGen<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((EmptyStructGen<int>)(object)o, Helper.Create(default(EmptyStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((EmptyStructGen<int>?)(object)o, Helper.Create(default(EmptyStructGen<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((EmptyStructGen<int>)(object)o, Helper.Create(default(EmptyStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((EmptyStructGen<int>?)(object)o, Helper.Create(default(EmptyStructGen<int>)));
    }

    public static void Run()
    {
        EmptyStructGen<int>? s = Helper.Create(default(EmptyStructGen<int>));

        Console.WriteLine("--- EmptyStructGen<int>? s = Helper.Create(default(EmptyStructGen<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- EmptyStructGen<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- EmptyStructGen<int> u = Helper.Create(default(EmptyStructGen<int>)) ---");
        EmptyStructGen<int> u = Helper.Create(default(EmptyStructGen<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<EmptyStructGen<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<EmptyStructGen<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest27
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructGen<int>)o, Helper.Create(default(NotEmptyStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructGen<int>?)o, Helper.Create(default(NotEmptyStructGen<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructGen<int>)o, Helper.Create(default(NotEmptyStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStructGen<int>?)o, Helper.Create(default(NotEmptyStructGen<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructGen<int>)(object)o, Helper.Create(default(NotEmptyStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStructGen<int>?)(object)o, Helper.Create(default(NotEmptyStructGen<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStructGen<int>)(object)o, Helper.Create(default(NotEmptyStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStructGen<int>?)(object)o, Helper.Create(default(NotEmptyStructGen<int>)));
    }

    public static void Run()
    {
        NotEmptyStructGen<int>? s = Helper.Create(default(NotEmptyStructGen<int>));

        Console.WriteLine("--- NotEmptyStructGen<int>? s = Helper.Create(default(NotEmptyStructGen<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructGen<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructGen<int> u = Helper.Create(default(NotEmptyStructGen<int>)) ---");
        NotEmptyStructGen<int> u = Helper.Create(default(NotEmptyStructGen<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStructGen<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStructGen<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest28
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGen<int>)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGen<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGen<int>)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGen<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGen<int>)(object)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGen<int>?)(object)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGen<int>)(object)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStructConstrainedGen<int>?)(object)o, Helper.Create(default(NotEmptyStructConstrainedGen<int>)));
    }

    public static void Run()
    {
        NotEmptyStructConstrainedGen<int>? s = Helper.Create(default(NotEmptyStructConstrainedGen<int>));

        Console.WriteLine("--- NotEmptyStructConstrainedGen<int>? s = Helper.Create(default(NotEmptyStructConstrainedGen<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructConstrainedGen<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructConstrainedGen<int> u = Helper.Create(default(NotEmptyStructConstrainedGen<int>)) ---");
        NotEmptyStructConstrainedGen<int> u = Helper.Create(default(NotEmptyStructConstrainedGen<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStructConstrainedGen<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStructConstrainedGen<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest29
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenA<int>)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenA<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenA<int>)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenA<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenA<int>)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenA<int>?)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenA<int>)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStructConstrainedGenA<int>?)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenA<int>)));
    }

    public static void Run()
    {
        NotEmptyStructConstrainedGenA<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenA<int>));

        Console.WriteLine("--- NotEmptyStructConstrainedGenA<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenA<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructConstrainedGenA<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructConstrainedGenA<int> u = Helper.Create(default(NotEmptyStructConstrainedGenA<int>)) ---");
        NotEmptyStructConstrainedGenA<int> u = Helper.Create(default(NotEmptyStructConstrainedGenA<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStructConstrainedGenA<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStructConstrainedGenA<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest30
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenQ<int>)o, Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQ<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenQ<int>)o, Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQ<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenQ<int>)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQ<int>?)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenQ<int>)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQ<int>?)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)));
    }

    public static void Run()
    {
        NotEmptyStructConstrainedGenQ<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenQ<int>));

        Console.WriteLine("--- NotEmptyStructConstrainedGenQ<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructConstrainedGenQ<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructConstrainedGenQ<int> u = Helper.Create(default(NotEmptyStructConstrainedGenQ<int>)) ---");
        NotEmptyStructConstrainedGenQ<int> u = Helper.Create(default(NotEmptyStructConstrainedGenQ<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStructConstrainedGenQ<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStructConstrainedGenQ<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest31
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenQA<int>)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQA<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenQA<int>)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQA<int>?)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenQA<int>)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQA<int>?)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NotEmptyStructConstrainedGenQA<int>)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NotEmptyStructConstrainedGenQA<int>?)(object)o, Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)));
    }

    public static void Run()
    {
        NotEmptyStructConstrainedGenQA<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenQA<int>));

        Console.WriteLine("--- NotEmptyStructConstrainedGenQA<int>? s = Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructConstrainedGenQA<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NotEmptyStructConstrainedGenQA<int> u = Helper.Create(default(NotEmptyStructConstrainedGenQA<int>)) ---");
        NotEmptyStructConstrainedGenQA<int> u = Helper.Create(default(NotEmptyStructConstrainedGenQA<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NotEmptyStructConstrainedGenQA<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NotEmptyStructConstrainedGenQA<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest32
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NestedStruct)o, Helper.Create(default(NestedStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NestedStruct?)o, Helper.Create(default(NestedStruct)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NestedStruct)o, Helper.Create(default(NestedStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NestedStruct?)o, Helper.Create(default(NestedStruct)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NestedStruct)(object)o, Helper.Create(default(NestedStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NestedStruct?)(object)o, Helper.Create(default(NestedStruct)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NestedStruct)(object)o, Helper.Create(default(NestedStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NestedStruct?)(object)o, Helper.Create(default(NestedStruct)));
    }

    public static void Run()
    {
        NestedStruct? s = Helper.Create(default(NestedStruct));

        Console.WriteLine("--- NestedStruct? s = Helper.Create(default(NestedStruct)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NestedStruct? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NestedStruct u = Helper.Create(default(NestedStruct)) ---");
        NestedStruct u = Helper.Create(default(NestedStruct));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NestedStruct>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NestedStruct>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest33
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((NestedStructGen<int>)o, Helper.Create(default(NestedStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((NestedStructGen<int>?)o, Helper.Create(default(NestedStructGen<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((NestedStructGen<int>)o, Helper.Create(default(NestedStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((NestedStructGen<int>?)o, Helper.Create(default(NestedStructGen<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((NestedStructGen<int>)(object)o, Helper.Create(default(NestedStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((NestedStructGen<int>?)(object)o, Helper.Create(default(NestedStructGen<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((NestedStructGen<int>)(object)o, Helper.Create(default(NestedStructGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((NestedStructGen<int>?)(object)o, Helper.Create(default(NestedStructGen<int>)));
    }

    public static void Run()
    {
        NestedStructGen<int>? s = Helper.Create(default(NestedStructGen<int>));

        Console.WriteLine("--- NestedStructGen<int>? s = Helper.Create(default(NestedStructGen<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- NestedStructGen<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- NestedStructGen<int> u = Helper.Create(default(NestedStructGen<int>)) ---");
        NestedStructGen<int> u = Helper.Create(default(NestedStructGen<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<NestedStructGen<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<NestedStructGen<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest34
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ExplicitFieldOffsetStruct)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct?)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ExplicitFieldOffsetStruct)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct?)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ExplicitFieldOffsetStruct)(object)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ExplicitFieldOffsetStruct?)(object)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ExplicitFieldOffsetStruct)(object)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ExplicitFieldOffsetStruct?)(object)o, Helper.Create(default(ExplicitFieldOffsetStruct)));
    }

    public static void Run()
    {
        ExplicitFieldOffsetStruct? s = Helper.Create(default(ExplicitFieldOffsetStruct));

        Console.WriteLine("--- ExplicitFieldOffsetStruct? s = Helper.Create(default(ExplicitFieldOffsetStruct)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ExplicitFieldOffsetStruct? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ExplicitFieldOffsetStruct u = Helper.Create(default(ExplicitFieldOffsetStruct)) ---");
        ExplicitFieldOffsetStruct u = Helper.Create(default(ExplicitFieldOffsetStruct));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ExplicitFieldOffsetStruct>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ExplicitFieldOffsetStruct>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest37
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((MarshalAsStruct)o, Helper.Create(default(MarshalAsStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((MarshalAsStruct?)o, Helper.Create(default(MarshalAsStruct)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((MarshalAsStruct)o, Helper.Create(default(MarshalAsStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((MarshalAsStruct?)o, Helper.Create(default(MarshalAsStruct)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((MarshalAsStruct)(object)o, Helper.Create(default(MarshalAsStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((MarshalAsStruct?)(object)o, Helper.Create(default(MarshalAsStruct)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((MarshalAsStruct)(object)o, Helper.Create(default(MarshalAsStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((MarshalAsStruct?)(object)o, Helper.Create(default(MarshalAsStruct)));
    }

    public static void Run()
    {
        MarshalAsStruct? s = Helper.Create(default(MarshalAsStruct));

        Console.WriteLine("--- MarshalAsStruct? s = Helper.Create(default(MarshalAsStruct)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- MarshalAsStruct? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- MarshalAsStruct u = Helper.Create(default(MarshalAsStruct)) ---");
        MarshalAsStruct u = Helper.Create(default(MarshalAsStruct));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<MarshalAsStruct>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<MarshalAsStruct>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest38
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ImplementOneInterface)o, Helper.Create(default(ImplementOneInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ImplementOneInterface?)o, Helper.Create(default(ImplementOneInterface)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ImplementOneInterface)o, Helper.Create(default(ImplementOneInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ImplementOneInterface?)o, Helper.Create(default(ImplementOneInterface)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ImplementOneInterface)(object)o, Helper.Create(default(ImplementOneInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ImplementOneInterface?)(object)o, Helper.Create(default(ImplementOneInterface)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ImplementOneInterface)(object)o, Helper.Create(default(ImplementOneInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ImplementOneInterface?)(object)o, Helper.Create(default(ImplementOneInterface)));
    }

    public static void Run()
    {
        ImplementOneInterface? s = Helper.Create(default(ImplementOneInterface));

        Console.WriteLine("--- ImplementOneInterface? s = Helper.Create(default(ImplementOneInterface)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementOneInterface? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementOneInterface u = Helper.Create(default(ImplementOneInterface)) ---");
        ImplementOneInterface u = Helper.Create(default(ImplementOneInterface));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ImplementOneInterface>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ImplementOneInterface>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest39
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ImplementTwoInterface)o, Helper.Create(default(ImplementTwoInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ImplementTwoInterface?)o, Helper.Create(default(ImplementTwoInterface)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ImplementTwoInterface)o, Helper.Create(default(ImplementTwoInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ImplementTwoInterface?)o, Helper.Create(default(ImplementTwoInterface)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ImplementTwoInterface)(object)o, Helper.Create(default(ImplementTwoInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ImplementTwoInterface?)(object)o, Helper.Create(default(ImplementTwoInterface)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ImplementTwoInterface)(object)o, Helper.Create(default(ImplementTwoInterface)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ImplementTwoInterface?)(object)o, Helper.Create(default(ImplementTwoInterface)));
    }

    public static void Run()
    {
        ImplementTwoInterface? s = Helper.Create(default(ImplementTwoInterface));

        Console.WriteLine("--- ImplementTwoInterface? s = Helper.Create(default(ImplementTwoInterface)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementTwoInterface? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementTwoInterface u = Helper.Create(default(ImplementTwoInterface)) ---");
        ImplementTwoInterface u = Helper.Create(default(ImplementTwoInterface));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ImplementTwoInterface>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ImplementTwoInterface>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest40
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ImplementOneInterfaceGen<int>)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>?)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ImplementOneInterfaceGen<int>)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>?)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ImplementOneInterfaceGen<int>)(object)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>?)(object)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ImplementOneInterfaceGen<int>)(object)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ImplementOneInterfaceGen<int>?)(object)o, Helper.Create(default(ImplementOneInterfaceGen<int>)));
    }

    public static void Run()
    {
        ImplementOneInterfaceGen<int>? s = Helper.Create(default(ImplementOneInterfaceGen<int>));

        Console.WriteLine("--- ImplementOneInterfaceGen<int>? s = Helper.Create(default(ImplementOneInterfaceGen<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementOneInterfaceGen<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementOneInterfaceGen<int> u = Helper.Create(default(ImplementOneInterfaceGen<int>)) ---");
        ImplementOneInterfaceGen<int> u = Helper.Create(default(ImplementOneInterfaceGen<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ImplementOneInterfaceGen<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ImplementOneInterfaceGen<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest41
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ImplementTwoInterfaceGen<int>)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ImplementTwoInterfaceGen<int>?)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ImplementTwoInterfaceGen<int>)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ImplementTwoInterfaceGen<int>?)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ImplementTwoInterfaceGen<int>)(object)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ImplementTwoInterfaceGen<int>?)(object)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ImplementTwoInterfaceGen<int>)(object)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ImplementTwoInterfaceGen<int>?)(object)o, Helper.Create(default(ImplementTwoInterfaceGen<int>)));
    }

    public static void Run()
    {
        ImplementTwoInterfaceGen<int>? s = Helper.Create(default(ImplementTwoInterfaceGen<int>));

        Console.WriteLine("--- ImplementTwoInterfaceGen<int>? s = Helper.Create(default(ImplementTwoInterfaceGen<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementTwoInterfaceGen<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementTwoInterfaceGen<int> u = Helper.Create(default(ImplementTwoInterfaceGen<int>)) ---");
        ImplementTwoInterfaceGen<int> u = Helper.Create(default(ImplementTwoInterfaceGen<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ImplementTwoInterfaceGen<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ImplementTwoInterfaceGen<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest42
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((ImplementAllInterface<int>)o, Helper.Create(default(ImplementAllInterface<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((ImplementAllInterface<int>?)o, Helper.Create(default(ImplementAllInterface<int>)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((ImplementAllInterface<int>)o, Helper.Create(default(ImplementAllInterface<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((ImplementAllInterface<int>?)o, Helper.Create(default(ImplementAllInterface<int>)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((ImplementAllInterface<int>)(object)o, Helper.Create(default(ImplementAllInterface<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((ImplementAllInterface<int>?)(object)o, Helper.Create(default(ImplementAllInterface<int>)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((ImplementAllInterface<int>)(object)o, Helper.Create(default(ImplementAllInterface<int>)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((ImplementAllInterface<int>?)(object)o, Helper.Create(default(ImplementAllInterface<int>)));
    }

    public static void Run()
    {
        ImplementAllInterface<int>? s = Helper.Create(default(ImplementAllInterface<int>));

        Console.WriteLine("--- ImplementAllInterface<int>? s = Helper.Create(default(ImplementAllInterface<int>)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementAllInterface<int>? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- ImplementAllInterface<int> u = Helper.Create(default(ImplementAllInterface<int>)) ---");
        ImplementAllInterface<int> u = Helper.Create(default(ImplementAllInterface<int>));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<ImplementAllInterface<int>>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<ImplementAllInterface<int>>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest43
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((WithMultipleGCHandleStruct)o, Helper.Create(default(WithMultipleGCHandleStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((WithMultipleGCHandleStruct?)o, Helper.Create(default(WithMultipleGCHandleStruct)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((WithMultipleGCHandleStruct)o, Helper.Create(default(WithMultipleGCHandleStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((WithMultipleGCHandleStruct?)o, Helper.Create(default(WithMultipleGCHandleStruct)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((WithMultipleGCHandleStruct)(object)o, Helper.Create(default(WithMultipleGCHandleStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((WithMultipleGCHandleStruct?)(object)o, Helper.Create(default(WithMultipleGCHandleStruct)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((WithMultipleGCHandleStruct)(object)o, Helper.Create(default(WithMultipleGCHandleStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((WithMultipleGCHandleStruct?)(object)o, Helper.Create(default(WithMultipleGCHandleStruct)));
    }

    public static void Run()
    {
        WithMultipleGCHandleStruct? s = Helper.Create(default(WithMultipleGCHandleStruct));

        Console.WriteLine("--- WithMultipleGCHandleStruct? s = Helper.Create(default(WithMultipleGCHandleStruct)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- WithMultipleGCHandleStruct? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- WithMultipleGCHandleStruct u = Helper.Create(default(WithMultipleGCHandleStruct)) ---");
        WithMultipleGCHandleStruct u = Helper.Create(default(WithMultipleGCHandleStruct));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<WithMultipleGCHandleStruct>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<WithMultipleGCHandleStruct>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest44
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((WithOnlyFXTypeStruct)o, Helper.Create(default(WithOnlyFXTypeStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((WithOnlyFXTypeStruct?)o, Helper.Create(default(WithOnlyFXTypeStruct)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((WithOnlyFXTypeStruct)o, Helper.Create(default(WithOnlyFXTypeStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((WithOnlyFXTypeStruct?)o, Helper.Create(default(WithOnlyFXTypeStruct)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((WithOnlyFXTypeStruct)(object)o, Helper.Create(default(WithOnlyFXTypeStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((WithOnlyFXTypeStruct?)(object)o, Helper.Create(default(WithOnlyFXTypeStruct)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((WithOnlyFXTypeStruct)(object)o, Helper.Create(default(WithOnlyFXTypeStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((WithOnlyFXTypeStruct?)(object)o, Helper.Create(default(WithOnlyFXTypeStruct)));
    }

    public static void Run()
    {
        WithOnlyFXTypeStruct? s = Helper.Create(default(WithOnlyFXTypeStruct));

        Console.WriteLine("--- WithOnlyFXTypeStruct? s = Helper.Create(default(WithOnlyFXTypeStruct)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- WithOnlyFXTypeStruct? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- WithOnlyFXTypeStruct u = Helper.Create(default(WithOnlyFXTypeStruct)) ---");
        WithOnlyFXTypeStruct u = Helper.Create(default(WithOnlyFXTypeStruct));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<WithOnlyFXTypeStruct>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<WithOnlyFXTypeStruct>(u), true, "BoxUnboxToQGenC");
    }
}


internal class NullableTest45
{
    private static bool BoxUnboxToNQ(object o)
    {
        try
        {
            return Helper.Compare((MixedAllStruct)o, Helper.Create(default(MixedAllStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(object o)
    {
        return Helper.Compare((MixedAllStruct?)o, Helper.Create(default(MixedAllStruct)));
    }

    private static bool BoxUnboxToNQV(ValueType o)
    {
        try
        {
            return Helper.Compare((MixedAllStruct)o, Helper.Create(default(MixedAllStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQV(ValueType o)
    {
        return Helper.Compare((MixedAllStruct?)o, Helper.Create(default(MixedAllStruct)));
    }

    private static bool BoxUnboxToNQGen<T>(T o)
    {
        try
        {
            return Helper.Compare((MixedAllStruct)(object)o, Helper.Create(default(MixedAllStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGen<T>(T o)
    {
        return Helper.Compare((MixedAllStruct?)(object)o, Helper.Create(default(MixedAllStruct)));
    }

    private static bool BoxUnboxToNQGenC<T>(T? o) where T : struct
    {
        try
        {
            return Helper.Compare((MixedAllStruct)(object)o, Helper.Create(default(MixedAllStruct)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQGenC<T>(T? o) where T : struct
    {
        return Helper.Compare((MixedAllStruct?)(object)o, Helper.Create(default(MixedAllStruct)));
    }

    public static void Run()
    {
        MixedAllStruct? s = Helper.Create(default(MixedAllStruct));

        Console.WriteLine("--- MixedAllStruct? s = Helper.Create(default(MixedAllStruct)) ---");
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), true, "BoxUnboxToQGenC");

        Console.WriteLine("--- MixedAllStruct? s = null ---");
        s = null;
        Assert.AreEqual(BoxUnboxToNQ(s), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(s), false, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(s), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(s), false, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(s), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(s), false, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC(s), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC(s), false, "BoxUnboxToQGenC");

        Console.WriteLine("--- MixedAllStruct u = Helper.Create(default(MixedAllStruct)) ---");
        MixedAllStruct u = Helper.Create(default(MixedAllStruct));
        Assert.AreEqual(BoxUnboxToNQ(u), true, "BoxUnboxToNQ");
        Assert.AreEqual(BoxUnboxToQ(u), true, "BoxUnboxToQ");
        Assert.AreEqual(BoxUnboxToNQV(u), true, "BoxUnboxToNQV");
        Assert.AreEqual(BoxUnboxToQV(u), true, "BoxUnboxToQV");
        Assert.AreEqual(BoxUnboxToNQGen(u), true, "BoxUnboxToNQGen");
        Assert.AreEqual(BoxUnboxToQGen(u), true, "BoxUnboxToQGen");
        Assert.AreEqual(BoxUnboxToNQGenC<MixedAllStruct>(u), true, "BoxUnboxToNQGenC");
        Assert.AreEqual(BoxUnboxToQGenC<MixedAllStruct>(u), true, "BoxUnboxToQGenC");
    }
}


public class Test_boxunboxvaluetype
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
        catch (System.Exception e)
        {
            Console.WriteLine("Test Failed" + e.ToString());
            Console.WriteLine(e);
            return 666;
        }

        Console.WriteLine("Test SUCCESS");
        return 100;
    }
}

