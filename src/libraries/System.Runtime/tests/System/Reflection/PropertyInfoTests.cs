// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public class PropertyInfoTests
    {
        public static IEnumerable<object[]> GetMethod_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { null };
            yield return new object[] { typeof(PropertyInfoTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_TestData))]
        public void GetMethod_Get_ReturnsExpected(MethodInfo result)
        {
            var property = new SubPropertyInfo
            {
                GetGetMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, property.GetMethod);
        }

        [Theory]
        [InlineData((PropertyAttributes)0, false)]
        [InlineData(PropertyAttributes.SpecialName, true)]
        [InlineData(PropertyAttributes.SpecialName | PropertyAttributes.RTSpecialName, true)]
        [InlineData(PropertyAttributes.RTSpecialName, false)]
        public void IsSpecialName_Get_ReturnsExpected(PropertyAttributes attributes, bool expected)
        {
            var property = new SubPropertyInfo
            {
                AttributesResult = attributes
            };
            Assert.Equal(expected, property.IsSpecialName);
        }

        [Fact]
        public void MemberType_Get_ReturnsExpected()
        {
            var property = new SubPropertyInfo();
            Assert.Equal(MemberTypes.Property, property.MemberType);
        }

        public static IEnumerable<object[]> SetMethod_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { null };
            yield return new object[] { typeof(PropertyInfoTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(SetMethod_TestData))]
        public void SetMethod_Get_ReturnsExpected(MethodInfo result)
        {
            var property = new SubPropertyInfo
            {
                GetSetMethodAction = nonPublicParam =>
                {
                    Assert.True(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, property.SetMethod);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            var property = new SubPropertyInfo();
            yield return new object[] { property, property, true };
            yield return new object[] { property, new SubPropertyInfo(), false };
            yield return new object[] { property, new object(), false };
            yield return new object[] { property, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(PropertyInfo property, object other, bool expected)
        {
            Assert.Equal(expected, property.Equals(other));
        }

        public static IEnumerable<object[]> OperatorEquals_TestData()
        {
            var property = new SubPropertyInfo();
            yield return new object[] { null, null, true };
            yield return new object[] { null, property, false };
            yield return new object[] { property, property, true };
            yield return new object[] { property, new SubPropertyInfo(), false };
            yield return new object[] { property, null, false };

            yield return new object[] { new AlwaysEqualsPropertyInfo(), null, false };
            yield return new object[] { null, new AlwaysEqualsPropertyInfo(), false };
            yield return new object[] { new AlwaysEqualsPropertyInfo(), new SubPropertyInfo(), true };
            yield return new object[] { new SubPropertyInfo(), new AlwaysEqualsPropertyInfo(), false };
            yield return new object[] { new AlwaysEqualsPropertyInfo(), new AlwaysEqualsPropertyInfo(), true };
        }

        [Theory]
        [MemberData(nameof(OperatorEquals_TestData))]
        public void OperatorEquals_Invoke_ReturnsExpected(PropertyInfo property1, PropertyInfo property2, bool expected)
        {
            Assert.Equal(expected, property1 == property2);
            Assert.Equal(!expected, property1 != property2);
        }

        public static IEnumerable<object[]> GetAccessors_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new MethodInfo[0] };
            yield return new object[] { new MethodInfo[] { null } };
            yield return new object[] { new MethodInfo[] { typeof(EventInfoTests).GetMethods()[0] } };
        }

        [Theory]
        [MemberData(nameof(GetAccessors_TestData))]
        public void GetAccessors_Invoke_ReturnsExpected(MethodInfo[] result)
        {
            var property = new SubPropertyInfo
            {
                GetAccessorsAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, property.GetAccessors());
        }

        [Fact]
        public void GetConstantValue_Invoke_ThrowsNotImplementedException()
        {
            var property = new SubPropertyInfo();
            Assert.Throws<NotImplementedException>(() => property.GetConstantValue());
        }

        [Theory]
        [MemberData(nameof(GetMethod_TestData))]
        public void GetGetMethod_Invoke_ReturnsExpected(MethodInfo result)
        {
            var property = new SubPropertyInfo
            {
                GetGetMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, property.GetGetMethod());
        }

        [Fact]
        public void GetHashCode_Invoke_ReturnsExpected()
        {
            var property = new SubPropertyInfo();
            Assert.NotEqual(0, property.GetHashCode());
            Assert.Equal(property.GetHashCode(), property.GetHashCode());
        }

        [Fact]
        public void GetOptionalCustomModifiers_Invoke_ReturnsExpected()
        {
            var property = new SubPropertyInfo();
            Assert.Same(Array.Empty<Type>(), property.GetOptionalCustomModifiers());
        }

        [Fact]
        public void GetRawConstantValue_Invoke_ThrowsNotImplementedException()
        {
            var property = new SubPropertyInfo();
            Assert.Throws<NotImplementedException>(() => property.GetRawConstantValue());
        }

        [Fact]
        public void GetRequiredCustomModifiers_Invoke_ReturnsExpected()
        {
            var property = new SubPropertyInfo();
            Assert.Same(Array.Empty<Type>(), property.GetRequiredCustomModifiers());
        }

        [Theory]
        [MemberData(nameof(SetMethod_TestData))]
        public void GetSetMethod_Invoke_ReturnsExpected(MethodInfo result)
        {
            var property = new SubPropertyInfo
            {
                GetSetMethodAction = nonPublicParam =>
                {
                    Assert.False(nonPublicParam);
                    return result;
                }
            };
            Assert.Same(result, property.GetSetMethod());
        }

        public static IEnumerable<object[]> GetValue_Object_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new object(), new object() };
        }

        [Theory]
        [MemberData(nameof(GetValue_Object_TestData))]
        public void GetValue_InvokeObject_ReturnsExpected(object obj, object result)
        {
            var property = new SubPropertyInfo
            {
                GetValueAction = (objParam, invokeAttrParam, binderParam, indexParam, cultureParam) =>
                {
                    Assert.Same(obj, objParam);
                    Assert.Equal(BindingFlags.Default, invokeAttrParam);
                    Assert.Null(binderParam);
                    Assert.Null(indexParam);
                    Assert.Null(cultureParam);
                    return result;
                }
            };
            Assert.Same(result, property.GetValue(obj));
        }

        public static IEnumerable<object[]> GetValue_Object_ObjectArray_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { new object(), Array.Empty<object>(), new object() };
            yield return new object[] { new object(), new object[] { null }, new object() };
            yield return new object[] { new object(), new object[] { new object() }, new object() };
        }

        [Theory]
        [MemberData(nameof(GetValue_Object_ObjectArray_TestData))]
        public void GetValue_InvokeObjectObjectArray_ReturnsExpected(object obj, object[] index, object result)
        {
            var property = new SubPropertyInfo
            {
                GetValueAction = (objParam, invokeAttrParam, binderParam, indexParam, cultureParam) =>
                {
                    Assert.Same(obj, objParam);
                    Assert.Equal(BindingFlags.Default, invokeAttrParam);
                    Assert.Null(binderParam);
                    Assert.Same(index, indexParam);
                    Assert.Null(cultureParam);
                    return result;
                }
            };
            Assert.Same(result, property.GetValue(obj, index));
        }

        public static IEnumerable<object[]> SetValue_Object_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new object(), new object() };
        }

        [Theory]
        [MemberData(nameof(SetValue_Object_TestData))]
        public void SetValue_InvokeObject_ReturnsExpected(object obj, object value)
        {
            int callCount = 0;
            var property = new SubPropertyInfo
            {
                SetValueAction = (objParam, valueParam, invokeAttrParam, binderParam, indexParam, cultureParam) =>
                {
                    Assert.Same(obj, objParam);
                    Assert.Same(value, valueParam);
                    Assert.Equal(BindingFlags.Default, invokeAttrParam);
                    Assert.Null(binderParam);
                    Assert.Null(indexParam);
                    Assert.Null(cultureParam);
                    callCount++;
                }
            };
            property.SetValue(obj, value);
            Assert.Equal(1, callCount);
        }

        public static IEnumerable<object[]> SetValue_Object_ObjectArray_TestData()
        {
            yield return new object[] { null, null, null };
            yield return new object[] { new object(), new object(), Array.Empty<object>() };
            yield return new object[] { new object(), new object(), new object[] { null } };
            yield return new object[] { new object(), new object(), new object[] { new object() } };
        }

        [Theory]
        [MemberData(nameof(SetValue_Object_ObjectArray_TestData))]
        public void SetValue_InvokeObjectObjectArray_ReturnsExpected(object obj, object value, object[] index)
        {
            int callCount = 0;
            var property = new SubPropertyInfo
            {
                SetValueAction = (objParam, valueParam, invokeAttrParam, binderParam, indexParam, cultureParam) =>
                {
                    Assert.Same(obj, objParam);
                    Assert.Same(value, valueParam);
                    Assert.Equal(BindingFlags.Default, invokeAttrParam);
                    Assert.Null(binderParam);
                    Assert.Same(index, indexParam);
                    Assert.Null(cultureParam);
                    callCount++;
                }
            };
            property.SetValue(obj, value, index);
            Assert.Equal(1, callCount);
        }

        private class AlwaysEqualsPropertyInfo : SubPropertyInfo
        {
            public override bool Equals(object obj) => true;

            public override int GetHashCode() => base.GetHashCode();
        }

        private class SubPropertyInfo : PropertyInfo
        {
            public PropertyAttributes AttributesResult { get; set; }

            public override PropertyAttributes Attributes => AttributesResult;

            public override bool CanRead => throw new NotImplementedException();

            public override bool CanWrite => throw new NotImplementedException();

            public override Type PropertyType => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public Func<bool, MethodInfo> GetGetMethodAction { get; set; }

            public override MethodInfo GetGetMethod(bool nonPublic) => GetGetMethodAction(nonPublic);
            public Func<bool, MethodInfo> GetSetMethodAction { get; set; }

            public override MethodInfo GetSetMethod(bool nonPublic) => GetSetMethodAction(nonPublic);

            public Func<bool, MethodInfo[]> GetAccessorsAction { get; set; }

            public override MethodInfo[] GetAccessors(bool nonPublic) => GetAccessorsAction(nonPublic);

            public override ParameterInfo[] GetIndexParameters() => throw new NotImplementedException();

            public Func<object, BindingFlags, Binder, object[], CultureInfo, object> GetValueAction { get; set; }

            public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) => GetValueAction(obj, invokeAttr, binder, index, culture);

            public Action<object, object, BindingFlags, Binder, object[], CultureInfo> SetValueAction { get; set; }

            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture) => SetValueAction(obj, value, invokeAttr, binder, index, culture);

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
        }
    }
}
