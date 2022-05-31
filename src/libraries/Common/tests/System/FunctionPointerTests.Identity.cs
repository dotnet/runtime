// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Tests;
using Xunit;

namespace System.Tests.Types
{
    // Also see ModifiedTypeTests which tests custom modifiers.
    // Unmodified Type instances are cached and keyed by the runtime.
    // Modified Type instances are created for each member.
    public partial class FunctionPointerTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestFunctionUnmanagedPointerReturn_DifferentReturnValue()
        {
            Type t = typeof(FunctionPointerHolder).Project();

            MethodInfo m1 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue1), Bindings);
            Type fcnPtr1 = m1.ReturnType;
            Assert.True(fcnPtr1.IsFunctionPointer);

            MethodInfo m2 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue2), Bindings);
            Type fcnPtr2 = m2.ReturnType;
            Assert.True(fcnPtr2.IsFunctionPointer);
            Assert.False(fcnPtr1.IsFunctionPointerEqual(fcnPtr2));
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.Field_Int), nameof(FunctionPointerHolder.Field_DateOnly))]
        [InlineData(nameof(FunctionPointerHolder.Field_DateOnly), nameof(FunctionPointerHolder.Field_Int))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestSigEqualityInDifferentModule_Field(string name, string otherName)
        {
            Type fph1 = typeof(FunctionPointerHolder).Project();
            Type fph2 = typeof(FunctionPointerHolderSeparateModule).Project();
            Assert.True(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph2, name)));

            // Verify other combinations fail
            Assert.False(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsFunctionPointerEqual(GetFuncPtr(fph2, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsFunctionPointerEqual(GetFuncPtr(fph2, otherName)));

            static Type GetFuncPtr(Type owner, string name) => owner.GetField(name, Bindings).FieldType;
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.Prop_Int), nameof(FunctionPointerHolder.Prop_DateOnly))]
        [InlineData(nameof(FunctionPointerHolder.Prop_DateOnly), nameof(FunctionPointerHolder.Prop_Int))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestSigEqualityInDifferentModule_Property(string name, string otherName)
        {
            Type fph1 = typeof(FunctionPointerHolder).Project();
            Type fph2 = typeof(FunctionPointerHolderSeparateModule).Project();
            Assert.True(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph2, name)));

            // Verify other combinations fail
            Assert.False(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsFunctionPointerEqual(GetFuncPtr(fph2, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsFunctionPointerEqual(GetFuncPtr(fph2, otherName)));

            static Type GetFuncPtr(Type owner, string name) => owner.GetProperty(name, Bindings).PropertyType;
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodReturnValue_Int), nameof(FunctionPointerHolder.MethodReturnValue_DateOnly))]
        [InlineData(nameof(FunctionPointerHolder.MethodReturnValue_DateOnly), nameof(FunctionPointerHolder.MethodReturnValue_Int))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void TestSigEqualityInDifferentModule_MethodReturn(string name, string otherName)
        {
            Type fph1 = typeof(FunctionPointerHolder).Project();
            Type fph2 = typeof(FunctionPointerHolderSeparateModule).Project();
            Assert.True(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph2, name)));

            // Verify other combinations fail
            Assert.False(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph1, name).IsFunctionPointerEqual(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsFunctionPointerEqual(GetFuncPtr(fph2, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsFunctionPointerEqual(GetFuncPtr(fph2, otherName)));

            static Type GetFuncPtr(Type owner, string name) => owner.GetMethod(name, Bindings).ReturnParameter.ParameterType;
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), nameof(FunctionPointerHolder.MethodCallConv_Cdecl_SuppressGCTransition))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), nameof(FunctionPointerHolder.MethodCallConv_Stdcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), nameof(FunctionPointerHolder.MethodCallConv_Thiscall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), nameof(FunctionPointerHolder.MethodCallConv_Fastcall))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void CallingConventionIdentity_Unmodified(string methodName1, string methodName2)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m1 = t.GetMethod(methodName1, Bindings);
            MethodInfo m2 = t.GetMethod(methodName2, Bindings);

            Type fnPtrType1 = m1.GetParameters()[0].ParameterType;
            Type fnPtrType2 = m2.GetParameters()[0].ParameterType;

            Assert.True(fnPtrType1.IsFunctionPointerEqual(fnPtrType2));
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), nameof(FunctionPointerHolder.MethodCallConv_Cdecl_SuppressGCTransition))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), nameof(FunctionPointerHolder.MethodCallConv_Stdcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), nameof(FunctionPointerHolder.MethodCallConv_Thiscall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), nameof(FunctionPointerHolder.MethodCallConv_Fastcall))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71883", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public static unsafe void CallingConventionIdentity_Modified(string methodName1, string methodName2)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m1 = t.GetMethod(methodName1, Bindings);
            MethodInfo m2 = t.GetMethod(methodName2, Bindings);

            Type fnPtrType1 = m1.GetParameters()[0].GetModifiedParameterType();
            Type fnPtrType2 = m2.GetParameters()[0].GetModifiedParameterType();

            Assert.True(fnPtrType1.IsFunctionPointerNotEqual(fnPtrType2));
        }
    }
}
