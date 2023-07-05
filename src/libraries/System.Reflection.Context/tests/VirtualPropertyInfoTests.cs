// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class VirtualPropertyInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new VirtualPropertyInfoCustomReflectionContext();
        // Points to a PropertyInfo instance created by reflection. This doesn't work in a reflection-only context.
        private readonly PropertyInfo[] _virtualProperties;
        // Fully functional virtual property with getter and setter.
        private readonly PropertyInfo _virtualProperty;
        private readonly PropertyInfo _noGetterVirtualProperty;
        private readonly PropertyInfo _noSetterVirtualProperty;
        // Test data
        private readonly TestObject _testObject = new TestObject("Age");

        public VirtualPropertyInfoTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _virtualProperties = customTypeInfo.DeclaredProperties.ToArray();
            _virtualProperty = _virtualProperties[2];
            _noGetterVirtualProperty = _virtualProperties[3];
            _noSetterVirtualProperty = _virtualProperties[4];
        }

        [Fact]
        public void Ctor_NullPropertyName_Throws()
        {
            TypeInfo typeInfo = typeof(NullPropertyNameCase).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            AssertExtensions.Throws<ArgumentNullException>("name", () => customTypeInfo.DeclaredProperties);
        }

        [Fact]
        public void Ctor_EmptyPropertyName_Throws()
        {
            TypeInfo typeInfo = typeof(EmptyPropertyNameCase).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            Assert.Throws<ArgumentException>(() => customTypeInfo.DeclaredProperties);
        }

        [Fact]
        public void Ctor_NullPropertyType_Throws()
        {
            TypeInfo typeInfo = typeof(NullPropertyTypeCase).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            AssertExtensions.Throws<ArgumentNullException>("propertyType", () => customTypeInfo.DeclaredProperties);
        }

        [Fact]
        public void Ctor_GetterAndSetterNull_Throws()
        {
            TypeInfo typeInfo = typeof(NullGetterAndSetterCase).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            Assert.Throws<ArgumentException>(() => customTypeInfo.DeclaredProperties);
        }

        [Fact]
        public void Ctor_WrongPropertyType_Throws()
        {
            TypeInfo typeInfo = typeof(WrongContextCase).GetTypeInfo();
            TypeInfo customTypeInfo = _customReflectionContext.MapType(typeInfo);
            Assert.Throws<ArgumentException>(() => customTypeInfo.DeclaredProperties);
        }

        [Fact]
        public void ProjectionTest()
        {
            Assert.Equal(ProjectionConstants.VirtualPropertyInfo, _virtualProperty.GetType().FullName);
        }

        [Fact]
        public void GetCustomAttributes_WithType_Test()
        {
            object[] attributes = _virtualProperty.GetCustomAttributes(typeof(TestPropertyAttribute), true);
            Assert.Single(attributes);
            Assert.IsType<TestPropertyAttribute>(attributes[0]);
        }

        [Fact]
        public void GetCustomAttributes_NoType_Test()
        {
            object[] attributes = _virtualProperty.GetCustomAttributes(false);
            Assert.Single(attributes);
            Assert.IsType<TestPropertyAttribute>(attributes[0]);
        }

        [Fact]
        public void GetCustomAttributesDataTest()
        {
            // This will never return any results as virtual properties never have custom attributes
            // defined in code as they are instantiated during runtime. But as the method is overridden
            // we call it for code coverage.
            IList<CustomAttributeData> customAttributesData = _virtualProperty.GetCustomAttributesData();
            Assert.Empty(customAttributesData);
        }

        [Fact]
        public void IsDefinedTest()
        {
            Assert.True(_virtualProperty.IsDefined(typeof(TestPropertyAttribute), true));
            Assert.True(_virtualProperty.IsDefined(typeof(TestPropertyAttribute), false));
            Assert.False(_virtualProperty.IsDefined(typeof(DataContractAttribute), true));
            Assert.False(_virtualProperty.IsDefined(typeof(DataContractAttribute), false));
        }

        [Fact]
        public void ToStringTest()
        {
            Assert.Equal(typeof(int).FullName + " " + "number", _virtualProperty.ToString());
        }

        [Fact]
        public void AttributesTest()
        {
            Assert.Equal(PropertyAttributes.None, _virtualProperty.Attributes);
        }

        [Fact]
        public void CanReadTest()
        {
            Assert.True(_virtualProperty.CanRead);
            Assert.False(_noGetterVirtualProperty.CanRead);
        }

        [Fact]
        public void CanWriteTest()
        {
            Assert.True(_virtualProperty.CanWrite);
            Assert.False(_noSetterVirtualProperty.CanWrite);
        }

        [Fact]
        public void MetadataTokenTest()
        {
            Assert.Throws<InvalidOperationException>(() => _virtualProperty.MetadataToken);
        }

        [Fact]
        public void ModuleTest()
        {
            Assert.Equal(ProjectionConstants.CustomModule, _virtualProperty.Module.GetType().FullName);
        }

        [Fact]
        public void ReflectedTypeTest()
        {
            Assert.Equal(ProjectionConstants.CustomType, _virtualProperty.ReflectedType.GetType().FullName);
        }

        [Fact]
        public void GetAccessorsTest()
        {
            MethodInfo[] virtualAccessors = _virtualProperty.GetAccessors(false);
            Assert.Equal(2, virtualAccessors.Length);
            Assert.Equal(ProjectionConstants.VirtualPropertyInfoGetter, virtualAccessors[0].GetType().FullName);
            Assert.Equal(ProjectionConstants.VirtualPropertyInfoSetter, virtualAccessors[1].GetType().FullName);

            virtualAccessors = _noGetterVirtualProperty.GetAccessors(false);
            Assert.Equal(1, virtualAccessors.Length);
            Assert.Equal(ProjectionConstants.VirtualPropertyInfoSetter, virtualAccessors[0].GetType().FullName);

            virtualAccessors = _noSetterVirtualProperty.GetAccessors(false);
            Assert.Equal(1, virtualAccessors.Length);
            Assert.Equal(ProjectionConstants.VirtualPropertyInfoGetter, virtualAccessors[0].GetType().FullName);
        }

        [Fact]
        public void GetIndexParametersTest()
        {
            // Projecting
            ParameterInfo[] customParameters = _virtualProperties[1].GetIndexParameters();
            Assert.Single(customParameters);
            Assert.Equal(ProjectionConstants.CustomParameter, customParameters[0].GetType().FullName);

            // Virtual
            customParameters = _noGetterVirtualProperty.GetIndexParameters();
            Assert.Empty(customParameters);
            // Invoke twice to get cached value.
            customParameters = _noGetterVirtualProperty.GetIndexParameters();
            Assert.Empty(customParameters);

            customParameters = _noSetterVirtualProperty.GetIndexParameters();
            Assert.Empty(customParameters);
            // Invoke twice to get cached value.
            customParameters = _noSetterVirtualProperty.GetIndexParameters();
            Assert.Empty(customParameters);
        }

        [Fact]
        public void GetValue_NoGetter_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                _noGetterVirtualProperty.GetValue(_testObject, BindingFlags.Default, null, null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void GetValue_HasGetter_Success()
        {
            object returnVal = _noSetterVirtualProperty.GetValue(_testObject, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
            Assert.Equal(42, returnVal);
        }

        [Fact]
        public void SetValue_NoSetter_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                _noSetterVirtualProperty.SetValue(_testObject, 42, BindingFlags.Default, null, null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void SetValue_HasSetter_Success()
        {
            _noGetterVirtualProperty.SetValue(_testObject, 42, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
        }

        [Fact]
        public void SetValue_HasSetterWithIndex_Success()
        {
            _noGetterVirtualProperty.SetValue(_testObject, 42, BindingFlags.Default, null, new object[] { }, CultureInfo.InvariantCulture);
        }

        [Fact]
        public void GetConstantValueTest()
        {
            Assert.Throws<InvalidOperationException>(() => _virtualProperty.GetConstantValue());
        }

        [Fact]
        public void GetRawConstantValueTest()
        {
            Assert.Throws<InvalidOperationException>(() => _virtualProperty.GetRawConstantValue());
        }

        [Fact]
        public void GetOptionalCustomModifiersTest()
        {
            Type[] customTypes = _virtualProperty.GetOptionalCustomModifiers();
            Assert.Empty(customTypes);
        }

        [Fact]
        public void GetRequiredCustomModifiersTest()
        {
            Type[] customTypes = _virtualProperty.GetRequiredCustomModifiers();
            Assert.Empty(customTypes);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            _virtualProperty.GetHashCode();
        }
    }
}
