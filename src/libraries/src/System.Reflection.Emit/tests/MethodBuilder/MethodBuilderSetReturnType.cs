// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderSetReturnType
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, TestAssemblyBuilderAccess);

            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
            return moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
        }

        [Fact]
        public void TestWithSingleGenericParameter()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[0].AsType();

            builder.SetReturnType(desiredReturnType);

            typeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void TestWithMultipleGenericParameters()
        {
            var methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[1].AsType();

            builder.SetReturnType(desiredReturnType);

            typeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void TestWithGenericAndNonGenericParameters()
        {
            string methodName = null;

            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeof(void);

            builder.SetReturnType(desiredReturnType);

            typeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void TestNotSetReturnTypeOnNonGenericMethod()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] parameterTypes = new Type[] { typeof(int) };

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(int),
                parameterTypes);

            Type desiredReturnType = typeof(void);

            builder.SetReturnType(desiredReturnType);

            typeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void TestOverwriteGenericReturnTypeWithNonGenericType()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeof(void);

            builder.SetReturnType(typeParameters[0].AsType());
            builder.SetReturnType(desiredReturnType);

            typeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void TestOverwriteGenericReturnTypeWithGenericType()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[1].AsType();

            builder.SetReturnType(typeParameters[0].AsType());
            builder.SetReturnType(desiredReturnType);

            typeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void TestOverwriteNonGenericReturnTypeWithGenericType()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] parameterTypes = new Type[] { typeof(int) };

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(int),
                parameterTypes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[0].AsType();

            builder.SetReturnType(desiredReturnType);

            typeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void TestAfterTypeCreated()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] parameterTypes = new Type[] { typeof(int) };

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(int),
                parameterTypes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[0].AsType();

            typeBuilder.CreateTypeInfo().AsType();

            builder.SetReturnType(desiredReturnType);
        }

        [Fact]
        public void TestNotThrowsOnNull()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            builder.SetReturnType(null);
        }

        private void VerifyReturnType(MethodBuilder builder, Type desiredReturnType)
        {
            MethodInfo methodInfo = builder.GetBaseDefinition();
            Type actualReturnType = methodInfo.ReturnType;

            Assert.Equal(desiredReturnType.Name, actualReturnType.Name);
            Assert.True(actualReturnType.Equals(desiredReturnType));
        }
    }
}
