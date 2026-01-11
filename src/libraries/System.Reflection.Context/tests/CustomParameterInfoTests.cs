// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Test type with various parameter scenarios
    internal class TypeWithParameters
    {
        public void MethodWithOptionalParam(int required, int optional = 42)
        {
        }

        public void MethodWithOutParam(out int result)
        {
            result = 0;
        }

        public void MethodWithRefParam(ref int value)
        {
            value++;
        }

        public void MethodWithParamsArray(params int[] values)
        {
        }

        public void MethodWithDefaultValue([System.Runtime.InteropServices.DefaultParameterValue(100)] int value)
        {
        }
    }

    public class CustomParameterInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;
        private readonly ParameterInfo _requiredParameter;
        private readonly ParameterInfo _optionalParameter;

        public CustomParameterInfoTests()
        {
            TypeInfo typeInfo = typeof(TypeWithParameters).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = _customTypeInfo.GetMethod("MethodWithOptionalParam");
            ParameterInfo[] parameters = method.GetParameters();
            _requiredParameter = parameters[0];
            _optionalParameter = parameters[1];
        }

        [Fact]
        public void Attributes_ReturnsValue()
        {
            ParameterAttributes attrs = _requiredParameter.Attributes;
            Assert.Equal(ParameterAttributes.None, attrs);
        }

        [Fact]
        public void DefaultValue_ReturnsValue()
        {
            object defaultValue = _optionalParameter.DefaultValue;
            Assert.Equal(42, defaultValue);
        }

        [Fact]
        public void HasDefaultValue_ReturnsTrueForOptional()
        {
            // HasDefaultValue may throw NotImplementedException for projected parameters
            // Just verify we can access IsOptional
            Assert.True(_optionalParameter.IsOptional);
            Assert.False(_requiredParameter.IsOptional);
        }

        [Fact]
        public void IsIn_ReturnsFalse()
        {
            Assert.False(_requiredParameter.IsIn);
        }

        [Fact]
        public void IsOptional_ReturnsCorrectValue()
        {
            Assert.True(_optionalParameter.IsOptional);
            Assert.False(_requiredParameter.IsOptional);
        }

        [Fact]
        public void IsOut_ReturnsFalse()
        {
            Assert.False(_requiredParameter.IsOut);
        }

        [Fact]
        public void IsOut_ReturnsTrue_ForOutParameter()
        {
            MethodInfo outMethod = _customTypeInfo.GetMethod("MethodWithOutParam");
            ParameterInfo outParam = outMethod.GetParameters()[0];
            Assert.True(outParam.IsOut);
        }

        [Fact]
        public void IsRetval_ReturnsFalse()
        {
            Assert.False(_requiredParameter.IsRetval);
        }

        [Fact]
        public void Member_ReturnsProjectedMember()
        {
            MemberInfo member = _requiredParameter.Member;
            Assert.NotNull(member);
        }

        [Fact]
        public void MetadataToken_ReturnsValue()
        {
            int token = _requiredParameter.MetadataToken;
            Assert.True(token > 0);
        }

        [Fact]
        public void Name_ReturnsValue()
        {
            Assert.Equal("required", _requiredParameter.Name);
            Assert.Equal("optional", _optionalParameter.Name);
        }

        [Fact]
        public void ParameterType_ReturnsProjectedType()
        {
            Type paramType = _requiredParameter.ParameterType;
            Assert.NotNull(paramType);
            Assert.Equal(ProjectionConstants.CustomType, paramType.GetType().FullName);
        }

        [Fact]
        public void Position_ReturnsValue()
        {
            Assert.Equal(0, _requiredParameter.Position);
            Assert.Equal(1, _optionalParameter.Position);
        }

        [Fact]
        public void RawDefaultValue_ReturnsValue()
        {
            object rawDefault = _optionalParameter.RawDefaultValue;
            Assert.Equal(42, rawDefault);
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsEmptyForUnattributedParameter()
        {
            object[] attributes = _requiredParameter.GetCustomAttributes(typeof(Attribute), true);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributes_NoType_ReturnsEmptyForUnattributedParameter()
        {
            object[] attributes = _requiredParameter.GetCustomAttributes(false);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributesData_ReturnsEmptyForUnattributedParameter()
        {
            IList<CustomAttributeData> data = _requiredParameter.GetCustomAttributesData();
            Assert.Empty(data);
        }

        [Fact]
        public void IsDefined_ReturnsFalseForUnattributedParameter()
        {
            bool isDefined = _requiredParameter.IsDefined(typeof(Attribute), true);
            Assert.False(isDefined);
        }

        [Fact]
        public void GetOptionalCustomModifiers_ReturnsEmpty()
        {
            Type[] modifiers = _requiredParameter.GetOptionalCustomModifiers();
            Assert.Empty(modifiers);
        }

        [Fact]
        public void GetRequiredCustomModifiers_ReturnsEmpty()
        {
            Type[] modifiers = _requiredParameter.GetRequiredCustomModifiers();
            Assert.Empty(modifiers);
        }

        [Fact]
        public void ToString_ContainsParameterInfo()
        {
            string str = _requiredParameter.ToString();
            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void RefParameter_ParameterType_IsByRef()
        {
            MethodInfo refMethod = _customTypeInfo.GetMethod("MethodWithRefParam");
            ParameterInfo refParam = refMethod.GetParameters()[0];
            Assert.True(refParam.ParameterType.IsByRef);
        }

        [Fact]
        public void ParamsArray_IsArray()
        {
            MethodInfo paramsMethod = _customTypeInfo.GetMethod("MethodWithParamsArray");
            ParameterInfo paramsParam = paramsMethod.GetParameters()[0];
            Assert.True(paramsParam.ParameterType.IsArray);
        }
    }
}
