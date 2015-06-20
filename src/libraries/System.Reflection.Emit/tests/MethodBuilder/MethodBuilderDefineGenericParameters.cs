// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineGenericParameters
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const string PosTestDynamicMethodName = "PosDynamicMethod";
        private const string NegTestDynamicMethodName = "NegDynamicMethod";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;

        private ModuleBuilder modHelper(AssemblyBuilder asmbuild)
        {
            return TestLibrary.Utilities.GetModuleBuilder(asmbuild, TestDynamicModuleName);
        }

        private TypeBuilder TestTypeBuilder
        {
            get
            {
                if (null == _testTypeBuilder)
                {
                    AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
                    AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        assemblyName, TestAssemblyBuilderAccess);
                    ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, "Module1");
                    _testTypeBuilder = moduleBuilder.DefineType(TestDynamicTypeName);
                }

                return _testTypeBuilder;
            }
        }

        private TypeBuilder _testTypeBuilder;

        [Fact]
        public void PosTest1()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);

            Assert.True(builder.IsGenericMethod);
            Assert.True(builder.IsGenericMethodDefinition);
        }

        [Fact]
        public void PosTest2()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);

            Assert.True(builder.IsGenericMethod);
            Assert.True(builder.IsGenericMethodDefinition);
        }

        [Fact]
        public void NegTest1()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            builder.SetImplementationFlags(MethodImplAttributes.Managed);
            Assert.Throws<InvalidOperationException>(() => { GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters("T"); });
        }

        [Fact]
        public void NegTest2()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);
            Assert.Throws<InvalidOperationException>(() => { builder.DefineGenericParameters(typeParamNames); });
        }

        [Fact]
        public void NegTest3()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            Assert.Throws<ArgumentNullException>(() => { GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(null); });
        }

        [Fact]
        public void NegTest4()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = new string[] { "T", null, "U" };
            Assert.Throws<ArgumentNullException>(() => { GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames); });
        }

        [Fact]
        public void NegTest5()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = new string[] { };
            Assert.Throws<ArgumentException>(() => { GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames); });
        }
    }
}
