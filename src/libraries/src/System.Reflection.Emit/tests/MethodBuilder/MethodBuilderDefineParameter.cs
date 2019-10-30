// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineParameter
    {
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual;

        [Fact]
        public void DefineParameter_TwoParameters()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] paramTypes = new Type[] { typeof(string), typeof(object) };
            Type returnType = typeof(int);

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, returnType, paramTypes);

            method.DefineParameter(0, ParameterAttributes.None, "returnName");
            method.DefineParameter(1, ParameterAttributes.In, "Param1");
            method.DefineParameter(2, ParameterAttributes.Out, "Param2");

            int expectedReturn = 3;
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldc_I4, expectedReturn);
            ilGenerator.Emit(OpCodes.Ret);

            Type resultType = type.CreateTypeInfo().AsType();

            ParameterInfo[] parameters = method.GetParameters();
            Assert.Equal("System.String Param1", parameters[0].ToString());
            Assert.Equal("System.Object Param2", parameters[1].ToString());

            // Invoke the method to verify it works correctly
            MethodInfo resultMethod = resultType.GetMethod("TestMethod");
            Assert.Equal(expectedReturn, resultMethod.Invoke(null, new object[] { "hello", new object() }));
        }

        [Fact]
        public void DefineParameter_HasDefaultAttribute()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            Type[] paramTypes = new Type[] { typeof(int) };
            MethodBuilder method = type.DefineMethod("TestMethod", TestMethodAttributes, typeof(void), paramTypes);

            ParameterBuilder parameter = method.DefineParameter(1, ParameterAttributes.HasDefault, "TestParam");
            VerifyParameterBuilder(parameter, "TestParam", ParameterAttributes.HasDefault, 1);
        }

        [Fact]
        public void DefineParameter_AllAttributes()
        {
            ParameterAttributes[] attributes = new ParameterAttributes[]
            {
                ParameterAttributes.HasDefault,
                ParameterAttributes.HasFieldMarshal,
                ParameterAttributes.In,
                ParameterAttributes.None,
                ParameterAttributes.Optional,
                ParameterAttributes.Out,
                ParameterAttributes.Retval
            };

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            Type[] paramTypes = Enumerable.Repeat(typeof(int), attributes.Length).ToArray();
            MethodBuilder method = type.DefineMethod("TestMethod", TestMethodAttributes, typeof(void), paramTypes);

            for (int i = 1; i < attributes.Length; ++i)
            {
                ParameterBuilder parameter = method.DefineParameter(i, attributes[i], "TestParam");
                VerifyParameterBuilder(parameter, "TestParam", attributes[i], i);
            }
        }

        [Fact]
        public void DefineParameter_AllAttributesCombined()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            Type[] paramTypes = new Type[] { typeof(int) };
            MethodBuilder method = type.DefineMethod("TestMethod", TestMethodAttributes, typeof(void), paramTypes);

            ParameterAttributes attribute =
            ParameterAttributes.HasDefault |
            ParameterAttributes.HasFieldMarshal |
            ParameterAttributes.In |
            ParameterAttributes.None |
            ParameterAttributes.Optional |
            ParameterAttributes.Out |
            ParameterAttributes.Retval;
            ParameterBuilder parameter = method.DefineParameter(1, attribute, "TestParam");
            VerifyParameterBuilder(parameter, "TestParam", attribute, 1);
        }

        [Fact]
        public void DefineParameter_NegativeAttribute()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            Type[] paramTypes = new Type[] { typeof(int) };
            MethodBuilder method = type.DefineMethod("TestMethod", TestMethodAttributes, typeof(void), paramTypes);
            ParameterBuilder parameter = method.DefineParameter(1, (ParameterAttributes)(-1), "TestParam");
        }

        [Fact]
        public void DefineParameter_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type returnType = typeof(int);
            Type[] paramTypes = new Type[] { typeof(string), typeof(object) };

            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public, returnType, paramTypes);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            Type resultType = type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => method.DefineParameter(1, ParameterAttributes.Retval, "param1"));
        }

        [Fact]
        public void DefineParameter_SingleParameter_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder builder = type.DefineMethod("TestMethod", TestMethodAttributes, typeof(void), new Type[] { typeof(int) });

            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => builder.DefineParameter(1, ParameterAttributes.HasDefault, "TestParam"));
        }

        [Theory]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public, ParameterAttributes.None, new Type[] { typeof(string), typeof(object) }, typeof(int))]
        [InlineData(TypeAttributes.Abstract, MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, ParameterAttributes.HasDefault, new Type[] { typeof(int) }, typeof(void))]
        public void DefineParameter_InvalidPosition_ThrowsArgumentOutOfRangeException(TypeAttributes typeAttributes, MethodAttributes methodAttributes, ParameterAttributes parameterAttributes, Type[] paramTypes, Type returnType)
        {
            TypeBuilder type = Helpers.DynamicType(typeAttributes);
            MethodBuilder method = type.DefineMethod("TestMethod", methodAttributes, returnType, paramTypes);

            Assert.Throws<ArgumentOutOfRangeException>(() => method.DefineParameter(-1, parameterAttributes, "Param1"));
            Assert.Throws<ArgumentOutOfRangeException>(() => method.DefineParameter(paramTypes.Length + 1, parameterAttributes, "Param1"));
        }

        [Theory]
        [InlineData(TypeAttributes.Public, MethodAttributes.Public, ParameterAttributes.None)]
        [InlineData(TypeAttributes.Abstract, MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, ParameterAttributes.HasDefault)]
        public void DefineParameter_NoParameters_NonZeroPosition_ThrowsArgumentOutOfRangeException(TypeAttributes typeAttributes, MethodAttributes methodAttributes, ParameterAttributes parameterAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(typeAttributes);
            MethodBuilder method = type.DefineMethod("TestMethod", methodAttributes);

            Assert.Throws<ArgumentOutOfRangeException>(() => method.DefineParameter(1, parameterAttributes, "Param1"));
            Assert.Throws<ArgumentOutOfRangeException>(() => method.DefineParameter(-1, parameterAttributes, "Param1"));
        }

        private static void VerifyParameterBuilder(ParameterBuilder parameter, string expectedName, ParameterAttributes expectedAttributes, int expectedPosition)
        {
            // This constant maps to ParameterAttributes.ReservedMask that is not available in the contract.
            const int ReservedMaskParameterAttribute = 0xF000;
            Assert.Equal(expectedName, parameter.Name);

            int removedReservedAttributes = (int)expectedAttributes & ~ReservedMaskParameterAttribute;
            Assert.Equal(removedReservedAttributes, (parameter.Attributes & removedReservedAttributes));
            Assert.Equal(expectedPosition, parameter.Position);
        }
    }
}
