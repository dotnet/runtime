// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetGenericArguments
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
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
        public void TestWithNonGenericMethod()
        {
            string methodName = null;

            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            Assert.Null(builder.GetGenericArguments());
        }

        [Fact]
        public void TestWithGenericMethod()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] desiredParameters = builder.DefineGenericParameters(typeParamNames);

            Type[] actualParameters = builder.GetGenericArguments();
            VerificationHelper(desiredParameters, actualParameters);
        }

        [Fact]
        public void TestWithSingleParameter()
        {
            string methodName = null;
            string paramName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            paramName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            Type[] paramTypes = new Type[] { typeof(int) };
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                methodName,
                MethodAttributes.Public,
                typeof(void),
                paramTypes);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] desiredParameters = builder.DefineGenericParameters(typeParamNames);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                ParameterAttributes.HasDefault,
                paramName);

            Type[] actualParameters = builder.GetGenericArguments();
            VerificationHelper(desiredParameters, actualParameters);
        }

        [Fact]
        public void TestWithGenericReturnType()
        {
            string methodName = null;
            methodName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                methodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] desiredParameters = builder.DefineGenericParameters(typeParamNames);

            builder.SetReturnType(desiredParameters[0].AsType());

            Type[] actualParameters = builder.GetGenericArguments();
            VerificationHelper(desiredParameters, actualParameters);
        }

        private void VerificationHelper(GenericTypeParameterBuilder[] desiredParameters, Type[] actualParameters)
        {
            if (null == desiredParameters)
                Assert.Null(actualParameters);
            if (null != desiredParameters)
            {
                Assert.NotNull(actualParameters);
                Assert.Equal(desiredParameters.Length, actualParameters.Length);
                for (int i = 0; i < actualParameters.Length; ++i)
                {
                    Assert.True(desiredParameters[i].Equals(actualParameters[i]));
                }
            }
        }
    }
}
