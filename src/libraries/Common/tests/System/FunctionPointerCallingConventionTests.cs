// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Tests;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests.Types
{
    public partial class FunctionPointerCallingConventionTests
    {
        private const BindingFlags Bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void ManagedCallingConvention(bool modified)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(nameof(FunctionPointerHolder.MethodCallConv_Managed), Bindings);
            Type fnPtrType = modified ? m.GetParameters()[0].ParameterType : m.GetParameters()[0].GetModifiedParameterType();

            Type[] callConvs = fnPtrType.GetFunctionPointerCallingConventions();
            Assert.Equal(0, callConvs.Length);
            Assert.False(fnPtrType.IsUnmanagedFunctionPointer);

            Type returnType = fnPtrType.GetFunctionPointerReturnType();
            Assert.Equal(0, returnType.GetOptionalCustomModifiers().Length);
            Assert.Equal(0, returnType.GetRequiredCustomModifiers().Length);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Stdcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Thiscall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Fastcall))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void UnmanagedCallConv_Param_Unmodified(string methodName)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(methodName, Bindings);

            Type fnPtrType = m.GetParameters()[0].ParameterType;
            Assert.True(fnPtrType.IsUnmanagedFunctionPointer);
            Assert.Equal(0, fnPtrType.GetFunctionPointerReturnType().GetOptionalCustomModifiers().Length);
            Assert.Equal(0, fnPtrType.GetFunctionPointerReturnType().GetRequiredCustomModifiers().Length);
            Assert.Equal(0, fnPtrType.GetFunctionPointerCallingConventions().Length);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), typeof(CallConvCdecl))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Stdcall), typeof(CallConvStdcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Thiscall), typeof(CallConvThiscall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Fastcall), typeof(CallConvFastcall))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void UnmanagedCallConv_Param_Modified(string methodName, Type callingConventionRuntime)
        {
            Type callingConvention = callingConventionRuntime.Project();
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(methodName, Bindings);

            Type fnPtrType = m.GetParameters()[0].GetModifiedParameterType();
            Assert.True(fnPtrType.IsUnmanagedFunctionPointer);
            Assert.Equal(0, fnPtrType.GetFunctionPointerReturnType().GetOptionalCustomModifiers().Length);
            Assert.Equal(0, fnPtrType.GetFunctionPointerReturnType().GetRequiredCustomModifiers().Length);
            Type[] callConvs = fnPtrType.GetFunctionPointerCallingConventions();
            Assert.Equal(1, callConvs.Length);
            Assert.Equal(callingConvention, callConvs[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void UnmanagedCallConvs_Return_Unmodified()
        {
            Type t = typeof(FunctionPointerHolder).Project();

            MethodInfo m1 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue_DifferentCallingConventions1), Bindings);
            Type fcnPtr1 = m1.ReturnType;
            Assert.True(fcnPtr1.IsUnmanagedFunctionPointer);

            MethodInfo m2 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue_DifferentCallingConventions2), Bindings);
            Type fcnPtr2 = m2.ReturnType;
            Assert.True(fcnPtr2.IsUnmanagedFunctionPointer);

            Assert.Equal(0, fcnPtr1.GetFunctionPointerCallingConventions().Length);
            Assert.Equal(0, fcnPtr2.GetFunctionPointerCallingConventions().Length);

            Assert.True(fcnPtr1.IsFunctionPointerEqual(fcnPtr2));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void UnmanagedCallConvs_Return_Modified()
        {
            Type t = typeof(FunctionPointerHolder).Project();

            MethodInfo m1 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue_DifferentCallingConventions1), Bindings);
            Type fcnPtr1 = m1.ReturnParameter.GetModifiedParameterType();
            Assert.True(fcnPtr1.IsUnmanagedFunctionPointer);

            MethodInfo m2 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue_DifferentCallingConventions2), Bindings);
            Type fcnPtr2 = m2.ReturnParameter.GetModifiedParameterType();
            Assert.True(fcnPtr2.IsUnmanagedFunctionPointer);

            Assert.NotSame(fcnPtr1, fcnPtr2);

            Type retType = fcnPtr1.GetFunctionPointerReturnType();
            Assert.True(typeof(int).Project().IsFunctionPointerEqual(retType.UnderlyingSystemType));

            Type[] modOpts = fcnPtr1.GetFunctionPointerReturnType().GetOptionalCustomModifiers();
            Assert.Equal(2, modOpts.Length);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl_SuppressGCTransition))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Stdcall_SuppressGCTransition))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Thiscall_SuppressGCTransition))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Fastcall_SuppressGCTransition))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void UnmanagedCallConv_PhysicalModifiers_Unmodified(string methodName)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(methodName, Bindings);

            Type fnPtrType = m.GetParameters()[0].ParameterType;
            Assert.True(fnPtrType.IsUnmanagedFunctionPointer);

            Assert.Equal(0, fnPtrType.GetFunctionPointerCallingConventions().Length);
            Assert.Equal(0, fnPtrType.GetOptionalCustomModifiers().Length);
            Assert.Equal(0, fnPtrType.GetRequiredCustomModifiers().Length);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl_SuppressGCTransition), typeof(CallConvCdecl))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Stdcall_SuppressGCTransition), typeof(CallConvStdcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Thiscall_SuppressGCTransition), typeof(CallConvThiscall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Fastcall_SuppressGCTransition), typeof(CallConvFastcall))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void UnmanagedCallConv_PhysicalModifiers_Modified(string methodName, Type callingConventionRuntime)
        {
            Type suppressGcTransitionType = typeof(CallConvSuppressGCTransition).Project();
            Type callingConvention = callingConventionRuntime.Project();
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(methodName, Bindings);

            Type fnPtrType = m.GetParameters()[0].GetModifiedParameterType();
            Assert.True(fnPtrType.IsUnmanagedFunctionPointer);

            Type[] callConvs = fnPtrType.GetFunctionPointerCallingConventions();
            Assert.Equal(2, callConvs.Length);
            Assert.Equal(suppressGcTransitionType, callConvs[0]);
            Assert.Equal(callingConvention, callConvs[1]);

            Type returnType = fnPtrType.GetFunctionPointerReturnType();
            Assert.Equal(2, returnType.GetOptionalCustomModifiers().Length);
            Assert.Equal(suppressGcTransitionType, returnType.GetOptionalCustomModifiers()[0]);
            Assert.Equal(callingConvention, returnType.GetOptionalCustomModifiers()[1]);
            Assert.Equal(0, returnType.GetRequiredCustomModifiers().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void GenericTypeParameter()
        {
            Type holder = typeof(FunctionPointerHolder).Project();
            FieldInfo f = holder.GetField(nameof(FunctionPointerHolder._genericType), Bindings);
            Type propType = f.FieldType.GetProperty("MyProp").GetModifiedPropertyType();

            // Requires skipping past the return parameter metadata in order to get to the metadata for the first parameter.
            Type paramType = propType.GetFunctionPointerParameterTypes()[1];
            Type[] cc = paramType.GetFunctionPointerCallingConventions();
            Assert.Equal(1, cc.Length);
            Assert.Equal(typeof(CallConvCdecl).Project(), cc[0]);
        }

        private unsafe partial class FunctionPointerHolder
        {
            public delegate* unmanaged[Cdecl, MemberFunction]<int> MethodUnmanagedReturnValue_DifferentCallingConventions1() => default;
            public delegate* unmanaged[Stdcall, MemberFunction]<int> MethodUnmanagedReturnValue_DifferentCallingConventions2() => default;

#pragma warning disable 0649
            public MyGenericClass<delegate*<int, int, int>[]> _genericType;
#pragma warning restore 0649

            // Methods to verify calling conventions and synthesized modopts.
            // The non-SuppressGCTransition variants are encoded with the CallKind byte.
            // The SuppressGCTransition variants are encoded as modopts (CallKind is "Unmananged").
            public void MethodCallConv_Managed(delegate* managed<ref MyClass, void> f) { }
            public void MethodCallConv_Cdecl(delegate* unmanaged[Cdecl]<void> f) { }
            public void MethodCallConv_Cdecl_SuppressGCTransition(delegate* unmanaged[Cdecl, SuppressGCTransition]<void> f) { }
            public void MethodCallConv_Stdcall(delegate* unmanaged[Stdcall]<void> f) { }
            public void MethodCallConv_Stdcall_SuppressGCTransition(delegate* unmanaged[Stdcall, SuppressGCTransition]<void> f) { }
            public void MethodCallConv_Thiscall(delegate* unmanaged[Thiscall]<void> f) { }
            public void MethodCallConv_Thiscall_SuppressGCTransition(delegate* unmanaged[Thiscall, SuppressGCTransition]<void> f) { }
            public void MethodCallConv_Fastcall(delegate* unmanaged[Fastcall]<void> f) { }
            public void MethodCallConv_Fastcall_SuppressGCTransition(delegate* unmanaged[Fastcall, SuppressGCTransition]<void> f) { }

            public class MyClass { }

            public unsafe class MyGenericClass<T>
            {
                public delegate*<T, delegate* unmanaged[Cdecl]<void>, void> MyProp { get; }
            }
        }
    }
}
