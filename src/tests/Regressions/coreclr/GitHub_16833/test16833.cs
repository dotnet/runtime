// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

namespace TestShufflingThunk
{
    // This is a regression test for shuffling thunk creation on Unix AMD64. The calling convention causes some interesting shuffles that
    // the code before the fix was not handling properly.
    struct SLongLong
    {
        public long x;
        public long y;
        public override string ToString()
        {
            return $"[{x}, {y}]";
        }
    }

    struct SIntDouble
    {
        public int x;
        public double y;
        public override string ToString()
        {
            return $"[{x}, {y}]";
        }
    }

    struct SInt
    {
        public int x;
        public override string ToString()
        {
            return $"[{x}]";
        }
    }

    struct SLargeReturnStruct
    {
        public long x;
        public long y;
        public long z;
        public string s;
        public override string ToString()
        {
            return $"{s} -> [{x}, {y}, {z}]";
        }
    }

    class TestClass
    {
        public static readonly string Test1Result = "Test1:  1, 2, 3, 4, [5, 6], 7";
        public static string Test1(int i1, int i2, int i3, int i4, SLongLong s, int i5)
        {
            return $"Test1:  {i1}, {i2}, {i3}, {i4}, {s}, {i5}";
        }
        public static string Test2(int i1, int i2, int i3, int i4, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10, int i5)
        {
            return $"Test2:  {i1}, {i2}, {i3}, {i4}, {s}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}, {f7}, {f8}, {f9}, {f10}, {i5}";
        }
        public static string Test3(int i1, int i2, int i3, int i4, int i5, SInt s, int i6)
        {
            return $"Test3:  {i1}, {i2}, {i3}, {i4}, {i5}, {s}, {i6}";
        }
        public static string Test4(int i1, int i2, int i3, int i4, SIntDouble s, double i5)
        {
            return $"Test4:  {i1}, {i2}, {i3}, {i4}, {s}, {i5}";
        }
        public static string Test5(int i1, int i2, int i3, int i4, SLongLong s)
        {
            return $"Test5:  {i1}, {i2}, {i3}, {i4}, {s}";
        }
        public static string Test6(int i1, int i2, int i3, int i4, int i5, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10)
        {
            return $"Test6:  {i1}, {i2}, {i3}, {i4}, {i5}, {s}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}, {f7}, {f8}, {f9}, {f10}";
        }

        public static SLargeReturnStruct Test1RB(int i1, int i2, int i3, int i4, SLongLong s, int i5)
        {
            string args = $"Test1RB:  {i1}, {i2}, {i3}, {i4}, {s}, {i5}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public static SLargeReturnStruct Test2RB(int i1, int i2, int i3, int i4, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10, int i5)
        {
            string args = $"Test2RB:  {i1}, {i2}, {i3}, {i4}, {s}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}, {f7}, {f8}, {f9}, {f10}, {i5}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public static SLargeReturnStruct Test3RB(int i1, int i2, int i3, int i4, int i5, SInt s, int i6)
        {
            string args = $"Test3RB:  {i1}, {i2}, {i3}, {i4}, {i5}, {s}, {i6}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public static SLargeReturnStruct Test4RB(int i1, int i2, int i3, int i4, SIntDouble s, double i5)
        {
            string args = $"Test4RB:  {i1}, {i2}, {i3}, {i4}, {s}, {i5}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public static SLargeReturnStruct Test5RB(int i1, int i2, int i3, int i4, SLongLong s)
        {
            string args = $"Test5RB:  {i1}, {i2}, {i3}, {i4}, {s}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public static SLargeReturnStruct Test6RB(int i1, int i2, int i3, int i4, int i5, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10)
        {
            string args = $"Test6RB:  {i1}, {i2}, {i3}, {i4}, {i5}, {s}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}, {f7}, {f8}, {f9}, {f10}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }

        public string Test1M(int i1, int i2, int i3, int i4, SLongLong s, int i5)
        {
            return $"Test1M: i1, {i2}, {i3}, {i4}, {s}, {i5}";
        }
        public string Test2M(int i1, int i2, int i3, int i4, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10, int i5)
        {
            return $"Test2M: i1, {i2}, {i3}, {i4}, {s}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}, {f7}, {f8}, {f9}, {f10}, {i5}";
        }
        public string Test3M(int i1, int i2, int i3, int i4, int i5, SInt s, int i6)
        {
            return $"Test3M: i1, {i2}, {i3}, {i4}, {i5}, {s}, {i6}";
        }
        public string Test4M(int i1, int i2, int i3, int i4, SIntDouble s, double i5)
        {
            return $"Test4M: i1, {i2}, {i3}, {i4}, {s}, {i5}";
        }
        public string Test5M(int i1, int i2, int i3, int i4, SLongLong s)
        {
            return $"Test5M: i1, {i2}, {i3}, {i4}, {s}";
        }
        public string Test6M(int i1, int i2, int i3, int i4, int i5, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10)
        {
            return $"Test6M: i1, {i2}, {i3}, {i4}, {i5}, {s}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}, {f7}, {f8}, {f9}, {f10}";
        }
        public SLargeReturnStruct Test1MRB(int i1, int i2, int i3, int i4, SLongLong s, int i5)
        {
            string args = $"Test1MRB: {i1}, {i2}, {i3}, {i4}, {s}, {i5}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public SLargeReturnStruct Test2MRB(int i1, int i2, int i3, int i4, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10, int i5)
        {
            string args = $"Test2MRB: {i1}, {i2}, {i3}, {i4}, {s}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}, {f7}, {f8}, {f9}, {f10}, {i5}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public SLargeReturnStruct Test3MRB(int i1, int i2, int i3, int i4, int i5, SInt s, int i6)
        {
            string args = $"Test3MRB: {i1}, {i2}, {i3}, {i4}, {i5}, {s}, {i6}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public SLargeReturnStruct Test4MRB(int i1, int i2, int i3, int i4, SIntDouble s, double i5)
        {
            string args = $"Test4MRB: {i1}, {i2}, {i3}, {i4}, {s}, {i5}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public SLargeReturnStruct Test5MRB(int i1, int i2, int i3, int i4, SLongLong s)
        {
            string args = $"Test5MRB: {i1}, {i2}, {i3}, {i4}, {s}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
        public SLargeReturnStruct Test6MRB(int i1, int i2, int i3, int i4, int i5, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10)
        {
            string args = $"Test6MRB: {i1}, {i2}, {i3}, {i4}, {i5}, {s}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}, {f7}, {f8}, {f9}, {f10}";
            return new SLargeReturnStruct { x = -1, y = -2, z = -3, s = args };
        }
    }

    public class Test16833
    {
        delegate string Delegate2m(TestClass tc, int i1, int i2, int i3, int i4, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10, int i5);
        delegate string Delegate6m(TestClass tc, int i1, int i2, int i3, int i4, int i5, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10);
        delegate SLargeReturnStruct Delegate2mrb(TestClass tc, int i1, int i2, int i3, int i4, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10, int i5);
        delegate SLargeReturnStruct Delegate6mrb(TestClass tc, int i1, int i2, int i3, int i4, int i5, SIntDouble s, double f1, double f2, double f3, double f4, double f5, double f6, double f7, double f8, double f9, double f10);

        static void CheckResult(ref int exitCode, string test, string result, string expected)
        {
            if (result != expected)
            {
                Console.WriteLine($"Test {test} failed. Expected \"{expected}\", got \"{result}\"");
                exitCode = 1;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int exitCode = 100;

            string result;

            var func1 = (Func<int, int, int, int, SLongLong, int, string>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, SLongLong, int, string>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test1)));
            
            SLongLong s1 = new SLongLong { x = 5, y = 6};
            result = func1(1, 2, 3, 4, s1, 7);
            CheckResult(ref exitCode, nameof(TestClass.Test1), result, "Test1:  1, 2, 3, 4, [5, 6], 7");

            var func2 = (Func<int, int, int, int, SIntDouble, double, double, double, double, double, double, double, double, double, double, int, string>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, SIntDouble, double, double, double, double, double, double, double, double, double, double, int, string>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test2)));

            SIntDouble s2 = new SIntDouble { x = 5, y = 6.0 };
            result = func2(1, 2, 3, 4, s2, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17);
            CheckResult(ref exitCode, nameof(TestClass.Test2), result, "Test2:  1, 2, 3, 4, [5, 6], 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17");

            var func3 = (Func<int, int, int, int, int, SInt, int, string>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, int, SInt, int, string>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test3)));

            SInt s3 = new SInt { x = 6 };
            result = func3(1, 2, 3, 4, 5, s3, 7);
            CheckResult(ref exitCode, nameof(TestClass.Test3), result, "Test3:  1, 2, 3, 4, 5, [6], 7");

            var func4 = (Func<int, int, int, int, SIntDouble, double, string>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, SIntDouble, double, string>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test4)));

            SIntDouble s4 = new SIntDouble { x = 5, y = 6.0 };
            result = func4(1, 2, 3, 4, s4, 7.0);
            CheckResult(ref exitCode, nameof(TestClass.Test4), result, "Test4:  1, 2, 3, 4, [5, 6], 7");

            var func5 = (Func<int, int, int, int, SLongLong, string>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, SLongLong, string>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test5)));

            SLongLong s5 = new SLongLong { x = 5, y = 6 };
            result = func5(1, 2, 3, 4, s1);
            CheckResult(ref exitCode, nameof(TestClass.Test5), result, "Test5:  1, 2, 3, 4, [5, 6]");

            var func6 = (Func<int, int, int, int, int, SIntDouble, double, double, double, double, double, double, double, double, double, double, string>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, int, SIntDouble, double, double, double, double, double, double, double, double, double, double, string>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test6)));

            SIntDouble s6 = new SIntDouble { x = 6, y = 7.0 };
            result = func6(1, 2, 3, 4, 5, s6, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17.0);
            CheckResult(ref exitCode, nameof(TestClass.Test6), result, "Test6:  1, 2, 3, 4, 5, [6, 7], 8, 9, 10, 11, 12, 13, 14, 15, 16, 17");

            TestClass tc = new TestClass();

            var func1m = (Func<TestClass, int, int, int, int, SLongLong, int, string>)Delegate.CreateDelegate(
                typeof(Func<TestClass, int, int, int, int, SLongLong, int, string>),
                null, 
                typeof(TestClass).GetMethod(nameof(TestClass.Test1M)));

            result = func1m(tc, 1, 2, 3, 4, s1, 7);
            CheckResult(ref exitCode, nameof(TestClass.Test1M), result, "Test1M: i1, 2, 3, 4, [5, 6], 7");

            var func2m = (Delegate2m)Delegate.CreateDelegate(
                typeof(Delegate2m),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test2M)));

            result = func2m(tc, 1, 2, 3, 4, s2, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17);
            CheckResult(ref exitCode, nameof(TestClass.Test2M), result, "Test2M: i1, 2, 3, 4, [5, 6], 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17");

            var func3m = (Func<TestClass, int, int, int, int, int, SInt, int, string>)Delegate.CreateDelegate(
                typeof(Func<TestClass, int, int, int, int, int, SInt, int, string>),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test3M)));

            result = func3m(tc, 1, 2, 3, 4, 5, s3, 7);
            CheckResult(ref exitCode, nameof(TestClass.Test3M), result, "Test3M: i1, 2, 3, 4, 5, [6], 7");

            var func4m = (Func<TestClass, int, int, int, int, SIntDouble, double, string>)Delegate.CreateDelegate(
                typeof(Func<TestClass, int, int, int, int, SIntDouble, double, string>),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test4M)));

            result = func4m(tc, 1, 2, 3, 4, s4, 7.0);
            CheckResult(ref exitCode, nameof(TestClass.Test4M), result, "Test4M: i1, 2, 3, 4, [5, 6], 7");

            var func5m = (Func<TestClass, int, int, int, int, SLongLong, string>)Delegate.CreateDelegate(
                typeof(Func<TestClass, int, int, int, int, SLongLong, string>),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test5M)));

            result = func5m(tc, 1, 2, 3, 4, s1);
            CheckResult(ref exitCode, nameof(TestClass.Test5M), result, "Test5M: i1, 2, 3, 4, [5, 6]");

            var func6m = (Delegate6m)Delegate.CreateDelegate(
                typeof(Delegate6m),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test6M)));

            result = func6m(tc, 1, 2, 3, 4, 5, s6, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17.0);
            CheckResult(ref exitCode, nameof(TestClass.Test6M), result, "Test6M: i1, 2, 3, 4, 5, [6, 7], 8, 9, 10, 11, 12, 13, 14, 15, 16, 17");

            var func1rb = (Func<int, int, int, int, SLongLong, int, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, SLongLong, int, SLargeReturnStruct>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test1RB)));


            SLargeReturnStruct result1 = func1rb(1, 2, 3, 4, s1, 7);
            CheckResult(ref exitCode, nameof(TestClass.Test1RB), result1.ToString(), "Test1RB:  1, 2, 3, 4, [5, 6], 7 -> [-1, -2, -3]");

            var func2rb = (Func<int, int, int, int, SIntDouble, double, double, double, double, double, double, double, double, double, double, int, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, SIntDouble, double, double, double, double, double, double, double, double, double, double, int, SLargeReturnStruct>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test2RB)));

            SLargeReturnStruct result2 = func2rb(1, 2, 3, 4, s2, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17);
            CheckResult(ref exitCode, nameof(TestClass.Test2RB), result2.ToString(), "Test2RB:  1, 2, 3, 4, [5, 6], 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 -> [-1, -2, -3]");

            var func3rb = (Func<int, int, int, int, int, SInt, int, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, int, SInt, int, SLargeReturnStruct>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test3RB)));

            SLargeReturnStruct result3 = func3rb(1, 2, 3, 4, 5, s3, 7);
            CheckResult(ref exitCode, nameof(TestClass.Test3RB), result3.ToString(), "Test3RB:  1, 2, 3, 4, 5, [6], 7 -> [-1, -2, -3]");

            var func4rb = (Func<int, int, int, int, SIntDouble, double, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, SIntDouble, double, SLargeReturnStruct>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test4RB)));

            SLargeReturnStruct result4 = func4rb(1, 2, 3, 4, s4, 7.0);
            CheckResult(ref exitCode, nameof(TestClass.Test4RB), result4.ToString(), "Test4RB:  1, 2, 3, 4, [5, 6], 7 -> [-1, -2, -3]");

            var func5rb = (Func<int, int, int, int, SLongLong, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, SLongLong, SLargeReturnStruct>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test5RB)));

            SLargeReturnStruct result5 = func5rb(1, 2, 3, 4, s1);
            CheckResult(ref exitCode, nameof(TestClass.Test5RB), result5.ToString(), "Test5RB:  1, 2, 3, 4, [5, 6] -> [-1, -2, -3]");

            var func6rb = (Func<int, int, int, int, int, SIntDouble, double, double, double, double, double, double, double, double, double, double, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<int, int, int, int, int, SIntDouble, double, double, double, double, double, double, double, double, double, double, SLargeReturnStruct>),
                typeof(TestClass).GetMethod(nameof(TestClass.Test6RB)));

            SLargeReturnStruct result6 = func6rb(1, 2, 3, 4, 5, s6, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17.0);
            CheckResult(ref exitCode, nameof(TestClass.Test6RB), result6.ToString(), "Test6RB:  1, 2, 3, 4, 5, [6, 7], 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 -> [-1, -2, -3]");

            var func1mrb = (Func<TestClass, int, int, int, int, SLongLong, int, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<TestClass, int, int, int, int, SLongLong, int, SLargeReturnStruct>),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test1MRB)));

            SLargeReturnStruct result1mrb = func1mrb(tc, 1, 2, 3, 4, s1, 7);
            CheckResult(ref exitCode, nameof(TestClass.Test1MRB), result1mrb.ToString(), "Test1MRB: 1, 2, 3, 4, [5, 6], 7 -> [-1, -2, -3]");

            var func2mrb = (Delegate2mrb)Delegate.CreateDelegate(
                typeof(Delegate2mrb),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test2MRB)));

            SLargeReturnStruct result2mrb = func2mrb(tc, 1, 2, 3, 4, s2, 7.0, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17);
            CheckResult(ref exitCode, nameof(TestClass.Test2MRB), result2mrb.ToString(), "Test2MRB: 1, 2, 3, 4, [5, 6], 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 -> [-1, -2, -3]");

            var func3mrb = (Func<TestClass, int, int, int, int, int, SInt, int, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<TestClass, int, int, int, int, int, SInt, int, SLargeReturnStruct>),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test3MRB)));

            SLargeReturnStruct result3mrb = func3mrb(tc, 1, 2, 3, 4, 5, s3, 7);
            CheckResult(ref exitCode, nameof(TestClass.Test3MRB), result3mrb.ToString(), "Test3MRB: 1, 2, 3, 4, 5, [6], 7 -> [-1, -2, -3]");

            var func4mrb = (Func<TestClass, int, int, int, int, SIntDouble, double, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<TestClass, int, int, int, int, SIntDouble, double, SLargeReturnStruct>),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test4MRB)));

            SLargeReturnStruct result4mrb = func4mrb(tc, 1, 2, 3, 4, s4, 7.0);
            CheckResult(ref exitCode, nameof(TestClass.Test4MRB), result4mrb.ToString(), "Test4MRB: 1, 2, 3, 4, [5, 6], 7 -> [-1, -2, -3]");

            var func5mrb = (Func<TestClass, int, int, int, int, SLongLong, SLargeReturnStruct>)Delegate.CreateDelegate(
                typeof(Func<TestClass, int, int, int, int, SLongLong, SLargeReturnStruct>),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test5MRB)));

            SLargeReturnStruct result5mrb = func5mrb(tc, 1, 2, 3, 4, s1);
            CheckResult(ref exitCode, nameof(TestClass.Test5MRB), result5mrb.ToString(), "Test5MRB: 1, 2, 3, 4, [5, 6] -> [-1, -2, -3]");

            var func6mrb = (Delegate6mrb)Delegate.CreateDelegate(
                typeof(Delegate6mrb),
                null,
                typeof(TestClass).GetMethod(nameof(TestClass.Test6MRB)));

            SLargeReturnStruct result6mrb = func6mrb(tc, 1, 2, 3, 4, 5, s6, 8.0, 9.0, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17.0);
            CheckResult(ref exitCode, nameof(TestClass.Test6MRB), result6mrb.ToString(), "Test6MRB: 1, 2, 3, 4, 5, [6, 7], 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 -> [-1, -2, -3]");

            if (exitCode == 100)
            {
                Console.WriteLine("Test SUCCEEDED");
            }

            return exitCode;
        }
    }
}
