// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Test types for extended type operations
    internal class GenericType<T>
    {
        public T Value { get; set; }
    }

    internal class DerivedTestObject : TestObject
    {
        public DerivedTestObject() : base("derived") { }
    }

    internal class NestedTypeContainer
    {
        public class NestedType { }
        private class PrivateNestedType { }
    }

    internal enum TestEnum
    {
        Value1 = 1,
        Value2 = 2
    }

    public class ExtendedTypeTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;

        public ExtendedTypeTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
        }

        [Fact]
        public void GetGenericArguments_ForGenericType_ReturnsProjectedTypes()
        {
            TypeInfo genericTypeInfo = typeof(GenericType<int>).GetTypeInfo();
            TypeInfo customGenericType = _customReflectionContext.MapType(genericTypeInfo);

            Type[] args = customGenericType.GetGenericArguments();
            Assert.Single(args);
            Assert.Equal(ProjectionConstants.CustomType, args[0].GetType().FullName);
        }

        [Fact]
        public void GetGenericTypeDefinition_ForGenericType_ReturnsProjectedType()
        {
            TypeInfo genericTypeInfo = typeof(GenericType<int>).GetTypeInfo();
            TypeInfo customGenericType = _customReflectionContext.MapType(genericTypeInfo);

            Type genericDef = customGenericType.GetGenericTypeDefinition();
            Assert.NotNull(genericDef);
            Assert.Equal(ProjectionConstants.CustomType, genericDef.GetType().FullName);
        }

        [Fact]
        public void MakeGenericType_ReturnsProjectedType()
        {
            TypeInfo genericDefInfo = typeof(GenericType<>).GetTypeInfo();
            TypeInfo customGenericDef = _customReflectionContext.MapType(genericDefInfo);

            Type genericType = customGenericDef.MakeGenericType(typeof(string));
            Assert.NotNull(genericType);
            Assert.Equal(ProjectionConstants.CustomType, genericType.GetType().FullName);
        }

        [Fact]
        public void IsGenericType_ReturnsTrue_ForGenericType()
        {
            TypeInfo genericTypeInfo = typeof(GenericType<int>).GetTypeInfo();
            TypeInfo customGenericType = _customReflectionContext.MapType(genericTypeInfo);
            Assert.True(customGenericType.IsGenericType);
        }

        [Fact]
        public void IsGenericTypeDefinition_ReturnsTrue_ForGenericDef()
        {
            TypeInfo genericDefInfo = typeof(GenericType<>).GetTypeInfo();
            TypeInfo customGenericDef = _customReflectionContext.MapType(genericDefInfo);
            Assert.True(customGenericDef.IsGenericTypeDefinition);
        }

        [Fact]
        public void ContainsGenericParameters_ReturnsTrue_ForGenericDef()
        {
            TypeInfo genericDefInfo = typeof(GenericType<>).GetTypeInfo();
            TypeInfo customGenericDef = _customReflectionContext.MapType(genericDefInfo);
            Assert.True(customGenericDef.ContainsGenericParameters);
        }

        [Fact]
        public void GetNestedTypes_ReturnsProjectedTypes()
        {
            TypeInfo containerTypeInfo = typeof(NestedTypeContainer).GetTypeInfo();
            TypeInfo customContainerType = _customReflectionContext.MapType(containerTypeInfo);

            Type[] nestedTypes = customContainerType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotEmpty(nestedTypes);
            Assert.All(nestedTypes, t => Assert.Equal(ProjectionConstants.CustomType, t.GetType().FullName));
        }

        [Fact]
        public void GetNestedType_ReturnsProjectedType()
        {
            TypeInfo containerTypeInfo = typeof(NestedTypeContainer).GetTypeInfo();
            TypeInfo customContainerType = _customReflectionContext.MapType(containerTypeInfo);

            Type nestedType = customContainerType.GetNestedType("NestedType", BindingFlags.Public);
            Assert.NotNull(nestedType);
            Assert.Equal(ProjectionConstants.CustomType, nestedType.GetType().FullName);
        }

        [Fact]
        public void DeclaringType_ForNestedType_ReturnsProjectedType()
        {
            TypeInfo nestedTypeInfo = typeof(NestedTypeContainer.NestedType).GetTypeInfo();
            TypeInfo customNestedType = _customReflectionContext.MapType(nestedTypeInfo);

            Type declaringType = customNestedType.DeclaringType;
            Assert.NotNull(declaringType);
            Assert.Equal(ProjectionConstants.CustomType, declaringType.GetType().FullName);
        }

        [Fact]
        public void IsEnum_ReturnsTrue_ForEnum()
        {
            TypeInfo enumTypeInfo = typeof(TestEnum).GetTypeInfo();
            TypeInfo customEnumType = _customReflectionContext.MapType(enumTypeInfo);
            Assert.True(customEnumType.IsEnum);
        }

        [Fact]
        public void GetEnumUnderlyingType_ReturnsProjectedType()
        {
            TypeInfo enumTypeInfo = typeof(TestEnum).GetTypeInfo();
            TypeInfo customEnumType = _customReflectionContext.MapType(enumTypeInfo);

            Type underlyingType = customEnumType.GetEnumUnderlyingType();
            Assert.NotNull(underlyingType);
            Assert.Equal(ProjectionConstants.CustomType, underlyingType.GetType().FullName);
        }

        [Fact]
        public void GetEnumNames_ReturnsNames()
        {
            TypeInfo enumTypeInfo = typeof(TestEnum).GetTypeInfo();
            TypeInfo customEnumType = _customReflectionContext.MapType(enumTypeInfo);

            string[] names = customEnumType.GetEnumNames();
            Assert.Contains("Value1", names);
            Assert.Contains("Value2", names);
        }

        [Fact]
        public void GetEnumValues_ReturnsValues()
        {
            TypeInfo enumTypeInfo = typeof(TestEnum).GetTypeInfo();
            TypeInfo customEnumType = _customReflectionContext.MapType(enumTypeInfo);

            Array values = customEnumType.GetEnumValues();
            Assert.NotEmpty(values);
        }

        [Fact]
        public void GetEnumName_ReturnsName()
        {
            TypeInfo enumTypeInfo = typeof(TestEnum).GetTypeInfo();
            TypeInfo customEnumType = _customReflectionContext.MapType(enumTypeInfo);

            string name = customEnumType.GetEnumName(TestEnum.Value1);
            Assert.Equal("Value1", name);
        }

        [Fact]
        public void IsEnumDefined_ReturnsTrue()
        {
            TypeInfo enumTypeInfo = typeof(TestEnum).GetTypeInfo();
            TypeInfo customEnumType = _customReflectionContext.MapType(enumTypeInfo);

            Assert.True(customEnumType.IsEnumDefined(TestEnum.Value1));
            Assert.True(customEnumType.IsEnumDefined(1));
        }

        [Fact]
        public void InvokeMember_InvokesMethod()
        {
            var target = new TestObject("test");
            object result = _customTypeInfo.InvokeMember(
                "GetMessage",
                BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                null,
                target,
                null,
                null,
                CultureInfo.InvariantCulture,
                null);
            Assert.Equal("test", result);
        }

        [Fact]
        public void GetInterfaceMap_ReturnsProjectedMapping()
        {
            TypeInfo listTypeInfo = typeof(List<int>).GetTypeInfo();
            TypeInfo customListType = _customReflectionContext.MapType(listTypeInfo);
            TypeInfo iListTypeInfo = typeof(IList<int>).GetTypeInfo();
            TypeInfo customIListType = _customReflectionContext.MapType(iListTypeInfo);

            InterfaceMapping mapping = customListType.GetInterfaceMap(customIListType);
            Assert.NotEmpty(mapping.InterfaceMethods);
            Assert.NotEmpty(mapping.TargetMethods);
        }

        [Fact]
        public void GetMember_ByMemberTypes_ReturnsProjectedMembers()
        {
            MemberInfo[] members = _customTypeInfo.GetMember("GetMessage", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
            Assert.Single(members);
        }

        [Fact]
        public void GetMember_ByMemberTypes_Constructor_ReturnsProjectedMembers()
        {
            MemberInfo[] members = _customTypeInfo.GetMember(".ctor", MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotEmpty(members);
        }

        [Fact]
        public void GetMember_ByMemberTypes_Property_ReturnsProjectedMembers()
        {
            MemberInfo[] members = _customTypeInfo.GetMember("A", MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotEmpty(members);
        }

        [Fact]
        public void GetMember_ByMemberTypes_Field_ReturnsProjectedMembers()
        {
            TypeInfo derivedTypeInfo = typeof(SecondTestObject).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);
            MemberInfo[] members = customDerivedType.GetMember("field", MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance);
            Assert.Single(members);
        }

        [Fact]
        public void GetMember_ByMemberTypes_Event_ReturnsProjectedMembers()
        {
            TypeInfo eventTypeInfo = typeof(TypeWithEvent).GetTypeInfo();
            TypeInfo customEventType = _customReflectionContext.MapType(eventTypeInfo);
            MemberInfo[] members = customEventType.GetMember("TestEvent", MemberTypes.Event, BindingFlags.Public | BindingFlags.Instance);
            Assert.Single(members);
        }

        [Fact]
        public void GetMember_ByMemberTypes_NestedType_ReturnsProjectedMembers()
        {
            TypeInfo containerTypeInfo = typeof(NestedTypeContainer).GetTypeInfo();
            TypeInfo customContainerType = _customReflectionContext.MapType(containerTypeInfo);
            MemberInfo[] members = customContainerType.GetMember("NestedType", MemberTypes.NestedType, BindingFlags.Public);
            Assert.Single(members);
        }

        [Fact]
        public void IsSubclassOf_BaseClass_ReturnsTrue()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedTestObject).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);
            Assert.True(customDerivedType.IsSubclassOf(_customTypeInfo));
        }

        [Fact]
        public void IsAssignableFrom_SameType_ReturnsTrue()
        {
            Assert.True(_customTypeInfo.IsAssignableFrom(_customTypeInfo));
        }

        [Fact]
        public void IsAssignableFrom_DerivedType_ReturnsTrue()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedTestObject).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);
            Assert.True(_customTypeInfo.IsAssignableFrom(customDerivedType));
        }

        [Fact]
        public void IsEquivalentTo_DifferentProjector_ReturnsFalse()
        {
            var otherContext = new TestCustomReflectionContext();
            TypeInfo otherTypeInfo = otherContext.MapType(typeof(TestObject).GetTypeInfo());
            Assert.False(_customTypeInfo.IsEquivalentTo(otherTypeInfo));
        }

        [Fact]
        public void GetArrayRank_ForArrayType_ReturnsRank()
        {
            TypeInfo arrayTypeInfo = typeof(int[,]).GetTypeInfo();
            TypeInfo customArrayType = _customReflectionContext.MapType(arrayTypeInfo);
            Assert.Equal(2, customArrayType.GetArrayRank());
        }

        [Fact]
        public void GetElementType_ForArrayType_ReturnsProjectedType()
        {
            Type arrayType = _customTypeInfo.MakeArrayType();
            TypeInfo customArrayType = _customReflectionContext.MapType(arrayType.GetTypeInfo());

            Type elementType = customArrayType.GetElementType();
            Assert.NotNull(elementType);
            Assert.Equal(ProjectionConstants.CustomType, elementType.GetType().FullName);
        }

        [Fact]
        public void GetGenericParameterConstraints_ForGenericParameter_ReturnsProjectedTypes()
        {
            TypeInfo genericDefInfo = typeof(GenericType<>).GetTypeInfo();
            TypeInfo customGenericDef = _customReflectionContext.MapType(genericDefInfo);

            Type[] typeParams = customGenericDef.GetGenericArguments();
            Assert.Single(typeParams);

            // Generic parameter T has no constraints
            Type[] constraints = typeParams[0].GetGenericParameterConstraints();
            Assert.Empty(constraints);
        }
    }
}
