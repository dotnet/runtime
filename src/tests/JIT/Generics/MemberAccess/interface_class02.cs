// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

interface IGen<T>
{
    T Property
    {
        get;
        set;
    }

    T this[int i]
    {
        get;
        set;
    }

    T Method(T t);

    T VMethod(T t);
}

class GenInt : IGen<int>
{
    public GenInt()
    {
        TArray = new int[10];
    }

    public int Field;

    public int[] TArray;

    public int Property
    {
        get { return Field; }
        set { Field = value; }
    }

    public int this[int i]
    {
        get { return TArray[i]; }
        set { TArray[i] = value; }
    }

    public int Method(int t)
    {
        return t;
    }

    public virtual int VMethod(int t)
    {
        return t;
    }

}

class GenString : IGen<string>
{
    public GenString()
    {
        TArray = new string[10];
    }

    public string Field;

    public string[] TArray;

    public string Property
    {
        get { return Field; }
        set { Field = value; }
    }

    public string this[int i]
    {
        get { return TArray[i]; }
        set { TArray[i] = value; }
    }

    public string Method(string t)
    {
        return t;
    }

    public virtual string VMethod(string t)
    {
        return t;
    }

}

public class Test_interface_class02
{
    [Fact]
    public static int TestEntryPoint()
    {
        int ret = 100;

        IGen<int> Gen_Int = new GenInt();

        Gen_Int.Property = 10;

        if (Gen_Int.Property != 10)
        {
            Console.WriteLine("Failed Property Access for IGen<int>");
            ret = 1;
        }

        for (int i = 0; (i < 10); i++)
        {
            Gen_Int[i] = 15;
            if (Gen_Int[i] != 15)
            {
                Console.WriteLine("Failed Indexer Access for IGen<int>");
                ret = 1;
            }
        }

        if (Gen_Int.Method(20) != 20)
        {
            Console.WriteLine("Failed Method Access for IGen<int>");
            ret = 1;
        }

        if (Gen_Int.VMethod(25) != 25)
        {
            Console.WriteLine("Failed Virtual Method Access for IGen<int>");
            ret = 1;
        }

        IGen<String> Gen_String = new GenString();

        Gen_String.Property = "Property";

        if (Gen_String.Property != "Property")
        {
            Console.WriteLine("Failed Property Access for IGen<String>");
            ret = 1;
        }

        for (int i = 0; (i < 10); i++)
        {
            Gen_String[i] = "ArrayString";
            if (Gen_String[i] != "ArrayString")
            {
                Console.WriteLine("Failed Indexer Access for IGen<String>");
                ret = 1;
            }
        }

        if (Gen_String.Method("Method") != "Method")
        {
            Console.WriteLine("Failed Method Access for IGen<String>");
            ret = 1;
        }

        if (Gen_String.VMethod("VirtualMethod") != "VirtualMethod")
        {
            Console.WriteLine("Failed Virtual Method Access for IGen<String>");
            ret = 1;
        }

        return ret;

    }
}
