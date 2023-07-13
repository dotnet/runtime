// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

// Regression test for bug 200492, in which the x64 codegen for
// integer casts would unconditionally suppress same-register
// 'mov's, which is incorrect for 32-bit same-register 'mov's
// that are needed to clear the upper 32 bits of a 64-bit
// register.  The top bits aren't guaranteed to be clear across
// function boundaries, and the runtime code that invokes
// custom attribute constructors passes garbage in those bits,
// so this test (like the original code in the bug report)
// uses custom attribute constructor arguments as the sources
// of the casts in question.
public class Program
{
    [AttributeUsage(AttributeTargets.Method)]
    class TestDoubleAttribute : System.Attribute
    {
        public double Field;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double PickDouble(double d, int dummy)
        {
            return d;
        }
        public TestDoubleAttribute(uint f)
        {
            // Need to clear any garbage in the top half of f's
            // register before converting to double.
            this.Field = PickDouble((double)f, 0);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Program.TestDouble(6)]
    public static bool DoubleTest()
    {
        var methodInfo = typeof(Program).GetTypeInfo().GetDeclaredMethod("DoubleTest");
        var attribute = methodInfo.GetCustomAttribute<TestDoubleAttribute>();

        return (attribute.Field == (double)6u);
    }

    [AttributeUsage(AttributeTargets.Method)]
    class TestUlongAttribute : System.Attribute
    {
        public ulong Field;

        [MethodImpl(MethodImplOptions.NoInlining)]
        void Store(ulong l)
        {
            Field = l;
        }

        public TestUlongAttribute(int i)
        {
            checked {
                // Need to clear any garbage in the top half
                // of i's register before passing to Store.
                Store((ulong)i);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [Program.TestUlong(6)]
    public static bool UlongTest()
    {
        var methodInfo = typeof(Program).GetTypeInfo().GetDeclaredMethod("UlongTest");
        var attribute = methodInfo.GetCustomAttribute<TestUlongAttribute>();

        return (attribute.Field == (ulong)6);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int errors = 0;

        if (!Program.DoubleTest()) {
            errors += 1;
        }

        if (!Program.UlongTest()) {
            errors += 1;
        }

        if (errors > 0) {
            Console.WriteLine("Fail");
        }
        else
        {
            Console.WriteLine("Pass");
        }

        return 100 + errors;
    }
}
