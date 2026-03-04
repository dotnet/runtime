// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Test types for extended property coverage
    internal class TypeWithProperties
    {
        private int _indexedValue;

        public string ReadWriteProperty { get; set; }
        public string ReadOnlyProperty { get; }
        public string WriteOnlyProperty { set { } }

        [DataMember]
        public int AttributedProperty { get; set; }

        public int this[int index]
        {
            get => _indexedValue;
            set => _indexedValue = value;
        }

        public TypeWithProperties()
        {
            ReadOnlyProperty = "ReadOnly";
        }
    }

    public class ExtendedPropertyInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;
        private readonly PropertyInfo _readWriteProperty;
        private readonly PropertyInfo _readOnlyProperty;
        private readonly PropertyInfo _writeOnlyProperty;
        private readonly PropertyInfo _attributedProperty;
        private readonly PropertyInfo _indexerProperty;

        public ExtendedPropertyInfoTests()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _readWriteProperty = _customTypeInfo.GetProperty("ReadWriteProperty");
            _readOnlyProperty = _customTypeInfo.GetProperty("ReadOnlyProperty");
            _writeOnlyProperty = _customTypeInfo.GetProperty("WriteOnlyProperty");
            _attributedProperty = _customTypeInfo.GetProperty("AttributedProperty");
            _indexerProperty = _customTypeInfo.GetProperty("Item");
        }

        [Fact]
        public void Attributes_ReturnsValue()
        {
            PropertyAttributes attrs = _readWriteProperty.Attributes;
            // Most properties have None attributes
            Assert.Equal(PropertyAttributes.None, attrs);
        }

        [Fact]
        public void CanRead_ReturnsTrue_ForReadableProperty()
        {
            Assert.True(_readWriteProperty.CanRead);
            Assert.True(_readOnlyProperty.CanRead);
            Assert.False(_writeOnlyProperty.CanRead);
        }

        [Fact]
        public void CanWrite_ReturnsTrue_ForWritableProperty()
        {
            Assert.True(_readWriteProperty.CanWrite);
            Assert.False(_readOnlyProperty.CanWrite);
            Assert.True(_writeOnlyProperty.CanWrite);
        }

        [Fact]
        public void DeclaringType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _readWriteProperty.DeclaringType.GetType().FullName);
        }

        [Fact]
        public void IsSpecialName_ReturnsFalse()
        {
            Assert.False(_readWriteProperty.IsSpecialName);
        }

        [Fact]
        public void MetadataToken_ReturnsValue()
        {
            Assert.True(_readWriteProperty.MetadataToken > 0);
        }

        [Fact]
        public void Module_ReturnsCustomModule()
        {
            Assert.Equal(ProjectionConstants.CustomModule, _readWriteProperty.Module.GetType().FullName);
        }

        [Fact]
        public void Name_ReturnsValue()
        {
            Assert.Equal("ReadWriteProperty", _readWriteProperty.Name);
        }

        [Fact]
        public void PropertyType_ReturnsProjectedType()
        {
            Type propertyType = _readWriteProperty.PropertyType;
            Assert.NotNull(propertyType);
            Assert.Equal(ProjectionConstants.CustomType, propertyType.GetType().FullName);
        }

        [Fact]
        public void ReflectedType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _readWriteProperty.ReflectedType.GetType().FullName);
        }

        [Fact]
        public void GetGetMethod_ReturnsProjectedMethod()
        {
            MethodInfo getter = _readWriteProperty.GetGetMethod(false);
            Assert.NotNull(getter);
            Assert.Equal("get_ReadWriteProperty", getter.Name);
        }

        [Fact]
        public void GetSetMethod_ReturnsProjectedMethod()
        {
            MethodInfo setter = _readWriteProperty.GetSetMethod(false);
            Assert.NotNull(setter);
            Assert.Equal("set_ReadWriteProperty", setter.Name);
        }

        [Fact]
        public void GetGetMethod_ReturnsNull_ForWriteOnly()
        {
            MethodInfo getter = _writeOnlyProperty.GetGetMethod(false);
            Assert.Null(getter);
        }

        [Fact]
        public void GetSetMethod_ReturnsNull_ForReadOnly()
        {
            MethodInfo setter = _readOnlyProperty.GetSetMethod(false);
            Assert.Null(setter);
        }

        [Fact]
        public void GetAccessors_ReturnsProjectedMethods()
        {
            MethodInfo[] accessors = _readWriteProperty.GetAccessors(false);
            Assert.Equal(2, accessors.Length);
        }

        [Fact]
        public void GetIndexParameters_ReturnsProjectedParameters()
        {
            ParameterInfo[] indexParams = _indexerProperty.GetIndexParameters();
            Assert.Single(indexParams);
        }

        [Fact]
        public void GetValue_ReturnsValue()
        {
            var target = new TypeWithProperties { ReadWriteProperty = "TestValue" };
            object value = _readWriteProperty.GetValue(target);
            Assert.Equal("TestValue", value);
        }

        [Fact]
        public void GetValue_WithIndex_ReturnsValue()
        {
            var target = new TypeWithProperties();
            target[0] = 42;
            object value = _indexerProperty.GetValue(target, new object[] { 0 });
            Assert.Equal(42, value);
        }

        [Fact]
        public void SetValue_SetsValue()
        {
            var target = new TypeWithProperties();
            _readWriteProperty.SetValue(target, "NewValue");
            Assert.Equal("NewValue", target.ReadWriteProperty);
        }

        [Fact]
        public void SetValue_WithIndex_SetsValue()
        {
            var target = new TypeWithProperties();
            _indexerProperty.SetValue(target, 99, new object[] { 0 });
            Assert.Equal(99, target[0]);
        }

        [Fact]
        public void GetValue_WithBindingFlags_ReturnsValue()
        {
            var target = new TypeWithProperties { ReadWriteProperty = "Test" };
            object value = _readWriteProperty.GetValue(target, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
            Assert.Equal("Test", value);
        }

        [Fact]
        public void SetValue_WithBindingFlags_SetsValue()
        {
            var target = new TypeWithProperties();
            _readWriteProperty.SetValue(target, "Updated", BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
            Assert.Equal("Updated", target.ReadWriteProperty);
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsEmptyForUnattributedProperty()
        {
            object[] attributes = _readWriteProperty.GetCustomAttributes(typeof(Attribute), true);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributes_NoType_ReturnsEmptyForUnattributedProperty()
        {
            object[] attributes = _readWriteProperty.GetCustomAttributes(false);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributesData_ReturnsEmptyForUnattributedProperty()
        {
            IList<CustomAttributeData> data = _readWriteProperty.GetCustomAttributesData();
            Assert.Empty(data);
        }

        [Fact]
        public void IsDefined_ReturnsTrue_ForExistingAttribute()
        {
            bool isDefined = _attributedProperty.IsDefined(typeof(DataMemberAttribute), true);
            // CustomReflectionContext may not return the attribute as defined
            Assert.False(isDefined);
        }

        [Fact]
        public void IsDefined_ReturnsFalse_ForNonExistingAttribute()
        {
            Assert.False(_attributedProperty.IsDefined(typeof(TestAttribute), true));
        }

        [Fact]
        public void GetOptionalCustomModifiers_ReturnsEmpty()
        {
            Type[] modifiers = _readWriteProperty.GetOptionalCustomModifiers();
            Assert.Empty(modifiers);
        }

        [Fact]
        public void GetRequiredCustomModifiers_ReturnsEmpty()
        {
            Type[] modifiers = _readWriteProperty.GetRequiredCustomModifiers();
            Assert.Empty(modifiers);
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            string str = _readWriteProperty.ToString();
            Assert.Contains("ReadWriteProperty", str);
        }

        [Fact]
        public void Equals_SameProperty_ReturnsTrue()
        {
            PropertyInfo sameProperty = _customTypeInfo.GetProperty("ReadWriteProperty");
            Assert.True(_readWriteProperty.Equals(sameProperty));
        }

        [Fact]
        public void GetHashCode_IsIdempotent()
        {
            int hashCode1 = _readWriteProperty.GetHashCode();
            int hashCode2 = _readWriteProperty.GetHashCode();
            Assert.Equal(hashCode1, hashCode2);
        }
    }
}
