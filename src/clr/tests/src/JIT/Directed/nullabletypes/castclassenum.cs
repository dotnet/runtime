// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System;


internal class NullableTest1
{
    private static bool BoxUnboxToNQ(Enum o)
    {
        try
        {
            return Helper.Compare((IntE)(ValueType)(object)o, Helper.Create(default(IntE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(Enum o)
    {
        return Helper.Compare((IntE?)(ValueType)(object)o, Helper.Create(default(IntE)));
    }

    public static void Run()
    {
        IntE? s = Helper.Create(default(IntE));

        Console.WriteLine("--- IntE? s = Helper.Create(default(IntE)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- IntE? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        IntE u = Helper.Create(default(IntE));

        Console.WriteLine("--- IntE u = Helper.Create(default(IntE)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest2
{
    private static bool BoxUnboxToNQ(Enum o)
    {
        try
        {
            return Helper.Compare((ByteE)(ValueType)(object)o, Helper.Create(default(ByteE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(Enum o)
    {
        return Helper.Compare((ByteE?)(ValueType)(object)o, Helper.Create(default(ByteE)));
    }

    public static void Run()
    {
        ByteE? s = Helper.Create(default(ByteE));

        Console.WriteLine("--- ByteE? s = Helper.Create(default(ByteE)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- ByteE? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        ByteE u = Helper.Create(default(ByteE));

        Console.WriteLine("--- ByteE u = Helper.Create(default(ByteE)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class NullableTest3
{
    private static bool BoxUnboxToNQ(Enum o)
    {
        try
        {
            return Helper.Compare((LongE)(ValueType)(object)o, Helper.Create(default(LongE)));
        }
        catch (NullReferenceException)
        {
            return o == null;
        }
    }

    private static bool BoxUnboxToQ(Enum o)
    {
        return Helper.Compare((LongE?)(ValueType)(object)o, Helper.Create(default(LongE)));
    }

    public static void Run()
    {
        LongE? s = Helper.Create(default(LongE));

        Console.WriteLine("--- LongE? s = Helper.Create(default(LongE)) ---");
        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsTrue(BoxUnboxToQ(s));

        Console.WriteLine("--- LongE? s = null ---");
        s = null;

        Assert.IsTrue(BoxUnboxToNQ(s));
        Assert.IsFalse(BoxUnboxToQ(s));

        LongE u = Helper.Create(default(LongE));

        Console.WriteLine("--- LongE u = Helper.Create(default(LongE)) ----");
        Assert.IsTrue(BoxUnboxToNQ(u));
        Assert.IsTrue(BoxUnboxToQ(u));
    }
}



internal class Test
{
    private static int Main()
    {
        try
        {
            NullableTest1.Run();
            NullableTest2.Run();
            NullableTest3.Run();
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

