// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestPlatforms.Browser)]
    public class ILGeneratorEmit4
    {
        [Fact]
        public void TestEmitCalliBlittable()
        {
            int a = 1, b = 1, result = 2;

            ModuleBuilder moduleBuilder = Helpers.DynamicModule();
            TypeBuilder typeBuilder = moduleBuilder.DefineType("T", TypeAttributes.Public);
            Type returnType = typeof(int);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("F",
                MethodAttributes.Public | MethodAttributes.Static, returnType, new Type[] { typeof(IntPtr), typeof(int), typeof(int) });
            methodBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);

            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, returnType, new Type[] { typeof(int), typeof(int) });
            il.Emit(OpCodes.Ret);

            Type dynamicType = typeBuilder.CreateType();

            var del = new Int32SumStdCall(Int32Sum);
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);

            object resultValue = dynamicType
                .GetMethod("F", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { funcPtr, a, b });

            GC.KeepAlive(del);

            Assert.IsType(returnType, resultValue);
            Assert.Equal(result, resultValue);
        }

        [Fact]
        public void TestDynamicMethodEmitCalliBlittable()
        {
            int a = 1, b = 1, result = 2;

            Type returnType = typeof(int);

            var dynamicMethod = new DynamicMethod("F", returnType, new Type[] { typeof(IntPtr), typeof(int), typeof(int) });

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, returnType, new Type[] { typeof(int), typeof(int) });
            il.Emit(OpCodes.Ret);

            var del = new Int32SumStdCall(Int32Sum);
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);

            object resultValue = dynamicMethod
                .Invoke(null, new object[] { funcPtr, a, b });

            GC.KeepAlive(del);

            Assert.IsType(returnType, resultValue);
            Assert.Equal(result, resultValue);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public void TestEmitCalliNonBlittable()
        {
            string input = "Test string!", result = "!gnirts tseT";

            ModuleBuilder moduleBuilder = Helpers.DynamicModule();
            TypeBuilder typeBuilder = moduleBuilder.DefineType("T", TypeAttributes.Public);
            Type returnType = typeof(string);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("F",
                MethodAttributes.Public | MethodAttributes.Static, returnType, new Type[] { typeof(IntPtr), typeof(string) });
            methodBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);

            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(OpCodes.Calli, CallingConvention.Cdecl, returnType, new Type[] { typeof(string) });
            il.Emit(OpCodes.Ret);

            Type dynamicType = typeBuilder.CreateType();

            var del = new StringReverseCdecl(StringReverse);
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);

            object resultValue = dynamicType
                .GetMethod("F", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { funcPtr, input });

            GC.KeepAlive(del);

            Assert.IsType(returnType, resultValue);
            Assert.Equal(result, resultValue);
        }

        [Fact]
        public void TestDynamicMethodEmitCalliNonBlittable()
        {
            string input = "Test string!", result = "!gnirts tseT";

            Type returnType = typeof(string);

            var dynamicMethod = new DynamicMethod("F", returnType, new Type[] { typeof(IntPtr), typeof(string) });

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(OpCodes.Calli, CallingConvention.Cdecl, returnType, new Type[] { typeof(string) });
            il.Emit(OpCodes.Ret);

            var del = new StringReverseCdecl(StringReverse);
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);

            object resultValue = dynamicMethod
                .Invoke(null, new object[] { funcPtr, input });

            GC.KeepAlive(del);

            Assert.IsType(returnType, resultValue);
            Assert.Equal(result, resultValue);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Int32SumStdCall(int a, int b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate string StringReverseCdecl(string a);

        private static int Int32Sum(int a, int b) => a + b;

        private static string StringReverse(string a) => string.Join("", a.Reverse());
    }
}
