// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class VirtualPropertyInfo_PropertyGetter_Tests
    {
        // Points to a PropertyInfo instance created by reflection. This doesn't work in a reflection-only context.
        private readonly MethodInfo _virtualPropertyGetter;
        private readonly TestObject _testObject = new TestObject("Age");

        public VirtualPropertyInfo_PropertyGetter_Tests()
        {
            var customReflectionContext = new VirtualPropertyInfoCustomReflectionContext();
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customTypeInfo = customReflectionContext.MapType(typeInfo);
            PropertyInfo virtualProperty = customTypeInfo.DeclaredProperties.ElementAt(2);
            _virtualPropertyGetter = virtualProperty.GetGetMethod(false);
        }

        [Fact]
        public void ProjectionTest()
        {
            Assert.Equal(ProjectionConstants.VirtualPropertyInfoGetter, _virtualPropertyGetter.GetType().FullName);
        }

        [Fact]
        public void GetCustomAttributes_WithType_Test()
        {
            object[] attributes = _virtualPropertyGetter.GetCustomAttributes(typeof(TestGetterSetterAttribute), true);
            Assert.Single(attributes);
            Assert.IsType<TestGetterSetterAttribute>(attributes[0]);
        }

        [Fact]
        public void GetCustomAttributes_NoType_Test()
        {
            object[] attributes = _virtualPropertyGetter.GetCustomAttributes(false);
            Assert.Single(attributes);
            Assert.IsType<TestGetterSetterAttribute>(attributes[0]);
        }

        [Fact]
        public void GetCustomAttributesDataTest()
        {
            // This will never return any results as virtual properties never have custom attributes
            // defined in code as they are instantiated during runtime. But as the method is overridden
            // we call it for code coverage.
            IList<CustomAttributeData> customAttributesData = _virtualPropertyGetter.GetCustomAttributesData();
            Assert.Empty(customAttributesData);
        }

        [Fact]
        public void IsDefinedTest()
        {
            Assert.True(_virtualPropertyGetter.IsDefined(typeof(TestGetterSetterAttribute), true));
            Assert.True(_virtualPropertyGetter.IsDefined(typeof(TestGetterSetterAttribute), false));
            Assert.False(_virtualPropertyGetter.IsDefined(typeof(DataContractAttribute), true));
            Assert.False(_virtualPropertyGetter.IsDefined(typeof(DataContractAttribute), false));
        }

        [Fact]
        public void Invoke_NotEmptyParameter_Throws()
        {
            Assert.Throws<TargetParameterCountException>(() =>
                _virtualPropertyGetter.Invoke(_testObject, BindingFlags.GetProperty, null, new object[] { 1 }, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Invoke_NullObject_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _virtualPropertyGetter.Invoke(null, BindingFlags.GetProperty, null, null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Invoke_WrongObject_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                _virtualPropertyGetter.Invoke("abc", BindingFlags.GetProperty, null, null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Invoke_EmptyParameters_Success()
        {
            object returnVal = _virtualPropertyGetter.Invoke(_testObject, BindingFlags.GetProperty, null, new object[] { }, CultureInfo.InvariantCulture);
            Assert.Equal(42, returnVal);
        }

        [Fact]
        public void Invoke_NullParameters_Success()
        {
            object returnVal = _virtualPropertyGetter.Invoke(_testObject, BindingFlags.GetProperty, null, null, CultureInfo.InvariantCulture);
            Assert.Equal(42, returnVal);
        }
    }
}
