// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
using System;
using Xunit;

class Gen<T>
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

    public virtual T VMethod(T t)
    {
        return t;
    }

}

public class Test_class_instance01
{
    [Fact]
    public static int TestEntryPoint()
    {
        int ret = 100;

        Gen<int> GenInt = new Gen<int>();

        GenInt.Field = 5;
        if (GenInt.Field != 5)
        {
            Console.WriteLine("Failed Field Access for Gen<int>");
            ret = 1;
        }

        GenInt.Property = 10;

        if (GenInt.Property != 10)
        {
            Console.WriteLine("Failed Property Access for Gen<int>");
            ret = 1;
        }

        GenInt.TArray = new int[10];

        if (GenInt.TArray.Length != 10)
        {
            Console.WriteLine("Failed T Array Creation for Gen<int>");
            ret = 1;
        }

        for (int i = 0; (i < 10); i++)
        {
            GenInt[i] = 15;
            if (GenInt[i] != 15)
            {
                Console.WriteLine("Failed Indexer Access for Gen<int>");
                ret = 1;
            }
        }

        if (GenInt.Method(20) != 20)
        {
            Console.WriteLine("Failed Method Access for Gen<int>");
            ret = 1;
        }

        if (GenInt.VMethod(25) != 25)
        {
            Console.WriteLine("Failed Virtual Method Access for Gen<int>");
            ret = 1;
        }

        Gen<String> GenString = new Gen<String>();

        GenString.Field = "Field";
        if (GenString.Field != "Field")
        {
            Console.WriteLine("Failed Field Access for Gen<String>");
            ret = 1;
        }

        GenString.Property = "Property";

        if (GenString.Property != "Property")
        {
            Console.WriteLine("Failed Property Access for Gen<String>");
            ret = 1;
        }

        GenString.TArray = new String[10];

        if (GenString.TArray.Length != 10)
        {
            Console.WriteLine("Failed T Array Creation for Gen<String>");
            ret = 1;
        }

        for (int i = 0; (i < 10); i++)
        {
            GenString[i] = "ArrayString";
            if (GenString[i] != "ArrayString")
            {
                Console.WriteLine("Failed Indexer Access for Gen<String>");
                ret = 1;
            }
        }

        if (GenString.Method("Method") != "Method")
        {
            Console.WriteLine("Failed Method Access for Gen<String>");
            ret = 1;
        }

        if (GenString.VMethod("VirtualMethod") != "VirtualMethod")
        {
            Console.WriteLine("Failed Virtual Method Access for Gen<String>");
            ret = 1;
        }
        if (ret == 100)
        {
            Console.WriteLine("Test Passes");
        }
        return ret;

    }
}
