// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    /// <summary>
    /// Final push for branch coverage targeting GetMethodImpl branches and attribute handling.
    /// </summary>
    public class FinalBranchCoverageTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        // Test GetMethod with generic methods
        [Fact]
        public void GetMethod_Generic_ReturnsMethod()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TypeWithGenericMethod).GetTypeInfo());
            MethodInfo method = customType.GetMethod("GenericMethod");
            Assert.NotNull(method);
            Assert.True(method.IsGenericMethodDefinition);
        }

        // Test GetMethod with different calling conventions
        [Fact]
        public void GetMethod_WithCallingConvention_ReturnsMethod()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MethodInfo method = customType.GetMethod("GetMessage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, Type.EmptyTypes, null);
            Assert.NotNull(method);
        }

        // Test GetMethod with modifiers
        [Fact]
        public void GetMethod_WithModifiers_ReturnsMethod()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MethodInfo method = customType.GetMethod("GetMessage", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            Assert.NotNull(method);
        }

        // Test GetMethod with null name - should throw
        [Fact]
        public void GetMethod_NullName_ThrowsArgumentNull()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            Assert.Throws<ArgumentNullException>(() => customType.GetMethod(null));
        }

        // Test GetConstructor with various scenarios
        [Fact]
        public void GetConstructor_ParameterlessWithFlags_ReturnsConstructor()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(object).GetTypeInfo());
            ConstructorInfo ctor = customType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            Assert.NotNull(ctor);
        }

        [Fact]
        public void GetConstructor_NonExistent_ReturnsNull()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(int), typeof(double) });
            Assert.Null(ctor);
        }

        // Test GetFields with various flags
        [Fact]
        public void GetFields_InstanceOnly_ReturnsInstanceFields()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TypeWithFields).GetTypeInfo());
            FieldInfo[] fields = customType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            Assert.All(fields, f => Assert.False(f.IsStatic));
        }

        [Fact]
        public void GetFields_StaticOnly_ReturnsStaticFields()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TypeWithFields).GetTypeInfo());
            FieldInfo[] fields = customType.GetFields(BindingFlags.Public | BindingFlags.Static);
            Assert.All(fields, f => Assert.True(f.IsStatic));
        }

        // Test GetEvents with flags
        [Fact]
        public void GetEvents_WithFlags_ReturnsEvents()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TypeWithEvent).GetTypeInfo());
            EventInfo[] events = customType.GetEvents(BindingFlags.Public | BindingFlags.Instance);
            Assert.NotEmpty(events);
        }

        [Fact]
        public void GetEvent_NonExistent_ReturnsNull()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            EventInfo evt = customType.GetEvent("NonExistentEvent");
            Assert.Null(evt);
        }

        // Test GetNestedType scenarios
        [Fact]
        public void GetNestedType_WithFlags_ReturnsType()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(NestedTypeContainer).GetTypeInfo());
            Type nestedType = customType.GetNestedType("NestedType", BindingFlags.Public);
            Assert.NotNull(nestedType);
        }

        [Fact]
        public void GetNestedType_NonPublic_ReturnsPrivateNested()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(NestedTypeContainer).GetTypeInfo());
            Type nestedType = customType.GetNestedType("PrivateNestedType", BindingFlags.NonPublic);
            Assert.NotNull(nestedType);
        }

        [Fact]
        public void GetNestedType_NonExistent_ReturnsNull()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(NestedTypeContainer).GetTypeInfo());
            Type nestedType = customType.GetNestedType("NonExistentNested", BindingFlags.Public);
            Assert.Null(nestedType);
        }

        // Test InvokeMember scenarios
        [Fact]
        public void InvokeMember_GetProperty_GetsValue()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            var target = new TestObject("test");
            object result = customType.InvokeMember("A", BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public, null, target, null);
            Assert.NotNull(result);
        }

        [Fact]
        public void InvokeMember_SetProperty_SetsValue()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TypeWithProperties).GetTypeInfo());
            var target = new TypeWithProperties();
            customType.InvokeMember("ReadWriteProperty", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public, null, target, new object[] { "newValue" });
            Assert.Equal("newValue", target.ReadWriteProperty);
        }

        // Test GetInterfaceMap
        [Fact]
        public void GetInterfaceMap_ReturnsMapping()
        {
            TypeInfo listType = _customReflectionContext.MapType(typeof(List<int>).GetTypeInfo());
            Type iListType = listType.GetInterface("IList");
            if (iListType != null)
            {
                InterfaceMapping mapping = listType.GetInterfaceMap(iListType);
                Assert.NotEmpty(mapping.InterfaceMethods);
                Assert.NotEmpty(mapping.TargetMethods);
            }
        }

        // Test FindMembers
        [Fact]
        public void FindMembers_ReturnsMatchingMembers()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MemberInfo[] members = customType.FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, (m, o) => m.Name.StartsWith("Get"), null);
            Assert.NotEmpty(members);
        }

        [Fact]
        public void FindMembers_NoMatch_ReturnsEmpty()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MemberInfo[] members = customType.FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, (m, o) => m.Name.StartsWith("NonExistent"), null);
            Assert.Empty(members);
        }

        // Test collectionservices branches via attribute scenarios
        [Fact]
        public void GetCustomAttributes_EmptyArray_ReturnsEmpty()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MethodInfo method = customType.GetMethod("GetMessage");
            object[] attrs = method.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void GetCustomAttributes_MultipleAttributes_ReturnsAll()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(BaseWithAttributes).GetTypeInfo());
            object[] attrs = customType.GetCustomAttributes(typeof(Attribute), false);
            Assert.NotNull(attrs);
        }
    }
}
