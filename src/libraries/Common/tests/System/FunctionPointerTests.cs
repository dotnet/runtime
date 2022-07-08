// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Reflection.Tests;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Tests.Types
{
    public partial class FunctionPointerTests
    {
        private const BindingFlags Bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestTypeMembers()
        {
            // Get an arbitrary function pointer
            TypeInfo t = (TypeInfo)typeof(FunctionPointerHolder).Project().GetField(nameof(FunctionPointerHolder.ToString_1), Bindings).FieldType;

            // Function pointer relevant members:
            Assert.Equal("System.Void()", t.ToString());
            Assert.Null(t.FullName);
            Assert.Null(t.AssemblyQualifiedName);
            Assert.Equal("*()", t.Name);
            Assert.Equal("System", t.Namespace); // All function pointers report "System"
            Assert.True(t.IsFunctionPointer);
            Assert.False(t.IsPointer); // A function pointer is not compatible with IsPointer semantics.
            Assert.False(t.IsUnmanagedFunctionPointer);
            Assert.NotNull(t.Module);
            Assert.NotNull(t.Assembly);

            // Common for all function pointers:
            Assert.Equal(TypeAttributes.Public, t.Attributes);
            Assert.Null(t.BaseType);
            Assert.False(t.ContainsGenericParameters);
            Assert.False(t.ContainsGenericParameters);
            Assert.False(t.DeclaredConstructors.Any());
            Assert.False(t.DeclaredEvents.Any());
            Assert.False(t.DeclaredFields.Any());
            Assert.False(t.DeclaredMembers.Any());
            Assert.False(t.DeclaredMethods.Any());
            Assert.False(t.DeclaredNestedTypes.Any());
            Assert.False(t.DeclaredProperties.Any());
            Assert.Null(t.DeclaringType);
            Assert.Equal(Guid.Empty, t.GUID);
            Assert.Throws<InvalidOperationException>(() => t.GenericParameterAttributes);
            Assert.Throws<InvalidOperationException>(() => t.GenericParameterPosition);
            Assert.Equal(0, t.GenericTypeArguments.Length);
            Assert.Equal(0, t.GenericTypeParameters.Length);
            Assert.False(t.HasElementType);
            Assert.False(t.IsAbstract);
            Assert.True(t.IsAnsiClass);
            Assert.False(t.IsArray);
            Assert.False(t.IsAutoClass);
            Assert.True(t.IsAutoLayout);
            Assert.False(t.IsByRef);
            Assert.False(t.IsByRefLike);
            Assert.False(t.IsCOMObject);
            Assert.True(t.IsClass);
            Assert.False(t.IsAbstract);
            Assert.False(t.IsConstructedGenericType);
            Assert.False(t.IsContextful);
            Assert.False(t.IsEnum);
            Assert.False(t.IsExplicitLayout);
            Assert.False(t.IsGenericMethodParameter);
            Assert.False(t.IsGenericParameter);
            Assert.False(t.IsGenericType);
            Assert.False(t.IsGenericTypeDefinition);
            Assert.False(t.IsGenericTypeParameter);
            Assert.False(t.IsImport);
            Assert.False(t.IsInterface);
            Assert.False(t.IsLayoutSequential);
            Assert.False(t.IsMarshalByRef);
            Assert.False(t.IsNested);
            Assert.False(t.IsNestedAssembly);
            Assert.False(t.IsNestedFamANDAssem);
            Assert.False(t.IsNestedFamORAssem);
            Assert.False(t.IsNestedFamily);
            Assert.False(t.IsNestedPrivate);
            Assert.False(t.IsNestedPublic);
            Assert.False(t.IsNotPublic);
            Assert.False(t.IsPrimitive);
            Assert.True(t.IsPublic);
            Assert.False(t.IsSZArray);
            Assert.False(t.IsSealed);

            try
            {
                // MetadataLoadContext throws here.
                Assert.True(t.IsSecurityCritical);
                Assert.False(t.IsSecuritySafeCritical);
                Assert.False(t.IsSecurityTransparent);
            }
            catch (InvalidOperationException) { }

            Assert.False(t.IsSerializable);
            Assert.False(t.IsSignatureType);
            Assert.False(t.IsSpecialName);
            Assert.False(t.IsTypeDefinition);
            Assert.False(t.IsUnicodeClass);
            Assert.False(t.IsValueType);
            Assert.False(t.IsVariableBoundArray);
            Assert.True(t.IsVisible);
            Assert.Equal(MemberTypes.TypeInfo, t.MemberType);
            Assert.True(t.MetadataToken != 0);
            Assert.Null(t.ReflectedType);
            Assert.Null(t.TypeInitializer);

            // Select methods
            Assert.Throws<ArgumentException>(() => t.GetArrayRank());
            Assert.Null(t.GetElementType());
        }

        private static void MyMethod(){}


        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestNonFunctionPointerThrows()
        {
            Assert.Throws<InvalidOperationException>(() => typeof(int).GetFunctionPointerCallingConventions());
            Assert.Throws<InvalidOperationException>(() => typeof(int).GetFunctionPointerParameterInfos());
            Assert.Throws<InvalidOperationException>(() => typeof(int).GetFunctionPointerReturnParameter());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestToString()
        {
            // Function pointer types are inline in metadata and can't be loaded independently so they do not support the
            // MetadataLoadContext Type.Project() test extension so we use fields and project on the owning class.
            Assert.Equal("System.Void()", GetType(1).ToString());       // delegate*<void>
            Assert.Equal("System.Void()", GetType(2).ToString());       // delegate*unmanaged<void>
            Assert.Equal("System.Int32()", GetType(3).ToString());      // delegate*<int>
            Assert.Equal("System.Int32()*", GetType(4).ToString());     // delegate*<int>*
            Assert.Equal("System.Int32()[]", GetType(5).ToString());    // delegate*<int>[]
            Assert.Equal("System.Int32()", GetType(6).
                GetElementType().ToString());                           // delegate*<int>[] 
            Assert.Equal("System.Int32()*[]", GetType(7).ToString());   // delegate*<int>*[]
            Assert.Equal("System.Int32()()", GetType(8).ToString());    // delegate*<delegate*<int>>               
            Assert.Equal("System.Boolean(System.String(System.Int32))",
                GetType(9).ToString());                                 // delegate*<delegate*<int, string>, bool>

            Type GetType(int i) => typeof(FunctionPointerHolder).Project().GetField("ToString_" + i, Bindings).FieldType;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestFunctionPointerReturn()
        {
            Type t = typeof(FunctionPointerHolder).Project();

            MethodInfo m1 = t.GetMethod(nameof(FunctionPointerHolder.MethodReturnValue1), Bindings);
            Type fcnPtr1 = m1.ReturnType;
            Assert.True(fcnPtr1.IsFunctionPointer);

            MethodInfo m2 = t.GetMethod(nameof(FunctionPointerHolder.MethodReturnValue2), Bindings);
            Type fcnPtr2 = m2.ReturnType;
            Assert.True(fcnPtr2.IsFunctionPointer);

            Assert.True(fcnPtr1.Equals(fcnPtr2));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestFunctionUnmanagedPointerReturn_DifferentReturnValue()
        {
            Type t = typeof(FunctionPointerHolder).Project();

            MethodInfo m1 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue1), Bindings);
            Type fcnPtr1 = m1.ReturnType;
            Assert.True(fcnPtr1.IsFunctionPointer);

            MethodInfo m2 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue2), Bindings);
            Type fcnPtr2 = m2.ReturnType;
            Assert.True(fcnPtr2.IsFunctionPointer);
            Assert.False(fcnPtr1.Equals(fcnPtr2));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestFunctionUnmanagedPointerReturn_DifferentCallingConventions()
        {
            Type t = typeof(FunctionPointerHolder).Project();

            MethodInfo m1 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue_DifferentCallingConventions1), Bindings);
            Type fcnPtr1 = m1.ReturnType;
            Assert.True(fcnPtr1.IsFunctionPointer);
            Assert.True(fcnPtr1.IsUnmanagedFunctionPointer);

            MethodInfo m2 = t.GetMethod(nameof(FunctionPointerHolder.MethodUnmanagedReturnValue_DifferentCallingConventions2), Bindings);
            Type fcnPtr2 = m2.ReturnType;
            Assert.True(fcnPtr2.IsFunctionPointer);
            Assert.True(fcnPtr2.IsUnmanagedFunctionPointer);

            // The modopts are considered part of the "type key"
            Assert.NotSame(fcnPtr1, fcnPtr2);
            Assert.False(fcnPtr1.Equals(fcnPtr2));

            FunctionPointerParameterInfo retInfo = fcnPtr1.GetFunctionPointerReturnParameter();
            Assert.Equal(typeof(int).Project(), retInfo.ParameterType);

            Type[] modOpts = fcnPtr1.GetFunctionPointerReturnParameter().GetOptionalCustomModifiers();
            Assert.Equal(2, modOpts.Length);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodReturnValue1),
            "MethodReturnValue1()",
            "Int32",
            "System.Int32()")]
        [InlineData(nameof(FunctionPointerHolder.SeveralArguments),
            "SeveralArguments()",
            "Double",
            "System.Double(System.String, System.Boolean*&, System.Tests.Types.FunctionPointerTests+FunctionPointerHolder+MyClass, System.Tests.Types.FunctionPointerTests+FunctionPointerHolder+MyStruct&)",
            "String", "Boolean*&", "MyClass", "MyStruct&")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestMethod(
            string methodName,
            string methodToStringPostfix,
            string expectedFcnPtrReturnName,
            string expectedFcnPtrFullName,
            params string[] expectedArgNames)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(methodName, Bindings);
            Assert.Equal(expectedFcnPtrFullName + " " + methodToStringPostfix, m.ToString());

            Type fnPtrType = m.ReturnType;
            Assert.Null(fnPtrType.FullName);
            Assert.Null(fnPtrType.AssemblyQualifiedName);
            Assert.Equal("System", fnPtrType.Namespace);
            Assert.Equal("*()", fnPtrType.Name);

            VerifyArg(fnPtrType.GetFunctionPointerReturnParameter(), expectedFcnPtrReturnName);

            for (int i = 0; i < expectedArgNames.Length; i++)
            {
                VerifyArg(fnPtrType.GetFunctionPointerParameterInfos()[i], expectedArgNames[i]);
            }

            static void VerifyArg(FunctionPointerParameterInfo paramInfo, string expected)
            {
                Assert.Equal(expected, paramInfo.ParameterType.Name);
                Assert.Equal(paramInfo.ParameterType.ToString(), paramInfo.ToString());
                Assert.Null(paramInfo.GetType().DeclaringType);
            }
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.Prop_Int), "System.Int32()")]
        [InlineData(nameof(FunctionPointerHolder.Prop_MyClass), "System.Tests.Types.FunctionPointerTests+FunctionPointerHolder+MyClass()")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestProperty(string name, string expectedToString)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            PropertyInfo p = t.GetProperty(name, Bindings);
            Assert.Equal(expectedToString + " " + name, p.ToString());

            Type fnPtrType = p.PropertyType;
            Assert.Equal(expectedToString, fnPtrType.ToString());
            VerifyFieldOrProperty(fnPtrType);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.Field_Int), "System.Int32()")]
        [InlineData(nameof(FunctionPointerHolder.Field_MyClass), "System.Tests.Types.FunctionPointerTests+FunctionPointerHolder+MyClass()")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestField(string name, string expectedToString)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            FieldInfo f = t.GetField(name, Bindings);
            Assert.Equal(expectedToString + " " + name, f.ToString());

            Type fnPtrType = f.FieldType;
            Assert.Equal(expectedToString, fnPtrType.ToString());
            VerifyFieldOrProperty(fnPtrType);
        }

        private static void VerifyFieldOrProperty(Type fnPtrType)
        {
            Assert.Null(fnPtrType.FullName);
            Assert.Null(fnPtrType.AssemblyQualifiedName);
            Assert.Equal("*()", fnPtrType.Name);
            Assert.Equal("System", fnPtrType.Namespace);
            Assert.Null(fnPtrType.DeclaringType);
            Assert.Null(fnPtrType.BaseType);
            Assert.Equal<Type>(Type.EmptyTypes, fnPtrType.GetFunctionPointerCallingConventions());
            Assert.Equal<Type>(Type.EmptyTypes, fnPtrType.GetFunctionPointerReturnParameter().GetRequiredCustomModifiers());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestManagedCallingConvention()
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(nameof(FunctionPointerHolder.MethodCallConv_Managed), Bindings);
            Type fnPtrType = m.GetParameters()[0].ParameterType;
            Type[] callConvs = fnPtrType.GetFunctionPointerCallingConventions();
            Assert.Equal(0, callConvs.Length);
            Assert.True(fnPtrType.IsFunctionPointer);
            Assert.False(fnPtrType.IsUnmanagedFunctionPointer);

            FunctionPointerParameterInfo returnParam = fnPtrType.GetFunctionPointerReturnParameter();
            Assert.Equal(0, returnParam.GetOptionalCustomModifiers().Length);
            Assert.Equal(0, returnParam.GetRequiredCustomModifiers().Length);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl), typeof(CallConvCdecl))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Fastcall), typeof(CallConvFastcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Stdcall), typeof(CallConvStdcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Thiscall), typeof(CallConvThiscall))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestBaseCallingConventions(string methodName, Type callingConventionRuntime)
        {
            Type callingConvention = callingConventionRuntime.Project();
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(methodName, Bindings);
            Type fnPtrType = m.GetParameters()[0].ParameterType;
            Type[] callConvs = fnPtrType.GetFunctionPointerCallingConventions();

            Assert.Equal(1, callConvs.Length);
            Assert.Equal(callingConvention, callConvs[0]);
            Assert.True(fnPtrType.IsFunctionPointer);
            Assert.True(fnPtrType.IsUnmanagedFunctionPointer);

            FunctionPointerParameterInfo returnParam = fnPtrType.GetFunctionPointerReturnParameter();
            Assert.Equal(1, returnParam.GetOptionalCustomModifiers().Length);
            Assert.Equal(callingConvention, returnParam.GetOptionalCustomModifiers()[0]);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Cdecl_SuppressGCTransition), typeof(CallConvCdecl))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Fastcall_SuppressGCTransition), typeof(CallConvFastcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Stdcall_SuppressGCTransition), typeof(CallConvStdcall))]
        [InlineData(nameof(FunctionPointerHolder.MethodCallConv_Thiscall_SuppressGCTransition), typeof(CallConvThiscall))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestOptionalCallingConventions(string methodName, Type callingConventionRuntime)
        {
            Type suppressGcTransitionType = typeof(CallConvSuppressGCTransition).Project();
            Type callingConvention = callingConventionRuntime.Project();
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(methodName, Bindings);
            Type fnPtrType = m.GetParameters()[0].ParameterType;

            FunctionPointerParameterInfo returnParam = fnPtrType.GetFunctionPointerReturnParameter();
            Type[] callConvs = fnPtrType.GetFunctionPointerCallingConventions();
            Assert.Equal(2, callConvs.Length);
            Assert.Equal(suppressGcTransitionType, callConvs[0]);
            Assert.Equal(callingConvention, callConvs[1]);
            Assert.True(fnPtrType.IsFunctionPointer);
            Assert.True(fnPtrType.IsUnmanagedFunctionPointer);
            Assert.Equal(2, returnParam.GetOptionalCustomModifiers().Length);
            Assert.Equal(suppressGcTransitionType, returnParam.GetOptionalCustomModifiers()[0]);
            Assert.Equal(callingConvention, returnParam.GetOptionalCustomModifiers()[1]);
            Assert.Equal(0, returnParam.GetRequiredCustomModifiers().Length);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.Field_Int), nameof(FunctionPointerHolder.Field_DateOnly))]
        [InlineData(nameof(FunctionPointerHolder.Field_DateOnly), nameof(FunctionPointerHolder.Field_Int))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestSigEqualityInDifferentModule_Field(string name, string otherName)
        {
            Type fph1 = typeof(FunctionPointerHolder).Project();
            Type fph2 = typeof(FunctionPointerHolderSeparateModule).Project();
            Assert.True(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, name)));

            // Verify other combinations fail
            Assert.False(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, otherName)));

            static Type GetFuncPtr(Type owner, string name) => owner.GetField(name, Bindings).FieldType;
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.Prop_Int), nameof(FunctionPointerHolder.Prop_DateOnly))]
        [InlineData(nameof(FunctionPointerHolder.Prop_DateOnly), nameof(FunctionPointerHolder.Prop_Int))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestSigEqualityInDifferentModule_Property(string name, string otherName)
        {
            Type fph1 = typeof(FunctionPointerHolder).Project();
            Type fph2 = typeof(FunctionPointerHolderSeparateModule).Project();
            Assert.True(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, name)));

            // Verify other combinations fail
            Assert.False(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, otherName)));

            static Type GetFuncPtr(Type owner, string name) => owner.GetProperty(name, Bindings).PropertyType;
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.MethodReturnValue_Int), nameof(FunctionPointerHolder.MethodReturnValue_DateOnly))]
        [InlineData(nameof(FunctionPointerHolder.MethodReturnValue_DateOnly), nameof(FunctionPointerHolder.MethodReturnValue_Int))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]        
        public static unsafe void TestSigEqualityInDifferentModule_MethodReturn(string name, string otherName)
        {
            Type fph1 = typeof(FunctionPointerHolder).Project();
            Type fph2 = typeof(FunctionPointerHolderSeparateModule).Project();
            Assert.True(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, name)));

            // Verify other combinations fail
            Assert.False(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph1, name).IsEqualOrReferenceEquals(GetFuncPtr(fph1, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, otherName)));
            Assert.False(GetFuncPtr(fph2, name).IsEqualOrReferenceEquals(GetFuncPtr(fph2, otherName)));

            static Type GetFuncPtr(Type owner, string name) => owner.GetMethod(name, Bindings).ReturnParameter.ParameterType;
        }

        // Tests to add: required modifiers, ask for optional return modifiers before\after calling conventions
        public unsafe class FunctionPointerHolder
        {
            public delegate*<void> ToString_1;
            public delegate*unmanaged<void> ToString_2;
            public delegate*<int> ToString_3;
            public delegate*<int>* ToString_4;
            public delegate*<int>[] ToString_5;
            public delegate*<int>[] ToString_6;
            public delegate*<int>*[] ToString_7;
            public delegate*<delegate*<int>> ToString_8;
            public delegate*<delegate*<int, string>, bool> ToString_9;

            public delegate* managed<int> Field_Int;
            public delegate* managed<DateOnly> Field_DateOnly; // Verify non-primitive
            public delegate* managed<MyClass> Field_MyClass;
            public delegate* managed<int> Prop_Int { get; }
            public delegate* managed<DateOnly> Prop_DateOnly { get; }
            public delegate* managed<MyClass> Prop_MyClass { get; }
            public delegate* managed<int> MethodReturnValue_Int() => default;
            public delegate* managed<DateOnly> MethodReturnValue_DateOnly() => default;
            public delegate* unmanaged<int> MethodUnmanagedReturnValue_Int() => default;
            public delegate* unmanaged<DateOnly> MethodUnmanagedReturnValue_DateOnly() => default;

            public delegate* managed<int> MethodReturnValue1() => default;
            public delegate* managed<int> MethodReturnValue2() => default;
            public delegate* unmanaged<int> MethodUnmanagedReturnValue1() => default;
            public delegate* unmanaged<bool> MethodUnmanagedReturnValue2() => default;


            public delegate* unmanaged[Cdecl, MemberFunction]<int> MethodUnmanagedReturnValue_DifferentCallingConventions1() => default;
            public delegate* unmanaged[Stdcall, MemberFunction]<int> MethodUnmanagedReturnValue_DifferentCallingConventions2() => default;

            public delegate* unmanaged[Stdcall, MemberFunction]<string, ref bool*, MyClass, in MyStruct, double> SeveralArguments() => default;

            // Methods to verify calling conventions and synthesized modopts.
            // The non-SuppressGCTransition variants are encoded with the CallKind byte.
            // The SuppressGCTransition variants are encoded as modopts (CallKind is "Unmananged").
            public void MethodCallConv_Managed(delegate* managed<ref MyClass, void> f) { }
            public void MethodCallConv_Cdecl(delegate* unmanaged[Cdecl]<void> f) { }
            public void MethodCallConv_Cdecl_SuppressGCTransition(delegate* unmanaged[Cdecl, SuppressGCTransition]<void> f) { }
            public void MethodCallConv_Fastcall(delegate* unmanaged[Fastcall]<void> f) { }
            public void MethodCallConv_Fastcall_SuppressGCTransition(delegate* unmanaged[Fastcall, SuppressGCTransition]<void> f) { }
            public void MethodCallConv_Stdcall(delegate* unmanaged[Stdcall]<void> f) { }
            public void MethodCallConv_Stdcall_SuppressGCTransition(delegate* unmanaged[Stdcall, SuppressGCTransition]<void> f) { }
            public void MethodCallConv_Thiscall(delegate* unmanaged[Thiscall]<void> f) { }
            public void MethodCallConv_Thiscall_SuppressGCTransition(delegate* unmanaged[Thiscall, SuppressGCTransition]<void> f) { }

            public class MyClass { }
            public struct MyStruct { }
        }
    }
}
