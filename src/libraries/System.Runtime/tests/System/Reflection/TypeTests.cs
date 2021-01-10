// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public class TypeTests
    {
        [Theory]
        [InlineData((TypeAttributes)0)]
        [InlineData(TypeAttributes.Abstract)]
        public void Attributes_GetCustom_ReturnsExpected(TypeAttributes attributes)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(attributes, type.Attributes);
        }

        [Fact]
        public void DeclaringType_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.Null(type.DeclaringType);
        }

        [Fact]
        public void DeclaringMethod_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.Null(type.DeclaringMethod);
        }

        [Fact]
        public void Delimiter_Get_ReturnsExpected()
        {
            Assert.Equal('.', Type.Delimiter);
        }

        [Fact]
        public void DefaultBinder_Get_ReturnsExpected()
        {
            Binder binder = Type.DefaultBinder;
            Assert.NotNull(binder);
            Assert.Same(binder, Type.DefaultBinder);
        }

        [Fact]
        public void EmptyTypes_Get_ReturnsExpected()
        {
            Assert.Same(Array.Empty<Type>(), Type.EmptyTypes);
            Assert.Same(Type.EmptyTypes, Type.EmptyTypes);
        }

        [Fact]
        public void FilterAttribute_Get_ReturnsExpected()
        {
            Assert.NotNull(Type.FilterAttribute);
            Assert.Same(Type.FilterAttribute, Type.FilterAttribute);
        }

        [Theory]
        [InlineData((MethodAttributes)0, true)]
        [InlineData(MethodAttributes.Public, true)]
        [InlineData(MethodAttributes.Assembly, false)]
        [InlineData(MethodAttributes.Static, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        [InlineData(MethodAttributes.Abstract, false)]
        [InlineData(MethodAttributes.SpecialName, true)]
        public void FilterAttribute_InvokeConstructor_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            MemberInfo m = typeof(List<int>).GetConstructors()[0];
            Assert.Equal(expected, Type.FilterAttribute.Invoke(m, (int)attributes));
        }

        [Theory]
        [InlineData((MethodAttributes)0, true)]
        [InlineData(MethodAttributes.Public, true)]
        [InlineData(MethodAttributes.Assembly, false)]
        [InlineData(MethodAttributes.Static, false)]
        [InlineData(MethodAttributes.Virtual, false)]
        [InlineData(MethodAttributes.Abstract, false)]
        [InlineData(MethodAttributes.SpecialName, false)]
        public void FilterAttribute_InvokeMethod_ReturnsExpected(MethodAttributes attributes, bool expected)
        {
            MemberInfo m = typeof(List<int>).GetMethod(nameof(List<int>.Sort), Array.Empty<Type>());
            Assert.Equal(expected, Type.FilterAttribute.Invoke(m, (int)attributes));
        }

        [Theory]
        [InlineData((FieldAttributes)0, true)]
        [InlineData(FieldAttributes.Public, true)]
        [InlineData(FieldAttributes.Assembly, false)]
        [InlineData(FieldAttributes.Static, true)]
        [InlineData(FieldAttributes.InitOnly, true)]
        [InlineData(FieldAttributes.Literal, false)]
        [InlineData(FieldAttributes.NotSerialized, false)]
        [InlineData(FieldAttributes.PinvokeImpl, false)]
        [InlineData(FieldAttributes.SpecialName, true)]
        public void FilterAttribute_InvokeField_ReturnsExpected(FieldAttributes attributes, bool expected)
        {
            MemberInfo m = typeof(Type).GetField(nameof(Type.Delimiter));
            Assert.Equal(expected, Type.FilterAttribute.Invoke(m, (int)attributes));
        }

        [Fact]
        public void FilterAttribute_InvokeEvent_ReturnsFalse()
        {
            MemberInfo m = typeof(Console).GetEvents()[0];
            Assert.False(Type.FilterAttribute.Invoke(m, 1));
        }

        [Fact]
        public void FilterAttribute_NullFilterCriteria_ThrowsInvalidFilterCriteriaException()
        {
            MemberInfo m = typeof(List<int>).GetProperties()[0];
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterAttribute.Invoke(m, null));
        }

        [Fact]
        public void FilterAttribute_NullM_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => Type.FilterAttribute.Invoke(null, 1));
        }

        [Fact]
        public void FilterAttribute_InvalidFilterCriteriaConstructor_ThrowsInvalidFilterCriteriaException()
        {
            MemberInfo m = typeof(List<int>).GetConstructors()[0];
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterAttribute.Invoke(m, new object()));
        }

        [Fact]
        public void FilterAttribute_InvalidFilterCriteriaMethod_ThrowsInvalidFilterCriteriaException()
        {
            MemberInfo m = typeof(List<int>).GetMethods()[0];
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterAttribute.Invoke(m, new object()));
        }

        [Fact]
        public void FilterAttribute_InvalidFilterCriteriaField_ThrowsInvalidFilterCriteriaException()
        {
            MemberInfo m = typeof(Type).GetFields()[0];
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterAttribute.Invoke(m, new object()));
        }

        [Fact]
        public void FilterAttribute_InvalidFilterCriteriaEvent_ThrowsInvalidFilterCriteriaException()
        {
            MemberInfo m = typeof(Console).GetEvents()[0];
            Assert.False(Type.FilterAttribute.Invoke(m, new object()));
        }

        [Fact]
        public void FilterName_Get_ReturnsExpected()
        {
            Assert.NotNull(Type.FilterName);
            Assert.Same(Type.FilterName, Type.FilterName);
        }

        [Fact]
        public void FilterNameIgnoreCase_Get_ReturnsExpected()
        {
            Assert.NotNull(Type.FilterNameIgnoreCase);
            Assert.Same(Type.FilterNameIgnoreCase, Type.FilterNameIgnoreCase);
        }

        [Fact]
        public void GenericParameterAttributes_Get_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GenericParameterAttributes);
        }

        [Fact]
        public void GenericParameterPosition_Get_ThrowsInvalidOperationException()
        {
            var type = new SubType();
            Assert.Throws<InvalidOperationException>(() => type.GenericParameterPosition);
        }

        [Fact]
        public void GenericTypeArguments_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.Empty(type.GenericTypeArguments);
        }

        [Fact]
        public void GenericTypeArguments_GetGenericType_ThrowsNotSupportedException()
        {
            var type = new CustomType
            {
                IsGenericTypeAction = () => true
            };
            Assert.Throws<NotSupportedException>(() => type.GenericTypeArguments);
        }

        [Fact]
        public void GenericTypeArguments_GetGenericTypeDefinition_ReturnsExpected()
        {
            var type = new CustomType
            {
                IsGenericTypeAction = () => true,
                IsGenericTypeDefinitionAction = () => true
            };
            Assert.Empty(type.GenericTypeArguments);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void HasElementType_GetCustomHasElementTypeImpl_ReturnsExpected(bool result)
        {
            var type = new SubType
            {
                HasElementTypeImplAction = () => result
            };
            Assert.Equal(result, type.HasElementType);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.Abstract, true)]
        [InlineData(TypeAttributes.Abstract | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsAbstract_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsAbstract);
        }

        [Theory]
        [InlineData((TypeAttributes)0, true)]
        [InlineData(TypeAttributes.AnsiClass | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.AutoClass, false)]
        [InlineData(TypeAttributes.UnicodeClass, false)]
        [InlineData(TypeAttributes.Abstract, true)]
        public void IsAnsiClass_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsAnsiClass);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsArray_GetCustomIsArrayImpl_ReturnsExpected(bool result)
        {
            var type = new SubType
            {
                IsArrayImplAction = () => result
            };
            Assert.Equal(result, type.IsArray);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.AutoClass, true)]
        [InlineData(TypeAttributes.AutoClass | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.UnicodeClass, false)]
        [InlineData(TypeAttributes.Abstract, false)]
        public void IsAutoClass_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsAutoClass);
        }

        [Theory]
        [InlineData((TypeAttributes)0, true)]
        [InlineData(TypeAttributes.AutoLayout | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.ExplicitLayout, false)]
        [InlineData(TypeAttributes.SequentialLayout, false)]
        [InlineData(TypeAttributes.Abstract, true)]
        public void IsAutoLayout_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsAutoLayout);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsByRef_GetCustomIsByRefImpl_ReturnsExpected(bool result)
        {
            var type = new SubType
            {
                IsByRefImplAction = () => result
            };
            Assert.Equal(result, type.IsByRef);
        }

        [Fact]
        public void IsByRefLike_Get_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.IsByRefLike);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.ExplicitLayout, true)]
        [InlineData(TypeAttributes.ExplicitLayout | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.SequentialLayout, false)]
        [InlineData(TypeAttributes.Abstract, false)]
        public void IsExplicitLayout_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsExplicitLayout);
        }

        [Theory]
        [InlineData(TypeAttributes.Class)]
        [InlineData(TypeAttributes.Class | TypeAttributes.Public)]
        [InlineData(TypeAttributes.Import)]
        public void IsClass_Get_ThrowsNotImplementedException(TypeAttributes attributes)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Throws<NotImplementedException>(() => type.IsClass);
        }

        [Theory]
        [InlineData(TypeAttributes.Interface)]
        public void IsClass_GetNotClass_ReturnsExpected(TypeAttributes attributes)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.False(type.IsClass);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false, true)]
        [InlineData(TypeAttributes.Class | TypeAttributes.Public, false, true)]
        [InlineData(TypeAttributes.Interface, false, false)]
        [InlineData(TypeAttributes.Import, false, true)]
        [InlineData((TypeAttributes)0, true, false)]
        [InlineData(TypeAttributes.Class | TypeAttributes.Public, true, false)]
        [InlineData(TypeAttributes.Interface, true, false)]
        [InlineData(TypeAttributes.Import, true, false)]
        public void IsClass_GetCustom_ReturnsExpected(TypeAttributes attributes, bool isValueTypeResult, bool expected)
        {
            var type = new CustomType
            {
                GetAttributeFlagsImplResult = attributes,
                IsValueTypeAction = () => isValueTypeResult
            };
            Assert.Equal(expected, type.IsClass);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsCOMObject_GetCustomIsCOMObjectImpl_ReturnsExpected(bool result)
        {
            var type = new SubType
            {
                IsCOMObjectImplAction = () => result
            };
            Assert.Equal(result, type.IsCOMObject);
        }

        [Fact]
        public void IsConstructedGenericType_Get_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.IsConstructedGenericType);
        }

        [Fact]
        public void IsContextful_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsContextful);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsContextful_GetCustomIsContextfulImpl_ReturnsExpected(bool result)
        {
            var type = new CustomType
            {
                IsContextfulImplAction = () => result
            };
            Assert.Equal(result, type.IsContextful);
        }

        [Fact]
        public void IsEnum_Get_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.IsEnum);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsEnum_GetCustomIsSubclassOf_ReturnsExpected(bool result)
        {
            var type = new CustomType
            {
                IsSubclassOfAction = c =>
                {
                    Assert.Equal(typeof(Enum), c);
                    return result;
                }
            };
            Assert.Equal(result, type.IsEnum);
        }

        [Fact]
        public void IsGenericParameter_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsGenericParameter);
        }

        [Fact]
        public void IsGenericMethodParameter_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsGenericMethodParameter);
        }

        public static IEnumerable<object[]> IsGenericMethodParameter_Custom_TestData()
        {
            yield return new object[] { true, null, false };
            yield return new object[] { true, typeof(TypeTests).GetMethods()[0], true };
            yield return new object[] { false, null, false };
            yield return new object[] { false, typeof(TypeTests).GetMethods()[0], false };
        }

        [Theory]
        [MemberData(nameof(IsGenericMethodParameter_Custom_TestData))]
        public void IsGenericMethodParameter_GetCustom_ReturnsExpected(bool isGenericParameter, MethodBase declaringMethod, bool expected)
        {
            var type = new CustomType
            {
                IsGenericParameterAction = () => isGenericParameter,
                DeclaringMethodAction = () => declaringMethod
            };
            Assert.Equal(expected, type.IsGenericMethodParameter);
        }

        [Fact]
        public void IsGenericType_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsGenericType);
        }

        [Fact]
        public void IsGenericTypeParameter_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsGenericTypeParameter);
        }

        public static IEnumerable<object[]> IsGenericTypeParameter_Custom_TestData()
        {
            yield return new object[] { true, null, true };
            yield return new object[] { true, typeof(TypeTests).GetMethods()[0], false };
            yield return new object[] { false, null, false };
            yield return new object[] { false, typeof(TypeTests).GetMethods()[0], false };
        }

        [Theory]
        [MemberData(nameof(IsGenericTypeParameter_Custom_TestData))]
        public void IsGenericTypeParameter_GetCustom_ReturnsExpected(bool isGenericParameter, MethodBase declaringMethod, bool expected)
        {
            var type = new CustomType
            {
                IsGenericParameterAction = () => isGenericParameter,
                DeclaringMethodAction = () => declaringMethod
            };
            Assert.Equal(expected, type.IsGenericTypeParameter);
        }

        [Fact]
        public void IsGenericTypeDefinition_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsGenericTypeDefinition);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.Import, true)]
        [InlineData(TypeAttributes.Import | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.Abstract, false)]
        public void IsImport_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsImport);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.SequentialLayout, true)]
        [InlineData(TypeAttributes.SequentialLayout | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.ExplicitLayout, false)]
        [InlineData(TypeAttributes.Abstract, false)]
        public void IsLayoutSequential_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsLayoutSequential);
        }

        [Fact]
        public void IsMarshalByRef_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsMarshalByRef);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsMarshalByRef_GetCustomIsMarshalByRefImpl_ReturnsExpected(bool result)
        {
            var type = new CustomType
            {
                IsMarshalByRefImplAction = () => result
            };
            Assert.Equal(result, type.IsMarshalByRef);
        }

        [Fact]
        public void IsNested_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsNested);
        }

        public static IEnumerable<object[]> IsNested_Custom_TestData()
        {
            yield return new object[] { null, false };
            yield return new object[] { typeof(TypeTests), true };
        }

        [Theory]
        [MemberData(nameof(IsNested_Custom_TestData))]
        public void IsNested_GetCustom_ReturnsExpected(Type declaringType, bool expected)
        {
            var type = new CustomType
            {
                DeclaringTypeAction = () => declaringType
            };
            Assert.Equal(expected, type.IsNested);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.NestedAssembly, true)]
        [InlineData(TypeAttributes.NestedAssembly | TypeAttributes.Import, true)]
        [InlineData(TypeAttributes.NestedFamANDAssem, false)]
        [InlineData(TypeAttributes.NestedFamORAssem, false)]
        [InlineData(TypeAttributes.NestedFamily, false)]
        [InlineData(TypeAttributes.NestedPrivate, false)]
        [InlineData(TypeAttributes.NestedPublic, false)]
        [InlineData(TypeAttributes.Public, false)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsNestedAssembly_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsNestedAssembly);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.NestedFamANDAssem, true)]
        [InlineData(TypeAttributes.NestedFamANDAssem | TypeAttributes.Import, true)]
        [InlineData(TypeAttributes.NestedAssembly, false)]
        [InlineData(TypeAttributes.NestedFamORAssem, false)]
        [InlineData(TypeAttributes.NestedFamily, false)]
        [InlineData(TypeAttributes.NestedPrivate, false)]
        [InlineData(TypeAttributes.NestedPublic, false)]
        [InlineData(TypeAttributes.Public, false)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsNestedFamANDAssem_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsNestedFamANDAssem);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.NestedFamily, true)]
        [InlineData(TypeAttributes.NestedFamily | TypeAttributes.Import, true)]
        [InlineData(TypeAttributes.NestedAssembly, false)]
        [InlineData(TypeAttributes.NestedFamORAssem, false)]
        [InlineData(TypeAttributes.NestedFamANDAssem, false)]
        [InlineData(TypeAttributes.NestedPrivate, false)]
        [InlineData(TypeAttributes.NestedPublic, false)]
        [InlineData(TypeAttributes.Public, false)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsNestedFamily_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsNestedFamily);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.NestedFamORAssem, true)]
        [InlineData(TypeAttributes.NestedFamORAssem | TypeAttributes.Import, true)]
        [InlineData(TypeAttributes.NestedAssembly, false)]
        [InlineData(TypeAttributes.NestedFamANDAssem, false)]
        [InlineData(TypeAttributes.NestedFamily, false)]
        [InlineData(TypeAttributes.NestedPrivate, false)]
        [InlineData(TypeAttributes.NestedPublic, false)]
        [InlineData(TypeAttributes.Public, false)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsNestedFamORAssem_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsNestedFamORAssem);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.NestedPrivate, true)]
        [InlineData(TypeAttributes.NestedPrivate | TypeAttributes.Import, true)]
        [InlineData(TypeAttributes.NestedFamANDAssem, false)]
        [InlineData(TypeAttributes.NestedFamORAssem, false)]
        [InlineData(TypeAttributes.NestedFamily, false)]
        [InlineData(TypeAttributes.NestedAssembly, false)]
        [InlineData(TypeAttributes.NestedPublic, false)]
        [InlineData(TypeAttributes.Public, false)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsNestedPrivate_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsNestedPrivate);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.NestedPublic, true)]
        [InlineData(TypeAttributes.NestedPublic | TypeAttributes.Import, true)]
        [InlineData(TypeAttributes.NestedFamANDAssem, false)]
        [InlineData(TypeAttributes.NestedFamORAssem, false)]
        [InlineData(TypeAttributes.NestedFamily, false)]
        [InlineData(TypeAttributes.NestedAssembly, false)]
        [InlineData(TypeAttributes.NestedPrivate, false)]
        [InlineData(TypeAttributes.Public, false)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsNestedPublic_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsNestedPublic);
        }

        [Theory]
        [InlineData((TypeAttributes)0, true)]
        [InlineData(TypeAttributes.Public, false)]
        [InlineData(TypeAttributes.Import, true)]
        public void IsNotPublic_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsNotPublic);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsPointer_GetCustomIsPointerImpl_ReturnsExpected(bool result)
        {
            var type = new SubType
            {
                IsPointerImplAction = () => result
            };
            Assert.Equal(result, type.IsPointer);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsPrimitive_GetCustomIsPrimitiveImpl_ReturnsExpected(bool result)
        {
            var type = new SubType
            {
                IsPrimitiveImplAction = () => result
            };
            Assert.Equal(result, type.IsPrimitive);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.Public | TypeAttributes.Import, true)]
        [InlineData(TypeAttributes.NestedAssembly, false)]
        [InlineData(TypeAttributes.NestedFamANDAssem, false)]
        [InlineData(TypeAttributes.NestedFamily, false)]
        [InlineData(TypeAttributes.NestedFamORAssem, false)]
        [InlineData(TypeAttributes.NestedPrivate, false)]
        [InlineData(TypeAttributes.NestedPublic, false)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsPublic_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsPublic);
        }

        [Fact]
        public void IsSecurityCritical_Get_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.IsSecurityCritical);
        }

        [Fact]
        public void IsSecuritySafeCritical_Get_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.IsSecuritySafeCritical);
        }

        [Fact]
        public void IsSecurityTransparent_Get_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.IsSecurityTransparent);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.Sealed, true)]
        [InlineData(TypeAttributes.Sealed | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsSealed_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsSealed);
        }

        [Fact]
        public void IsSignatureType_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsSignatureType);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.SpecialName, true)]
        [InlineData(TypeAttributes.SpecialName | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.Import, false)]
        public void IsSpecialName_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsSpecialName);
        }

        [Fact]
        public void IsSZArray_Get_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.IsSZArray);
        }

        [Fact]
        public void IsTypeDefinition_Get_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.IsTypeDefinition);
        }

        [Theory]
        [InlineData((TypeAttributes)0, false)]
        [InlineData(TypeAttributes.UnicodeClass, true)]
        [InlineData(TypeAttributes.UnicodeClass | TypeAttributes.Public, true)]
        [InlineData(TypeAttributes.AutoClass, false)]
        [InlineData(TypeAttributes.Abstract, false)]
        public void IsUnicodeClass_GetCustom_ReturnsExpected(TypeAttributes attributes, bool expected)
        {
            var type = new SubType
            {
                GetAttributeFlagsImplResult = attributes
            };
            Assert.Equal(expected, type.IsUnicodeClass);
        }

        [Fact]
        public void IsValueType_Get_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.IsValueType);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsValueType_GetCustomIsSubclassOf_ReturnsExpected(bool result)
        {
            var type = new CustomType
            {
                IsSubclassOfAction = c =>
                {
                    Assert.Equal(typeof(ValueType), c);
                    return result;
                }
            };
            Assert.Equal(result, type.IsValueType);
        }

        [Fact]
        public void IsVariableBoundArray_GetArray_ThrowsNotImplementedException()
        {
            var type = new SubType
            {
                IsArrayImplAction = () => true
            };
            Assert.Throws<NotImplementedException>(() => type.IsVariableBoundArray);
        }

        [Theory]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public void IsVariableBoundArray_GetCustom_ReturnsExpected(bool isArray, bool isSZArray, bool expected)
        {
            var type = new CustomType
            {
                IsArrayImplAction = () => isArray,
                IsSZArrayAction = () => isSZArray
            };
            Assert.Equal(expected, type.IsVariableBoundArray);
        }

        [Fact]
        public void MemberType_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.Equal(MemberTypes.TypeInfo, type.MemberType);
        }

        [Fact]
        public void Missing_Get_ReturnsExpected()
        {
            Assert.Same(Missing.Value, Type.Missing);
            Assert.Same(Type.Missing, Type.Missing);
        }

        [Fact]
        public void ReflectedType_Get_ReturnsExpected()
        {
            var type = new SubType();
            Assert.Null(type.ReflectedType);
        }

        [Fact]
        public void StructLayoutAttribute_Get_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.StructLayoutAttribute);
        }

        [Fact]
        public void TypeHandle_Get_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.TypeHandle);
        }

        public static IEnumerable<object[]> TypeInitializer_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { typeof(TypeTests).GetConstructors()[0] };
        }

        [Theory]
        [MemberData(nameof(TypeInitializer_TestData))]
        public void TypeInitializer_Get_ReturnsExpected(ConstructorInfo result)
        {
            var type = new SubType
            {
                GetConstructorImplAction = (bindingAttr, binder, callConvention, types, modifiers) =>
                {
                    Assert.Equal(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, bindingAttr);
                    Assert.Null(binder);
                    Assert.Equal(CallingConventions.Any, callConvention);
                    Assert.Same(Type.EmptyTypes, types);
                    Assert.Null(modifiers);
                    return result;
                }
            };
            Assert.Same(result, type.TypeInitializer);
        }

        [Fact]
        private void Equals_InvokeObjectNullUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType();
            Assert.True(type.Equals((object)type));
            Assert.True(type.Equals((object)new SubType()));
            Assert.True(type.Equals((object)new CustomType()));
            Assert.False(type.Equals((object)typeof(int)));
            Assert.False(type.Equals(new object()));
            Assert.False(type.Equals((object)null));
        }

        [Fact]
        private void Equals_InvokeObjectSameUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType();
            type.UnderlyingSystemTypeResult = type;
            Assert.True(type.Equals((object)type));
            Assert.False(type.Equals((object)new SubType()));
            Assert.False(type.Equals((object)new CustomType()));
            Assert.False(type.Equals((object)typeof(int)));
            Assert.False(type.Equals(new object()));
            Assert.False(type.Equals((object)null));
        }

        [Fact]
        private void Equals_InvokeObjectCustomUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType();
            type.UnderlyingSystemTypeResult = typeof(int);
            Assert.True(type.Equals((object)type));
            Assert.False(type.Equals((object)new SubType()));
            Assert.False(type.Equals((object)new CustomType()));
            Assert.True(type.Equals((object)typeof(int)));
            Assert.False(type.Equals(new object()));
            Assert.False(type.Equals((object)null));
        }

        [Fact]
        private void Equals_InvokeTypeUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType();
            Assert.True(type.Equals(type));
            Assert.True(type.Equals(new SubType()));
            Assert.True(type.Equals(new CustomType()));
            Assert.False(type.Equals(typeof(int)));
            Assert.False(type.Equals(null));
        }

        [Fact]
        private void Equals_InvokeTypeSameUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType();
            type.UnderlyingSystemTypeResult = type;
            Assert.True(type.Equals(type));
            Assert.False(type.Equals(new SubType()));
            Assert.False(type.Equals(new CustomType()));
            Assert.False(type.Equals(typeof(int)));
            Assert.False(type.Equals(null));
        }

        [Fact]
        private void Equals_InvokeTypeCustomUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType();
            type.UnderlyingSystemTypeResult = typeof(int);
            Assert.True(type.Equals(type));
            Assert.False(type.Equals(new SubType()));
            Assert.False(type.Equals(new CustomType()));
            Assert.True(type.Equals(typeof(int)));
            Assert.False(type.Equals(null));
        }

        [Fact]
        public void GetArrayRank_Invoke_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetArrayRank());
        }

        [Fact]
        public void GetDefaultMembers_Invoke_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.GetDefaultMembers());
        }

        [Fact]
        public void GetGenericArguments_Invoke_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetGenericArguments());
        }

        [Fact]
        public void GetGenericParameterConstraints_Invoke_ThrowsInvalidOperationException()
        {
            var type = new SubType();
            Assert.Throws<InvalidOperationException>(() => type.GetGenericParameterConstraints());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetGenericParameterConstraints_InvokeGenericParameter_ThrowsInvalidOperationException(bool isGenericParameter)
        {
            var type = new CustomType
            {
                IsGenericParameterAction = () => isGenericParameter
            };
            Assert.Throws<InvalidOperationException>(() => type.GetGenericParameterConstraints());
        }

        [Fact]
        public void GetGenericTypeDefinition_Invoke_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetGenericTypeDefinition());
        }

        [Fact]
        public void GetHashCode_InvokeNullUnderlyingSystemType_ThrowsNullReferenceException()
        {
            var type = new SubType();
            Assert.Throws<NullReferenceException>(() => type.GetHashCode());
        }

        [Fact]
        public void GetHashCode_InvokeSameUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType();
            type.UnderlyingSystemTypeResult = typeof(int);
            Assert.Equal(typeof(int).GetHashCode(), type.GetHashCode());
            Assert.Equal(type.GetHashCode(), type.GetHashCode());
        }

        [Fact]
        public void GetHashCode_InvokeCustomUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType
            {
                UnderlyingSystemTypeResult = typeof(int)
            };
            Assert.Equal(typeof(int).GetHashCode(), type.GetHashCode());
            Assert.Equal(type.GetHashCode(), type.GetHashCode());
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void GetInterfaceMap_Invoke_ThrowsNotSupportedException(Type interfaceType)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetInterfaceMap(interfaceType));
        }

        [Fact]
        public void GetType_Invoke_ReturnsExpected()
        {
            var type = new SubType();
            Assert.Equal(typeof(SubType), type.GetType());
        }

        public static IEnumerable<object[]> GetTypeArray_TestData()
        {
            yield return new object[] { Array.Empty<object>(), Array.Empty<Type>() };
            yield return new object[] { new object[] { new object() }, new Type[] { typeof(object) } };
        }

        [Theory]
        [MemberData(nameof(GetTypeArray_TestData))]
        public void GetTypeArray_Invoke_ReturnsExpected(object[] args, Type[] expected)
        {
            Assert.Equal(expected, Type.GetTypeArray(args));
        }

        [Fact]
        public void GetTypeArray_NullArgs_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("args", () => Type.GetTypeArray(null));
        }

        [Fact]
        public void GetTypeArray_NullArgInArgs_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("args", () => Type.GetTypeArray(new object[] { null }));
        }

        public static IEnumerable<object[]> GetTypeCode_TestData()
        {
            yield return new object[] { null, TypeCode.Empty };
            yield return new object[] { typeof(int), TypeCode.Int32 };
        }

        [Theory]
        [MemberData(nameof(GetTypeCode_TestData))]
        public void GetTypeCode_Invoke_ReturnsExpected(Type type, TypeCode expected)
        {
            Assert.Equal(expected, Type.GetTypeCode(type));
        }

        [Theory]
        [InlineData(TypeCode.Double)]
        public void GetTypeCode_InvokeCustom_ReturnsExpected(TypeCode result)
        {
            var type = new CustomType
            {
                GetTypeCodeImplAction = () => result
            };
            Assert.Equal(result, Type.GetTypeCode(type));
        }

        public static IEnumerable<object[]> GetTypeCodeImpl_TestData()
        {
            yield return new object[] { null, TypeCode.Object };
            yield return new object[] { typeof(int), TypeCode.Int32 };
        }

        [Theory]
        [MemberData(nameof(GetTypeCodeImpl_TestData))]
        public void GetTypeCodeImpl_Invoke_ReturnsExpected(Type underlyingSystemType, TypeCode expected)
        {
            var type = new ProtectedType
            {
                UnderlyingSystemTypeResult = underlyingSystemType
            };
            Assert.Equal(expected, type.GetTypeCodeImpl());
        }

        [Fact]
        public void GetTypeCodeImpl_InvokeSameUnderlyingType_ReturnsExpected()
        {
            var type = new ProtectedType();
            type.UnderlyingSystemTypeResult = type;
            Assert.Equal(TypeCode.Object, type.GetTypeCodeImpl());
        }

        [Fact]
        public void GetTypeHandle_Invoke_ReturnsExpected()
        {
            var o1 = new object();
            var o2 = new object();
            int o3 = 1;
            Assert.Equal(Type.GetTypeHandle(o1), Type.GetTypeHandle(o1));
            Assert.Equal(Type.GetTypeHandle(o1), Type.GetTypeHandle(o2));
            Assert.NotEqual(Type.GetTypeHandle(o1), Type.GetTypeHandle(o3));
        }

        [Fact]
        public void GetTypeHandle_NullO_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(null, () => Type.GetTypeHandle(null));
        }

        [Fact]
        public void IsContextfulImpl_Invoke_ReturnsExpected()
        {
            var type = new ProtectedType();
            Assert.False(type.IsContextfulImpl());
        }

        [Fact]
        public void IsMarshalByRefImpl_Invoke_ReturnsExpected()
        {
            var type = new ProtectedType();
            Assert.False(type.IsMarshalByRefImpl());
        }

        public static IEnumerable<object[]> GetConstructor_TypeArray_TestData()
        {
            yield return new object[] { Array.Empty<Type>(), null };
            yield return new object[] { new Type[] { typeof(int) }, typeof(TypeTests).GetConstructors()[0] };
        }

        [Theory]
        [MemberData(nameof(GetConstructor_TypeArray_TestData))]
        public void GetConstructor_InvokeTypeArray_ReturnsExpected(Type[] types, ConstructorInfo result)
        {
            var type = new SubType
            {
                GetConstructorImplAction = (bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetConstructor(types));
        }

        public static IEnumerable<object[]> GetConstructor_BindingFlagsBinderTypeArrayParameterModifierArray_TestData()
        {
            yield return new object[] { BindingFlags.Public, null, Array.Empty<Type>(), null, null };
            yield return new object[] { BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(int) }, new ParameterModifier[0], typeof(TypeTests).GetConstructors()[0] };
            yield return new object[] { BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(int) }, new ParameterModifier[1], typeof(TypeTests).GetConstructors()[0] };
        }

        [Theory]
        [MemberData(nameof(GetConstructor_BindingFlagsBinderTypeArrayParameterModifierArray_TestData))]
        public void GetConstructor_InvokeBindingFlagsBinderTypeArrayParameterModifierArray_ReturnsExpected(BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers, ConstructorInfo result)
        {
            var type = new SubType
            {
                GetConstructorImplAction = (bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetConstructor(bindingAttr, binder, types, modifiers));
        }

        public static IEnumerable<object[]> GetConstructor_BindingFlagsBinderCallingConventionsTypeArrayParameterModifierArray_TestData()
        {
            yield return new object[] { BindingFlags.Public, null, CallingConventions.Standard, Array.Empty<Type>(), null, null };
            yield return new object[] { BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { typeof(int) }, new ParameterModifier[0], typeof(TypeTests).GetConstructors()[0] };
            yield return new object[] { BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { typeof(int) }, new ParameterModifier[1], typeof(TypeTests).GetConstructors()[0] };
        }

        [Theory]
        [MemberData(nameof(GetConstructor_BindingFlagsBinderCallingConventionsTypeArrayParameterModifierArray_TestData))]
        public void GetConstructor_InvokeBindingFlagsBinderCallingConventionsTypeArrayParameterModifierArray_ReturnsExpected(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers, ConstructorInfo result)
        {
            var type = new SubType
            {
                GetConstructorImplAction = (bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Equal(callConvention, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetConstructor(bindingAttr, binder, callConvention, types, modifiers));
        }

        [Fact]
        public void GetConstructor_NullTypes_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("types", () => type.GetConstructor(null));
            Assert.Throws<ArgumentNullException>("types", () => type.GetConstructor(BindingFlags.Public, Type.DefaultBinder, null, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetConstructor(BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, null, new ParameterModifier[0]));
        }

        [Fact]
        public void GetConstructor_NullTypeInTypes_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("types", () => type.GetConstructor(new Type[] { null }));
            Assert.Throws<ArgumentNullException>("types", () => type.GetConstructor(BindingFlags.Public, Type.DefaultBinder, new Type[] { null }, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetConstructor(BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { null }, new ParameterModifier[0]));
        }

        public static IEnumerable<object[]> GetConstructors_TypeArray_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new ConstructorInfo[0] };
            yield return new object[] { new ConstructorInfo[] { typeof(TypeTests).GetConstructors()[0] } };
            yield return new object[] { new ConstructorInfo[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetConstructors_TypeArray_TestData))]
        public void GetConstructors_InvokeTypeArray_ReturnsExpected(ConstructorInfo[] result)
        {
            var type = new SubType
            {
                GetConstructorsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetConstructors());
        }

        [Fact]
        public void GetEnumUnderlyingType_Invoke_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.GetEnumUnderlyingType());
        }

        public static IEnumerable<object[]> GetEnumUnderlyingType_Custom_TestData()
        {
            yield return new object[] { new FieldInfo[] { typeof(ConsoleColor).GetFields()[0] }, typeof(int) };
        }

        [Theory]
        [MemberData(nameof(GetEnumUnderlyingType_Custom_TestData))]
        public void GetEnumUnderlyingType_InvokeCustom_ReturnsExpected(FieldInfo[] fields, Type expected)
        {
            var type = new CustomType
            {
                IsSubclassOfAction = c =>
                {
                    Assert.Equal(typeof(Enum), c);
                    return true;
                },
                GetFieldsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, bindingAttrParam);
                    return fields;
                }
            };
            Assert.Equal(expected, type.GetEnumUnderlyingType());
        }

        public static IEnumerable<object[]> GetEnumUnderlyingType_CustomInvalid_TestData()
        {
            yield return new object[] { new FieldInfo[0] };
            yield return new object[] { new FieldInfo[] { typeof(ConsoleColor).GetFields()[0], typeof(ConsoleColor).GetFields()[0] }};
        }

        [Theory]
        [MemberData(nameof(GetEnumUnderlyingType_CustomInvalid_TestData))]
        public void GetEnumUnderlyingType_InvokeCustomInvalid_ThrowsArgumentException(FieldInfo[] fields)
        {
            var type = new CustomType
            {
                IsSubclassOfAction = c =>
                {
                    Assert.Equal(typeof(Enum), c);
                    return true;
                },
                GetFieldsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, bindingAttrParam);
                    return fields;
                }
            };
            Assert.Throws<ArgumentException>("enumType", () => type.GetEnumUnderlyingType());
        }

        [Fact]
        public void GetEnumUnderlyingType_InvokeCustomNullField_ThrowsNullReferenceException()
        {
            var type = new CustomType
            {
                IsSubclassOfAction = c =>
                {
                    Assert.Equal(typeof(Enum), c);
                    return true;
                },
                GetFieldsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, bindingAttrParam);
                    return new FieldInfo[] { null };
                }
            };
            Assert.Throws<NullReferenceException>(() => type.GetEnumUnderlyingType());
        }

        [Fact]
        public void GetEnumValues_Invoke_ThrowsNotImplementedException()
        {
            var type = new SubType();
            Assert.Throws<NotImplementedException>(() => type.GetEnumValues());
        }

        [Fact]
        public void GetEnumValues_InvokeCustom_ReturnsExpected()
        {
            var type = new CustomType
            {
                IsSubclassOfAction = c =>
                {
                    Assert.Equal(typeof(Enum), c);
                    return true;
                }
            };
            Assert.Throws<NotImplementedException>(() => type.GetEnumValues());
        }

        public static IEnumerable<object[]> GetEvent_TypeArray_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, typeof(Console).GetEvents()[0] };
            yield return new object[] { "Event", typeof(Console).GetEvents()[0] };
        }

        [Theory]
        [MemberData(nameof(GetEvent_TypeArray_TestData))]
        public void GetEvent_InvokeTypeArray_ReturnsExpected(string name, EventInfo result)
        {
            var type = new SubType
            {
                GetEventAction = (nameParam, bindingAttrParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetEvent(name));
        }

        public static IEnumerable<object[]> GetEvents_TypeArray_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new EventInfo[0] };
            yield return new object[] { new EventInfo[] { typeof(Console).GetEvents()[0] } };
            yield return new object[] { new EventInfo[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetEvents_TypeArray_TestData))]
        public void GetEvents_InvokeTypeArray_ReturnsExpected(EventInfo[] result)
        {
            var type = new SubType
            {
                GetEventsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetEvents());
        }

        public static IEnumerable<object[]> GetField_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, typeof(ConsoleColor).GetFields()[0] };
            yield return new object[] { "Field", typeof(ConsoleColor).GetFields()[0] };
        }

        [Theory]
        [MemberData(nameof(GetField_TestData))]
        public void GetField_InvokeString_ReturnsExpected(string name, FieldInfo result)
        {
            var type = new SubType
            {
                GetFieldAction = (nameParam, bindingAttrParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetField(name));
        }

        public static IEnumerable<object[]> GetFields_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new FieldInfo[0] };
            yield return new object[] { new FieldInfo[] { typeof(ConsoleColor).GetFields()[0] } };
            yield return new object[] { new FieldInfo[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetFields_TestData))]
        public void GetFields_InvokeTypeArray_ReturnsExpected(FieldInfo[] result)
        {
            var type = new SubType
            {
                GetFieldsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetFields());
        }

        public static IEnumerable<object[]> GetInterface_String_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, typeof(int) };
            yield return new object[] { "Member", typeof(string) };
        }

        [Theory]
        [MemberData(nameof(GetInterface_String_TestData))]
        public void GetInterface_InvokeString_ThrowsNotSupportedException(string name, Type result)
        {
            var type = new SubType
            {
                GetInterfaceAction = (nameParam, ignoreCaseParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.False(ignoreCaseParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetInterface(name));
        }

        public static IEnumerable<object[]> GetMember_String_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { string.Empty };
            yield return new object[] { "Member" };
        }

        [Theory]
        [MemberData(nameof(GetMember_String_TestData))]
        public void GetMember_InvokeString_ThrowsNotSupportedException(string name)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetMember(name));
        }

        public static IEnumerable<object[]> GetMember_StringCustom_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, new MemberInfo[0] };
            yield return new object[] { "Member", new MemberInfo[] { typeof(TypeTests).GetMembers()[0] } };
        }

        [Theory]
        [MemberData(nameof(GetMember_StringCustom_TestData))]
        public void GetMember_InvokeStringCustom_ThrowsNotSupportedException(string name, MemberInfo[] result)
        {
            var type = new CustomType
            {
                GetMemberAction = (nameParam, memberTypesParam, bindingAttrParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(MemberTypes.All, memberTypesParam);
                    Assert.Equal(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMember(name));
        }

        public static IEnumerable<object[]> GetMember_String_BindingFlags_TestData()
        {
            yield return new object[] { null, BindingFlags.Public };
            yield return new object[] { string.Empty, BindingFlags.Public };
            yield return new object[] { "Member", BindingFlags.Public };
        }

        [Theory]
        [MemberData(nameof(GetMember_String_BindingFlags_TestData))]
        public void GetMember_InvokeStringBindingFlags_ThrowsNotSupportedException(string name, BindingFlags bindingAttr)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetMember(name, bindingAttr));
        }

        public static IEnumerable<object[]> GetMember_String_BindingFlagsCustom_TestData()
        {
            yield return new object[] { null, BindingFlags.Public, null };
            yield return new object[] { string.Empty, BindingFlags.Public, new MemberInfo[0] };
            yield return new object[] { "Member", BindingFlags.Public, new MemberInfo[] { typeof(TypeTests).GetMembers()[0] } };
        }

        [Theory]
        [MemberData(nameof(GetMember_String_BindingFlagsCustom_TestData))]
        public void GetMember_InvokeStringBindingFlagsCustom_ThrowsNotSupportedException(string name, BindingFlags bindingAttr, MemberInfo[] result)
        {
            var type = new CustomType
            {
                GetMemberAction = (nameParam, memberTypesParam, bindingAttrParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(MemberTypes.All, memberTypesParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMember(name, bindingAttr));
        }

        public static IEnumerable<object[]> GetMember_String_MemberTypes_BindingFlags_TestData()
        {
            yield return new object[] { null, MemberTypes.All, BindingFlags.Public };
            yield return new object[] { string.Empty, MemberTypes.All, BindingFlags.Public };
            yield return new object[] { "Member", MemberTypes.Constructor - 1, BindingFlags.Public };
        }

        [Theory]
        [MemberData(nameof(GetMember_String_MemberTypes_BindingFlags_TestData))]
        public void GetMember_InvokeStringMemberTypesBindingFlags_ThrowsNotSupportedException(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            var t = new SubType();
            Assert.Throws<NotSupportedException>(() => t.GetMember(name, type, bindingAttr));
        }

        public static IEnumerable<object[]> GetMembers_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new MemberInfo[0] };
            yield return new object[] { new MemberInfo[] { typeof(List<int>).GetMembers()[0] } };
            yield return new object[] { new MemberInfo[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetMembers_TestData))]
        public void GetMembers_Invoke_ReturnsExpected(MemberInfo[] result)
        {
            var type = new SubType
            {
                GetMembersAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMembers());
        }

        public static IEnumerable<object[]> GetMethod_String_TestData()
        {
            yield return new object[] { string.Empty, null };
            yield return new object[] { "Name", typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_TestData))]
        public void GetMethod_InvokeString_ReturnsExpected(string name, MethodInfo result)
        {
            var type = new SubType
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Null(typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name));
        }

        public static IEnumerable<object[]> GetMethod_String_BindingFlags_TestData()
        {
            yield return new object[] { string.Empty, BindingFlags.Public, null };
            yield return new object[] { "Name", BindingFlags.Public, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_BindingFlags_TestData))]
        public void GetMethod_InvokeStringBindingFlags_ReturnsExpected(string name, BindingFlags bindingAttr, MethodInfo result)
        {
            var type = new SubType
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Null(typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, bindingAttr));
        }

        public static IEnumerable<object[]> GetMethod_String_TypeArray_TestData()
        {
            yield return new object[] { string.Empty, Array.Empty<Type>(), null };
            yield return new object[] { "Name", new Type[] { typeof(int) }, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_TypeArray_TestData))]
        public void GetMethod_InvokeStringTypeArray_ReturnsExpected(string name, Type[] types, MethodInfo result)
        {
            var type = new SubType
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, types));
        }

        public static IEnumerable<object[]> GetMethod_String_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, Array.Empty<Type>(), null, null };
            yield return new object[] { "Name", new Type[] { typeof(int) }, new ParameterModifier[0], typeof(TypeTests).GetMethods()[0] };
            yield return new object[] { "Name", new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_TypeArray_ParameterModifierArray_TestData))]
        public void GetMethod_InvokeStringTypeArrayParameterModifierArray_ReturnsExpected(string name, Type[] types, ParameterModifier[] modifiers, MethodInfo result)
        {
            var type = new SubType
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_BindingFlags_Binder_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, BindingFlags.Public, null, Array.Empty<Type>(), null, null };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(int) }, new ParameterModifier[0], typeof(TypeTests).GetMethods()[0] };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_BindingFlags_Binder_TypeArray_ParameterModifierArray_TestData))]
        public void GetMethod_InvokeStringBindingFlagsBinderTypeArrayParameterModifierArray_ReturnsExpected(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers, MethodInfo result)
        {
            var type = new SubType
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, bindingAttr, binder, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, BindingFlags.Public, null, CallingConventions.Any, Array.Empty<Type>(), null, null };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { typeof(int) }, new ParameterModifier[0], typeof(TypeTests).GetMethods()[0] };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArray_TestData))]
        public void GetMethod_InvokeStringBindingFlagsBinderCallingConventionsTypeArrayParameterModifierArray_ReturnsExpected(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers, MethodInfo result)
        {
            var type = new SubType
            {
                GetMethodImplAction = (nameParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Equal(callConvention, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_Int_TypeArray_TestData()
        {
            yield return new object[] { string.Empty, 0, Array.Empty<Type>() };
            yield return new object[] { "Name", 1, new Type[] { typeof(int) } };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_Int_TypeArray_TestData))]
        public void GetMethod_InvokeStringIntTypeArray_ReturnsExpected(string name, int genericParameterCount, Type[] types)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetMethod(name, genericParameterCount, types));
        }

        public static IEnumerable<object[]> GetMethod_String_Int_TypeArrayCustom_TestData()
        {
            yield return new object[] { string.Empty, 0, Array.Empty<Type>(), null };
            yield return new object[] { "Name", 1, new Type[] { typeof(int) }, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_Int_TypeArrayCustom_TestData))]
        public void GetMethod_InvokeStringTypeArrayCustom_ReturnsExpected(string name, int genericParameterCount, Type[] types, MethodInfo result)
        {
            var type = new CustomType
            {
                GetMethodImplAction2 = (nameParam, genericParameterCountParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(genericParameterCount, genericParameterCountParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, genericParameterCount, types));
        }

        public static IEnumerable<object[]> GetMethod_String_Int_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, 0, Array.Empty<Type>(), null };
            yield return new object[] { "Name", 1, new Type[] { typeof(int) }, new ParameterModifier[0] };
            yield return new object[] { "Name", 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() } };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_Int_TypeArray_ParameterModifierArray_TestData))]
        public void GetMethod_InvokeStringIntTypeArrayParameterModifierArray_ReturnsExpected(string name, int genericParameterCount, Type[] types, ParameterModifier[] modifiers)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetMethod(name, genericParameterCount, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_Int_TypeArray_ParameterModifierArrayCustom_TestData()
        {
            yield return new object[] { string.Empty, 0, Array.Empty<Type>(), null, null };
            yield return new object[] { "Name", 1, new Type[] { typeof(int) }, new ParameterModifier[0], typeof(TypeTests).GetMethods()[0] };
            yield return new object[] { "Name", 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_Int_TypeArray_ParameterModifierArrayCustom_TestData))]
        public void GetMethod_InvokeStringTypeArrayParameterModifierArrayCustom_ReturnsExpected(string name, int genericParameterCount, Type[] types, ParameterModifier[] modifiers, MethodInfo result)
        {
            var type = new CustomType
            {
                GetMethodImplAction2 = (nameParam, genericParameterCountParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(genericParameterCount, genericParameterCountParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, genericParameterCount, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_Int_BindingFlags_Binder_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, 0, BindingFlags.Public, null, Array.Empty<Type>(), null };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(int) }, new ParameterModifier[0] };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() } };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_Int_BindingFlags_Binder_TypeArray_ParameterModifierArray_TestData))]
        public void GetMethod_InvokeStringIntBindingFlagsBinderTypeArrayParameterModifierArray_ReturnsExpected(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetMethod(name, genericParameterCount, bindingAttr, binder, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_Int_BindingFlags_Binder_TypeArray_ParameterModifierArrayCustom_TestData()
        {
            yield return new object[] { string.Empty, 0, BindingFlags.Public, null, Array.Empty<Type>(), null, null };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(int) }, new ParameterModifier[0], typeof(TypeTests).GetMethods()[0] };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_Int_BindingFlags_Binder_TypeArray_ParameterModifierArrayCustom_TestData))]
        public void GetMethod_InvokeStringIntBindingFlagsBinderTypeArrayParameterModifierArrayCustom_ReturnsExpected(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers, MethodInfo result)
        {
            var type = new CustomType
            {
                GetMethodImplAction2 = (nameParam, genericParameterCountParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(genericParameterCount, genericParameterCountParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Equal(CallingConventions.Any, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, genericParameterCount, bindingAttr, binder, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_Int_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, 0, BindingFlags.Public, null, CallingConventions.Any, Array.Empty<Type>(), null };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { typeof(int) }, new ParameterModifier[0] };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() } };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_Int_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArray_TestData))]
        public void GetMethod_InvokeStringIntBindingFlagsBinderCallingConventionsTypeArrayParameterModifierArray_ReturnsExpected(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.GetMethod(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethod_String_Int_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArrayCustom_TestData()
        {
            yield return new object[] { string.Empty, 0, BindingFlags.Public, null, CallingConventions.Any, Array.Empty<Type>(), null, null };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { typeof(int) }, new ParameterModifier[0], typeof(TypeTests).GetMethods()[0] };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(TypeTests).GetMethods()[0] };
        }

        [Theory]
        [MemberData(nameof(GetMethod_String_Int_BindingFlags_Binder_CallingConventions_TypeArray_ParameterModifierArrayCustom_TestData))]
        public void GetMethod_InvokeStringIntBindingFlagsBinderCallingConventionsTypeArrayParameterModifierArrayCustom_ReturnsExpected(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers, MethodInfo result)
        {
            var type = new CustomType
            {
                GetMethodImplAction2 = (nameParam, genericParameterCountParam, bindingAttrParam, binderParam, callConventionParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(genericParameterCount, genericParameterCountParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Equal(callConvention, callConventionParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethod(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers));
        }

        [Fact]
        public void GetMethod_NullName_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null));
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null, BindingFlags.Public));
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null, Array.Empty<Type>()));
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null, Array.Empty<Type>(), new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null, BindingFlags.Public, Type.DefaultBinder, Array.Empty<Type>(), new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null, 0, Array.Empty<Type>()));
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null, 0, Array.Empty<Type>(), new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null, 0, BindingFlags.Public, Type.DefaultBinder, Array.Empty<Type>(), new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("name", () => type.GetMethod(null, 0, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, Array.Empty<Type>(), new ParameterModifier[0]));
        }

        [Fact]
        public void GetMethod_NullTypes_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", null));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", null, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", BindingFlags.Public, Type.DefaultBinder, null, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, null, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", 0, null, null));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", 0, null, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", 0, BindingFlags.Public, Type.DefaultBinder, null, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", 0, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, null, new ParameterModifier[0]));
        }

        [Fact]
        public void GetMethod_NullTypeInTypes_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", new Type[] { null }));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", new Type[] { null }, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", BindingFlags.Public, Type.DefaultBinder, new Type[] { null }, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { null }, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", 0, new Type[] { null }));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", 0, new Type[] { null }, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", 0, BindingFlags.Public, Type.DefaultBinder, new Type[] { null }, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetMethod("name", 0, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { null }, new ParameterModifier[0]));
        }

        [Fact]
        public void GetMethod_NegativeGenericParameterCount_ThrowsArgumentException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentException>("genericParameterCount", () => type.GetMethod("name", -1, Array.Empty<Type>()));
            Assert.Throws<ArgumentException>("genericParameterCount", () => type.GetMethod("name", -1, Array.Empty<Type>(), new ParameterModifier[0]));
            Assert.Throws<ArgumentException>("genericParameterCount", () => type.GetMethod("name", -1, BindingFlags.Public, Type.DefaultBinder, Array.Empty<Type>(), new ParameterModifier[0]));
            Assert.Throws<ArgumentException>("genericParameterCount", () => type.GetMethod("name", -1, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, Array.Empty<Type>(), new ParameterModifier[0]));
        }

        public static IEnumerable<object[]> GetMethodImpl_TestData()
        {
            yield return new object[] { null, -1, BindingFlags.Public, null, CallingConventions.Any, null, null };
            yield return new object[] { string.Empty, 0, BindingFlags.Public, null, CallingConventions.Any, Array.Empty<Type>(), null };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Any, new Type[] { null }, new ParameterModifier[0] };
            yield return new object[] { "Name", 1, BindingFlags.Public, Type.DefaultBinder, CallingConventions.Standard - 1, new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() } };
        }

        [Theory]
        [MemberData(nameof(GetMethodImpl_TestData))]
        public void GetMethodImpl_Invoke_ThrowsNotSupportedException(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            var type = new ProtectedType();
            Assert.Throws<NotSupportedException>(() => type.GetMethodImpl(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers));
        }

        public static IEnumerable<object[]> GetMethods_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new MethodInfo[0] };
            yield return new object[] { new MethodInfo[] { typeof(List<int>).GetMethods()[0] } };
            yield return new object[] { new MethodInfo[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetMethods_TestData))]
        public void GetMethods_Invoke_ReturnsExpected(MethodInfo[] result)
        {
            var type = new SubType
            {
                GetMethodsAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetMethods());
        }

        public static IEnumerable<object[]> GetNestedType_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, typeof(List<int>).GetNestedTypes()[0] };
            yield return new object[] { "NestedType", typeof(List<int>).GetNestedTypes()[0] };
        }

        [Theory]
        [MemberData(nameof(GetNestedType_TestData))]
        public void GetNestedType_InvokeTypeArray_ReturnsExpected(string name, Type result)
        {
            var type = new SubType
            {
                GetNestedTypeAction = (nameParam, bindingAttrParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetNestedType(name));
        }

        public static IEnumerable<object[]> GetNestedTypes_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { Array.Empty<Type>() };
            yield return new object[] { new Type[] { typeof(List<int>).GetNestedTypes()[0] } };
            yield return new object[] { new Type[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetNestedTypes_TestData))]
        public void GetNestedTypes_Invoke_ReturnsExpected(Type[] result)
        {
            var type = new SubType
            {
                GetNestedTypesAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetNestedTypes());
        }

        public static IEnumerable<object[]> GetProperty_String_TestData()
        {
            yield return new object[] { string.Empty, null };
            yield return new object[] { "Name", typeof(List<int>).GetProperties()[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperty_String_TestData))]
        public void GetProperty_InvokeString_ReturnsExpected(string name, PropertyInfo result)
        {
            var type = new SubType
            {
                GetPropertyImplAction = (nameParam, bindingAttrParam, binderParam, returnTypeParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Null(returnTypeParam);
                    Assert.Null(typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetProperty(name));
        }

        public static IEnumerable<object[]> GetProperty_String_BindingFlags_TestData()
        {
            yield return new object[] { string.Empty, BindingFlags.Public, null };
            yield return new object[] { "Name", BindingFlags.Public, typeof(List<int>).GetProperties()[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperty_String_BindingFlags_TestData))]
        public void GetProperty_InvokeStringBindingFlags_ReturnsExpected(string name, BindingFlags bindingAttr, PropertyInfo result)
        {
            var type = new SubType
            {
                GetPropertyImplAction = (nameParam, bindingAttrParam, binderParam, returnTypeParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Null(returnTypeParam);
                    Assert.Null(typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetProperty(name, bindingAttr));
        }

        public static IEnumerable<object[]> GetProperty_String_Type_TestData()
        {
            yield return new object[] { string.Empty, null, null };
            yield return new object[] { "Name", typeof(int), typeof(List<int>).GetProperties()[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperty_String_Type_TestData))]
        public void GetProperty_InvokeStringType_ReturnsExpected(string name, Type returnType, PropertyInfo result)
        {
            var type = new SubType
            {
                GetPropertyImplAction = (nameParam, bindingAttrParam, binderParam, returnTypeParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Same(returnType, returnTypeParam);
                    Assert.Null(typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetProperty(name, returnType));
        }

        public static IEnumerable<object[]> GetProperty_String_TypeArray_TestData()
        {
            yield return new object[] { string.Empty, Array.Empty<Type>(), null };
            yield return new object[] { "Name", new Type[] { null }, typeof(List<int>).GetProperties()[0] };
            yield return new object[] { "Name", new Type[] { typeof(int) }, typeof(List<int>).GetProperties()[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperty_String_TypeArray_TestData))]
        public void GetProperty_InvokeStringTypeArray_ReturnsExpected(string name, Type[] types, PropertyInfo result)
        {
            var type = new SubType
            {
                GetPropertyImplAction = (nameParam, bindingAttrParam, binderParam, returnTypeParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Null(returnTypeParam);
                    Assert.Same(types, typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetProperty(name, types));
        }

        public static IEnumerable<object[]> GetProperty_String_Type_TypeArray_TestData()
        {
            yield return new object[] { string.Empty, null, Array.Empty<Type>(), null };
            yield return new object[] { "Name", typeof(string), new Type[] { null }, typeof(List<int>).GetProperties()[0] };
            yield return new object[] { "Name", typeof(string), new Type[] { typeof(int) }, typeof(List<int>).GetProperties()[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperty_String_Type_TypeArray_TestData))]
        public void GetProperty_InvokeStringTypeTypeArray_ReturnsExpected(string name, Type returnType, Type[] types, PropertyInfo result)
        {
            var type = new SubType
            {
                GetPropertyImplAction = (nameParam, bindingAttrParam, binderParam, returnTypeParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Same(returnType, returnTypeParam);
                    Assert.Same(types, typesParam);
                    Assert.Null(modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetProperty(name, returnType, types));
        }

        public static IEnumerable<object[]> GetProperty_String_Type_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, null, Array.Empty<Type>(), null, null };
            yield return new object[] { "Name", typeof(string), new Type[] { null }, new ParameterModifier[0], typeof(List<int>).GetProperties()[0] };
            yield return new object[] { "Name", typeof(string), new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(List<int>).GetProperties()[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperty_String_Type_TypeArray_ParameterModifierArray_TestData))]
        public void GetProperty_InvokeStringTypeTypeArrayParameterModifierArray_ReturnsExpected(string name, Type returnType, Type[] types, ParameterModifier[] modifiers, PropertyInfo result)
        {
            var type = new SubType
            {
                GetPropertyImplAction = (nameParam, bindingAttrParam, binderParam, returnTypeParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    Assert.Null(binderParam);
                    Assert.Same(returnType, returnTypeParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetProperty(name, returnType, types, modifiers));
        }

        public static IEnumerable<object[]> GetProperty_String_BindingFlags_Binder_Type_TypeArray_ParameterModifierArray_TestData()
        {
            yield return new object[] { string.Empty, BindingFlags.Public, null, null, Array.Empty<Type>(), null, null };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, typeof(string), new Type[] { null }, new ParameterModifier[0], typeof(List<int>).GetProperties()[0] };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, typeof(string), new Type[] { typeof(int) }, new ParameterModifier[] { new ParameterModifier() }, typeof(List<int>).GetProperties()[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperty_String_BindingFlags_Binder_Type_TypeArray_ParameterModifierArray_TestData))]
        public void GetProperty_InvokeStringBindingFlagsBinderTypeTypeArrayParameterModifierArray_ReturnsExpected(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers, PropertyInfo result)
        {
            var type = new SubType
            {
                GetPropertyImplAction = (nameParam, bindingAttrParam, binderParam, returnTypeParam, typesParam, modifiersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(bindingAttr, bindingAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Same(returnType, returnTypeParam);
                    Assert.Same(types, typesParam);
                    Assert.Same(modifiers, modifiersParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetProperty(name, bindingAttr, binder, returnType, types, modifiers));
        }

        [Fact]
        public void GetProperty_NullName_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("name", () => type.GetProperty(null));
            Assert.Throws<ArgumentNullException>("name", () => type.GetProperty(null, BindingFlags.Public));
            Assert.Throws<ArgumentNullException>("name", () => type.GetProperty(null, typeof(int)));
            Assert.Throws<ArgumentNullException>("name", () => type.GetProperty(null, Array.Empty<Type>()));
            Assert.Throws<ArgumentNullException>("name", () => type.GetProperty(null, typeof(int), Array.Empty<Type>()));
            Assert.Throws<ArgumentNullException>("name", () => type.GetProperty(null, typeof(int), Array.Empty<Type>(), new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("name", () => type.GetProperty(null, BindingFlags.Public, Type.DefaultBinder, typeof(int), Array.Empty<Type>(), new ParameterModifier[0]));
        }

        [Fact]
        public void GetProperty_NullTypes_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("types", () => type.GetProperty("name", (Type[])null));
            Assert.Throws<ArgumentNullException>("types", () => type.GetProperty("name", typeof(int), null));
            Assert.Throws<ArgumentNullException>("types", () => type.GetProperty("name", typeof(int), null, new ParameterModifier[0]));
            Assert.Throws<ArgumentNullException>("types", () => type.GetProperty("name", BindingFlags.Public, Type.DefaultBinder, typeof(int), null, new ParameterModifier[0]));
        }

        public static IEnumerable<object[]> GetProperties_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new PropertyInfo[0] };
            yield return new object[] { new PropertyInfo[] { typeof(List<int>).GetProperties()[0] } };
            yield return new object[] { new PropertyInfo[] { null } };
        }

        [Theory]
        [MemberData(nameof(GetProperties_TestData))]
        public void GetProperties_Invoke_ReturnsExpected(PropertyInfo[] result)
        {
            var type = new SubType
            {
                GetPropertiesAction = (bindingAttrParam) =>
                {
                    Assert.Equal(BindingFlags.Instance| BindingFlags.Static | BindingFlags.Public, bindingAttrParam);
                    return result;
                }
            };
            Assert.Same(result, type.GetProperties());
        }

        public static IEnumerable<object[]> InvokeMember_String_BindingFlags_Binder_Object_ObjectArray_TestData()
        {
            yield return new object[] { null, BindingFlags.Public, null, null, null, null };
            yield return new object[] { string.Empty, BindingFlags.Public, Type.DefaultBinder, new object(), Array.Empty<object>(), new object() };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, new object(), new object[] { null }, new object() };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, new object(), new object[] { new object() }, new object() };
        }

        [Theory]
        [MemberData(nameof(InvokeMember_String_BindingFlags_Binder_Object_ObjectArray_TestData))]
        public void InvokeMember_InvokeStringBindingFlagsBinderObjectObjectArray_ReturnsExpected(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, object result)
        {
            var type = new SubType
            {
                InvokeMemberAction = (nameParam, invokeAttrParam, binderParam, targetParam, argsParam, modifiersParam, cultureParam, namedParametersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(invokeAttr, invokeAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Same(target, targetParam);
                    Assert.Same(args, argsParam);
                    Assert.Null(modifiersParam);
                    Assert.Null(cultureParam);
                    Assert.Null(namedParametersParam);
                    return result;
                }
            };
            Assert.Same(result, type.InvokeMember(name, invokeAttr, binder, target, args));
        }

        public static IEnumerable<object[]> InvokeMember_String_BindingFlags_Binder_Object_ObjectArray_CultureInfo_TestData()
        {
            yield return new object[] { null, BindingFlags.Public, null, null, null, null, null };
            yield return new object[] { string.Empty, BindingFlags.Public, Type.DefaultBinder, new object(), Array.Empty<object>(), CultureInfo.InvariantCulture, new object() };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, new object(), new object[] { null }, new CultureInfo("en-US"), new object() };
            yield return new object[] { "Name", BindingFlags.Public, Type.DefaultBinder, new object(), new object[] { new object() }, new CultureInfo("en-US"), new object() };
        }

        [Theory]
        [MemberData(nameof(InvokeMember_String_BindingFlags_Binder_Object_ObjectArray_CultureInfo_TestData))]
        public void InvokeMember_InvokeStringBindingFlagsBinderObjectObjectArrayCultureInfo_ReturnsExpected(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, CultureInfo culture, object result)
        {
            var type = new SubType
            {
                InvokeMemberAction = (nameParam, invokeAttrParam, binderParam, targetParam, argsParam, modifiersParam, cultureParam, namedParametersParam) =>
                {
                    Assert.Equal(name, nameParam);
                    Assert.Equal(invokeAttr, invokeAttrParam);
                    Assert.Same(binder, binderParam);
                    Assert.Same(target, targetParam);
                    Assert.Same(args, argsParam);
                    Assert.Null(modifiersParam);
                    Assert.Same(culture, cultureParam);
                    Assert.Null(namedParametersParam);
                    return result;
                }
            };
            Assert.Same(result, type.InvokeMember(name, invokeAttr, binder, target, args, culture));
        }

        [Fact]
        public void IsEquivalentTo_Invoke_ReturnsExpected()
        {
            var type = new SubType();
            Assert.True(type.IsEquivalentTo(type));
            Assert.False(type.IsEquivalentTo(typeof(object)));
            Assert.False(type.IsEquivalentTo(null));
        }

        [Fact]
        public void IsInstanceOfType_Invoke_ReturnsExpected()
        {
            var type = new SubType();
            Assert.False(type.IsInstanceOfType(type));
            Assert.False(type.IsInstanceOfType(new CustomType()));
            Assert.False(type.IsInstanceOfType(new object()));
            Assert.False(type.IsInstanceOfType(new TypeTests()));
            Assert.False(type.IsInstanceOfType(null));
        }

        [Fact]
        public void IsInstanceOfType_InvokeCustomUnderlyingSystemType_ReturnsExpected()
        {
            var type = new SubType
            {
                UnderlyingSystemTypeResult = typeof(SubType)
            };
            Assert.True(type.IsInstanceOfType(type));
            Assert.True(type.IsInstanceOfType(new CustomType()));
            Assert.False(type.IsInstanceOfType(new object()));
            Assert.False(type.IsInstanceOfType(new TypeTests()));
            Assert.False(type.IsInstanceOfType(null));
        }

        [Fact]
        public void MakeArrayType_Invoke_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.MakeArrayType());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(255)]
        [InlineData(256)]
        public void MakeArrayType_InvokeInt_ThrowsNotSupportedException(int rank)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.MakeArrayType(rank));
        }

        [Fact]
        public void MakeByRefType_Invoke_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.MakeByRefType());
        }
        [Theory]
        [InlineData(0, "!!0")]
        [InlineData(1, "!!1")]
        public void MakeGenericMethodParameter_Invoke_Success(int position, string expectedToString)
        {
            var type = new SubType();
            Type t = Type.MakeGenericMethodParameter(position);
            Assert.Throws<NotSupportedException>(() => t.Assembly);
            Assert.Null(t.AssemblyQualifiedName);
            Assert.Throws<NotSupportedException>(() => t.Attributes);
            Assert.Throws<NotSupportedException>(() => t.BaseType);
            Assert.True(t.ContainsGenericParameters);
            Assert.Throws<NotSupportedException>(() => t.CustomAttributes);
            Assert.Throws<NotSupportedException>(() => t.DeclaringMethod);
            Assert.Throws<NotSupportedException>(() => t.DeclaringType);
            Assert.Null(t.FullName);
            Assert.Same(t.GenericTypeArguments, t.GenericTypeArguments);
            Assert.Empty(t.GenericTypeArguments);
            Assert.Throws<NotSupportedException>(() => t.GenericParameterAttributes);
            Assert.Equal(position, t.GenericParameterPosition);
            Assert.Throws<NotSupportedException>(() => t.GUID);
            Assert.False(t.IsArray);
            Assert.False(t.IsByRef);
            Assert.False(t.IsByRefLike);
            Assert.Throws<NotSupportedException>(() => t.IsCOMObject);
            Assert.False(t.IsConstructedGenericType);
            Assert.Throws<NotSupportedException>(() => t.IsContextful);
            Assert.Throws<NotSupportedException>(() => t.IsEnum);
            Assert.True(t.IsGenericParameter);
            Assert.True(t.IsGenericMethodParameter);
            Assert.False(t.IsGenericType);
            Assert.False(t.IsGenericTypeDefinition);
            Assert.False(t.IsGenericTypeParameter);
            Assert.Throws<NotSupportedException>(() => t.IsMarshalByRef);
            Assert.Throws<NotSupportedException>(() => t.IsPrimitive);
            Assert.Throws<NotSupportedException>(() => t.IsSecurityCritical);
            Assert.Throws<NotSupportedException>(() => t.IsSecuritySafeCritical);
            Assert.Throws<NotSupportedException>(() => t.IsSecurityTransparent);
            Assert.Throws<NotSupportedException>(() => t.IsSerializable);
            Assert.True(t.IsSignatureType);
            Assert.False(t.IsSZArray);
            Assert.False(t.IsTypeDefinition);
            Assert.Throws<NotSupportedException>(() => t.IsValueType);
            Assert.False(t.IsVariableBoundArray);
            Assert.False(t.HasElementType);
            Assert.Equal(MemberTypes.TypeInfo, t.MemberType);
            Assert.Throws<NotSupportedException>(() => t.MetadataToken);
            Assert.Throws<NotSupportedException>(() => t.Module);
            Assert.Equal(expectedToString, t.Name);
            Assert.Null(t.Namespace);
            Assert.Throws<NotSupportedException>(() => t.ReflectedType);
            Assert.Throws<NotSupportedException>(() => t.StructLayoutAttribute);
            Assert.Throws<NotSupportedException>(() => t.TypeHandle);
            Assert.Same(t, t.UnderlyingSystemType);
            Assert.Throws<NotSupportedException>(() => t.FindInterfaces(null, null));
            Assert.Throws<NotSupportedException>(() => t.FindMembers(MemberTypes.All, BindingFlags.Public, null, null));
            Assert.Throws<ArgumentException>(null, () => t.GetArrayRank());
            Assert.Throws<NotSupportedException>(() => t.GetConstructors());
            Assert.Throws<NotSupportedException>(() => t.GetCustomAttributesData());
            Assert.Throws<NotSupportedException>(() => t.GetCustomAttributes(true));
            Assert.Throws<NotSupportedException>(() => t.GetCustomAttributes(typeof(int), true));
            Assert.Throws<NotSupportedException>(() => t.GetDefaultMembers());
            Assert.Null(t.GetElementType());
            Assert.Throws<NotSupportedException>(() => t.GetEnumName(1));
            Assert.Throws<NotSupportedException>(() => t.GetEnumNames());
            Assert.Throws<NotSupportedException>(() => t.GetEnumUnderlyingType());
            Assert.Throws<NotSupportedException>(() => t.GetEnumValues());
            Assert.Throws<NotSupportedException>(() => t.GetEvent("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetEvent("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetEvents());
            Assert.Throws<NotSupportedException>(() => t.GetField("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetField("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetFields());
            Assert.Throws<InvalidOperationException>(() => t.GetGenericTypeDefinition());
            Assert.Same(t.GetGenericArguments(), t.GetGenericArguments());
            Assert.Empty(t.GetGenericArguments());
            Assert.Throws<NotSupportedException>(() => t.GetGenericParameterConstraints());
            Assert.Throws<NotSupportedException>(() => t.GetInterface("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetInterfaceMap(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.GetInterfaces());
            Assert.Throws<NotSupportedException>(() => t.GetMember("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetMember("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetMember("Name", MemberTypes.All, BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetMembers());
            Assert.Throws<NotSupportedException>(() => t.GetMethod("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetMethod("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetMethod("Name", 0, Array.Empty<Type>()));
            Assert.Throws<NotSupportedException>(() => t.GetMethods());
            Assert.Throws<NotSupportedException>(() => t.GetNestedType("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetNestedType("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetNestedTypes());
            Assert.Throws<NotSupportedException>(() => t.GetProperty("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetProperty("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetProperties());
            Assert.Throws<NotSupportedException>(() => t.HasSameMetadataDefinitionAs(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.IsAssignableFrom(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.IsDefined(typeof(Attribute)));
            Assert.Throws<NotSupportedException>(() => t.IsDefined(typeof(int), true));
            Assert.Throws<NotSupportedException>(() => t.IsEnumDefined(1));
            Assert.Throws<NotSupportedException>(() => t.IsEquivalentTo(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.IsInstanceOfType(new object()));
            Assert.Throws<NotSupportedException>(() => t.IsSubclassOf(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.InvokeMember("Name", BindingFlags.Public, Type.DefaultBinder, new object(), Array.Empty<object>()));
            Assert.Throws<IndexOutOfRangeException>(() => t.MakeArrayType(-1));
            Assert.Throws<IndexOutOfRangeException>(() => t.MakeArrayType(0));
            Assert.NotNull(t.MakeArrayType(1));
            Assert.NotNull(t.MakeByRefType());
            Assert.NotNull(t.MakePointerType());
            Assert.Throws<NotSupportedException>(() => t.MakeGenericType(typeof(int)));
            Assert.Equal(expectedToString, t.ToString());
            Assert.Throws<NotSupportedException>(() => Type.GetTypeCode(t));
        }

        [Fact]
        public void MakeGenericMethodParameter_NegativePosition_ThrowsArgumentException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentException>("position", () => Type.MakeGenericMethodParameter(-1));
        }

        public static IEnumerable<object[]> MakeGenericSignatureType_TestData()
        {
            yield return new object[] { typeof(string), Array.Empty<Type>(), "[]" };
            yield return new object[] { typeof(int).MakeByRefType(), Array.Empty<Type>(), "[]" };
            yield return new object[] { typeof(int).MakePointerType(), Array.Empty<Type>(), "[]" };
            yield return new object[] { typeof(int[]), Array.Empty<Type>(), "[]" };
            yield return new object[] { typeof(int[,]), Array.Empty<Type>(), "[]" };
            yield return new object[] { typeof(Span<int>), Array.Empty<Type>(), "[]" };
            yield return new object[] { typeof(List<>), Array.Empty<Type>(), "[]" };
            yield return new object[] { typeof(List<int>), Array.Empty<Type>(), "[]" };
            yield return new object[] { typeof(List<int>), new Type[] { typeof(string) }, "[System.String]" };
            yield return new object[] { typeof(List<int>), new Type[] { typeof(string), typeof(string) }, "[System.String,System.String]" };
            yield return new object[] { typeof(List<int>), new Type[] { typeof(List<>) }, $"[{typeof(List<>).ToString()}]" };
            yield return new object[] { typeof(List<int>), new Type[] { typeof(List<>).MakeGenericType(typeof(List<>)) }, $"[{typeof(List<>).MakeGenericType(typeof(List<>)).ToString()}]" };
            yield return new object[] { typeof(List<int>), new Type[] { typeof(List<int>) }, $"[{typeof(List<int>).ToString()}]" };
        }

        [Theory]
        [MemberData(nameof(MakeGenericSignatureType_TestData))]
        public void MakeGenericSignatureType_Invoke_Success(Type genericTypeDefinition, Type[] typeArguments, string expectedToString)
        {
            var type = new SubType();
            Type t = Type.MakeGenericSignatureType(genericTypeDefinition, typeArguments);
            Assert.Throws<NotSupportedException>(() => t.Assembly);
            Assert.Null(t.AssemblyQualifiedName);
            Assert.Throws<NotSupportedException>(() => t.Attributes);
            Assert.Throws<NotSupportedException>(() => t.BaseType);
            Assert.Equal(typeArguments.Any(t => t.ContainsGenericParameters), t.ContainsGenericParameters);
            Assert.Throws<NotSupportedException>(() => t.CustomAttributes);
            Assert.Throws<NotSupportedException>(() => t.DeclaringMethod);
            Assert.Throws<NotSupportedException>(() => t.DeclaringType);
            Assert.Null(t.FullName);
            Assert.NotSame(t.GenericTypeArguments, t.GenericTypeArguments);
            Assert.NotSame(typeArguments, t.GenericTypeArguments);
            Assert.Equal(typeArguments, t.GenericTypeArguments);
            Assert.Throws<NotSupportedException>(() => t.GenericParameterAttributes);
            Assert.Throws<InvalidOperationException>(() => t.GenericParameterPosition);
            Assert.Throws<NotSupportedException>(() => t.GUID);
            Assert.False(t.IsArray);
            Assert.False(t.IsByRef);
            Assert.Equal(genericTypeDefinition.IsByRefLike, t.IsByRefLike);
            Assert.Throws<NotSupportedException>(() => t.IsCOMObject);
            Assert.True(t.IsConstructedGenericType);
            Assert.Throws<NotSupportedException>(() => t.IsContextful);
            Assert.Throws<NotSupportedException>(() => t.IsEnum);
            Assert.False(t.IsGenericParameter);
            Assert.False(t.IsGenericMethodParameter);
            Assert.True(t.IsGenericType);
            Assert.False(t.IsGenericTypeDefinition);
            Assert.False(t.IsGenericTypeParameter);
            Assert.Throws<NotSupportedException>(() => t.IsMarshalByRef);
            Assert.Throws<NotSupportedException>(() => t.IsPrimitive);
            Assert.Throws<NotSupportedException>(() => t.IsSecurityCritical);
            Assert.Throws<NotSupportedException>(() => t.IsSecuritySafeCritical);
            Assert.Throws<NotSupportedException>(() => t.IsSecurityTransparent);
            Assert.Throws<NotSupportedException>(() => t.IsSerializable);
            Assert.True(t.IsSignatureType);
            Assert.False(t.IsSZArray);
            Assert.False(t.IsTypeDefinition);
            Assert.Throws<NotSupportedException>(() => t.IsValueType);
            Assert.False(t.IsVariableBoundArray);
            Assert.False(t.HasElementType);
            Assert.Equal(MemberTypes.TypeInfo, t.MemberType);
            Assert.Throws<NotSupportedException>(() => t.MetadataToken);
            Assert.Throws<NotSupportedException>(() => t.Module);
            Assert.Equal(genericTypeDefinition.Name, t.Name);
            Assert.Equal(genericTypeDefinition.Namespace, t.Namespace);
            Assert.Throws<NotSupportedException>(() => t.ReflectedType);
            Assert.Throws<NotSupportedException>(() => t.StructLayoutAttribute);
            Assert.Throws<NotSupportedException>(() => t.TypeHandle);
            Assert.Same(t, t.UnderlyingSystemType);
            Assert.Throws<NotSupportedException>(() => t.FindInterfaces(null, null));
            Assert.Throws<NotSupportedException>(() => t.FindMembers(MemberTypes.All, BindingFlags.Public, null, null));
            Assert.Throws<ArgumentException>(null, () => t.GetArrayRank());
            Assert.Throws<NotSupportedException>(() => t.GetConstructors());
            Assert.Throws<NotSupportedException>(() => t.GetCustomAttributesData());
            Assert.Throws<NotSupportedException>(() => t.GetCustomAttributes(true));
            Assert.Throws<NotSupportedException>(() => t.GetCustomAttributes(typeof(int), true));
            Assert.Throws<NotSupportedException>(() => t.GetDefaultMembers());
            Assert.Null(t.GetElementType());
            Assert.Throws<NotSupportedException>(() => t.GetEnumName(1));
            Assert.Throws<NotSupportedException>(() => t.GetEnumNames());
            Assert.Throws<NotSupportedException>(() => t.GetEnumUnderlyingType());
            Assert.Throws<NotSupportedException>(() => t.GetEnumValues());
            Assert.Throws<NotSupportedException>(() => t.GetEvent("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetEvent("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetEvents());
            Assert.Throws<NotSupportedException>(() => t.GetField("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetField("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetFields());
            Assert.Same(genericTypeDefinition, t.GetGenericTypeDefinition());
            Assert.NotSame(t.GetGenericArguments(), t.GetGenericArguments());
            Assert.NotSame(typeArguments, t.GetGenericArguments());
            Assert.Equal(typeArguments, t.GetGenericArguments());
            Assert.Throws<NotSupportedException>(() => t.GetGenericParameterConstraints());
            Assert.Throws<NotSupportedException>(() => t.GetInterface("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetInterfaceMap(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.GetInterfaces());
            Assert.Throws<NotSupportedException>(() => t.GetMember("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetMember("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetMember("Name", MemberTypes.All, BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetMembers());
            Assert.Throws<NotSupportedException>(() => t.GetMethod("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetMethod("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetMethod("Name", 0, Array.Empty<Type>()));
            Assert.Throws<NotSupportedException>(() => t.GetMethods());
            Assert.Throws<NotSupportedException>(() => t.GetNestedType("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetNestedType("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetNestedTypes());
            Assert.Throws<NotSupportedException>(() => t.GetProperty("Name"));
            Assert.Throws<NotSupportedException>(() => t.GetProperty("Name", BindingFlags.Public));
            Assert.Throws<NotSupportedException>(() => t.GetProperties());
            Assert.Throws<NotSupportedException>(() => t.HasSameMetadataDefinitionAs(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.IsAssignableFrom(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.IsDefined(typeof(Attribute)));
            Assert.Throws<NotSupportedException>(() => t.IsDefined(typeof(int), true));
            Assert.Throws<NotSupportedException>(() => t.IsEnumDefined(1));
            Assert.Throws<NotSupportedException>(() => t.IsEquivalentTo(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.IsInstanceOfType(new object()));
            Assert.Throws<NotSupportedException>(() => t.IsSubclassOf(typeof(int)));
            Assert.Throws<NotSupportedException>(() => t.InvokeMember("Name", BindingFlags.Public, Type.DefaultBinder, new object(), Array.Empty<object>()));
            Assert.Throws<IndexOutOfRangeException>(() => t.MakeArrayType(-1));
            Assert.Throws<IndexOutOfRangeException>(() => t.MakeArrayType(0));
            Assert.NotNull(t.MakeArrayType(1));
            Assert.NotNull(t.MakeByRefType());
            Assert.NotNull(t.MakePointerType());
            Assert.Throws<NotSupportedException>(() => t.MakeGenericType(typeof(int)));
            Assert.Equal(genericTypeDefinition.ToString() + expectedToString, t.ToString());
            Assert.Throws<NotSupportedException>(() => Type.GetTypeCode(t));
        }

        [Fact]
        public void MakeGenericSignatureType_NullGenericTypeDefinition_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("genericTypeDefinition", () => Type.MakeGenericSignatureType(null, Array.Empty<Type>()));
        }

        [Fact]
        public void MakeGenericSignatureType_NullTypeArguments_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("typeArguments", () => Type.MakeGenericSignatureType(typeof(List<>), null));
        }

        [Fact]
        public void MakeGenericSignatureType_NullTypeInTypeArguments_ThrowsArgumentNullException()
        {
            var type = new SubType();
            Assert.Throws<ArgumentNullException>("typeArguments", () => Type.MakeGenericSignatureType(typeof(List<>), new Type[] { null }));
        }

        public static IEnumerable<object[]> MakeGenericType_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { Array.Empty<Type>() };
            yield return new object[] { new Type[] { null } };
            yield return new object[] { new Type[] { typeof(int) } };
        }

        [Theory]
        [MemberData(nameof(MakeGenericType_TestData))]
        public void MakeGenericType_Invoke_ThrowsNotSupportedException(Type[] typeArguments)
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.MakeGenericType(typeArguments));
        }

        [Fact]
        public void MakePointerType_Invoke_ThrowsNotSupportedException()
        {
            var type = new SubType();
            Assert.Throws<NotSupportedException>(() => type.MakePointerType());
        }

        public static IEnumerable<object[]> ReflectionOnlyGetType_TestData()
        {
            yield return new object[] { null, true, true };
            yield return new object[] { string.Empty, true, false };
            yield return new object[] { "TypeName", false, true };
            yield return new object[] { "TypeName", false, false };
        }

        [Theory]
        [MemberData(nameof(ReflectionOnlyGetType_TestData))]
        public void ReflectionOnlyGetType_Invoke_ThrowsPlatformNotSupportedException(string typeName, bool throwIfNotFound, bool ignoreCase)
        {
            Assert.Throws<PlatformNotSupportedException>(() => Type.ReflectionOnlyGetType(typeName, throwIfNotFound, ignoreCase));
        }

        [Theory]
        [InlineData(null, "Type: ")]
        [InlineData("", "Type: ")]
        [InlineData("Name", "Type: Name")]
        public void ToString_Invoke_ReturnsExpected(string name, string expected)
        {
            var type = new SubType
            {
                NameResult = name
            };
            Assert.Equal(expected, type.ToString());
        }

        private class CustomType : SubType
        {
            public Func<MethodBase> DeclaringMethodAction { get; set; }

            public override MethodBase DeclaringMethod
            {
                get
                {
                    if (DeclaringMethodAction == null)
                    {
                        return base.DeclaringMethod;
                    }

                    return DeclaringMethodAction();
                }
            }

            public Func<Type> DeclaringTypeAction { get; set; }

            public override Type DeclaringType
            {
                get
                {
                    if (DeclaringTypeAction == null)
                    {
                        return base.DeclaringType;
                    }

                    return DeclaringTypeAction();
                }
            }

            public Func<bool> IsGenericParameterAction { get; set; }

            public override bool IsGenericParameter
            {
                get
                {
                    if (IsGenericParameterAction == null)
                    {
                        return base.IsGenericParameter;
                    }

                    return IsGenericParameterAction();
                }
            }

            public Func<bool> IsGenericTypeAction { get; set; }

            public override bool IsGenericType
            {
                get
                {
                    if (IsGenericTypeAction == null)
                    {
                        return base.IsGenericType;
                    }

                    return IsGenericTypeAction();
                }
            }

            public Func<bool> IsGenericTypeDefinitionAction { get; set; }

            public override bool IsGenericTypeDefinition
            {
                get
                {
                    if (IsGenericTypeDefinitionAction == null)
                    {
                        return base.IsGenericTypeDefinition;
                    }

                    return IsGenericTypeDefinitionAction();
                }
            }

            public Func<bool> IsSZArrayAction { get; set; }

            public override bool IsSZArray
            {
                get
                {
                    if (IsSZArrayAction == null)
                    {
                        return base.IsSZArray;
                    }

                    return IsSZArrayAction();
                }
            }

            public Func<TypeCode> GetTypeCodeImplAction { get; set; }

            protected override TypeCode GetTypeCodeImpl()
            {
                if (GetTypeCodeImplAction == null)
                {
                    return base.GetTypeCodeImpl();
                }

                return GetTypeCodeImplAction();
            }

            public Func<bool> IsContextfulImplAction { get; set; }

            protected override bool IsContextfulImpl()
            {
                if (IsContextfulImplAction == null)
                {
                    return base.IsContextfulImpl();
                }

                return IsContextfulImplAction();
            }

            public Func<bool> IsMarshalByRefImplAction { get; set; }

            protected override bool IsMarshalByRefImpl()
            {
                if (IsMarshalByRefImplAction == null)
                {
                    return base.IsMarshalByRefImpl();
                }

                return IsMarshalByRefImplAction();
            }

            public Func<Type, bool> IsSubclassOfAction { get; set; }

            public override bool IsSubclassOf(Type c)
            {
                if (IsSubclassOfAction == null)
                {
                    return base.IsSubclassOf(c);
                }

                return IsSubclassOfAction(c);
            }

            public Func<bool> IsValueTypeAction { get; set; }

            protected override bool IsValueTypeImpl()
            {
                if (IsValueTypeAction == null)
                {
                    return base.IsValueTypeImpl();
                }

                return IsValueTypeAction();
            }

            public Func<string, MemberTypes, BindingFlags, MemberInfo[]> GetMemberAction { get; set; }

            public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
            {
                if (GetMemberAction == null)
                {
                    return base.GetMember(name, type, bindingAttr);
                }

                return GetMemberAction(name, type, bindingAttr);
            }

            public Func<string, int, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[], MethodInfo> GetMethodImplAction2 { get; set; }

            protected override MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
            {
                if (GetMethodImplAction2 == null)
                {
                    return base.GetMethodImpl(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);
                }

                return GetMethodImplAction2(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);
            }
        }

        private class ProtectedType : SubType
        {
            public new TypeCode GetTypeCodeImpl() => base.GetTypeCodeImpl();

            public new bool IsContextfulImpl() => base.IsContextfulImpl();

            public new bool IsMarshalByRefImpl() => base.IsMarshalByRefImpl();

            public new bool IsValueTypeImpl() => base.IsValueTypeImpl();

            public new MethodInfo GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
                => base.GetMethodImpl(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);
        }

        private class SubType : Type
        {
            public override Assembly Assembly => throw new NotImplementedException();

            public override string AssemblyQualifiedName => throw new NotImplementedException();

            public override Type BaseType => throw new NotImplementedException();

            public override string FullName => throw new NotImplementedException();

            public override Guid GUID => throw new NotImplementedException();

            public override Module Module => throw new NotImplementedException();

            public override string Namespace => throw new NotImplementedException();

            public Type UnderlyingSystemTypeResult { get; set; }

            public override Type UnderlyingSystemType => UnderlyingSystemTypeResult;

            public string NameResult { get; set; }

            public override string Name => NameResult;

            public Func<BindingFlags, ConstructorInfo[]> GetConstructorsAction { get; set; }

            public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
                => GetConstructorsAction(bindingAttr);

            public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

            public override Type GetElementType() => throw new NotImplementedException();

            public Func<string, BindingFlags, EventInfo> GetEventAction { get; set; }

            public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
                => GetEventAction(name, bindingAttr);

            public Func<BindingFlags, EventInfo[]> GetEventsAction { get; set; }

            public override EventInfo[] GetEvents(BindingFlags bindingAttr)
                => GetEventsAction(bindingAttr);

            public Func<string, BindingFlags, FieldInfo> GetFieldAction { get; set; }

            public override FieldInfo GetField(string name, BindingFlags bindingAttr)
                => GetFieldAction(name, bindingAttr);

            public Func<BindingFlags, FieldInfo[]> GetFieldsAction { get; set; }

            public override FieldInfo[] GetFields(BindingFlags bindingAttr)
                => GetFieldsAction(bindingAttr);

            public Func<string, bool, Type> GetInterfaceAction { get; set; }

            public override Type GetInterface(string name, bool ignoreCase)
                => GetInterfaceAction(name, ignoreCase);

            public override Type[] GetInterfaces() => throw new NotImplementedException();

            public Func<BindingFlags, MemberInfo[]> GetMembersAction { get; set; }

            public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
                => GetMembersAction(bindingAttr);

            public Func<BindingFlags, MethodInfo[]> GetMethodsAction { get; set; }

            public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
                => GetMethodsAction(bindingAttr);

            public Func<string, BindingFlags, Type> GetNestedTypeAction { get; set; }

            public override Type GetNestedType(string name, BindingFlags bindingAttr)
                => GetNestedTypeAction(name, bindingAttr);

            public Func<BindingFlags, Type[]> GetNestedTypesAction { get; set; }

            public override Type[] GetNestedTypes(BindingFlags bindingAttr)
                => GetNestedTypesAction(bindingAttr);

            public Func<BindingFlags, PropertyInfo[]> GetPropertiesAction { get; set; }

            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
                => GetPropertiesAction(bindingAttr);

            public Func<string, BindingFlags, Binder, object, object[], ParameterModifier[], CultureInfo, string[], object> InvokeMemberAction { get; set; }

            public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
                => InvokeMemberAction(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);

            public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

            public TypeAttributes GetAttributeFlagsImplResult { get; set; }

            protected override TypeAttributes GetAttributeFlagsImpl() => GetAttributeFlagsImplResult;

            public Func<BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[], ConstructorInfo> GetConstructorImplAction { get; set; }

            protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
                => GetConstructorImplAction(bindingAttr, binder, callConvention, types, modifiers);

            public Func<string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[], MethodInfo> GetMethodImplAction { get; set; }

            protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
                => GetMethodImplAction(name, bindingAttr, binder, callConvention, types, modifiers);

            public Func<string, BindingFlags, Binder, Type, Type[], ParameterModifier[], PropertyInfo> GetPropertyImplAction { get; set; }

            protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
                => GetPropertyImplAction(name, bindingAttr, binder, returnType, types, modifiers);

            public Func<bool> HasElementTypeImplAction { get; set; }

            protected override bool HasElementTypeImpl() => HasElementTypeImplAction();

            public Func<bool> IsArrayImplAction { get; set; }

            protected override bool IsArrayImpl() => IsArrayImplAction();

            public Func<bool> IsByRefImplAction { get; set; }

            protected override bool IsByRefImpl() => IsByRefImplAction();

            public Func<bool> IsCOMObjectImplAction { get; set; }

            protected override bool IsCOMObjectImpl() => IsCOMObjectImplAction();

            public Func<bool> IsPointerImplAction { get; set; }

            protected override bool IsPointerImpl() => IsPointerImplAction();

            public Func<bool> IsPrimitiveImplAction { get; set; }

            protected override bool IsPrimitiveImpl() => IsPrimitiveImplAction();
        }
    }
}
