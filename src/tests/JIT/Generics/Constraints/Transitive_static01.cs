// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public interface IFoo { }

public class Transition<T> where T : IFoo { }

public class FooClass : IFoo { }

public struct FooStruct : IFoo { }

public class GenClass<T> where T : IFoo
{
    public static Transition<T> TransitiveConstraint()
    {
        return new Transition<T>();
    }
}

public struct GenStruct<T> where T : IFoo
{
    public static Transition<T> TransitiveConstraint()
    {
        return new Transition<T>();
    }
}
public class Test_Transitive_static01
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
        Eval(GenClass<FooClass>.TransitiveConstraint().GetType().Equals(typeof(Transition<FooClass>)));
        Eval(GenClass<FooStruct>.TransitiveConstraint().GetType().Equals(typeof(Transition<FooStruct>)));

        Eval(GenStruct<FooClass>.TransitiveConstraint().GetType().Equals(typeof(Transition<FooClass>)));
        Eval(GenStruct<FooStruct>.TransitiveConstraint().GetType().Equals(typeof(Transition<FooStruct>)));

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

