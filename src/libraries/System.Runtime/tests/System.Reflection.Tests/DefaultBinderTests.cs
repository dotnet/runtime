// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Tests
{
    public class DefaultBinderTests
    {
        private const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        private const BindingFlags invokeFlags = flags | BindingFlags.InvokeMethod | BindingFlags.OptionalParamBinding;
        private static Binder binder = Type.DefaultBinder;

        [Fact]
        public static void DefaultBinderAllUnspecifiedParametersHasDefaultsTest()
        {
            MethodInfo[] methods = typeof(Sample).GetMethods();
            object[] methodArgs = new object[] { "value" };
            MethodBase method = binder.BindToMethod(flags, methods, ref methodArgs, null, null, new[] { "par1" }, out var _);
            Assert.NotNull(method);
            Assert.Equal("MethodMoreParameters", method.Name);
        }

        [Fact]
        public static void DefaultBinderNamedParametersInOrderTest()
        {
            MethodInfo[] methods = typeof(Sample).GetMethods();
            object[] methodArgs = new object[] { "value", 1 };
            MethodBase method = binder.BindToMethod(flags, methods, ref methodArgs, null, null, new[] { "param1", "param2" }, out var _);
            Assert.NotNull(method);
            Assert.Equal("SampleMethod", method.Name);
        }

        [Fact]
        public static void DefaultBinderNamedParametersOutOrderTest()
        {
            MethodInfo[] methods = typeof(Sample).GetMethods();
            object[] methodArgs = new object[] { 1, "value" };
            MethodBase method = binder.BindToMethod(flags, methods, ref methodArgs, null, null, new[] { "param2", "param1" }, out var _);
            Assert.NotNull(method);
            Assert.Equal("SampleMethod", method.Name);
        }

        [Fact]
        public static void DefaultBinderNamedParametersSkippedOrderTest()
        {
            MethodInfo[] methods = typeof(Sample).GetMethods();
            object[] methodArgs = new object[] { "value", true };
            MethodBase method = binder.BindToMethod(flags, methods, ref methodArgs, null, null, new[] { "param1", "param3" }, out var _);
            Assert.NotNull(method);
            Assert.Equal("SampleMethod", method.Name);
        }

        [Fact]
        public static void DefaultBinderNamedParametersMissingRequiredParameterThrowsMissingMethodException()
        {
            MethodInfo[] methods = new MethodInfo[] { typeof(Sample).GetMethod("NoDefaultParameterMethod") };
            object[] methodArgs = new object[] { "value", 1 };
            Assert.Throws<MissingMethodException>(() => binder.BindToMethod(flags, methods, ref methodArgs, null, null, new[] { "param1", "param2" }, out var _));
        }

        [Fact]
        public static void DefaultBinderNamedParametersMixedOrderNoDefaults ()
        {
            MethodInfo[] methods = typeof(Sample).GetMethods();
            object[] methodArgs = new object[] { true, "value", 3.14, 1 };
            MethodBase method = binder.BindToMethod(flags, methods, ref methodArgs, null, null, new[] { "param3", "param1", "param4", "param2" }, out var _);
            Assert.NotNull(method);
            Assert.Equal("NoDefaultParameterMethod", method.Name);
        }

        [Fact]
        public static void DefaultBinderNoNamedParametersInOrderNoDefaults()
        {
            MethodInfo[] methods = typeof(Sample).GetMethods();
            object[] methodArgs = new object[] { "value", 1, true, 3.14 };
            MethodBase method = binder.BindToMethod(flags, methods, ref methodArgs, null, null, null, out var _);
            Assert.NotNull(method);
            Assert.Equal("NoDefaultParameterMethod", method.Name);
        }

        [Fact]
        public static void DefaultBinderNoNamedParametersNoArgumentsAllDefaults()
        {
            MethodInfo[] methods = typeof(Test).GetMethods();
            object[] methodArgs = new object[] { null, null };
            MethodBase method = binder.BindToMethod(flags, methods, ref methodArgs, null, null, null, out var _);
            Assert.NotNull(method);
            Assert.Equal("SampleMethod", method.Name);
        }

        [Fact]
        public static void DefaultBinderNoNamedParametersOutOrderThrows()
        {
            MethodInfo[] methods = typeof(Sample).GetMethods();
            object[] methodArgs = new object[] { true, "value", 3.14, 1 };
            Assert.Throws<MissingMethodException>(() => binder.BindToMethod(flags, methods, ref methodArgs, null, null, null, out var _));
        }

        [Fact]
        public static void DefaultBinderNamedParametersSkippedAndOutOfOrderTest()
        {
            MethodInfo[] methods = typeof(Sample).GetMethods();
            object[] methodArgs = new object[] { 8, "value" };
            MethodBase method = binder.BindToMethod(flags, methods, ref methodArgs, null, null, new[] { "par5", "par1" }, out var _);
            Assert.NotNull(method);
            Assert.Equal("MethodMoreParameters", method.Name);
        }

        [Fact]
        public void InvokeWithIncorrectTargetTypeThrowsCorrectException()
        {
            Type t = typeof(Sample);
            object incorrectInstance = Activator.CreateInstance(t);
            MethodInvoker invoker = MethodInvoker.Create(typeof(Test).GetMethod(nameof(Test.TestMethod)));

            TargetException ex = Assert.Throws<TargetException>(() => invoker.Invoke(obj: incorrectInstance, "NotAnInt"));
            Assert.Equal("Object type Test does not match target type Sample.", ex.Message);
        }

        [Fact]
        public static void InvokeWithNamedParameters1st2ndTest()
        {
            Type t = typeof(Sample);
            object instance = Activator.CreateInstance(t);
            object[] methodArgs = new object[] { "value", 3 };
            string[] paramNames = new string[] { "param1", "param2" };

            int result = (int)t.InvokeMember("SampleMethod", invokeFlags, null, instance, methodArgs, null, null, paramNames);
            Assert.Equal(3, result);
        }

        [Fact]
        public static void InvokeWithNamedParameters1st3rd()
        {
            Type t = typeof(Sample);
            object instance = Activator.CreateInstance(t);
            object[] methodArgs = new object[] { "value", true };
            string[] paramNames = new string[] { "param1", "param3" };

            int result = (int)t.InvokeMember("SampleMethod", invokeFlags, null, instance, methodArgs, null, null, paramNames);
            Assert.Equal(7, result);
        }

        [Fact]
        public static void InvokeWithNamedParameters1st4th()
        {
            Type t = typeof(Sample);
            object instance = Activator.CreateInstance(t);
            object[] methodArgs = new object[] { "value", true };
            string[] paramNames = new string[] { "param1", "param4" };

            int result = (int)t.InvokeMember("AnotherMethod", invokeFlags, null, instance, methodArgs, null, null, paramNames);
            Assert.Equal(7, result);
        }

        [Fact]
        public static void InvokeWithNamedParameters4th1st()
        {
            Type t = typeof(Sample);
            object instance = Activator.CreateInstance(t);
            object[] methodArgs = new object[] { true, "value" };
            string[] paramNames = new string[] { "param4", "param1" };

            int result = (int)t.InvokeMember("AnotherMethod", invokeFlags, null, instance, methodArgs, null, null, paramNames);
            Assert.Equal(7, result);
        }

        [Fact]
        public static void InvokeWithNamedParametersOutOfOrder()
        {
            Type t = typeof(Sample);
            object instance = Activator.CreateInstance(t);
            object[] methodArgs = new object[] { 3, "value2", true, "value" };
            string[] paramNames = new string[] { "param3", "param2", "param4", "param1" };

            int result = (int)t.InvokeMember("AnotherMethod", invokeFlags, null, instance, methodArgs, null, null, paramNames);
            Assert.Equal(8, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public static void InvokeWithCreateInstance(string name)
        {
            Assert.IsType<Sample>(typeof(Sample).InvokeMember(name, BindingFlags.CreateInstance, null, null, null));
        }

        public class Test
        {
            public void TestMethod(int param1) { }
            public void SampleMethod(int param2 = 2, bool param3 = false) { }
        }

        public class Sample
        {
            public static int SampleMethod(int param1 = 2)
            {
                return 1;
            }

            public static int SampleMethod(int param2 = 2, bool param3 = false)
            {
                return 1;
            }

            public void NoDefaultParameterMethod(string param1, int param2, bool param3, double param4) { }

            public static int SampleMethod(string param1, int param2 = 2, bool param3 = false)
            {
                if (param3)
                {
                    return param2 + param1.Length;
                }

                return param2;
            }

            public int AnotherMethod(string param1, string param2 = "", int param3 = 2, bool param4 = false)
            {
                if (param4)
                {
                    return param3 + param1.Length;
                }

                return param3;
            }

            public void MethodMoreParameters(string par1, string par2 = "", int par3 = 2, bool par4 = false, short par5 = 1) { }
        }
    }
}
