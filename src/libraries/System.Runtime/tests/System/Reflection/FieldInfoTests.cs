// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public class FieldInfoTests
    {
        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.Assembly, true)]
        [InlineData(FieldAttributes.Assembly | FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.Family, false)]
        [InlineData(FieldAttributes.FamANDAssem, false)]
        [InlineData(FieldAttributes.FamORAssem, false)]
        [InlineData(FieldAttributes.Private, false)]
        [InlineData(FieldAttributes.Public, false)]
        [InlineData(FieldAttributes.InitOnly, false)]
        public void IsAssembly_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsAssembly);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.Family, true)]
        [InlineData(FieldAttributes.Family | FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.Assembly, false)]
        [InlineData(FieldAttributes.FamANDAssem, false)]
        [InlineData(FieldAttributes.FamORAssem, false)]
        [InlineData(FieldAttributes.Private, false)]
        [InlineData(FieldAttributes.Public, false)]
        [InlineData(FieldAttributes.InitOnly, false)]
        public void IsFamily_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsFamily);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.FamANDAssem, true)]
        [InlineData(FieldAttributes.FamANDAssem | FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.Family, false)]
        [InlineData(FieldAttributes.Assembly, false)]
        [InlineData(FieldAttributes.FamORAssem, false)]
        [InlineData(FieldAttributes.Private, false)]
        [InlineData(FieldAttributes.Public, false)]
        [InlineData(FieldAttributes.InitOnly, false)]
        public void IsFamilyAndAssembly_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsFamilyAndAssembly);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.FamORAssem, true)]
        [InlineData(FieldAttributes.FamORAssem | FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.Family, false)]
        [InlineData(FieldAttributes.FamANDAssem, false)]
        [InlineData(FieldAttributes.Assembly, false)]
        [InlineData(FieldAttributes.Private, false)]
        [InlineData(FieldAttributes.Public, false)]
        [InlineData(FieldAttributes.InitOnly, false)]
        public void IsFamilyOrAssembly_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsFamilyOrAssembly);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.InitOnly | FieldAttributes.Literal, true)]
        [InlineData(FieldAttributes.Literal, false)]
        public void IsInitOnly_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsInitOnly);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.Literal, true)]
        [InlineData(FieldAttributes.Literal | FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.InitOnly, false)]
        public void IsLiteral_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsLiteral);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.NotSerialized, true)]
        [InlineData(FieldAttributes.NotSerialized | FieldAttributes.Literal, true)]
        [InlineData(FieldAttributes.Literal, false)]
        public void IsNotSerialized_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsNotSerialized);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.PinvokeImpl, true)]
        [InlineData(FieldAttributes.PinvokeImpl | FieldAttributes.Literal, true)]
        [InlineData(FieldAttributes.Literal, false)]
        public void IsPinvokeImpl_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsPinvokeImpl);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.Private, true)]
        [InlineData(FieldAttributes.Private | FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.Family, false)]
        [InlineData(FieldAttributes.FamANDAssem, false)]
        [InlineData(FieldAttributes.FamORAssem, false)]
        [InlineData(FieldAttributes.Assembly, false)]
        [InlineData(FieldAttributes.Public, false)]
        [InlineData(FieldAttributes.InitOnly, false)]
        public void IsPrivate_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsPrivate);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.Public, true)]
        [InlineData(FieldAttributes.Public | FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.Family, false)]
        [InlineData(FieldAttributes.FamANDAssem, false)]
        [InlineData(FieldAttributes.FamORAssem, false)]
        [InlineData(FieldAttributes.Assembly, false)]
        [InlineData(FieldAttributes.Private, false)]
        [InlineData(FieldAttributes.InitOnly, false)]
        public void IsPublic_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsPublic);
        }

        [Fact]
        public void IsSecurityCritical_Get_ReturnsExpected()
        {
            var field = new SubFieldInfo();
            Assert.True(field.IsSecurityCritical);
        }

        [Fact]
        public void IsSecuritySafeCritical_Get_ReturnsExpected()
        {
            var field = new SubFieldInfo();
            Assert.False(field.IsSecuritySafeCritical);
        }

        [Fact]
        public void IsSecurityTransparent_Get_ReturnsExpected()
        {
            var field = new SubFieldInfo();
            Assert.False(field.IsSecurityTransparent);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.SpecialName, true)]
        [InlineData(FieldAttributes.SpecialName | FieldAttributes.Literal, true)]
        [InlineData(FieldAttributes.Literal, false)]
        public void IsSpecialName_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsSpecialName);
        }

        [Theory]
        [InlineData((FieldAttributes)0, false)]
        [InlineData(FieldAttributes.Static, true)]
        [InlineData(FieldAttributes.Static | FieldAttributes.Literal, true)]
        [InlineData(FieldAttributes.Literal, false)]
        public void IsStatic_Get_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            var field = new SubFieldInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, field.IsStatic);
        }

        [Fact]
        public void MemberType_Get_ReturnsExpected()
        {
            var field = new SubFieldInfo();
            Assert.Equal(MemberTypes.Field, field.MemberType);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var field = new SubFieldInfo();
            yield return new object[] { field, field, true };
            yield return new object[] { field, new SubFieldInfo(), false };
            yield return new object[] { field, new object(), false };
            yield return new object[] { field, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(FieldInfo field, object other, bool expected)
        {
            Assert.Equal(expected, field.Equals(other));
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var field = new SubFieldInfo();
            yield return new object[] { null, null, true };
            yield return new object[] { null, field, false };
            yield return new object[] { field, field, true };
            yield return new object[] { field, new SubFieldInfo(), false };
            yield return new object[] { field, null, false };

            yield return new object[] { new AlwaysEqualsFieldInfo(), null, false };
            yield return new object[] { null, new AlwaysEqualsFieldInfo(), false };
            yield return new object[] { new AlwaysEqualsFieldInfo(), new SubFieldInfo(), true };
            yield return new object[] { new SubFieldInfo(), new AlwaysEqualsFieldInfo(), false };
            yield return new object[] { new AlwaysEqualsFieldInfo(), new AlwaysEqualsFieldInfo(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(FieldInfo field1, FieldInfo field2, bool expected)
        {
            Assert.Equal(expected, field1 == field2);
            Assert.Equal(!expected, field1 != field2);
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var field = new SubFieldInfo();
            Assert.NotEqual(0, field.GetHashCode());
            Assert.Equal(field.GetHashCode(), field.GetHashCode());
        }

        [Fact]
        public void GetOptionalCustomModifiers_Invoke_ThrowsNotImplementedException()
        {
            var field = new SubFieldInfo();
            Assert.Throws<NotImplementedException>(() => field.GetOptionalCustomModifiers());
        }

        [Fact]
        public void GetRequiredCustomModifiers_Invoke_ThrowsNotImplementedException()
        {
            var field = new SubFieldInfo();
            Assert.Throws<NotImplementedException>(() => field.GetRequiredCustomModifiers());
        }

        [Fact]
        public void GetValueDirect_Invoke_ThrowsNotSupportedException()
        {
            var field = new SubFieldInfo();
            Assert.Throws<NotSupportedException>(() => field.GetValueDirect(default));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        public void SetValueDirect_Invoke_ThrowsNotSupportedException(object value)
        {
            var field = new SubFieldInfo();
            Assert.Throws<NotSupportedException>(() => field.SetValueDirect(default, value));
        }

        private class AlwaysEqualsFieldInfo : SubFieldInfo
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class SubFieldInfo : FieldInfo
        {
            public FieldAttributes AttributesResult { get; set; }

            public override FieldAttributes Attributes => AttributesResult;

            public override RuntimeFieldHandle FieldHandle => throw new NotImplementedException();

            public override Type FieldType => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override object GetValue(object obj) => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) => throw new NotImplementedException();
        }
    }
}
