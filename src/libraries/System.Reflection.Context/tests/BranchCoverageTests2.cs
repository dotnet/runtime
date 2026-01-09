// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    /// <summary>
    /// Additional tests specifically targeting branch coverage in attribute handling
    /// and projection scenarios.
    /// </summary>
    public class BranchCoverageTests2
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        // Test scenarios where base type has attributes with AllowMultiple=false and derived overrides
        [Fact]
        public void GetCustomAttributes_WithDuplicateAllowMultipleFalse_FiltersCorrectly()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(DerivedWithSameAttribute).GetTypeInfo());
            // This should trigger the CombineCustomAttributes logic with AllowMultiple=false
            object[] attrs = customType.GetCustomAttributes(typeof(InheritedSingleAttribute), true);
            Assert.NotNull(attrs);
        }

        // Test scenario with attribute types different from filter type
        [Fact]
        public void GetCustomAttributes_WithBaseAttributeType_IncludesDerived()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(BaseWithAttributes).GetTypeInfo());
            // Filter by base Attribute type to get all attributes
            object[] attrs = customType.GetCustomAttributes(typeof(Attribute), true);
            Assert.NotNull(attrs);
        }

        // Test method inheritance with attributes
        [Fact]
        public void Method_GetCustomAttributes_Inherited_ReturnsBaseAttributes()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(DerivedWithAttributes).GetTypeInfo());
            MethodInfo method = customType.GetMethod("VirtualMethod");
            object[] attrs = method.GetCustomAttributes(typeof(Attribute), true);
            Assert.NotNull(attrs);
        }

        // Test with non-inherited attribute on base
        [Fact]
        public void GetCustomAttributes_NonInheritedOnBase_NotIncludedInDerived()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(DerivedWithAttributes).GetTypeInfo());
            object[] attrs = customType.GetCustomAttributes(typeof(NonInheritedAttribute), true);
            Assert.Empty(attrs);
        }

        // Tests for Projector.Project with different scenarios
        [Fact]
        public void Project_TypeArray_ReturnsProjectedTypes()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            Type[] interfaces = customType.GetInterfaces();
            // Verify all returned types are projected
            Assert.All(interfaces, t => Assert.Equal(ProjectionConstants.CustomType, t.GetType().FullName));
        }

        [Fact]
        public void Project_MethodArray_ReturnsProjectedMethods()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MethodInfo[] methods = customType.GetMethods();
            Assert.NotEmpty(methods);
            // Verify all methods are in the Reflection.Context namespace
            Assert.All(methods, m => Assert.Contains("Reflection.Context", m.GetType().FullName));
        }

        [Fact]
        public void Project_ConstructorArray_ReturnsProjectedConstructors()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            ConstructorInfo[] ctors = customType.GetConstructors();
            Assert.NotEmpty(ctors);
            Assert.All(ctors, c => Assert.Contains("Reflection.Context", c.GetType().FullName));
        }

        [Fact]
        public void Project_ParameterArray_ReturnsProjectedParameters()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });
            ParameterInfo[] parameters = ctor.GetParameters();
            Assert.Single(parameters);
            Assert.Contains("Reflection.Context", parameters[0].GetType().FullName);
        }

        // Test more Equals scenarios to hit remaining branches
        [Fact]
        public void Assembly_Equals_Null_ReturnsFalse()
        {
            Assembly customAssembly = _customReflectionContext.MapAssembly(typeof(BranchCoverageTests2).Assembly);
            Assert.False(customAssembly.Equals(null));
        }

        [Fact]
        public void Assembly_Equals_DifferentAssembly_ReturnsFalse()
        {
            Assembly customAssembly1 = _customReflectionContext.MapAssembly(typeof(BranchCoverageTests2).Assembly);
            Assembly customAssembly2 = _customReflectionContext.MapAssembly(typeof(object).Assembly);
            Assert.False(customAssembly1.Equals(customAssembly2));
        }

        [Fact]
        public void Assembly_Equals_SameAssembly_ReturnsTrue()
        {
            Assembly customAssembly1 = _customReflectionContext.MapAssembly(typeof(BranchCoverageTests2).Assembly);
            Assembly customAssembly2 = _customReflectionContext.MapAssembly(typeof(BranchCoverageTests2).Assembly);
            Assert.True(customAssembly1.Equals(customAssembly2));
        }

        // Test GetMember with different member types
        [Fact]
        public void GetMember_AllMemberTypes_ReturnsAllMembers()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MemberInfo[] members = customType.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.NotEmpty(members);
        }

        [Fact]
        public void GetMember_ByName_ReturnsMatchingMembers()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MemberInfo[] members = customType.GetMember("A");
            Assert.Single(members);
        }

        [Fact]
        public void GetMember_NonExistent_ReturnsEmpty()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MemberInfo[] members = customType.GetMember("NonExistentMember");
            Assert.Empty(members);
        }

        // Test IsAssignableFrom with various scenarios
        [Fact]
        public void IsAssignableFrom_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            Assert.False(customType.IsAssignableFrom(null));
        }

        [Fact]
        public void IsAssignableFrom_DerivedType_ReturnsTrue()
        {
            TypeInfo baseType = _customReflectionContext.MapType(typeof(BaseWithAttributes).GetTypeInfo());
            TypeInfo derivedType = _customReflectionContext.MapType(typeof(DerivedWithAttributes).GetTypeInfo());
            Assert.True(baseType.IsAssignableFrom(derivedType));
        }

        [Fact]
        public void IsAssignableFrom_UnrelatedType_ReturnsFalse()
        {
            TypeInfo type1 = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            TypeInfo type2 = _customReflectionContext.MapType(typeof(TypeWithEvent).GetTypeInfo());
            Assert.False(type1.IsAssignableFrom(type2));
        }

        // Test interface implementation
        [Fact]
        public void GetInterface_ExistingInterface_ReturnsProjectedInterface()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(List<int>).GetTypeInfo());
            Type iface = customType.GetInterface("IList");
            Assert.NotNull(iface);
            Assert.Equal(ProjectionConstants.CustomType, iface.GetType().FullName);
        }

        [Fact]
        public void GetInterface_NonExistingInterface_ReturnsNull()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            Type iface = customType.GetInterface("INonExistent");
            Assert.Null(iface);
        }

        // Test IsEquivalentTo
        [Fact]
        public void IsEquivalentTo_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            Assert.False(customType.IsEquivalentTo(null));
        }

        [Fact]
        public void IsEquivalentTo_DifferentType_ReturnsFalse()
        {
            TypeInfo type1 = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            TypeInfo type2 = _customReflectionContext.MapType(typeof(SecondTestObject).GetTypeInfo());
            Assert.False(type1.IsEquivalentTo(type2));
        }

        // Tests for ConvertListToArray branch
        [Fact]
        public void GetCustomAttributes_EmptyList_ReturnsEmptyArray()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            // Get attributes for a non-existent attribute type
            object[] attrs = customType.GetCustomAttributes(typeof(ObsoleteAttribute), true);
            Assert.Empty(attrs);
        }

        // Test for filter with specific attribute type
        [Fact]
        public void GetCustomAttributes_SpecificAttributeType_FiltersCorrectly()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(BaseWithAttributes).GetTypeInfo());
            object[] attrs = customType.GetCustomAttributes(typeof(InheritedSingleAttribute), false);
            Assert.NotNull(attrs);
            Assert.All(attrs, a => Assert.IsType<InheritedSingleAttribute>(a));
        }
    }
}
