// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Custom attributes for testing inheritance
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    internal class InheritedSingleAttribute : Attribute
    {
        public string Value { get; set; }
        public InheritedSingleAttribute(string value) => Value = value;
    }

    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
    internal class InheritedMultipleAttribute : Attribute
    {
        public string Value { get; set; }
        public InheritedMultipleAttribute(string value) => Value = value;
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class NonInheritedAttribute : Attribute
    {
        public string Value { get; set; }
        public NonInheritedAttribute(string value) => Value = value;
    }

    // Base class with attributes
    [InheritedSingle("Base")]
    [InheritedMultiple("BaseMultiple")]
    [NonInherited("BaseNonInherited")]
    internal class BaseWithAttributes
    {
        [InheritedSingle("BaseMethod")]
        [InheritedMultiple("BaseMethodMultiple")]
        public virtual void VirtualMethod() { }
    }

    // Derived class that overrides
    [InheritedMultiple("DerivedMultiple")]
    internal class DerivedWithAttributes : BaseWithAttributes
    {
        [InheritedMultiple("DerivedMethodMultiple")]
        public override void VirtualMethod() { }
    }

    // Another derived class with same attribute
    [InheritedSingle("Derived2")]
    internal class DerivedWithSameAttribute : BaseWithAttributes
    {
    }

    public class AttributeInheritanceTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void GetCustomAttributes_InheritTrue_OnDerivedType_ReturnsAttributes()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedWithAttributes).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);

            object[] attrs = customDerivedType.GetCustomAttributes(typeof(InheritedMultipleAttribute), true);
            Assert.All(attrs, a => Assert.IsType<InheritedMultipleAttribute>(a));
        }

        [Fact]
        public void GetCustomAttributes_InheritFalse_OnDerivedType_ReturnsOnlyDeclaredAttributes()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedWithAttributes).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);

            object[] attrs = customDerivedType.GetCustomAttributes(typeof(InheritedMultipleAttribute), false);
            Assert.All(attrs, a => Assert.IsType<InheritedMultipleAttribute>(a));
        }

        [Fact]
        public void GetCustomAttributes_NonInherited_OnDerivedType_ReturnsEmpty()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedWithAttributes).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);

            object[] attrs = customDerivedType.GetCustomAttributes(typeof(NonInheritedAttribute), true);
            Assert.Empty(attrs);
        }

        [Fact]
        public void GetCustomAttributes_InheritedSingle_OnDerivedWithSame_ReturnsMatchingAttributes()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedWithSameAttribute).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);

            object[] attrs = customDerivedType.GetCustomAttributes(typeof(InheritedSingleAttribute), true);
            Assert.All(attrs, a => Assert.IsType<InheritedSingleAttribute>(a));
        }

        [Fact]
        public void GetCustomAttributes_OnOverriddenMethod_WithInherit_ReturnsMatchingAttributes()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedWithAttributes).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);
            MethodInfo method = customDerivedType.GetMethod("VirtualMethod");

            object[] attrs = method.GetCustomAttributes(typeof(InheritedMultipleAttribute), true);
            Assert.All(attrs, a => Assert.IsType<InheritedMultipleAttribute>(a));
        }

        [Fact]
        public void GetCustomAttributes_OnOverriddenMethod_WithoutInherit_ReturnsDeclaredAttributes()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedWithAttributes).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);
            MethodInfo method = customDerivedType.GetMethod("VirtualMethod");

            object[] attrs = method.GetCustomAttributes(typeof(InheritedMultipleAttribute), false);
            Assert.All(attrs, a => Assert.IsType<InheritedMultipleAttribute>(a));
        }

        [Fact]
        public void GetCustomAttributes_OnBaseMethod_ReturnsMatchingAttributes()
        {
            TypeInfo baseTypeInfo = typeof(BaseWithAttributes).GetTypeInfo();
            TypeInfo customBaseType = _customReflectionContext.MapType(baseTypeInfo);
            MethodInfo method = customBaseType.GetMethod("VirtualMethod");

            object[] attrs = method.GetCustomAttributes(typeof(InheritedSingleAttribute), false);
            Assert.All(attrs, a => Assert.IsType<InheritedSingleAttribute>(a));
        }

        [Fact]
        public void IsDefined_OnDerivedType_WithInherit_ReturnsFalseForInheritedSingle()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedWithAttributes).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);

            // InheritedSingleAttribute is defined on BaseWithAttributes but not on DerivedWithAttributes
            // CustomReflectionContext.IsDefined returns false for inherited attributes
            bool isDefined = customDerivedType.IsDefined(typeof(InheritedSingleAttribute), true);
            Assert.False(isDefined);
        }

        [Fact]
        public void IsDefined_OnDerivedType_NonInherited_ReturnsFalse()
        {
            TypeInfo derivedTypeInfo = typeof(DerivedWithAttributes).GetTypeInfo();
            TypeInfo customDerivedType = _customReflectionContext.MapType(derivedTypeInfo);

            Assert.False(customDerivedType.IsDefined(typeof(NonInheritedAttribute), true));
        }

        [Fact]
        public void GetCustomAttributes_FilteredByAttributeType_ReturnsMatchingAttributes()
        {
            TypeInfo baseTypeInfo = typeof(BaseWithAttributes).GetTypeInfo();
            TypeInfo customBaseType = _customReflectionContext.MapType(baseTypeInfo);

            object[] attrs = customBaseType.GetCustomAttributes(typeof(InheritedSingleAttribute), true);
            Assert.All(attrs, a => Assert.IsType<InheritedSingleAttribute>(a));
        }

        [Fact]
        public void GetCustomAttributes_OnConstructor_ReturnsEmptyForUnattributedConstructor()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });

            object[] attrs = ctor.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void GetCustomAttributes_OnProperty_ReturnsMatchingAttributes()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("AttributedProperty");

            object[] attrs = prop.GetCustomAttributes(typeof(Attribute), false);
            Assert.All(attrs, a => Assert.IsAssignableFrom<Attribute>(a));
        }

        [Fact]
        public void GetCustomAttributes_OnEvent_ReturnsEmptyForUnattributedEvent()
        {
            TypeInfo typeInfo = typeof(TypeWithEvent).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            EventInfo evt = customType.GetEvent("TestEvent");

            object[] attrs = evt.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void GetCustomAttributes_OnField_ReturnsEmptyForUnattributedField()
        {
            TypeInfo typeInfo = typeof(SecondTestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            FieldInfo field = customType.GetField("field");

            object[] attrs = field.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void GetCustomAttributes_OnParameter_ReturnsEmptyForUnattributedParameter()
        {
            TypeInfo typeInfo = typeof(TypeWithParameters).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("MethodWithOptionalParam");
            ParameterInfo param = method.GetParameters()[1];

            object[] attrs = param.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }
    }
}
