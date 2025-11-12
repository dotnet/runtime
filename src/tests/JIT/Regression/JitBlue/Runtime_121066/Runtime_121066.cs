// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_121066
{
    public static int preciseInitCctorsRun = 0;

    class MyPreciseInitClass<T>
    {
        static MyPreciseInitClass()
        {
            preciseInitCctorsRun++;
        }

        public static void TriggerCctorClass()
        {
        }

        public static void TriggerCctorMethod<U>()
        { }
    }

    class MyClass<T>
    {
        static Type staticVarType = typeof(MyClass<T>);
        public Type GetTypeOf()
        {
            return typeof(MyClass<T>);
        }
        public static Type GetTypeOfStatic()
        {
            return typeof(MyClass<T>);
        }

        public static Type GetTypeThroughStaticVar()
        {
            return staticVarType;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.True(TestPreciseInitCctors());
    }

    public static bool TestPreciseInitCctors()
    {
        if (preciseInitCctorsRun != 0)
        {
            Console.WriteLine("preciseInitCctorsRun should be 0, but is {0}", preciseInitCctorsRun);
            return false;
        }
        MyPreciseInitClass<int>.TriggerCctorClass();
        if (preciseInitCctorsRun != 1)
        {
            Console.WriteLine("preciseInitCctorsRun should be 1, but is {0}", preciseInitCctorsRun);
            return false;
        }
        MyPreciseInitClass<short>.TriggerCctorMethod<int>();
        if (preciseInitCctorsRun != 2)
        {
            Console.WriteLine("TriggerCctorClass should return 2, but is {0}", preciseInitCctorsRun);
            return false;
        }

        object o = new MyPreciseInitClass<double>();
        if (preciseInitCctorsRun != 3)
        {
            Console.WriteLine("TriggerCctorClass should return 3, but is {0}", preciseInitCctorsRun);
            return false;
        }

        MyPreciseInitClass<object>.TriggerCctorClass();
        if (preciseInitCctorsRun != 4)
        {
            Console.WriteLine("preciseInitCctorsRun should be 4 but is {0}", preciseInitCctorsRun);
            return false;
        }
        MyPreciseInitClass<string>.TriggerCctorMethod<object>();
        if (preciseInitCctorsRun != 5)
        {
            Console.WriteLine("TriggerCctorClass should return 5, but is {0}", preciseInitCctorsRun);
            return false;
        }

        o = new MyPreciseInitClass<Type>();
        if (preciseInitCctorsRun != 6)
        {
            Console.WriteLine("TriggerCctorClass should return 6,  but is {0}", preciseInitCctorsRun);
            return false;
        }

        return true;
    }
}
