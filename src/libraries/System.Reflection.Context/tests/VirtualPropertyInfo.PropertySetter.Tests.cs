// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class VirtualPropertyInfo_PropertySetter_Tests
    {
        // Points to a PropertyInfo instance created by reflection. This doesn't work in a reflection-only context.
        private readonly MethodInfo _virtualPropertySetter;
        private readonly TestObject _testObject = new TestObject("Age");

        public VirtualPropertyInfo_PropertySetter_Tests()
        {
            var customReflectionContext = new VirtualPropertyInfoCustomReflectionContext();
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customTypeInfo = customReflectionContext.MapType(typeInfo);
            PropertyInfo virtualProperty = customTypeInfo.DeclaredProperties.ElementAt(2);
            _virtualPropertySetter = virtualProperty.GetSetMethod(false);
        }

        [Fact]
        public void ProjectionTest()
        {
            Assert.Equal(ProjectionConstants.VirtualPropertyInfoSetter, _virtualPropertySetter.GetType().FullName);
        }

        [Fact]
        public void GetCustomAttributes_WithType_Test()
        {
            object[] attributes = _virtualPropertySetter.GetCustomAttributes(typeof(TestGetterSetterAttribute), true);
            Assert.Single(attributes);
            Assert.IsType<TestGetterSetterAttribute>(attributes[0]);
        }

        [Fact]
        public void GetCustomAttributes_NoType_Test()
        {
            object[] attributes = _virtualPropertySetter.GetCustomAttributes(false);
            Assert.Single(attributes);  
            Assert.IsType<TestGetterSetterAttribute>(attributes[0]);
        }

        [Fact]
        public void GetCustomAttributesDataTest()
        {
            // This will never return any results as virtual properties never have custom attributes 
            // defined in code as they are instantiated during runtime. But as the method is overriden
            // we call it for code coverage.
            IList<CustomAttributeData> customAttributesData = _virtualPropertySetter.GetCustomAttributesData();
            Assert.Empty(customAttributesData);
        }

        [Fact]
        public void IsDefinedTest()
        {
            Assert.True(_virtualPropertySetter.IsDefined(typeof(TestGetterSetterAttribute), true));
            Assert.True(_virtualPropertySetter.IsDefined(typeof(TestGetterSetterAttribute), false));
            Assert.False(_virtualPropertySetter.IsDefined(typeof(DataContractAttribute), true));
            Assert.False(_virtualPropertySetter.IsDefined(typeof(DataContractAttribute), false));
        }

        [Fact]
        public void GetParametersTest()
        {
            ParameterInfo[] virtualParameters = _virtualPropertySetter.GetParameters();
            Assert.Single(virtualParameters);
            Assert.Equal(ProjectionConstants.VirtualParameter, virtualParameters[0].GetType().FullName);
        }

        [Fact]
        public void Invoke_NullParameter_Throws()
        {
            Assert.Throws<TargetParameterCountException>(() => 
                _virtualPropertySetter.Invoke(null, BindingFlags.GetProperty, null, null, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Invoke_NotSingleParameter_Throws()
        {
            Assert.Throws<TargetParameterCountException>(() =>
                _virtualPropertySetter.Invoke(_testObject, BindingFlags.GetProperty, null, new object[] { }, CultureInfo.InvariantCulture));
            Assert.Throws<TargetParameterCountException>(() =>
                _virtualPropertySetter.Invoke(_testObject, BindingFlags.GetProperty, null, new object[] { "a", 1 }, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Invoke_NullObject_Throws()
        {
            Assert.Throws<TargetException>(() =>
                _virtualPropertySetter.Invoke(null, BindingFlags.GetProperty, null, new object[] { 2 }, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Invoke_WrongObject_Throws()
        {
            Assert.Throws<TargetException>(() =>
                _virtualPropertySetter.Invoke("text", BindingFlags.GetProperty, null, new object[] { 2 }, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Invoke_ValidArguments_Success()
        {
            object returnVal = _virtualPropertySetter.Invoke(_testObject, BindingFlags.GetProperty, null, new object[] { 2 }, CultureInfo.InvariantCulture);
            Assert.Null(returnVal);

        }

        [Fact]
        public void CallingConventionTest()
        {
            Assert.Equal(CallingConventions.HasThis | CallingConventions.Standard, _virtualPropertySetter.CallingConvention);
        }

        [Fact]
        public void ContainsGenericParametersTest()
        {
            Assert.False(_virtualPropertySetter.ContainsGenericParameters);
        }

        [Fact]
        public void IsGenericMethodTest()
        {
            Assert.False(_virtualPropertySetter.IsGenericMethod);
        }

        [Fact]
        public void IsGenericMethodDefinitionTest()
        {
            Assert.False(_virtualPropertySetter.IsGenericMethodDefinition);
        }

        [Fact]
        public void MethodHandleTest()
        {
            Assert.Throws<NotSupportedException>(() => _virtualPropertySetter.MethodHandle);
        }

        [Fact]
        public void ModuleTest()
        {
            Module customModule = _virtualPropertySetter.Module;
            Assert.Equal(ProjectionConstants.CustomModule, customModule.GetType().FullName);
        }

        [Fact]
        public void ReturnParameterTest()
        {
            ParameterInfo virtualReturnParameter = _virtualPropertySetter.ReturnParameter;
            Assert.Equal(ProjectionConstants.VirtualReturnParameter, virtualReturnParameter.GetType().FullName);
            ParameterInfo clone = _virtualPropertySetter.ReturnParameter;

            Assert.Equal(virtualReturnParameter.GetHashCode(), clone.GetHashCode());
            Assert.True(virtualReturnParameter.Equals(clone));
        }

        [Fact]
        public void ReturnTypeCustomAttributes()
        {
            ICustomAttributeProvider virtualReturnParameter = _virtualPropertySetter.ReturnTypeCustomAttributes;
            Assert.Equal(ProjectionConstants.VirtualReturnParameter, virtualReturnParameter.GetType().FullName);
        }

        [Fact]
        public void GetBaseDefinition()
        {
            Assert.Equal(_virtualPropertySetter, _virtualPropertySetter.GetBaseDefinition());
        }

        [Fact]
        public void GetGenericArgumentsTest()
        {
            object[] attributes = _virtualPropertySetter.GetGenericArguments();
            Assert.IsType<Type[]>(attributes);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetGenericMethodDefinitionTest()
        {
            Assert.Throws<InvalidOperationException>(() => _virtualPropertySetter.GetGenericMethodDefinition());
        }

        [Fact]
        public void GetMethodImplementationFlagsTest()
        {
            Assert.Equal(MethodImplAttributes.IL, _virtualPropertySetter.GetMethodImplementationFlags());
        }

        [Fact]
        public void MakeGenericMethodTest()
        {
            Assert.Throws<InvalidOperationException>(() => _virtualPropertySetter.MakeGenericMethod());
        }

        [Fact]
        public void GetCustomAttributesTest()
        {
            Assert.True(_virtualPropertySetter.Equals(_virtualPropertySetter));
        }
    }
}
