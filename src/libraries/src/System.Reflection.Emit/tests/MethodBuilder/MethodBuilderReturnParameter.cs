// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderReturnParameter
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const string TestDynamicMethodName = "TestDynamicMethod";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();
        private readonly byte[] _defaultILArray = new byte[]  {
            0x00,
            0x72,
            0x01,
            0x00,
            0x00,
            0x70,
            0x28,
            0x04,
            0x00,
            0x00,
            0x0a,
            0x00,
            0x2a
        };

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, TestAssemblyBuilderAccess);

            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
            return moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
        }

        [Fact]
        public void TestWithNotSetReturnType()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                TestDynamicMethodName,
                MethodAttributes.Public);

            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            Type ret = typeBuilder.CreateTypeInfo().AsType();
            ParameterInfo actualReturnParameter = builder.ReturnParameter;

            Assert.True(actualReturnParameter.ParameterType.Equals(typeof(void)));
        }

        [Fact]
        public void TestWithVoidReturnType()
        {
            string strParamName = null;
            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            Type[] paramTypes = new Type[] { typeof(int) };
            Type expectedReturnType = typeof(void);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                TestDynamicMethodName,
                MethodAttributes.Public,
                expectedReturnType,
                paramTypes);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                ParameterAttributes.HasDefault,
                strParamName);

            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            Type ret = typeBuilder.CreateTypeInfo().AsType();
            ParameterInfo actualReturnParameter = builder.ReturnParameter;

            Assert.True(actualReturnParameter.ParameterType.Equals(expectedReturnType));
        }

        [Fact]
        public void TestWithValueTypeReturnType()
        {
            string strParamName = null;
            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            Type[] paramTypes = new Type[] { typeof(int) };
            Type expectedReturnType = typeof(int);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                TestDynamicMethodName,
                MethodAttributes.Public,
                expectedReturnType,
                paramTypes);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                ParameterAttributes.HasDefault,
                strParamName);

            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            Type ret = typeBuilder.CreateTypeInfo().AsType();
            ParameterInfo actualReturnParameter = builder.ReturnParameter;

            Assert.True(actualReturnParameter.ParameterType.Equals(expectedReturnType));
        }

        [Fact]
        public void TestWithReferenceTypeReturnType()
        {
            string strParamName = null;
            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            Type[] paramTypes = new Type[] { typeof(int) };
            Type expectedReturnType = typeof(string);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                TestDynamicMethodName,
                MethodAttributes.Public,
                expectedReturnType,
                paramTypes);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                ParameterAttributes.HasDefault,
                strParamName);

            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            Type ret = typeBuilder.CreateTypeInfo().AsType();
            ParameterInfo actualReturnParameter = builder.ReturnParameter;

            Assert.True(actualReturnParameter.ParameterType.Equals(expectedReturnType));
        }

        [Fact]
        public void TestThrowsExceptionForDeclaringTypeNotCreated()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                TestDynamicMethodName,
                MethodAttributes.Public);

            Assert.Throws<InvalidOperationException>(() => { ParameterInfo actualReturnParameter = builder.ReturnParameter; });
        }

        [Fact]
        public void TestThrowsExceptionWithNoMethodBodyDefined()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                TestDynamicMethodName,
                MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual);

            Type ret = typeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { ParameterInfo actualReturnParameter = builder.ReturnParameter; });
        }
    }
}
