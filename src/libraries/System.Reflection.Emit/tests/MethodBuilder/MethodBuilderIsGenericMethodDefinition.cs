// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderIsGenericMethodDefinition
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
        public void TestWithNonGenericMethod()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            Assert.False(builder.IsGenericMethodDefinition);
        }

        [Fact]
        public void TestWithGenericMethod()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);

            Assert.True(builder.IsGenericMethodDefinition);
        }

        [Fact]
        public void TestWithGenericAndNonGenericParameters()
        {
            string methodName = null;
            string strParamName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            strParamName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] paramTypes = new Type[] { typeof(int) };
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                methodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                ParameterAttributes.HasDefault,
                strParamName);

            Assert.True(builder.IsGenericMethodDefinition);
        }

        [Fact]
        public void TestWithNonGenericParameters()
        {
            string methodName = null;
            string strParamName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            strParamName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] paramTypes = new Type[] { typeof(int) };
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                methodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                ParameterAttributes.HasDefault,
                strParamName);

            Assert.False(builder.IsGenericMethodDefinition);
        }
    }
}
