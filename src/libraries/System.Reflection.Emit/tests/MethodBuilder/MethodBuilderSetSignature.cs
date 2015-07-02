// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderSetSignature
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, TestAssemblyBuilderAccess);

            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);

            return moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
        }

        [Fact]
        public void TestGenericMethodWithSingleGenericParameterSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            builder.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithMultipleGenericParameters()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[1];

            builder.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithReturnTypeRequiredModifiersSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            builder.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithReturnTypeRequiredAndOptionalModifiersSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                null,
                null,
                null);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithReturnTypeOptionalModifiersSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                null,
                null,
                null);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithParameterTypesSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int) };

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                desiredParamType,
                null,
                null);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithMultipleParametersSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int), typeParameters[1].AsType() };

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                desiredParamType,
                null,
                null);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithParameterTypeRequiredModifierSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int) };
            Type[][] parameterTypeRequiredCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
            }

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                desiredParamType,
                parameterTypeRequiredCustomModifiers,
                null);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithParameterTypeRequiredAndOptionModifierSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int) };
            Type[][] parameterTypeRequiredCustomModifiers = new Type[desiredParamType.Length][];
            Type[][] parameterTypeOptionalCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                desiredParamType,
                parameterTypeRequiredCustomModifiers,
                parameterTypeOptionalCustomModifiers);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestGenericMethodWithParameterTypeOptionalModifierSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(int) };
            Type[][] parameterTypeOptionalCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                desiredParamType,
                null,
                parameterTypeOptionalCustomModifiers);

            VerifyMethodSignature(typeBuilder, builder, desiredReturnType.AsType());
        }

        [Fact]
        public void TestWithNonGenericMethod()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] parameterTypes = new Type[] { typeof(string), typeof(object) };
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(void),
                parameterTypes);
            string[] parameterNames = new string[parameterTypes.Length];
            for (int i = 0; i < parameterNames.Length; ++i)
            {
                parameterNames[i] = "P" + i.ToString();
                builder.DefineParameter(i + 1, ParameterAttributes.In, parameterNames[i]);
            }

            Type desiredReturnType = typeof(void);
            Type[] desiredParamType = new Type[] { typeof(int) };
            Type[][] parameterTypeRequiredCustomModifiers = new Type[desiredParamType.Length][];
            Type[][] parameterTypeOptionalCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            builder.SetSignature(
                desiredReturnType,
                null,
                null,
                desiredParamType,
                parameterTypeRequiredCustomModifiers,
                parameterTypeOptionalCustomModifiers);
        }

        [Fact]
        public void TestWithNullOnParameters()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            builder.SetSignature(null, null, null, null, null, null);

            VerifyMethodSignature(typeBuilder, builder, null);
        }

        [Fact]
        public void TestWithNullOnReturnTypeWithModifiersSetToWrongTypes()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            builder.SetSignature(
                null,
                null,
                null,
                null,
                null,
                null);

            VerifyMethodSignature(typeBuilder, builder, null);
        }

        [Fact]
        public void TestWithNullOnReturnTypeWithModifiersSetCorrectly()
        {
            string methodName = null;
            int arraySize = 0;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            arraySize = TestLibrary.Generator.GetByte();

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = null;
            Type[][] parameterTypeRequiredCustomModifiers = new Type[arraySize][];
            Type[][] parameterTypeOptionalCustomModifiers = new Type[arraySize][];
            for (int i = 0; i < arraySize; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                desiredParamType,
                parameterTypeRequiredCustomModifiers,
                parameterTypeOptionalCustomModifiers);
        }

        [Fact]
        public void TestWithNullOnReturnTypeModifiers()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];
            Type[] desiredParamType = new Type[] { typeof(void) };
            Type[][] parameterTypeRequiredCustomModifiers = new Type[desiredParamType.Length][];
            Type[][] parameterTypeOptionalCustomModifiers = new Type[desiredParamType.Length][];
            for (int i = 0; i < desiredParamType.Length; ++i)
            {
                parameterTypeRequiredCustomModifiers[i] = null;
                parameterTypeOptionalCustomModifiers[i] = null;
            }

            builder.SetSignature(
                desiredReturnType.AsType(),
                null,
                null,
                desiredParamType,
                parameterTypeRequiredCustomModifiers,
                parameterTypeOptionalCustomModifiers);
        }

        private void VerifyMethodSignature(TypeBuilder typeBuilder, MethodBuilder builder, Type desiredReturnType)
        {
            Type ret = typeBuilder.CreateTypeInfo().AsType();
            MethodInfo methodInfo = builder.GetBaseDefinition();
            Type actualReturnType = methodInfo.ReturnType;

            if (desiredReturnType == null)
                Assert.Null(actualReturnType);
            if (desiredReturnType != null)
            {
                Assert.NotNull(actualReturnType);
                Assert.Equal(desiredReturnType.Name, actualReturnType.Name);
                Assert.True(actualReturnType.Equals(desiredReturnType));
            }
        }
    }
}
