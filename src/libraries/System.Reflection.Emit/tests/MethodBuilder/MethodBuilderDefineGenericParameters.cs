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

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        assemblyName, TestAssemblyBuilderAccess);
            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, "Module1");
            return moduleBuilder.DefineType(TestDynamicTypeName);
        }

        [Fact]
        public void TestWithSingleTypeParam()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);

            Assert.True(builder.IsGenericMethod);
            Assert.True(builder.IsGenericMethodDefinition);
        }

        [Fact]
        public void TestWithMultipleTypeParam()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);

            Assert.True(builder.IsGenericMethod);
            Assert.True(builder.IsGenericMethodDefinition);
        }

        [Fact]
        public void TestThrowsExceptionOnSetImplementationFlagsCalledPreviously()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            builder.SetImplementationFlags(MethodImplAttributes.Managed);
            Assert.Throws<InvalidOperationException>(() => { GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters("T"); });
        }

        [Fact]
        public void TestThrowsExceptionOnMethodCompleted()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames);
            Assert.Throws<InvalidOperationException>(() => { builder.DefineGenericParameters(typeParamNames); });
        }

        [Fact]
        public void TestThrowsExceptionForNull()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            Assert.Throws<ArgumentNullException>(() => { GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(null); });
        }

        [Fact]
        public void TestThrowsExceptionForNullMember()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = new string[] { "T", null, "U" };
            Assert.Throws<ArgumentNullException>(() => { GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames); });
        }

        [Fact]
        public void TestThrowsExceptionForEmptyArray()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(PosTestDynamicMethodName,
                MethodAttributes.Public);
            string[] typeParamNames = new string[] { };
            Assert.Throws<ArgumentException>(() => { GenericTypeParameterBuilder[] parameters = builder.DefineGenericParameters(typeParamNames); });
        }
    }
}
