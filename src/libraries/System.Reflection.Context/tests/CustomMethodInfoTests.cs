// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class CustomMethodInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly MethodInfo _customMethod;
        private readonly TypeInfo _customTypeInfo;

        public CustomMethodInfoTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _customMethod = _customTypeInfo.GetMethod("GetMessage");
        }

        [Fact]
        public void Attributes_ReturnsValue()
        {
            Assert.NotEqual((MethodAttributes)0, _customMethod.Attributes);
        }

        [Fact]
        public void CallingConvention_ReturnsValue()
        {
            Assert.NotEqual((CallingConventions)0, _customMethod.CallingConvention);
        }

        [Fact]
        public void ContainsGenericParameters_ReturnsFalse()
        {
            Assert.False(_customMethod.ContainsGenericParameters);
        }

        [Fact]
        public void DeclaringType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _customMethod.DeclaringType.GetType().FullName);
        }

        [Fact]
        public void IsGenericMethod_ReturnsFalse()
        {
            Assert.False(_customMethod.IsGenericMethod);
        }

        [Fact]
        public void IsGenericMethodDefinition_ReturnsFalse()
        {
            Assert.False(_customMethod.IsGenericMethodDefinition);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15191", TestRuntimes.Mono)]
        public void IsSecurityCritical_ReturnsValue()
        {
            bool value = _customMethod.IsSecurityCritical;
            Assert.True(value);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15191", TestRuntimes.Mono)]
        public void IsSecuritySafeCritical_ReturnsValue()
        {
            bool value = _customMethod.IsSecuritySafeCritical;
            Assert.False(value);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15191", TestRuntimes.Mono)]
        public void IsSecurityTransparent_ReturnsValue()
        {
            bool value = _customMethod.IsSecurityTransparent;
            Assert.False(value);
        }

        [Fact]
        public void MetadataToken_ReturnsValue()
        {
            Assert.True(_customMethod.MetadataToken > 0);
        }

        [Fact]
        public void MethodHandle_ReturnsValue()
        {
            RuntimeMethodHandle handle = _customMethod.MethodHandle;
            Assert.NotEqual(default, handle);
        }

        [Fact]
        public void Module_ReturnsCustomModule()
        {
            Assert.Equal(ProjectionConstants.CustomModule, _customMethod.Module.GetType().FullName);
        }

        [Fact]
        public void Name_ReturnsValue()
        {
            Assert.Equal("GetMessage", _customMethod.Name);
        }

        [Fact]
        public void ReflectedType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _customMethod.ReflectedType.GetType().FullName);
        }

        [Fact]
        public void ReturnParameter_ReturnsProjectedParameter()
        {
            ParameterInfo returnParam = _customMethod.ReturnParameter;
            Assert.NotNull(returnParam);
        }

        [Fact]
        public void ReturnType_ReturnsProjectedType()
        {
            Type returnType = _customMethod.ReturnType;
            Assert.Equal(typeof(string).Name, returnType.Name);
        }

        [Fact]
        public void ReturnTypeCustomAttributes_ReturnsValue()
        {
            ICustomAttributeProvider provider = _customMethod.ReturnTypeCustomAttributes;
            Assert.NotNull(provider);
        }

        [Fact]
        public void GetBaseDefinition_ReturnsProjectedMethod()
        {
            MethodInfo baseDef = _customMethod.GetBaseDefinition();
            Assert.NotNull(baseDef);
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsTestAttribute()
        {
            // TestCustomReflectionContext adds TestAttribute to GetMessage method
            object[] attributes = _customMethod.GetCustomAttributes(typeof(Attribute), true);
            Assert.Contains(attributes, a => a is TestAttribute);
        }

        [Fact]
        public void GetCustomAttributes_NoType_ReturnsTestAttribute()
        {
            // TestCustomReflectionContext adds TestAttribute to GetMessage method
            object[] attributes = _customMethod.GetCustomAttributes(false);
            Assert.Contains(attributes, a => a is TestAttribute);
        }

        [Fact]
        public void GetCustomAttributesData_ReturnsDataForTestAttribute()
        {
            // TestCustomReflectionContext adds TestAttribute to GetMessage method
            IList<CustomAttributeData> data = _customMethod.GetCustomAttributesData();
            Assert.NotEmpty(data);
        }

        [Fact]
        public void IsDefined_ForTestAttribute_ReturnsTrue()
        {
            // TestCustomReflectionContext adds TestAttribute to GetMessage method
            bool isDefined = _customMethod.IsDefined(typeof(TestAttribute), true);
            Assert.True(isDefined);
        }

        [Fact]
        public void GetGenericArguments_ReturnsProjectedTypes()
        {
            Type[] args = _customMethod.GetGenericArguments();
            Assert.Empty(args);
        }

        [Fact]
        public void GetMethodBody_ReturnsBodyWithLocals()
        {
            MethodBody body = _customMethod.GetMethodBody();
            Assert.NotNull(body);
            Assert.NotNull(body.LocalVariables);
        }

        [Fact]
        public void GetMethodImplementationFlags_ReturnsIL()
        {
            MethodImplAttributes flags = _customMethod.GetMethodImplementationFlags();
            Assert.Equal(MethodImplAttributes.IL, flags);
        }

        [Fact]
        public void GetParameters_ReturnsEmptyForNoArgMethod()
        {
            ParameterInfo[] parameters = _customMethod.GetParameters();
            Assert.Empty(parameters);
        }

        [Fact]
        public void Invoke_ReturnsValue()
        {
            var testObj = new TestObject("Hello");
            object result = _customMethod.Invoke(testObj, BindingFlags.Default, null, null, CultureInfo.InvariantCulture);
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void CreateDelegate_ReturnsDelegate()
        {
            var testObj = new TestObject("World");
            Delegate del = _customMethod.CreateDelegate(typeof(Func<string>), testObj);
            Assert.NotNull(del);
            Assert.Equal("World", ((Func<string>)del)());
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            string str = _customMethod.ToString();
            Assert.Contains("GetMessage", str);
        }

        [Fact]
        public void Equals_SameMethod_ReturnsTrue()
        {
            MethodInfo sameMethod = _customTypeInfo.GetMethod("GetMessage");
            Assert.True(_customMethod.Equals(sameMethod));
        }

        [Fact]
        public void GetHashCode_IsIdempotent()
        {
            int hashCode1 = _customMethod.GetHashCode();
            int hashCode2 = _customMethod.GetHashCode();
            Assert.Equal(hashCode1, hashCode2);
        }
    }
}
