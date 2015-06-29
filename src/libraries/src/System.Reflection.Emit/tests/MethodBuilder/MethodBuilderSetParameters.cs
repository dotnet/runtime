// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderSetParameters
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;
        private readonly byte[] _defaultILArray = new byte[]
        {
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

            methodName = "PosTest1";
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T" };
            Type[] typeParameters =
                builder.DefineGenericParameters(typeParamNames).Select(a => a.AsType()).ToArray();

            builder.SetParameters(typeParameters);
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            MethodInfo method = type.GetMethod(builder.Name);
            ParameterInfo[] parameters = method.GetParameters();
            VerificationHelper(parameters, typeParameters, typeParamNames);
        }

        [Fact]
        public void PosTest2()
        {
            string methodName = null;

            methodName = "PosTest2";
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            string[] typeParamNames = { "T", "U" };
            Type[] typeParameters =
                builder.DefineGenericParameters(typeParamNames).Select(a => a.AsType()).ToArray();

            builder.SetParameters(typeParameters);
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            MethodInfo method = type.GetMethod(builder.Name);
            ParameterInfo[] parameters = method.GetParameters();
            VerificationHelper(parameters, typeParameters, typeParamNames);
        }

        [Fact]
        public void PosTest3()
        {
            string methodName = null;

            methodName = "PosTest3";
            Type[] parameterTypes = new Type[] { typeof(int) };
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(void),
                parameterTypes);

            string[] typeParamNames = { "T" };
            Type[] typeParameters =
                builder.DefineGenericParameters(typeParamNames).Select(a => a.AsType()).ToArray();

            builder.SetParameters(typeParameters);
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            MethodInfo method = type.GetMethod(builder.Name);
            ParameterInfo[] parameters = method.GetParameters();
            VerificationHelper(parameters, typeParameters, typeParamNames);
        }

        [Fact]
        public void PosTest4()
        {
            string methodName = null;
            _testTypeBuilder = null;
            methodName = "PosTest4";

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);
            string[] typeParamNames = { "T" };
            Type[] typeParameters =
                builder.DefineGenericParameters(typeParamNames).Select(a => a.AsType()).ToArray();

            builder.SetParameters(typeParameters);
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            builder.SetParameters(typeParameters);
        }

        [Fact]
        public void NegTest1()
        {
            string methodName = null;
            methodName = "NegTest1";
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            builder.SetParameters(null);
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            MethodInfo method = type.GetMethod(builder.Name);
            ParameterInfo[] parameters = method.GetParameters();
            VerificationHelper(parameters, new Type[] { }, null);
        }

        [Fact]
        public void NegTest2()
        {
            string methodName = null;
            methodName = "NegTest2";
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            builder.SetParameters(new Type[] { });
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            MethodInfo method = type.GetMethod(builder.Name);
            ParameterInfo[] parameters = method.GetParameters();
            VerificationHelper(parameters, new Type[] { }, null);
        }

        [Fact]
        public void NegTest3()
        {
            string methodName = null;
            methodName = "NegTest3";
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes);

            builder.SetParameters();
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            MethodInfo method = type.GetMethod(builder.Name);
            ParameterInfo[] parameters = method.GetParameters();
            VerificationHelper(parameters, new Type[] { }, null);
        }

        [Fact]
        public void NegTest4()
        {
            string methodName = null;
            methodName = "NegTest4";
            Type[] parameterTypes = new Type[] { typeof(int) };

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(void),
                parameterTypes);
            string[] typeParamNames = { "T" };
            Type[] typeParameters =
                builder.DefineGenericParameters(typeParamNames).Select(a => a.AsType()).ToArray();

            builder.SetParameters(typeParameters);
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type type = TestTypeBuilder.CreateTypeInfo().AsType();

            MethodInfo method = type.GetMethod(builder.Name);
            ParameterInfo[] parameters = method.GetParameters();
            VerificationHelper(parameters, typeParameters, typeParamNames);
        }

        [Fact]
        public void NegTest6()
        {
            string methodName = null;

            methodName = "NegTest6";
            Type[] parameterTypes = new Type[] { typeof(int) };

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                TestMethodAttributes,
                typeof(void),
                parameterTypes);
            string[] typeParamNames = { "T" };
            Type[] typeParameters =
                builder.DefineGenericParameters(typeParamNames).Select(a => a.AsType()).ToArray();

            Type[] desiredParameters = new Type[typeParameters.Length + 1];
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                desiredParameters[i] = typeParameters[i];
            }
            desiredParameters[typeParameters.Length] = null;

            builder.SetParameters(desiredParameters);
            ILGenerator ilgen = builder.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Assert.Throws<ArgumentNullException>(() => { Type type = TestTypeBuilder.CreateTypeInfo().AsType(); });
        }

        private void VerificationHelper(ParameterInfo[] parameters, Type[] parameterTypes, string[] parameterName)
        {
            if (parameterTypes == null)
                Assert.Null(parameters);
            if (parameterTypes != null)
            {
                Assert.NotNull(parameters);
                Assert.Equal(parameterTypes.Length, parameters.Length);
                for (int i = 0; i < parameters.Length; ++i)
                {
                    ParameterInfo parameter = parameters[i];
                    if (null != parameter.Name)
                    {
                        Assert.Equal(parameterName[i], parameter.Name);
                    }
                    else
                    {
                        Assert.True(parameter.ParameterType.Name.Equals(parameterName[i]));
                    }

                    Assert.Equal(i, parameter.Position);
                }
            }
        }
    }
}
