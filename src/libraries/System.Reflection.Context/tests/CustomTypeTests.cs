// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class CustomTypeTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;

        public CustomTypeTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
        }

        [Fact]
        public void Assembly_ReturnsCustomAssembly()
        {
            Assert.Equal(ProjectionConstants.CustomAssembly, _customTypeInfo.Assembly.GetType().FullName);
        }

        [Fact]
        public void AssemblyQualifiedName_ReturnsValue()
        {
            Assert.NotNull(_customTypeInfo.AssemblyQualifiedName);
            Assert.Contains("TestObject", _customTypeInfo.AssemblyQualifiedName);
        }

        [Fact]
        public void BaseType_ReturnsCustomType()
        {
            Assert.NotNull(_customTypeInfo.BaseType);
            Assert.Equal(ProjectionConstants.CustomType, _customTypeInfo.BaseType.GetType().FullName);
        }

        [Fact]
        public void ContainsGenericParameters_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.ContainsGenericParameters);
        }

        [Fact]
        public void DeclaringType_ReturnsNull()
        {
            // TestObject is not a nested type
            Assert.Null(_customTypeInfo.DeclaringType);
        }

        [Fact]
        public void FullName_ReturnsValue()
        {
            Assert.Equal(typeof(TestObject).FullName, _customTypeInfo.FullName);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/99459", TestRuntimes.Mono)]
        public void GUID_ReturnsValue()
        {
            Guid guid = _customTypeInfo.GUID;
            Assert.NotEqual(Guid.Empty, guid);
        }

        [Fact]
        public void IsEnum_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsEnum);
        }

        [Fact]
        public void IsGenericParameter_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsGenericParameter);
        }

        [Fact]
        public void IsGenericType_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsGenericType);
        }

        [Fact]
        public void IsGenericTypeDefinition_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsGenericTypeDefinition);
        }

        [Fact]
        public void IsSecurityCritical_ReturnsValue()
        {
            bool value = _customTypeInfo.IsSecurityCritical;
            Assert.True(value);
        }

        [Fact]
        public void IsSecuritySafeCritical_ReturnsValue()
        {
            bool value = _customTypeInfo.IsSecuritySafeCritical;
            Assert.False(value);
        }

        [Fact]
        public void IsSecurityTransparent_ReturnsValue()
        {
            bool value = _customTypeInfo.IsSecurityTransparent;
            Assert.False(value);
        }

        [Fact]
#pragma warning disable SYSLIB0050 // Type or member is obsolete
        public void IsSerializable_ReturnsValue()
        {
            bool value = _customTypeInfo.IsSerializable;
            Assert.False(value);
        }
#pragma warning restore SYSLIB0050

        [Fact]
        public void MetadataToken_ReturnsValue()
        {
            Assert.True(_customTypeInfo.MetadataToken > 0);
        }

        [Fact]
        public void Module_ReturnsCustomModule()
        {
            Assert.Equal(ProjectionConstants.CustomModule, _customTypeInfo.Module.GetType().FullName);
        }

        [Fact]
        public void Name_ReturnsValue()
        {
            Assert.Equal("TestObject", _customTypeInfo.Name);
        }

        [Fact]
        public void Namespace_ReturnsValue()
        {
            Assert.Equal(typeof(TestObject).Namespace, _customTypeInfo.Namespace);
        }

        [Fact]
        public void ReflectedType_ReturnsNull()
        {
            // For a top-level type, ReflectedType is null
            Assert.Null(_customTypeInfo.ReflectedType);
        }

        [Fact]
        public void StructLayoutAttribute_ReturnsValue()
        {
            // Class has auto layout by default
            StructLayoutAttribute? attr = _customTypeInfo.StructLayoutAttribute;
            Assert.NotNull(attr);
        }

        [Fact]
        public void TypeHandle_ReturnsValue()
        {
            RuntimeTypeHandle handle = _customTypeInfo.TypeHandle;
            Assert.NotEqual(default, handle);
        }

        [Fact]
        public void UnderlyingSystemType_ReturnsValue()
        {
            Assert.NotNull(_customTypeInfo.UnderlyingSystemType);
        }

        [Fact]
        public void GetDefaultMembers_ReturnsIndexerProperty()
        {
            // TestObject has DefaultMemberAttribute("Item") for the indexer
            MemberInfo[] members = _customTypeInfo.GetDefaultMembers();
            Assert.Single(members);
            Assert.Equal("Item", members[0].Name);
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsEmptyDueToContextOverride()
        {
            // TestCustomReflectionContext's GetCustomAttributes doesn't pass through type attributes
            // so GetCustomAttributes returns empty even though GetCustomAttributesData shows the data
            object[] attributes = _customTypeInfo.GetCustomAttributes(typeof(Attribute), true);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributes_NoType_ReturnsEmptyDueToContextOverride()
        {
            // TestCustomReflectionContext's GetCustomAttributes doesn't pass through type attributes
            object[] attributes = _customTypeInfo.GetCustomAttributes(false);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributesData_ReturnsDataContractAndDefaultMember()
        {
            // GetCustomAttributesData returns the raw attribute data including DataContract and DefaultMember
            IList<CustomAttributeData> data = _customTypeInfo.GetCustomAttributesData();
            Assert.Equal(2, data.Count);
            Assert.All(data, cad => Assert.Equal(ProjectionConstants.ProjectingCustomAttributeData, cad.GetType().FullName));
        }

        [Fact]
        public void IsDefined_ExistingAttribute_ReturnsValue()
        {
            bool isDefined = _customTypeInfo.IsDefined(typeof(DataContractAttribute), true);
            Assert.False(isDefined);
        }

        [Fact]
        public void IsDefined_NonExistingAttribute_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsDefined(typeof(TestAttribute), true));
        }

        [Fact]
        public void GetEvents_ReturnsProjectedEvents()
        {
            EventInfo[] events = _customTypeInfo.GetEvents();
            Assert.NotNull(events);
        }

        [Fact]
        public void GetGenericArguments_ReturnsProjectedTypes()
        {
            Type[] args = _customTypeInfo.GetGenericArguments();
            Assert.Empty(args);
        }

        [Fact]
        public void GetInterfaces_ReturnsProjectedTypes()
        {
            Type[] interfaces = _customTypeInfo.GetInterfaces();
            Assert.NotNull(interfaces);
        }

        [Fact]
        public void GetInterface_ReturnsNull()
        {
            Type? iface = _customTypeInfo.GetInterface("IDisposable", false);
            Assert.Null(iface);
        }

        [Fact]
        public void GetConstructors_ReturnsProjectedConstructors()
        {
            ConstructorInfo[] ctors = _customTypeInfo.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.NotEmpty(ctors);
        }

        [Fact]
        public void GetConstructor_ReturnsProjectedConstructor()
        {
            ConstructorInfo? ctor = _customTypeInfo.GetConstructor(new[] { typeof(string) });
            Assert.NotNull(ctor);
        }

        [Fact]
        public void GetMethods_ReturnsProjectedMethods()
        {
            MethodInfo[] methods = _customTypeInfo.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            Assert.NotEmpty(methods);
        }

        [Fact]
        public void GetMethod_ReturnsProjectedMethod()
        {
            MethodInfo? method = _customTypeInfo.GetMethod("GetMessage");
            Assert.NotNull(method);
        }

        [Fact]
        public void GetFields_ReturnsProjectedFields()
        {
            FieldInfo[] fields = _customTypeInfo.GetFields(BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(fields);
        }

        [Fact]
        public void GetField_ReturnsProjectedField()
        {
            TypeInfo derivedTypeInfo = typeof(SecondTestObject).GetTypeInfo();
            TypeInfo customDerivedTypeInfo = _customReflectionContext.MapType(derivedTypeInfo);
            FieldInfo? field = customDerivedTypeInfo.GetField("field");
            Assert.NotNull(field);
        }

        [Fact]
        public void GetProperties_ReturnsProjectedProperties()
        {
            PropertyInfo[] properties = _customTypeInfo.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.NotEmpty(properties);
        }

        [Fact]
        public void GetProperty_ReturnsProjectedProperty()
        {
            PropertyInfo? property = _customTypeInfo.GetProperty("A");
            Assert.NotNull(property);
        }

        [Fact]
        public void GetNestedTypes_ReturnsProjectedTypes()
        {
            Type[] nestedTypes = _customTypeInfo.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(nestedTypes);
        }

        [Fact]
        public void GetNestedType_ReturnsNull()
        {
            Type? nestedType = _customTypeInfo.GetNestedType("NonExistent", BindingFlags.Public);
            Assert.Null(nestedType);
        }

        [Fact]
        public void GetMembers_ReturnsProjectedMembers()
        {
            MemberInfo[] members = _customTypeInfo.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            Assert.NotEmpty(members);
        }

        [Fact]
        public void GetMember_ReturnsProjectedMembers()
        {
            MemberInfo[] members = _customTypeInfo.GetMember("GetMessage", MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotEmpty(members);
        }

        [Fact]
        public void IsAssignableFrom_SameType_ReturnsTrue()
        {
            Assert.True(_customTypeInfo.IsAssignableFrom(_customTypeInfo));
        }

        [Fact]
        public void IsAssignableFrom_DifferentType_ReturnsFalse()
        {
            TypeInfo stringType = _customReflectionContext.MapType(typeof(string).GetTypeInfo());
            Assert.False(_customTypeInfo.IsAssignableFrom(stringType));
        }

        [Fact]
        public void IsInstanceOfType_ReturnsValue()
        {
            var testObj = new TestObject("test");
            bool isInstance = _customTypeInfo.IsInstanceOfType(testObj);
            Assert.True(isInstance);
        }

        [Fact]
        public void IsSubclassOf_ReturnsFalse()
        {
            TypeInfo stringType = _customReflectionContext.MapType(typeof(string).GetTypeInfo());
            Assert.False(_customTypeInfo.IsSubclassOf(stringType));
        }

        [Fact]
        public void IsEquivalentTo_SameType_ReturnsTrue()
        {
            Assert.True(_customTypeInfo.IsEquivalentTo(_customTypeInfo));
        }

        [Fact]
        public void MakeArrayType_ReturnsProjectedType()
        {
            Type arrayType = _customTypeInfo.MakeArrayType();
            Assert.True(arrayType.IsArray);
            Assert.Equal(ProjectionConstants.CustomType, arrayType.GetType().FullName);
        }

        [Fact]
        public void MakeArrayType_WithRank_ReturnsProjectedType()
        {
            Type arrayType = _customTypeInfo.MakeArrayType(2);
            Assert.True(arrayType.IsArray);
            Assert.Equal(ProjectionConstants.CustomType, arrayType.GetType().FullName);
        }

        [Fact]
        public void MakePointerType_ReturnsProjectedType()
        {
            Type pointerType = _customTypeInfo.MakePointerType();
            Assert.True(pointerType.IsPointer);
            Assert.Equal(ProjectionConstants.CustomType, pointerType.GetType().FullName);
        }

        [Fact]
        public void MakeByRefType_ReturnsProjectedType()
        {
            Type byRefType = _customTypeInfo.MakeByRefType();
            Assert.True(byRefType.IsByRef);
            Assert.Equal(ProjectionConstants.CustomType, byRefType.GetType().FullName);
        }

        [Fact]
        public void GetElementType_ReturnsNull()
        {
            // TestObject is not an array, pointer, or byref type
            Assert.Null(_customTypeInfo.GetElementType());
        }

        [Fact]
        public void HasElementType_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.HasElementType);
        }

        [Fact]
        public void IsArray_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsArray);
        }

        [Fact]
        public void IsByRef_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsByRef);
        }

        [Fact]
        public void IsPointer_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsPointer);
        }

        [Fact]
        public void IsPrimitive_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsPrimitive);
        }

        [Fact]
        public void IsCOMObject_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsCOMObject);
        }

        [Fact]
        public void IsContextful_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsContextful);
        }

        [Fact]
        public void IsMarshalByRef_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsMarshalByRef);
        }

        [Fact]
        public void IsValueType_ReturnsFalse()
        {
            Assert.False(_customTypeInfo.IsValueType);
        }

        [Fact]
        public void Attributes_ReturnsValue()
        {
            TypeAttributes attrs = _customTypeInfo.Attributes;
            Assert.NotEqual((TypeAttributes)0, attrs);
        }

        [Fact]
        public void TypeCode_ReturnsObject()
        {
            Assert.Equal(TypeCode.Object, Type.GetTypeCode(_customTypeInfo));
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            string str = _customTypeInfo.ToString();
            Assert.Contains("TestObject", str);
        }

        [Fact]
        public void Equals_SameType_ReturnsTrue()
        {
            TypeInfo sameType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            Assert.True(_customTypeInfo.Equals(sameType));
        }

        [Fact]
        public void GetHashCode_IsIdempotent()
        {
            int hashCode1 = _customTypeInfo.GetHashCode();
            int hashCode2 = _customTypeInfo.GetHashCode();
            Assert.Equal(hashCode1, hashCode2);
        }
    }
}
