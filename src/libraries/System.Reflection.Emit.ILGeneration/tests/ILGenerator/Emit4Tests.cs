// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestPlatforms.Browser)]
    public class ILGeneratorEmit4
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/133123", TestPlatforms.Wasi, TargetFrameworkMonikers.Any, TestRuntimes.CoreCLR)]
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
        public void TestEmitCalliManagedBlittable()
        {
            int a = 1, b = 1, result = 2;

            ModuleBuilder moduleBuilder = Helpers.DynamicModule();
            TypeBuilder typeBuilder = moduleBuilder.DefineType("T", TypeAttributes.Public);
            Type returnType = typeof(int);

            MethodBuilder methodBuilder = typeBuilder.DefineMethod("F",
                MethodAttributes.Public | MethodAttributes.Static, returnType, new Type[] { typeof(IntPtr), typeof(int), typeof(int) });
            methodBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);

            MethodInfo method = typeof(ILGeneratorEmit4).GetMethod(nameof(ILGeneratorEmit4.Int32Sum), BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException("method is null");
            IntPtr funcPtr = method.MethodHandle.GetFunctionPointer();

            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, returnType, new Type[] { typeof(int), typeof(int) }, null);
            il.Emit(OpCodes.Ret);

            Type dynamicType = typeBuilder.CreateType();

            object resultValue = dynamicType
                .GetMethod("F", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { funcPtr, a, b });

            Assert.IsType(returnType, resultValue);
            Assert.Equal(result, resultValue);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/133123", TestPlatforms.Wasi, TargetFrameworkMonikers.Any, TestRuntimes.CoreCLR)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/118965", typeof(PlatformDetection), nameof(PlatformDetection.IsCoreClrInterpreter))]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/118965", typeof(PlatformDetection), nameof(PlatformDetection.IsCoreClrInterpreter))]
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

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124149", TestRuntimes.Mono)]
        public void TestDynamicMethodEmitCalliFnPtrManaged()
        {
            Type fnPtrType = Type.MakeFunctionPointerSignatureType(typeof(int), [typeof(int), typeof(int)]);
            DynamicMethod dynamicMethod = new("Test", typeof(int), [typeof(nint)]);

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_S, (byte)10);
            il.Emit(OpCodes.Ldc_I4_S, (byte)5);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(fnPtrType);
            il.Emit(OpCodes.Ret);

            object resultValue = dynamicMethod.Invoke(null, [((Func<int, int, int>)Int32Sum).Method.MethodHandle.GetFunctionPointer()]);

            Assert.Equal(15, resultValue);
        }

        [Fact]
        public unsafe void TestDynamicMethodFnPtrParameterAndLocal()
        {
            DynamicMethod dynamicMethod = new("Test", typeof(int), [typeof(delegate*<int, int, int>)]);

            ILGenerator il = dynamicMethod.GetILGenerator();
            LocalBuilder local = il.DeclareLocal(typeof(delegate*<int, int, int>));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Ldc_I4_S, (byte)10);
            il.Emit(OpCodes.Ldc_I4_S, (byte)5);
            il.Emit(OpCodes.Ldloc, local);
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(int), [typeof(int), typeof(int)], null);
            il.Emit(OpCodes.Ret);

            object resultValue = dynamicMethod.Invoke(null, [((Func<int, int, int>)Int32Sum).Method.MethodHandle.GetFunctionPointer()]);

            Assert.Equal(15, resultValue);
        }

        [Fact]
        public unsafe void TestDynamicMethodNestedFnPtrParameter()
        {
            DynamicMethod dynamicMethod = new("Test", typeof(int), [typeof(delegate*<int, delegate*<int, int>, int>), typeof(delegate*<int, int>)]);

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_S, (byte)21);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(int), [typeof(int), typeof(delegate*<int, int>)], null);
            il.Emit(OpCodes.Ret);

            object resultValue = dynamicMethod.Invoke(null, [(nint)(delegate*<int, delegate*<int, int>, int>)&Apply, (nint)(delegate*<int, int>)&Twice]);

            Assert.Equal(42, resultValue);
        }

        [Fact]
        public unsafe void TestDynamicMethodFnPtrWithClassAndValueTypeParameters()
        {
            DynamicMethod dynamicMethod = new("Test", typeof(int), [typeof(delegate*<Version, TimeSpan, int>), typeof(Version)]);

            ILGenerator il = dynamicMethod.GetILGenerator();
            LocalBuilder local = il.DeclareLocal(typeof(delegate*<Version, TimeSpan, int>));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldsfld, typeof(TimeSpan).GetField(nameof(TimeSpan.MaxValue)));
            il.Emit(OpCodes.Ldloc, local);
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(int), [typeof(Version), typeof(TimeSpan)], null);
            il.Emit(OpCodes.Ret);

            object resultValue = dynamicMethod.Invoke(null, [(nint)(delegate*<Version, TimeSpan, int>)&MajorPlusHours, new Version(3, 0)]);

            Assert.Equal(3 + TimeSpan.MaxValue.Hours, resultValue);
        }

        [Fact]
        public unsafe void TestDynamicMethodUnmanagedFnPtrParameter()
        {
            DynamicMethod dynamicMethod = new("Test", typeof(nint), [typeof(delegate* unmanaged[Cdecl]<nint>)]);

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(OpCodes.Calli, CallingConvention.Cdecl, typeof(nint), Type.EmptyTypes);
            il.Emit(OpCodes.Ret);

            nint result = (nint)dynamicMethod.Invoke(null, [(nint)(delegate* unmanaged[Cdecl]<nint>)&GetInt32SumUnmanaged])!;

            Assert.Equal(15, ((delegate*<int, int, int>)result)(10, 5));
        }

        [Fact]
        public unsafe void TestDynamicMethodEmitCallFnPtrReturnType()
        {
            DynamicMethod dynamicMethod = new("Test", typeof(int), Type.EmptyTypes);

            ILGenerator il = dynamicMethod.GetILGenerator();
            LocalBuilder local = il.DeclareLocal(typeof(nint));
            il.EmitCall(OpCodes.Call, typeof(ILGeneratorEmit4).GetMethod(nameof(GetInt32Sum), BindingFlags.NonPublic | BindingFlags.Static), null);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Ldc_I4_S, (byte)10);
            il.Emit(OpCodes.Ldc_I4_S, (byte)5);
            il.Emit(OpCodes.Ldloc, local);
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(int), [typeof(int), typeof(int)], null);
            il.Emit(OpCodes.Ret);

            object resultValue = dynamicMethod.Invoke(null, null);

            Assert.Equal(15, resultValue);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public unsafe void TestDynamicMethodEmitCalliFnPtrReturnType(bool unmanaged)
        {
            DynamicMethod dynamicMethod = new("Test", typeof(delegate*<int, int, int>), [typeof(nint)]);

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (unmanaged)
                il.EmitCalli(OpCodes.Calli, CallingConvention.Cdecl, typeof(delegate*<int, int, int>), Type.EmptyTypes);
            else
                il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(delegate*<int, int, int>), Type.EmptyTypes, null);
            il.Emit(OpCodes.Ret);

            nint getter = unmanaged
                ? (nint)(delegate* unmanaged[Cdecl]<nint>)&GetInt32SumUnmanaged
                : (nint)(delegate*<delegate*<int, int, int>>)&GetInt32Sum;
            delegate*<int, int, int> fn = ((Int32SumGetter)dynamicMethod.CreateDelegate(typeof(Int32SumGetter)))(getter);

            Assert.Equal(15, fn(10, 5));
        }

        [Fact]
        public unsafe void TestDynamicMethodEmitCalliSignatureHelperFnPtrParameter()
        {
            SignatureHelper sig = SignatureHelper.GetMethodSigHelper(null, typeof(int), [typeof(int), typeof(delegate*<int, int>)]);
            DynamicMethod dynamicMethod = new("Test", typeof(int), [typeof(nint), typeof(nint)]);

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_S, (byte)21);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Calli, sig);
            il.Emit(OpCodes.Ret);

            object resultValue = dynamicMethod.Invoke(null, [(nint)(delegate*<int, delegate*<int, int>, int>)&Apply, (nint)(delegate*<int, int>)&Twice]);

            Assert.Equal(42, resultValue);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124149", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/133123", TestPlatforms.Wasi, TargetFrameworkMonikers.Any, TestRuntimes.CoreCLR)]
        public void TestDynamicMethodEmitCalliFnPtrStdCall()
        {
            int a = 1, b = 1, result = 2;

            Type returnType = typeof(int);

            var dynamicMethod = new DynamicMethod("F", returnType, [typeof(nint), typeof(int), typeof(int)]);

            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(Type.MakeFunctionPointerSignatureType(
                returnType,
                parameterTypes: [typeof(int), typeof(int)],
                isUnmanaged: true,
                callingConventions: [typeof(CallConvStdcall)]));
            il.Emit(OpCodes.Ret);

            var del = new Int32SumStdCall(Int32Sum);
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(del);

            object resultValue = dynamicMethod
                .Invoke(null, [funcPtr, a, b]);

            GC.KeepAlive(del);

            Assert.IsType(returnType, resultValue);
            Assert.Equal(result, resultValue);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124149", TestRuntimes.Mono)]
        public unsafe void TestDynamicMethodEmitCalli_NestedFunctionPointer()
        {
            // delegate*<delegate* unmanaged[Stdcall, MemberFunction]<short, bool>, int>
            Type type = Type.MakeFunctionPointerSignatureType(
                typeof(int),
                [Type.MakeFunctionPointerSignatureType(typeof(bool), [typeof(short)], true, [typeof(CallConvStdcall), typeof(CallConvMemberFunction)])]);

            DynamicMethod dynamicMethod = new("F", typeof(int), [typeof(nint)]);
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(type);
            il.Emit(OpCodes.Ret);

            object result = dynamicMethod.Invoke(null, [(nint)(delegate*<delegate* unmanaged[Stdcall, MemberFunction]<short, bool>, int>)(&ComplexSignature)]);
            Assert.Equal(5, (int)result);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124149", TestRuntimes.Mono)]
        public unsafe void TestDynamicMethodEmitCalli_NestedFunctionPointer2()
        {
            // delegate*<delegate* unmanaged[Cdecl]<delegate*<delegate* unmanaged[Fastcall, SuppressGCTransition]<List<short>*>>>, delegate*<delegate* unmanaged[Stdcall, MemberFunction]<short, bool>, int>>
            Type type = Type.MakeFunctionPointerSignatureType(
                Type.MakeFunctionPointerSignatureType(
                    typeof(int),
                    [Type.MakeFunctionPointerSignatureType(
                        typeof(bool),
                        [typeof(short)], true,
                        [typeof(CallConvStdcall), typeof(CallConvMemberFunction)])], false),
                [Type.MakeFunctionPointerSignatureType(
                    Type.MakeFunctionPointerSignatureType(
                        Type.MakeFunctionPointerSignatureType(
                            typeof(List<>).MakeGenericType(typeof(short)).MakePointerType(),
                            [], true,
                            [typeof(CallConvFastcall), typeof(CallConvSuppressGCTransition)]),
                        [], false),
                    [], true,
                    [typeof(CallConvCdecl)])]);

            DynamicMethod dynamicMethod = new("F", typeof(nint), [typeof(nint)]);
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Conv_I);
            il.Emit(OpCodes.Ldarg_0);
            il.EmitCalli(type);
            il.Emit(OpCodes.Ret);

            object result = dynamicMethod.Invoke(
                null,
                [(nint)(delegate*<delegate* unmanaged[Cdecl]<delegate*<delegate* unmanaged[Fastcall, SuppressGCTransition]<List<short>*>>>,
                delegate*<delegate* unmanaged[Stdcall, MemberFunction]<short, bool>, int>>)(&EvenMoreComplexSignature)]);
            Assert.Equal((nint)(delegate*<delegate* unmanaged[Stdcall, MemberFunction]<short, bool>, int>)(&ComplexSignature), (nint)result);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Int32SumStdCall(int a, int b);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate string StringReverseCdecl(string a);

        private unsafe delegate delegate*<int, int, int> Int32SumGetter(nint getter);

        private static int Int32Sum(int a, int b) => a + b;

        private unsafe static delegate*<int, int, int> GetInt32Sum() => &Int32Sum;

        // Returns nint: Mono cannot create the UnmanagedCallersOnly wrapper for a function pointer return type
        // (https://github.com/dotnet/runtime/issues/136448).
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private unsafe static nint GetInt32SumUnmanaged() => (nint)(delegate*<int, int, int>)&Int32Sum;

        private unsafe static int Apply(int value, delegate*<int, int> fn) => fn(value);

        private static int Twice(int value) => value * 2;

        private static int MajorPlusHours(Version version, TimeSpan time) => version.Major + time.Hours;

        private static string StringReverse(string a) => string.Join("", a.Reverse());

        private unsafe static int ComplexSignature(delegate* unmanaged[Stdcall, MemberFunction]<short, bool> arg) => 5;

        private unsafe static delegate*<delegate* unmanaged[Stdcall, MemberFunction]<short, bool>, int> EvenMoreComplexSignature(
            delegate* unmanaged[Cdecl]<delegate*<delegate* unmanaged[Fastcall, SuppressGCTransition]<List<short>*>>> arg) => &ComplexSignature;
    }
}
