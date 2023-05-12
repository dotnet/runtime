// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public interface IFoo
{
    Type InterfaceMethod();
}

public class FooClass : IFoo
{
    public Type InterfaceMethod()
    {
        return this.GetType();
    }
}

public struct FooStruct : IFoo
{
    public Type InterfaceMethod()
    {
        return this.GetType();
    }
}

public class GenClass<T> where T : IFoo
{
    public static bool CallOnConstraint(T t)
    {
        return (t.InterfaceMethod().Equals(typeof(T)));
    }
}

public struct GenStruct<T> where T : IFoo
{
    public static bool CallOnConstraint(T t)
    {
        return (t.InterfaceMethod().Equals(typeof(T)));
    }
}
public class Test_Call_static01
{
    public static int counter = 0;
    public static bool result = true;
    public static void Eval(bool exp)
    {
        counter++;
        if (!exp)
        {
            result = exp;
            Console.WriteLine("Test Failed at location: " + counter);
        }

    }

    [Fact]
    public static int TestEntryPoint()
    {
        Eval(GenClass<FooClass>.CallOnConstraint(new FooClass()));
        Eval(GenClass<FooStruct>.CallOnConstraint(new FooStruct()));
        Eval(GenStruct<FooClass>.CallOnConstraint(new FooClass()));
        Eval(GenStruct<FooStruct>.CallOnConstraint(new FooStruct()));

        if (result)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Test Failed");
            return 1;
        }
    }

}

