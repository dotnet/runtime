// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Reflection.Tests;
using Xunit;

namespace System.Tests.Types
{
    // Also see ModifiedTypeTests which tests custom modifiers.
    public partial class FunctionPointerTests
    {
        private const BindingFlags Bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void TypeMembers()
        {
            // Get an arbitrary function pointer
            TypeInfo t = (TypeInfo)typeof(FunctionPointerHolder).Project().GetField(nameof(FunctionPointerHolder.ToString_1), Bindings).FieldType;

            // Function pointer relevant members:
            Assert.Equal("System.Void()", t.ToString());
            Assert.Null(t.FullName);
            Assert.Null(t.AssemblyQualifiedName);
            Assert.Equal(string.Empty, t.Name);
            Assert.Null(t.Namespace);
            Assert.True(t.IsFunctionPointer);
            Assert.False(t.IsPointer);
            Assert.False(t.IsUnmanagedFunctionPointer);

            // Common for all function pointers:
            Assert.NotNull(t.Assembly);
            Assert.Equal(TypeAttributes.Public, t.Attributes);
            Assert.Null(t.BaseType);
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

            if (FunctionPointerTestsExtensions.IsMetadataLoadContext)
            {
                Assert.Throws<InvalidOperationException>(() => t.IsSecurityCritical);
                Assert.Throws<InvalidOperationException>(() => t.IsSecuritySafeCritical);
                Assert.Throws<InvalidOperationException>(() => t.IsSecurityTransparent);
            }
            else
            {
                Assert.True(t.IsSecurityCritical);
                Assert.False(t.IsSecuritySafeCritical);
                Assert.False(t.IsSecurityTransparent);
            }

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
            Assert.NotNull(t.Module);
            Assert.Null(t.ReflectedType);
            Assert.Null(t.TypeInitializer);
            Assert.Throws<ArgumentException>(() => t.GetArrayRank());
            Assert.Null(t.GetElementType());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void NonFunctionPointerThrows()
        {
            Assert.Throws<InvalidOperationException>(() => typeof(int).GetFunctionPointerCallingConventions());
            Assert.Throws<InvalidOperationException>(() => typeof(int).GetFunctionPointerParameterTypes());
            Assert.Throws<InvalidOperationException>(() => typeof(int).GetFunctionPointerReturnType());
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
        public static unsafe void FunctionPointerReturn()
        {
            Type t = typeof(FunctionPointerHolder).Project();

            MethodInfo m1 = t.GetMethod(nameof(FunctionPointerHolder.MethodReturnValue1), Bindings);
            Type fcnPtr1 = m1.ReturnType;
            Assert.True(fcnPtr1.IsFunctionPointer);

            MethodInfo m2 = t.GetMethod(nameof(FunctionPointerHolder.MethodReturnValue2), Bindings);
            Type fcnPtr2 = m2.ReturnType;
            Assert.True(fcnPtr2.IsFunctionPointer);

            Assert.True(fcnPtr1.IsFunctionPointerEqual(fcnPtr2));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void RequiredModifiers()
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(nameof(FunctionPointerHolder.RequiredModifiers), Bindings);
            Type fcnPtr1 = m.ReturnParameter.GetModifiedParameterType();

            Type[] parameters = fcnPtr1.GetFunctionPointerParameterTypes();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(1, parameters[0].GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(Runtime.InteropServices.InAttribute).Project(), parameters[0].GetRequiredCustomModifiers()[0]);
            Assert.Equal(1, parameters[1].GetRequiredCustomModifiers().Length);
            Assert.Equal(typeof(Runtime.InteropServices.OutAttribute).Project(), parameters[1].GetRequiredCustomModifiers()[0]);
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
        public static unsafe void MethodInfo(
            string methodName,
            string methodToStringPostfix,
            string expectedFcnPtrReturnName,
            string expectedFcnPtrFullName,
            params string[] expectedArgNames)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            MethodInfo m = t.GetMethod(methodName, Bindings);
            Assert.Equal(expectedFcnPtrFullName + " " + methodToStringPostfix, m.ToString());

            Type fnPtrType = m.ReturnParameter.GetModifiedParameterType();
            Assert.Null(fnPtrType.FullName);
            Assert.Null(fnPtrType.AssemblyQualifiedName);
            Assert.Equal("", fnPtrType.Name);

            Assert.Equal(fnPtrType.GetFunctionPointerReturnType().Name, expectedFcnPtrReturnName);

            for (int i = 0; i < expectedArgNames.Length; i++)
            {
                Assert.Equal(fnPtrType.GetFunctionPointerParameterTypes()[i].Name, expectedArgNames[i]);
            }
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.Prop_Int), "System.Int32()")]
        [InlineData(nameof(FunctionPointerHolder.Prop_MyClass), "System.Tests.Types.FunctionPointerTests+FunctionPointerHolder+MyClass()")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Property(string name, string expectedToString)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            PropertyInfo p = t.GetProperty(name, Bindings);
            Assert.Equal(expectedToString + " " + name, p.ToString());

            Type fnPtrType = p.PropertyType;
            Assert.Equal(expectedToString, fnPtrType.ToString());
            VerifyFieldOrProperty(fnPtrType);

            fnPtrType = p.GetModifiedPropertyType();
            Assert.Equal(expectedToString, fnPtrType.ToString());
            VerifyFieldOrProperty(fnPtrType);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.Field_Int), "System.Int32()")]
        [InlineData(nameof(FunctionPointerHolder.Field_MyClass), "System.Tests.Types.FunctionPointerTests+FunctionPointerHolder+MyClass()")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/71095", TestRuntimes.Mono)]
        public static unsafe void Field(string name, string expectedToString)
        {
            Type t = typeof(FunctionPointerHolder).Project();
            FieldInfo f = t.GetField(name, Bindings);
            Assert.Equal(expectedToString + " " + name, f.ToString());

            Type fnPtrType = f.FieldType;
            Assert.Equal(expectedToString, fnPtrType.ToString());
            VerifyFieldOrProperty(fnPtrType);

            fnPtrType = f.GetModifiedFieldType();
            Assert.Equal(expectedToString, fnPtrType.ToString());
            VerifyFieldOrProperty(fnPtrType);
        }

        private static void VerifyFieldOrProperty(Type fnPtrType)
        {
            Assert.Null(fnPtrType.FullName);
            Assert.Null(fnPtrType.AssemblyQualifiedName);
            Assert.Equal("", fnPtrType.Name);
            Assert.Equal<Type>(Type.EmptyTypes, fnPtrType.GetFunctionPointerCallingConventions());
            Assert.Equal<Type>(Type.EmptyTypes, fnPtrType.GetFunctionPointerReturnType().GetRequiredCustomModifiers());
        }

        private unsafe class FunctionPointerHolder
        {
#pragma warning disable 0649
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
            public delegate* managed<MyClass> Field_MyClass;
#pragma warning restore 0649

            public delegate* managed<int> Prop_Int { get; }
            public delegate* managed<MyClass> Prop_MyClass { get; }

            public delegate* managed<int> MethodReturnValue1() => default;
            public delegate* managed<int> MethodReturnValue2() => default;

            public delegate* unmanaged[Stdcall, MemberFunction]<string, ref bool*, MyClass, in MyStruct, double> SeveralArguments() => default;
            public delegate*<in int, out int, void> RequiredModifiers() => default;

            public class MyClass { }
            public struct MyStruct { }
        }
    }
}
