// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ParameterBuilderSetConstant
    {
        public static IEnumerable<object[]> SetConstant_ReferenceTypes_TestData()
        {
            yield return new object[] { typeof(object) };
            yield return new object[] { typeof(string) };
            yield return new object[] { typeof(UserDefinedClass) };
        }

        public static IEnumerable<object[]> SetConstant_NullableValueTypes_TestData()
        {
            yield return new object[] { typeof(bool?) };
            yield return new object[] { typeof(byte?) };
            yield return new object[] { typeof(char?) };
            yield return new object[] { typeof(DateTime?) };
            yield return new object[] { typeof(decimal?) };
            yield return new object[] { typeof(double?) };
            yield return new object[] { typeof(float?) };
            yield return new object[] { typeof(int?) };
            yield return new object[] { typeof(long?) };
            yield return new object[] { typeof(sbyte?) };
            yield return new object[] { typeof(short?) };
            yield return new object[] { typeof(uint?) };
            yield return new object[] { typeof(ulong?) };
            yield return new object[] { typeof(UserDefinedStruct?) };
            yield return new object[] { typeof(ushort?) };
        }

        public static IEnumerable<object[]> SetConstant_ValueTypes_TestData()
        {
            yield return new object[] { typeof(bool) };
            yield return new object[] { typeof(byte) };
            yield return new object[] { typeof(char) };
            yield return new object[] { typeof(DateTime) };
            yield return new object[] { typeof(decimal) };
            yield return new object[] { typeof(double) };
            yield return new object[] { typeof(float) };
            yield return new object[] { typeof(int) };
            yield return new object[] { typeof(long) };
            yield return new object[] { typeof(sbyte) };
            yield return new object[] { typeof(short) };
            yield return new object[] { typeof(uint) };
            yield return new object[] { typeof(ulong) };
            yield return new object[] { typeof(UserDefinedStruct) };
            yield return new object[] { typeof(ushort) };
        }

        [Theory]
        [MemberData(nameof(SetConstant_ReferenceTypes_TestData))]
        [MemberData(nameof(SetConstant_NullableValueTypes_TestData))]
        public void SetConstant_Null_fully_supported(Type parameterType)
        {
            SetConstant_Null(parameterType);
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Passing null for SetConstant on value types not supported on NETFX")]
        [MemberData(nameof(SetConstant_ValueTypes_TestData))]
        public void SetConstant_Null_not_supported_on_NETFX(Type parameterType)
        {
            SetConstant_Null(parameterType);
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Passing non-null value for SetConstant on nullable enum types not supported on NETFX")]
        [InlineData(typeof(AttributeTargets?), AttributeTargets.All, (int)AttributeTargets.All)]
        [InlineData(typeof(AttributeTargets?), (int)AttributeTargets.All, (int)AttributeTargets.All)]
        public void SetConstant_NonNull_on_nullable_enum_not_supported_on_NETFX(Type parameterType, object valueToWrite, object expectedValueWhenRead)
        {
            SetConstant(parameterType, valueToWrite, expectedValueWhenRead);
        }

        private void SetConstant_Null(Type parameterType)
        {
            SetConstant(parameterType, null, null);
        }

        private void SetConstant(Type parameterType, object valueToWrite, object expectedValueWhenRead)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Interface | TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { parameterType });
            ParameterBuilder parameter = method.DefineParameter(1, ParameterAttributes.Optional | ParameterAttributes.HasDefault, "arg");

            parameter.SetConstant(valueToWrite);

            ParameterInfo createdParameter = GetCreatedParameter(type, "TestMethod", 1);
            Assert.Equal(true, createdParameter.HasDefaultValue);
            Assert.Equal(expectedValueWhenRead, createdParameter.DefaultValue);
        }

        private static ParameterInfo GetCreatedParameter(TypeBuilder type, string methodName, int parameterIndex)
        {
            Type createdType = type.CreateTypeInfo().AsType();
            MethodInfo createdMethod = createdType.GetMethod(methodName);
            if (parameterIndex > 0)
            {
                return createdMethod.GetParameters()[parameterIndex - 1];
            }
            else
            {
                return createdMethod.ReturnParameter;
            }
        }

        private class UserDefinedClass
        {
        }

        private struct UserDefinedStruct
        {
        }
    }
}
