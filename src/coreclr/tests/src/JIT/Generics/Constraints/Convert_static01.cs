// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

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
public class Test
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

    public static int Main()
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

