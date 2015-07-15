// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineParameter1b
    {
        private const int MinStringLength = 1;
        private const int MaxStringLength = 256;

        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const string PosTestDynamicMethodName = "PosDynamicMethod";
        private const string NegTestDynamicMethodName = "NegDynamicMethod";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, TestAssemblyBuilderAccess);

            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
            return moduleBuilder.DefineType(TestDynamicTypeName, TypeAttributes.Abstract);
        }

        [Fact]
        public void TestWithHasDefault()
        {
            string strParamName = null;

            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            Type[] paramTypes = new Type[] { typeof(int) };
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                PosTestDynamicMethodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                ParameterAttributes.HasDefault,
                strParamName);

            Assert.NotNull(paramBuilder);
            VerifyParameterBuilder(paramBuilder, strParamName, ParameterAttributes.HasDefault, 1);
        }

        [Fact]
        public void TestForEveryParameterAtributeFlag()
        {
            string strParamName = null;

            ParameterAttributes[] attributes = new ParameterAttributes[] {
                ParameterAttributes.HasDefault,
                ParameterAttributes.HasFieldMarshal,
                ParameterAttributes.In,
                ParameterAttributes.None,
                ParameterAttributes.Optional,
                ParameterAttributes.Out,
                ParameterAttributes.Retval
            };

            Type[] paramTypes = new Type[attributes.Length];
            for (int i = 0; i < paramTypes.Length; ++i)
            {
                paramTypes[i] = typeof(int);
            }

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                PosTestDynamicMethodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);

            for (int i = 1; i < attributes.Length; ++i)
            {
                strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

                ParameterBuilder paramBuilder = builder.DefineParameter(i, attributes[i], strParamName);

                Assert.NotNull(paramBuilder);
                VerifyParameterBuilder(paramBuilder, strParamName, attributes[i], i);
            }
        }

        [Fact]
        public void TestForCombinationOfParamterAttributeFlags()
        {
            string strParamName = null;

            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            Type[] paramTypes = new Type[] { typeof(int) };
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                PosTestDynamicMethodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);
            ParameterAttributes attribute =
            ParameterAttributes.HasDefault |
            ParameterAttributes.HasFieldMarshal |
            ParameterAttributes.In |
            ParameterAttributes.None |
            ParameterAttributes.Optional |
            ParameterAttributes.Out |
            ParameterAttributes.Retval;

            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                attribute,
                strParamName);

            Assert.NotNull(paramBuilder);
            VerifyParameterBuilder(paramBuilder, strParamName, attribute, 1);
        }

        [Fact]
        public void TestForNegativeFlag()
        {
            string strParamName = null;
            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            Type[] paramTypes = new Type[] { typeof(int) };
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                PosTestDynamicMethodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);
            ParameterBuilder paramBuilder = builder.DefineParameter(
                1,
                (ParameterAttributes)(-1),
                strParamName);
        }



        [Fact]
        public void TestThrowsExceptionWithNoParameter()
        {
            string strParamName = null;
            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                PosTestDynamicMethodName,
                TestMethodAttributes);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ParameterBuilder paramBuilder = builder.DefineParameter(1, ParameterAttributes.HasDefault, strParamName);
            });
        }

        [Fact]
        public void TestThrowsExceptionForNegativePosition()
        {
            string strParamName = null;
            int paramPos = 0;

            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            paramPos = _generator.GetInt32();
            if (paramPos > 0)
            {
                paramPos = 0 - paramPos;
            }

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(
                PosTestDynamicMethodName,
                TestMethodAttributes);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ParameterBuilder paramBuilder = builder.DefineParameter(paramPos, ParameterAttributes.HasDefault, strParamName);
            });
        }

        [Fact]
        public void TestThrowsExceptionForPositionGreaterThanNumberOfParameters()
        {
            string strParamName = null;
            int paramPos = 0;

            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] paramTypes = new Type[] {
                typeof(int)
            };
            while (paramPos < paramTypes.Length)
            {
                paramPos = _generator.GetInt32();
            }

            TypeBuilder typeBuilder = GetTestTypeBuilder();

            MethodBuilder builder = typeBuilder.DefineMethod(
                PosTestDynamicMethodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ParameterBuilder paramBuilder = builder.DefineParameter(paramPos, ParameterAttributes.HasDefault, strParamName);
            });
        }

        [Fact]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            string strParamName = null;

            strParamName = _generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] paramTypes = new Type[] {
                typeof(int)
            };

            TypeBuilder typeBuilder = GetTestTypeBuilder();

            MethodBuilder builder = typeBuilder.DefineMethod(
                PosTestDynamicMethodName,
                TestMethodAttributes,
                typeof(void),
                paramTypes);

            typeBuilder.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() =>
            {
                ParameterBuilder paramBuilder = builder.DefineParameter(1, ParameterAttributes.HasDefault, strParamName);
            });
        }

        private void VerifyParameterBuilder(
            ParameterBuilder actualBuilder,
            string desiredName,
            ParameterAttributes desiredAttributes,
            int desiredPosition)
        {
            const int ReservedMaskParameterAttribute = 0xF000; // This constant maps to ParameterAttributes.ReservedMask that is not available in the contract.
            Assert.Equal(desiredName, actualBuilder.Name);

            int removedReservedAttributes = (int)desiredAttributes & ~ReservedMaskParameterAttribute;
            Assert.Equal((int)removedReservedAttributes, (actualBuilder.Attributes & (int)removedReservedAttributes));
            Assert.Equal(desiredPosition, actualBuilder.Position);
        }
    }
}
