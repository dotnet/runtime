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
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[0].AsType();

            builder.SetReturnType(desiredReturnType);

            TestTypeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void PosTest2()
        {
            _testTypeBuilder = null;
            var methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[1].AsType();

            builder.SetReturnType(desiredReturnType);

            TestTypeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void PosTest3()
        {
            string methodName = null;

            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeof(void);

            builder.SetReturnType(desiredReturnType);

            TestTypeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void PosTest4()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] parameterTypes = new Type[] { typeof(int) };

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(int),
                parameterTypes);

            Type desiredReturnType = typeof(void);

            builder.SetReturnType(desiredReturnType);

            TestTypeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void PosTest5()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeof(void);

            builder.SetReturnType(typeParameters[0].AsType());
            builder.SetReturnType(desiredReturnType);

            TestTypeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void PosTest6()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[1].AsType();

            builder.SetReturnType(typeParameters[0].AsType());
            builder.SetReturnType(desiredReturnType);

            TestTypeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void PosTest7()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] parameterTypes = new Type[] { typeof(int) };

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(int),
                parameterTypes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[0].AsType();

            builder.SetReturnType(desiredReturnType);

            TestTypeBuilder.CreateTypeInfo().AsType();

            VerifyReturnType(builder, desiredReturnType);
        }

        [Fact]
        public void PosTest8()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] parameterTypes = new Type[] { typeof(int) };

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(int),
                parameterTypes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            Type desiredReturnType = typeParameters[0].AsType();

            TestTypeBuilder.CreateTypeInfo().AsType();

            builder.SetReturnType(desiredReturnType);
        }

        [Fact]
        public void NegTest1()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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
