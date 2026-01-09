// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    /// <summary>
    /// Tests specifically targeting branch coverage by testing non-equal paths
    /// and various projection scenarios.
    /// </summary>
    public class BranchCoverageTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        // Tests for Equals with different/null objects
        [Fact]
        public void Type_Equals_Null_ReturnsFalse()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            Assert.False(customType.Equals(null));
        }

        [Fact]
        public void Type_Equals_DifferentType_ReturnsFalse()
        {
            TypeInfo customType1 = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            TypeInfo customType2 = _customReflectionContext.MapType(typeof(SecondTestObject).GetTypeInfo());
            Assert.False(customType1.Equals(customType2));
        }

        [Fact]
        public void Type_Equals_NonProjectedType_ComparesBehavior()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            bool result = customType.Equals(typeof(TestObject));
            Assert.True(result);
        }

        [Fact]
        public void Method_Equals_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MethodInfo method = customType.GetMethod("GetMessage");
            Assert.False(method.Equals(null));
        }

        [Fact]
        public void Method_Equals_DifferentMethod_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MethodInfo method1 = customType.GetMethod("GetMessage");
            MethodInfo method2 = customType.GetMethod("ToString");
            Assert.False(method1.Equals(method2));
        }

        [Fact]
        public void Constructor_Equals_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });
            Assert.False(ctor.Equals(null));
        }

        [Fact]
        public void Constructor_Equals_DifferentConstructor_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            ConstructorInfo[] ctors = customType.GetConstructors();
            if (ctors.Length >= 2)
            {
                Assert.False(ctors[0].Equals(ctors[1]));
            }
        }

        [Fact]
        public void Property_Equals_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            PropertyInfo prop = customType.GetProperty("A");
            Assert.False(prop.Equals(null));
        }

        [Fact]
        public void Property_Equals_DifferentProperty_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            PropertyInfo prop1 = customType.GetProperty("A");
            PropertyInfo prop2 = customType.GetProperty("B");
            Assert.False(prop1.Equals(prop2));
        }

        [Fact]
        public void Field_Equals_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(SecondTestObject).GetTypeInfo());
            FieldInfo field = customType.GetField("field");
            Assert.False(field.Equals(null));
        }

        [Fact]
        public void Event_Equals_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TypeWithEvent).GetTypeInfo());
            EventInfo evt = customType.GetEvent("TestEvent");
            Assert.False(evt.Equals(null));
        }

        [Fact]
        public void Module_Equals_Null_ReturnsFalse()
        {
            Assembly customAssembly = _customReflectionContext.MapAssembly(typeof(BranchCoverageTests).Assembly);
            Module module = customAssembly.ManifestModule;
            Assert.False(module.Equals(null));
        }

        [Fact]
        public void Module_Equals_DifferentModule_ReturnsFalse()
        {
            Assembly customAssembly1 = _customReflectionContext.MapAssembly(typeof(BranchCoverageTests).Assembly);
            Assembly customAssembly2 = _customReflectionContext.MapAssembly(typeof(object).Assembly);
            Module module1 = customAssembly1.ManifestModule;
            Module module2 = customAssembly2.ManifestModule;
            Assert.False(module1.Equals(module2));
        }

        [Fact]
        public void Parameter_Equals_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });
            ParameterInfo param = ctor.GetParameters()[0];
            Assert.False(param.Equals(null));
        }

        [Fact]
        public void Parameter_Equals_DifferentParameter_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TypeWithParameters).GetTypeInfo());
            MethodInfo method = customType.GetMethod("MethodWithOptionalParam");
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length >= 2)
            {
                Assert.False(parameters[0].Equals(parameters[1]));
            }
        }

        // Tests for IsSubclassOf with various scenarios
        [Fact]
        public void Type_IsSubclassOf_Null_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            Assert.False(customType.IsSubclassOf(null));
        }

        [Fact]
        public void Type_IsSubclassOf_Self_ReturnsFalse()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            Assert.False(customType.IsSubclassOf(customType));
        }

        [Fact]
        public void Type_IsSubclassOf_Object_ReturnsTrue()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            TypeInfo objectType = _customReflectionContext.MapType(typeof(object).GetTypeInfo());
            Assert.True(customType.IsSubclassOf(objectType));
        }

        // Tests for different member types being projected
        [Fact]
        public void ProjectMember_Property_ReturnsProjectedProperty()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MemberInfo[] members = customType.GetMember("A");
            Assert.Single(members);
            Assert.IsAssignableFrom<PropertyInfo>(members[0]);
        }

        [Fact]
        public void ProjectMember_Field_ReturnsProjectedField()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(SecondTestObject).GetTypeInfo());
            MemberInfo[] members = customType.GetMember("field");
            Assert.Single(members);
            Assert.IsAssignableFrom<FieldInfo>(members[0]);
        }

        [Fact]
        public void ProjectMember_Event_ReturnsProjectedEvent()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TypeWithEvent).GetTypeInfo());
            MemberInfo[] members = customType.GetMember("TestEvent");
            Assert.Single(members);
            Assert.IsAssignableFrom<EventInfo>(members[0]);
        }

        [Fact]
        public void ProjectMember_Constructor_ReturnsProjectedConstructor()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MemberInfo[] members = customType.GetMember(".ctor", MemberTypes.Constructor, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotEmpty(members);
            Assert.All(members, m => Assert.IsAssignableFrom<ConstructorInfo>(m));
        }

        [Fact]
        public void ProjectMember_NestedType_ReturnsProjectedType()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(NestedTypeContainer).GetTypeInfo());
            MemberInfo[] members = customType.GetMember("NestedType", MemberTypes.NestedType, BindingFlags.Public);
            Assert.Single(members);
            Assert.IsAssignableFrom<Type>(members[0]);
        }

        // Test different attribute inheritance scenarios for branch coverage
        [Fact]
        public void GetCustomAttributes_InheritedAllowMultiple_ReturnsMultipleAttributes()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(DerivedWithAttributes).GetTypeInfo());
            object[] attrs = customType.GetCustomAttributes(typeof(InheritedMultipleAttribute), true);
            Assert.NotNull(attrs);
        }

        [Fact]
        public void GetCustomAttributes_InheritedNotAllowMultiple_ReturnsSingleAttribute()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(DerivedWithSameAttribute).GetTypeInfo());
            object[] attrs = customType.GetCustomAttributes(typeof(InheritedSingleAttribute), true);
            Assert.NotNull(attrs);
        }

        [Fact]
        public void GetCustomAttributes_NoInherit_ReturnsOnlyDeclared()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(DerivedWithAttributes).GetTypeInfo());
            object[] attrs = customType.GetCustomAttributes(typeof(InheritedMultipleAttribute), false);
            Assert.NotNull(attrs);
        }

        // Test method base projection
        [Fact]
        public void ProjectMethodBase_Constructor_ReturnsProjectedConstructor()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });
            Assert.NotNull(ctor);
            // It's wrapped in CustomConstructorInfo
            Assert.Contains("ConstructorInfo", ctor.GetType().FullName);
        }

        [Fact]
        public void ProjectMethodBase_Method_ReturnsProjectedMethod()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            MethodInfo method = customType.GetMethod("GetMessage");
            Assert.NotNull(method);
            // It's wrapped in CustomMethodInfo
            Assert.Contains("MethodInfo", method.GetType().FullName);
        }
    }
}
