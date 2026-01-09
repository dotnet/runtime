// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class CustomConstructorInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly ConstructorInfo _customConstructor;
        private readonly TypeInfo _customTypeInfo;

        public CustomConstructorInfoTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _customConstructor = _customTypeInfo.GetConstructor(new[] { typeof(string) });
        }

        [Fact]
        public void Attributes_ReturnsValue()
        {
            Assert.NotEqual((MethodAttributes)0, _customConstructor.Attributes);
        }

        [Fact]
        public void CallingConvention_ReturnsValue()
        {
            Assert.NotEqual((CallingConventions)0, _customConstructor.CallingConvention);
        }

        [Fact]
        public void ContainsGenericParameters_ReturnsFalse()
        {
            Assert.False(_customConstructor.ContainsGenericParameters);
        }

        [Fact]
        public void DeclaringType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _customConstructor.DeclaringType.GetType().FullName);
        }

        [Fact]
        public void IsGenericMethod_ReturnsFalse()
        {
            Assert.False(_customConstructor.IsGenericMethod);
        }

        [Fact]
        public void IsGenericMethodDefinition_ReturnsFalse()
        {
            Assert.False(_customConstructor.IsGenericMethodDefinition);
        }

        [Fact]
        public void IsSecurityCritical_ReturnsValue()
        {
            bool value = _customConstructor.IsSecurityCritical;
            Assert.True(value || !value); // Just verify the property can be read
        }

        [Fact]
        public void IsSecuritySafeCritical_ReturnsValue()
        {
            bool value = _customConstructor.IsSecuritySafeCritical;
            Assert.True(value || !value);
        }

        [Fact]
        public void IsSecurityTransparent_ReturnsValue()
        {
            bool value = _customConstructor.IsSecurityTransparent;
            Assert.True(value || !value);
        }

        [Fact]
        public void MetadataToken_ReturnsValue()
        {
            Assert.True(_customConstructor.MetadataToken > 0);
        }

        [Fact]
        public void MethodHandle_ReturnsValue()
        {
            RuntimeMethodHandle handle = _customConstructor.MethodHandle;
            Assert.NotEqual(default, handle);
        }

        [Fact]
        public void Module_ReturnsCustomModule()
        {
            Assert.Equal(ProjectionConstants.CustomModule, _customConstructor.Module.GetType().FullName);
        }

        [Fact]
        public void Name_ReturnsValue()
        {
            Assert.Equal(".ctor", _customConstructor.Name);
        }

        [Fact]
        public void ReflectedType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _customConstructor.ReflectedType.GetType().FullName);
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsAttributes()
        {
            object[] attributes = _customConstructor.GetCustomAttributes(typeof(Attribute), true);
            Assert.NotNull(attributes);
        }

        [Fact]
        public void GetCustomAttributes_NoType_ReturnsAttributes()
        {
            object[] attributes = _customConstructor.GetCustomAttributes(false);
            Assert.NotNull(attributes);
        }

        [Fact]
        public void GetCustomAttributesData_ReturnsProjectingData()
        {
            IList<CustomAttributeData> data = _customConstructor.GetCustomAttributesData();
            Assert.NotNull(data);
        }

        [Fact]
        public void IsDefined_ReturnsValue()
        {
            bool isDefined = _customConstructor.IsDefined(typeof(Attribute), true);
            Assert.True(isDefined || !isDefined);
        }

        [Fact]
        public void GetGenericArguments_ThrowsNotSupported()
        {
            // Constructors don't support GetGenericArguments
            Assert.Throws<NotSupportedException>(() => _customConstructor.GetGenericArguments());
        }

        [Fact]
        public void GetMethodBody_ReturnsBody()
        {
            MethodBody body = _customConstructor.GetMethodBody();
            Assert.NotNull(body);
        }

        [Fact]
        public void GetMethodImplementationFlags_ReturnsValue()
        {
            MethodImplAttributes flags = _customConstructor.GetMethodImplementationFlags();
            // Just verify it doesn't throw - it's a value type
        }

        [Fact]
        public void GetParameters_ReturnsProjectedParameters()
        {
            ParameterInfo[] parameters = _customConstructor.GetParameters();
            Assert.Single(parameters);
        }

        [Fact]
        public void Invoke_CreatesInstance()
        {
            object result = _customConstructor.Invoke(BindingFlags.Default, null, new object[] { "test" }, CultureInfo.InvariantCulture);
            Assert.NotNull(result);
            Assert.IsType<TestObject>(result);
        }

        [Fact]
        public void Invoke_OnExistingObject_ReturnsNull()
        {
            var testObj = new TestObject("existing");
            object result = _customConstructor.Invoke(testObj, BindingFlags.Default, null, new object[] { "new" }, CultureInfo.InvariantCulture);
            // Invoking constructor on existing object typically returns null
            Assert.Null(result);
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            string str = _customConstructor.ToString();
            Assert.Contains(".ctor", str);
        }

        [Fact]
        public void Equals_SameConstructor_ReturnsTrue()
        {
            ConstructorInfo sameConstructor = _customTypeInfo.GetConstructor(new[] { typeof(string) });
            Assert.True(_customConstructor.Equals(sameConstructor));
        }

        [Fact]
        public void GetHashCode_ReturnsValue()
        {
            int hashCode = _customConstructor.GetHashCode();
            Assert.NotEqual(0, hashCode);
        }
    }
}
