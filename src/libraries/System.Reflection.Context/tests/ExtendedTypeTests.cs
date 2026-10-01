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

    internal unsafe class FunctionPointerHolder
    {
        public void CdeclParameter(delegate* unmanaged[Cdecl]<void> f) { }
        public void ManagedParameter(delegate*<string, int> f) { }
        public void UnmanagedParameter(delegate* unmanaged<string, int> f) { }
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/89157", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
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

        [Theory]
        [InlineData(typeof(Span<int>), true)]
        [InlineData(typeof(ReadOnlySpan<char>), true)]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(int?), false)]
        [InlineData(typeof(List<int>), false)]
        [InlineData(typeof(TestEnum), false)]
        public void IsByRefLike_ReturnsUnderlyingValue(Type type, bool expected)
        {
            TypeInfo customType = _customReflectionContext.MapType(type.GetTypeInfo());
            Assert.Equal(expected, customType.IsByRefLike);
        }

        [Fact]
        public void GetEnumValuesAsUnderlyingType_ReturnsValues()
        {
            TypeInfo customEnumType = _customReflectionContext.MapType(typeof(TestEnum).GetTypeInfo());

            Array values = customEnumType.GetEnumValuesAsUnderlyingType();
            Assert.Equal(new int[] { 1, 2 }, Assert.IsType<int[]>(values));
        }

        [Fact]
        public void GetEnumValuesAsUnderlyingType_ThrowsForNonEnum()
        {
            Assert.Throws<ArgumentException>(() => _customTypeInfo.GetEnumValuesAsUnderlyingType());
        }

        [Fact]
        public void GetNullableUnderlyingType_ForNullable_ReturnsProjectedType()
        {
            TypeInfo customNullableType = _customReflectionContext.MapType(typeof(int?).GetTypeInfo());

            Type underlyingType = customNullableType.GetNullableUnderlyingType();
            Assert.Equal(ProjectionConstants.CustomType, underlyingType.GetType().FullName);
            Assert.Equal(typeof(int), underlyingType.UnderlyingSystemType);
        }

        [Fact]
        public void GetNullableUnderlyingType_ForNullableDefinition_ReturnsProjectedGenericParameter()
        {
            TypeInfo customNullableDef = _customReflectionContext.MapType(typeof(Nullable<>).GetTypeInfo());

            Type underlyingType = customNullableDef.GetNullableUnderlyingType();
            Assert.Equal(ProjectionConstants.CustomType, underlyingType.GetType().FullName);
            Assert.True(underlyingType.IsGenericParameter);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(TestEnum))]
        [InlineData(typeof(List<int>))]
        public void GetNullableUnderlyingType_ForNonNullable_ReturnsNull(Type type)
        {
            TypeInfo customType = _customReflectionContext.MapType(type.GetTypeInfo());
            Assert.Null(customType.GetNullableUnderlyingType());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124149", TestRuntimes.Mono)]
        public void MakeFunctionPointerType_ReturnsProjectedType(bool isUnmanaged)
        {
            TypeInfo customReturnType = _customReflectionContext.MapType(typeof(int).GetTypeInfo());
            TypeInfo customParameterType = _customReflectionContext.MapType(typeof(string).GetTypeInfo());

            Type functionPointerType = customReturnType.MakeFunctionPointerType([customParameterType], isUnmanaged);
            Assert.Equal(ProjectionConstants.CustomType, functionPointerType.GetType().FullName);
            Assert.Equal(typeof(int).MakeFunctionPointerType([typeof(string)], isUnmanaged), functionPointerType.UnderlyingSystemType);
            Assert.True(functionPointerType.IsFunctionPointer);
            Assert.Equal(isUnmanaged, functionPointerType.IsUnmanagedFunctionPointer);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124149", TestRuntimes.Mono)]
        public void MakeFunctionPointerType_NullOrMixedParameterTypes_ReturnsProjectedType()
        {
            TypeInfo customReturnType = _customReflectionContext.MapType(typeof(int).GetTypeInfo());
            TypeInfo customParameterType = _customReflectionContext.MapType(typeof(string).GetTypeInfo());

            Type noParameters = customReturnType.MakeFunctionPointerType(null);
            Assert.Equal(ProjectionConstants.CustomType, noParameters.GetType().FullName);
            Assert.Equal(typeof(int).MakeFunctionPointerType(null), noParameters.UnderlyingSystemType);

            Type mixedParameters = customReturnType.MakeFunctionPointerType([customParameterType, typeof(long)]);
            Assert.Equal(ProjectionConstants.CustomType, mixedParameters.GetType().FullName);
            Assert.Equal(typeof(int).MakeFunctionPointerType([typeof(string), typeof(long)]), mixedParameters.UnderlyingSystemType);
        }

        [Theory]
        [InlineData(typeof(int), false, false, true, false)]
        [InlineData(typeof(int[]), true, false, false, false)]
        [InlineData(typeof(int[,]), false, true, false, false)]
        [InlineData(typeof(List<int>), false, false, false, true)]
        [InlineData(typeof(List<>), false, false, true, false)]
        public void ArrayAndGenericShape_ReturnsUnderlyingValues(Type type, bool isSZArray, bool isVariableBoundArray, bool isTypeDefinition, bool isConstructedGenericType)
        {
            TypeInfo customType = _customReflectionContext.MapType(type.GetTypeInfo());

            Assert.Equal(isSZArray, customType.IsSZArray);
            Assert.Equal(isVariableBoundArray, customType.IsVariableBoundArray);
            Assert.Equal(isTypeDefinition, customType.IsTypeDefinition);
            Assert.Equal(isConstructedGenericType, customType.IsConstructedGenericType);
        }

        [Theory]
        [InlineData(nameof(FunctionPointerHolder.ManagedParameter), false)]
        [InlineData(nameof(FunctionPointerHolder.UnmanagedParameter), true)]
        public void FunctionPointerMembers_ReturnUnderlyingValues(string methodName, bool isUnmanaged)
        {
            Type functionPointerType = typeof(FunctionPointerHolder).GetMethod(methodName).GetParameters()[0].ParameterType;
            TypeInfo customType = _customReflectionContext.MapType(functionPointerType.GetTypeInfo());

            Assert.True(customType.IsFunctionPointer);
            Assert.Equal(isUnmanaged, customType.IsUnmanagedFunctionPointer);
            Assert.Empty(customType.GetFunctionPointerCallingConventions());

            Type returnType = customType.GetFunctionPointerReturnType();
            Assert.Equal(ProjectionConstants.CustomType, returnType.GetType().FullName);
            Assert.Equal(typeof(int), returnType.UnderlyingSystemType);

            Type parameterType = Assert.Single(customType.GetFunctionPointerParameterTypes());
            Assert.Equal(ProjectionConstants.CustomType, parameterType.GetType().FullName);
            Assert.Equal(typeof(string), parameterType.UnderlyingSystemType);
        }

        [Fact]
        public void GetFunctionPointerCallingConventions_ForModifiedType_ReturnsProjectedTypes()
        {
            ParameterInfo parameter = typeof(FunctionPointerHolder).GetMethod(nameof(FunctionPointerHolder.CdeclParameter)).GetParameters()[0];
            TypeInfo customType = _customReflectionContext.MapType(parameter.GetModifiedParameterType().GetTypeInfo());

            Type callingConvention = Assert.Single(customType.GetFunctionPointerCallingConventions());
            Assert.Equal(ProjectionConstants.CustomType, callingConvention.GetType().FullName);
            Assert.Equal(typeof(System.Runtime.CompilerServices.CallConvCdecl), callingConvention.UnderlyingSystemType);
        }

        [Fact]
        public void FunctionPointerMembers_ForNonFunctionPointer_ReturnUnderlyingValues()
        {
            Assert.False(_customTypeInfo.IsFunctionPointer);
            Assert.False(_customTypeInfo.IsUnmanagedFunctionPointer);
            Assert.Throws<InvalidOperationException>(() => _customTypeInfo.GetFunctionPointerReturnType());
            Assert.Throws<InvalidOperationException>(() => _customTypeInfo.GetFunctionPointerParameterTypes());
            Assert.Throws<InvalidOperationException>(() => _customTypeInfo.GetFunctionPointerCallingConventions());
        }

        [Theory]
        [InlineData(typeof(NestedTypeContainer), MemberTypes.TypeInfo)]
        [InlineData(typeof(NestedTypeContainer.NestedType), MemberTypes.NestedType)]
        public void MemberType_ReturnsUnderlyingValue(Type type, MemberTypes expected)
        {
            TypeInfo customType = _customReflectionContext.MapType(type.GetTypeInfo());
            Assert.Equal(expected, customType.MemberType);
        }
    }
}
