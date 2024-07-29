// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public interface IFoo
{

}

public class FooClass : IFoo
{

}

public struct FooStruct : IFoo
{

}

public class GenClass<T> where T : IFoo
{
    public static IFoo ConvertToConstraint(T t)
    {
        return t;
    }
}

public struct GenStruct<T> where T : IFoo
{
    public static IFoo ConvertToConstraint(T t)
    {
        return t;
    }
}
public class Test_Convert_static01
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
        Eval(GenClass<FooClass>.ConvertToConstraint(new FooClass()).GetType().Equals(typeof(FooClass)));
        Eval(GenClass<FooStruct>.ConvertToConstraint(new FooStruct()).GetType().Equals(typeof(FooStruct)));

        Eval(GenStruct<FooClass>.ConvertToConstraint(new FooClass()).GetType().Equals(typeof(FooClass)));
        Eval(GenStruct<FooStruct>.ConvertToConstraint(new FooStruct()).GetType().Equals(typeof(FooStruct)));

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

