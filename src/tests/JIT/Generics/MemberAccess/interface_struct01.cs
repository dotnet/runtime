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
}

struct Gen<T> : IGen<T>
{

    public T Field;

    public T[] TArray;

    public T Property
    {
        get { return Field; }
        set { Field = value; }
    }

    public T this[int i]
    {
        get { return TArray[i]; }
        set { TArray[i] = value; }
    }

    public T Method(T t)
    {
        return t;
    }

}

public class Test_interface_struct01
{
    [Fact]
    public static int TestEntryPoint()
    {
        int ret = 100;

        Gen<int> GenIntStruct = new Gen<int>();
        GenIntStruct.TArray = new int[10];
        IGen<int> GenInt = GenIntStruct;

        GenInt.Property = 10;

        if (GenInt.Property != 10)
        {
            Console.WriteLine("Failed Property Access for IGen<int>");
            ret = 1;
        }

        for (int i = 0; (i < 10); i++)
        {
            GenInt[i] = 15;
            if (GenInt[i] != 15)
            {
                Console.WriteLine("Failed Indexer Access for IGen<int>");
                ret = 1;
            }
        }

        if (GenInt.Method(20) != 20)
        {
            Console.WriteLine("Failed Method Access for IGen<int>");
            ret = 1;
        }

        Gen<string> GenStringStruct = new Gen<string>();
        GenStringStruct.TArray = new string[10];
        IGen<string> GenString = GenStringStruct;

        GenString.Property = "Property";

        if (GenString.Property != "Property")
        {
            Console.WriteLine("Failed Property Access for IGen<String>");
            ret = 1;
        }

        for (int i = 0; (i < 10); i++)
        {
            GenString[i] = "ArrayString";
            if (GenString[i] != "ArrayString")
            {
                Console.WriteLine("Failed Indexer Access for IGen<String>");
                ret = 1;
            }
        }

        if (GenString.Method("Method") != "Method")
        {
            Console.WriteLine("Failed Method Access for IGen<String>");
            ret = 1;
        }

        return ret;

    }
}
