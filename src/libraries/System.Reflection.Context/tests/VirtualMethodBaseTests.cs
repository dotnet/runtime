// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Tests for virtual property accessors to improve coverage of VirtualMethodBase
    public class VirtualMethodBaseTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new VirtualPropertyAddingContext();
        private readonly TypeInfo _customTypeInfo;
        private readonly PropertyInfo _virtualProperty;
        private readonly MethodInfo _getter;
        private readonly MethodInfo _setter;

        public VirtualMethodBaseTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _virtualProperty = _customTypeInfo.GetProperty("VirtualProperty");
            _getter = _virtualProperty.GetGetMethod(true);
            _setter = _virtualProperty.GetSetMethod(true);
        }

        [Fact]
        public void VirtualGetter_Exists()
        {
            Assert.NotNull(_getter);
        }

        [Fact]
        public void VirtualSetter_Exists()
        {
            Assert.NotNull(_setter);
        }

        [Fact]
        public void VirtualGetter_Attributes_HasPublic()
        {
            Assert.True(_getter.Attributes.HasFlag(MethodAttributes.Public));
        }

        [Fact]
        public void VirtualGetter_CallingConvention_HasThis()
        {
            Assert.True(_getter.CallingConvention.HasFlag(CallingConventions.HasThis));
        }

        [Fact]
        public void VirtualGetter_ContainsGenericParameters_ReturnsFalse()
        {
            Assert.False(_getter.ContainsGenericParameters);
        }

        [Fact]
        public void VirtualGetter_IsGenericMethod_ReturnsFalse()
        {
            Assert.False(_getter.IsGenericMethod);
        }

        [Fact]
        public void VirtualGetter_IsGenericMethodDefinition_ReturnsFalse()
        {
            Assert.False(_getter.IsGenericMethodDefinition);
        }

        [Fact]
        public void VirtualGetter_MethodHandle_ThrowsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => _getter.MethodHandle);
        }

        [Fact]
        public void VirtualGetter_Module_ReturnsValue()
        {
            Module module = _getter.Module;
            Assert.NotNull(module);
        }

        [Fact]
        public void VirtualGetter_ReflectedType_EqualsDeclaringType()
        {
            Assert.Equal(_getter.DeclaringType, _getter.ReflectedType);
        }

        [Fact]
        public void VirtualGetter_ReturnParameter_ReturnsValue()
        {
            ParameterInfo returnParam = _getter.ReturnParameter;
            Assert.NotNull(returnParam);
        }

        [Fact]
        public void VirtualGetter_ReturnTypeCustomAttributes_ReturnsValue()
        {
            ICustomAttributeProvider provider = _getter.ReturnTypeCustomAttributes;
            Assert.NotNull(provider);
        }

        [Fact]
        public void VirtualGetter_GetBaseDefinition_ReturnsSelf()
        {
            MethodInfo baseDef = _getter.GetBaseDefinition();
            Assert.Equal(_getter, baseDef);
        }

        [Fact]
        public void VirtualGetter_GetGenericArguments_ReturnsEmpty()
        {
            Type[] args = _getter.GetGenericArguments();
            Assert.Empty(args);
        }

        [Fact]
        public void VirtualGetter_GetGenericMethodDefinition_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _getter.GetGenericMethodDefinition());
        }

        [Fact]
        public void VirtualGetter_GetMethodImplementationFlags_ReturnsIL()
        {
            MethodImplAttributes flags = _getter.GetMethodImplementationFlags();
            Assert.Equal(MethodImplAttributes.IL, flags);
        }

        [Fact]
        public void VirtualGetter_GetParameters_ReturnsEmpty()
        {
            ParameterInfo[] parameters = _getter.GetParameters();
            Assert.Empty(parameters);
        }

        [Fact]
        public void VirtualGetter_MakeGenericMethod_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _getter.MakeGenericMethod(typeof(int)));
        }

        [Fact]
        public void VirtualGetter_GetCustomAttributes_WithType_ReturnsEmpty()
        {
            object[] attrs = _getter.GetCustomAttributes(typeof(Attribute), false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void VirtualGetter_GetCustomAttributes_NoType_ReturnsEmpty()
        {
            object[] attrs = _getter.GetCustomAttributes(false);
            Assert.Empty(attrs);
        }

        [Fact]
        public void VirtualGetter_GetCustomAttributesData_ReturnsEmpty()
        {
            IList<CustomAttributeData> data = _getter.GetCustomAttributesData();
            Assert.Empty(data);
        }

        [Fact]
        public void VirtualGetter_IsDefined_ReturnsFalse()
        {
            Assert.False(_getter.IsDefined(typeof(Attribute), false));
        }

        [Fact]
        public void VirtualGetter_Equals_SameMethod_ReturnsTrue()
        {
            MethodInfo sameGetter = _virtualProperty.GetGetMethod(true);
            Assert.True(_getter.Equals(sameGetter));
        }

        [Fact]
        public void VirtualGetter_GetHashCode_ReturnsValue()
        {
            int hashCode = _getter.GetHashCode();
            Assert.NotEqual(0, hashCode);
        }

        [Fact]
        public void VirtualGetter_ToString_ReturnsValue()
        {
            string str = _getter.ToString();
            Assert.NotNull(str);
            Assert.Contains("get_VirtualProperty", str);
        }

        [Fact]
        public void VirtualSetter_GetParameters_ReturnsValue()
        {
            ParameterInfo[] parameters = _setter.GetParameters();
            Assert.NotEmpty(parameters);
        }

        [Fact]
        public void VirtualSetter_ToString_ReturnsValue()
        {
            string str = _setter.ToString();
            Assert.NotNull(str);
            Assert.Contains("set_VirtualProperty", str);
        }
    }

    // Additional tests for ReflectionContextProjector
    public class ReflectionContextProjectorTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void MapType_WithNull_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => _customReflectionContext.MapType((TypeInfo)null));
        }

        [Fact]
        public void MapAssembly_WithNull_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => _customReflectionContext.MapAssembly(null));
        }

        [Fact]
        public void GetTypeForObject_ReturnsProjectedType()
        {
            var testObj = new TestObject("test");
            TypeInfo typeInfo = _customReflectionContext.GetTypeForObject(testObj);
            Assert.NotNull(typeInfo);
            Assert.Equal(ProjectionConstants.CustomType, typeInfo.GetType().FullName);
        }
    }
}
