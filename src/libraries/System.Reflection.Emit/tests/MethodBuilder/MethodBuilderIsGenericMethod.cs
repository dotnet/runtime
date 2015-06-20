// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderIsGenericMethod
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;

        private TypeBuilder TestTypeBuilder
        {
            get
            {
                if (null == _testTypeBuilder)
                {
                    AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
                    AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        assemblyName, TestAssemblyBuilderAccess);

                    ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);


                    _testTypeBuilder = moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
                }

                return _testTypeBuilder;
            }
        }

        private TypeBuilder _testTypeBuilder;

        [Fact]
        public void PosTest1()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            Assert.False(builder.IsGenericMethod);
        }

        [Fact]
        public void PosTest2()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);

            Assert.True(builder.IsGenericMethod);
        }

        [Fact]
        public void PosTest3()
        {
            string methodName = null;
            string strParamName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            strParamName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] paramTypes = new Type[] { typeof(int) };
            MethodBuilder builder = TestTypeBuilder.DefineMethod(
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

            Assert.True(builder.IsGenericMethod);
        }

        [Fact]
        public void PosTest4()
        {
            string methodName = null;
            string strParamName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            strParamName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] paramTypes = new Type[] { typeof(int) };
            MethodBuilder builder = TestTypeBuilder.DefineMethod(
                methodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                ParameterAttributes.HasDefault,
                strParamName);

            Assert.False(builder.IsGenericMethod);
        }
    }
}
