// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
namespace DebuggerTests
{
    public class EvaluateTestsClass
    {
        public class TestEvaluate
        {
            public int a;
            public int b;
            public int c;
            public DateTime dt = new DateTime(2000, 5, 4, 3, 2, 1);
            public void run(int g, int h, string valString)
            {
                int d = g + 1;
                int e = g + 2;
                int f = g + 3;
                int i = d + e + f;
                var local_dt = new DateTime(2010, 9, 8, 7, 6, 5);
                a = 1;
                b = 2;
                c = 3;
                a = a + 1;
                b = b + 1;
                c = c + 1;
            }
        }

        public static void EvaluateLocals()
        {
            TestEvaluate f = new TestEvaluate();
            f.run(100, 200, "test");

            var f_s = new EvaluateTestsStruct();
            f_s.EvaluateTestsStructInstanceMethod(100, 200, "test");
            f_s.GenericInstanceMethodOnStruct<int>(100, 200, "test");

            var f_g_s = new EvaluateTestsGenericStruct<int>();
            f_g_s.EvaluateTestsGenericStructInstanceMethod(100, 200, "test");
            Console.WriteLine($"a: {f.a}, b: {f.b}, c: {f.c}");
        }

    }

    public struct EvaluateTestsStruct
    {
        public int a;
        public int b;
        public int c;
        DateTime dateTime;
        public void EvaluateTestsStructInstanceMethod(int g, int h, string valString)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            int i = d + e + f;
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            a = a + 1;
            b = b + 1;
            c = c + 1;
        }

        public void GenericInstanceMethodOnStruct<T>(int g, int h, string valString)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            int i = d + e + f;
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            T t = default(T);
            a = a + 1;
            b = b + 1;
            c = c + 1;
        }
    }

    public struct EvaluateTestsGenericStruct<T>
    {
        public int a;
        public int b;
        public int c;
        DateTime dateTime;
        public void EvaluateTestsGenericStructInstanceMethod(int g, int h, string valString)
        {
            int d = g + 1;
            int e = g + 2;
            int f = g + 3;
            int i = d + e + f;
            a = 1;
            b = 2;
            c = 3;
            dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
            T t = default(T);
            a = a + 1;
            b = b + 2;
            c = c + 3;
        }
    }
}