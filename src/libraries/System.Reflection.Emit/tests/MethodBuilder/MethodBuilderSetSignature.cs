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
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            builder.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest2()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[1];

            builder.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest3()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParameters =
                builder.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder desiredReturnType = typeParameters[0];

            builder.SetSignature(desiredReturnType.AsType(), null, null, null, null, null);

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest4()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest5()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest6()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest7()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest8()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest9()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest10()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

            VerifyMethodSignature(builder, desiredReturnType.AsType());
        }

        [Fact]
        public void PosTest11()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] parameterTypes = new Type[] { typeof(string), typeof(object) };
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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
        public void NegTest1()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            builder.SetSignature(null, null, null, null, null, null);

            VerifyMethodSignature(builder, null);
        }

        [Fact]
        public void NegTest2()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

            VerifyMethodSignature(builder, null);
        }

        [Fact]
        public void NegTest3()
        {
            string methodName = null;
            int arraySize = 0;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            arraySize = TestLibrary.Generator.GetByte();

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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
        public void NegTest4()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

        [Fact]
        public void NegTest5()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
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

        private void VerifyMethodSignature(MethodBuilder builder, Type desiredReturnType)
        {
            Type ret = TestTypeBuilder.CreateTypeInfo().AsType();
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
