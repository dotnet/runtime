// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this test has a generic type with a constraint that it needs to implement an interface constrained on a function pointer
// we want to make sure we can load such type.

using System;
using System.Text;
using Xunit;

public class Test_FunctionPointerConstraints {
    [Fact]
    public static int TestEntryPoint()
    {
        MyClass<MyClass2> myClass = new MyClass<MyClass2>();

        string result = myClass.MyMethod(new MyClass2());

        bool pass = result == "Func1 Func2 ";
        if (pass)
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine($"result: {result}");
            Console.WriteLine("FAIL");
            return 101;
        }
    }

    public unsafe class MyClass<T> where T : I1<delegate*<string>[]>
    {
        public string MyMethod(T param)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in param.Get())
            {
                stringBuilder.Append(item());
            }
            
            return stringBuilder.ToString();
        }
    }

    public unsafe class MyClass2 : I1<delegate*<string>[]>
    {
        private static string Func1() => "Func1 ";
        private static string Func2() => "Func2 ";

        public unsafe delegate*<string>[] Get()
        {
            return new delegate*<string>[]
            {
                &Func1,
                &Func2
            };
        }
    }
    public interface I1<U>
    {
        U Get();
    }
}

