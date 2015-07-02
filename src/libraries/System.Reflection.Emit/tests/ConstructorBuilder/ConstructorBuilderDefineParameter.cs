// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderDefineParameter
    {
        private const string AssemblyName = "ConstructorBuilderDefineParameter";
        private const string DefaultModuleName = "DynamicModule";
        private const string DefaultTypeName = "DynamicType";
        private const AssemblyBuilderAccess DefaultAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const MethodAttributes DefaultMethodAttribute = MethodAttributes.Public;
        private const CallingConventions DefaultCallingConvention = CallingConventions.Standard;

        private TypeBuilder _typeBuilder;

        private ParameterAttributes[] _supportedAttributes = new ParameterAttributes[] {
                ParameterAttributes.None,
                ParameterAttributes.HasDefault,
                ParameterAttributes.HasFieldMarshal,
                ParameterAttributes.In,
                ParameterAttributes.None,
                ParameterAttributes.Optional,
                ParameterAttributes.Out,
                ParameterAttributes.Retval };
        private ModuleBuilder _testModuleBuilder;

        private ModuleBuilder TestModuleBuilder
        {
            get
            {
                AssemblyName name = new AssemblyName(AssemblyName);
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, DefaultAssemblyBuilderAccess);
                _testModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(assembly, "Module1");

                return _testModuleBuilder;
            }
        }

        [Fact]
        public void TestValidDataWithMultipleParameters()
        {
            int i = 0;
            Type[] parameterTypes = new Type[]
            {
                typeof(int),
                typeof(string)
            };

            for (i = 1; i < _supportedAttributes.Length; ++i)
            {
                ConstructorBuilder constructor = CreateConstructorBuilder("PosTest1_Type" + i,
                    parameterTypes);

                constructor.DefineParameter(1, _supportedAttributes[i - 1], "parameter1" + i);
                constructor.DefineParameter(2, _supportedAttributes[i], "parameter2" + i);

                ILGenerator ilg = constructor.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[] { }));
                ilg.Emit(OpCodes.Ret);

                _typeBuilder.CreateTypeInfo().AsType();
                ParameterInfo[] definedParams = constructor.GetParameters();

                Assert.Equal(2, definedParams.Length);
            }
        }

        [Fact]
        public void TestValidDataWithSingleParameter()
        {
            int i = 0;

            Type[] parameterTypes = new Type[]
            {
                typeof(ConstructorBuilderDefineParameter)
            };

            for (; i < _supportedAttributes.Length; ++i)
            {
                ConstructorBuilder constructor = CreateConstructorBuilder("PosTest2_Type" + i,
                    parameterTypes);

                constructor.DefineParameter(1, _supportedAttributes[i], "parameter1" + i);
                ILGenerator ilg = constructor.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[] { }));
                ilg.Emit(OpCodes.Ret);

                _typeBuilder.CreateTypeInfo().AsType();
                ParameterInfo[] definedParams = constructor.GetParameters();

                Assert.Equal(1, definedParams.Length);
            }
        }

        [Fact]
        public void TestValidDataWithNullName()
        {
            int i = 0;

            Type[] parameterTypes = new Type[]
            {
                typeof(ConstructorBuilderDefineParameter)
            };

            for (; i < _supportedAttributes.Length; ++i)
            {
                ConstructorBuilder constructor = CreateConstructorBuilder("PosTest3_Type" + i,
                    parameterTypes);

                constructor.DefineParameter(1, _supportedAttributes[i], null);
                ILGenerator ilg = constructor.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[] { }));
                ilg.Emit(OpCodes.Ret);

                _typeBuilder.CreateTypeInfo().AsType();
                ParameterInfo[] definedParams = constructor.GetParameters();
                Assert.Equal(1, definedParams.Length);
            }
        }

        [Fact]
        public void TestValidDataWithMultipleParametersAndNullName()
        {
            int i = 0;

            Type[] parameterTypes = new Type[]
            {
                typeof(int),
                typeof(string)
            };

            for (i = 1; i < _supportedAttributes.Length; ++i)
            {
                ConstructorBuilder constructor = CreateConstructorBuilder("PosTest4_Type" + i,
                    parameterTypes);

                constructor.DefineParameter(1, _supportedAttributes[i - 1], null);
                constructor.DefineParameter(2, _supportedAttributes[i], null);
                ILGenerator ilg = constructor.GetILGenerator();
                ilg.Emit(OpCodes.Ldarg_0);
                ilg.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[] { }));
                ilg.Emit(OpCodes.Ret);

                _typeBuilder.CreateTypeInfo().AsType();
                ParameterInfo[] definedParams = constructor.GetParameters();
                Assert.Equal(2, definedParams.Length);
            }
        }

        [Fact]
        public void TestParameterWithZeroSequence()
        {
            CreateConstructorBuilder("PosTest5_Type1", new Type[] { }).DefineParameter(0, ParameterAttributes.None, "p");
            CreateConstructorBuilder("PosTest5_Type5", new Type[] { typeof(int) }).DefineParameter(0, ParameterAttributes.None, "p");
        }

        [Fact]
        public void TestThrowsExceptionOnIncorrectPosition()
        {
            // ArgumentOutOfRangeException should be thrown when position is less than or equal to zero, or it is greater than the number of parameters of the constructor.
            NegTestCaseVerificationHelper(CreateConstructorBuilder("NegTest1_Type2", new Type[] { }),
                -1, ParameterAttributes.None, "p", typeof(ArgumentOutOfRangeException));
            NegTestCaseVerificationHelper(CreateConstructorBuilder("NegTest1_Type3", new Type[] { }),
                1, ParameterAttributes.None, "p", typeof(ArgumentOutOfRangeException));
            NegTestCaseVerificationHelper(CreateConstructorBuilder("NegTest1_Type4", new Type[] { typeof(int) }),
                2, ParameterAttributes.None, "p", typeof(ArgumentOutOfRangeException));
        }

        [Fact]
        public void TestThrowsExceptionOnCreateTypeCalled()
        {
            ConstructorBuilder constructor = CreateConstructorBuilder("NegTest2_Type1", new Type[] { typeof(int) });
            constructor.GetILGenerator().Emit(OpCodes.Ret);

            TypeBuilder type = _typeBuilder;
            type.CreateTypeInfo().AsType();

            NegTestCaseVerificationHelper(constructor,
                1, ParameterAttributes.None, "p", typeof(InvalidOperationException));
        }

        private ConstructorBuilder CreateConstructorBuilder(string typeName, Type[] parameterTypes)
        {
            _typeBuilder = TestModuleBuilder.DefineType(typeName);

            return _typeBuilder.DefineConstructor(
                DefaultMethodAttribute,
                DefaultCallingConvention,
                parameterTypes);
        }

        private void NegTestCaseVerificationHelper(
            ConstructorBuilder constructor,
            int sequence,
            ParameterAttributes attribute,
            string paramName,
            Type desiredException)
        {
            Assert.Throws(desiredException, () => { constructor.DefineParameter(sequence, attribute, paramName); });
        }
    }
}
