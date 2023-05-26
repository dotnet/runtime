// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

struct Gen<T>
{
    static public T Field;

    static public T[] TArray;

    static public T Property
    {
        get { return Field; }
        set { Field = value; }
    }

    static public T Method(T t)
    {
        return t;
    }
}

public class Test_struct_static01
{
    [Fact]
    public static int TestEntryPoint()
    {
        int ret = 100;

        Gen<int>.Field = 5;
        if (Gen<int>.Field != 5)
        {
            Console.WriteLine("Failed Field Access for Gen<int>");
            ret = 1;
        }

        Gen<int>.Property = 10;

        if (Gen<int>.Property != 10)
        {
            Console.WriteLine("Failed Property Access for Gen<int>");
            ret = 1;
        }

        Gen<int>.TArray = new int[10];

        if (Gen<int>.TArray.Length != 10)
        {
            Console.WriteLine("Failed T Array Creation for Gen<int>");
            ret = 1;
        }

        for (int i = 0; (i < 10); i++)
        {
            Gen<int>.TArray[i] = 15;
            if (Gen<int>.TArray[i] != 15)
            {
                Console.WriteLine("Failed Indexer Access for Gen<int>");
                ret = 1;
            }
        }

        if (Gen<int>.Method(20) != 20)
        {
            Console.WriteLine("Failed Method Access for Gen<int>");
            ret = 1;
        }


        Gen<String>.Field = "Field";
        if (Gen<String>.Field != "Field")
        {
            Console.WriteLine("Failed Field Access for Gen<String>");
            ret = 1;
        }

        Gen<String>.Property = "Property";

        if (Gen<String>.Property != "Property")
        {
            Console.WriteLine("Failed Property Access for Gen<String>");
            ret = 1;
        }

        Gen<String>.TArray = new String[10];

        if (Gen<String>.TArray.Length != 10)
        {
            Console.WriteLine("Failed T Array Creation for Gen<String>");
            ret = 1;
        }

        for (int i = 0; (i < 10); i++)
        {
            Gen<String>.TArray[i] = "ArrayString";
            if (Gen<String>.TArray[i] != "ArrayString")
            {
                Console.WriteLine("Failed Indexer Access for Gen<String>");
                ret = 1;
            }
        }

        if (Gen<String>.Method("Method") != "Method")
        {
            Console.WriteLine("Failed Method Access for Gen<String>");
            ret = 1;
        }

        return ret;

    }
}
