// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace SampleDynamicTests
{
    public delegate int MultiplyDelegate(int a, int b);

    public class DelegateTest
    {
        public int Mul(int a, int b)
        {
            return a * b;
        }

        [Fact]
        public static void DelegateTest_RunTest()
        {
            DelegateTest del = new DelegateTest();
            dynamic d = (MultiplyDelegate)del.Mul;
            int result = d(4, 6);
            Assert.Equal(24, result);
        }

        private static int Foo(string s, dynamic d, object o)
        {
            return s.Length + d.ToString().Length + o.ToString().Length;
        }

        private static void Bar(string s, dynamic d, object o)
        {
        }

        [Fact]
        public static void VariantFuncTest()
        {
            Func<string, dynamic, object, int> del = Foo;
            Func<string, dynamic, string, int> func = del;

            dynamic arg = "saw";

            Assert.Equal(16, func("came", arg, "conquered"));
        }

        [Fact]
        public static void VariantActTest()
        {
            Action<string, dynamic, object> del = Bar;
            Action<string, dynamic, string> act = del;

            dynamic arg = "saw";

            act("came", arg, "conquered");
        }
    }
}
