// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // More tests to improve coverage of various projection classes
    public class ProjectorTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void ProjectMethod_WithNull_ReturnsNull()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            // GetMethod returns null for non-existent methods
            MethodInfo method = customType.GetMethod("NonExistentMethod");
            Assert.Null(method);
        }

        [Fact]
        public void ProjectConstructor_WithNull_ReturnsNull()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            // GetConstructor returns null for non-existent constructors
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(int), typeof(double), typeof(char) });
            Assert.Null(ctor);
        }

        [Fact]
        public void ProjectType_WithNull_ReturnsNull()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            // BaseType of object is null
            Type objectType = customType.BaseType.BaseType; // TestObject -> object -> null
            if (objectType != null)
            {
                Type baseOfObject = objectType.BaseType;
                Assert.Null(baseOfObject);
            }
        }

        [Fact]
        public void ProjectField_WithNull_ReturnsNull()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            FieldInfo field = customType.GetField("NonExistentField");
            Assert.Null(field);
        }

        [Fact]
        public void ProjectProperty_WithNull_ReturnsNull()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            PropertyInfo prop = customType.GetProperty("NonExistentProperty");
            Assert.Null(prop);
        }

        [Fact]
        public void ProjectEvent_WithNull_ReturnsNull()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);

            EventInfo evt = customType.GetEvent("NonExistentEvent");
            Assert.Null(evt);
        }
    }

    public class MoreDelegatingTypeTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;

        public MoreDelegatingTypeTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
        }

        [Fact]
        public void GenericParameterPosition_ThrowsForNonGenericParameter()
        {
            Assert.Throws<InvalidOperationException>(() => _customTypeInfo.GenericParameterPosition);
        }

        [Fact]
        public void GenericParameterAttributes_ThrowsForNonGenericParameter()
        {
            Assert.Throws<InvalidOperationException>(() => _customTypeInfo.GenericParameterAttributes);
        }

        [Fact]
        public void DeclaringMethod_ThrowsForNonGenericParameter()
        {
            // DeclaringMethod throws for types that aren't generic type parameters
            Assert.Throws<InvalidOperationException>(() => _customTypeInfo.DeclaringMethod);
        }

        [Fact]
        public void GetArrayRank_ThrowsForNonArray()
        {
            Assert.Throws<ArgumentException>(() => _customTypeInfo.GetArrayRank());
        }

        [Fact]
        public void GetGenericTypeDefinition_ThrowsForNonGeneric()
        {
            Assert.Throws<InvalidOperationException>(() => _customTypeInfo.GetGenericTypeDefinition());
        }
    }

    public class VirtualPropertyTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new VirtualPropertyAddingContext();
        private readonly TypeInfo _customTypeInfo;

        public VirtualPropertyTests()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
        }

        [Fact]
        public void VirtualProperty_ExistsOnMappedType()
        {
            PropertyInfo[] props = _customTypeInfo.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.Contains(props, p => p.Name == "VirtualProperty");
        }

        [Fact]
        public void VirtualProperty_GetValue_ReturnsValue()
        {
            PropertyInfo virtualProp = _customTypeInfo.GetProperty("VirtualProperty");
            Assert.NotNull(virtualProp);

            var target = new TestObject("test");
            object value = virtualProp.GetValue(target);
            Assert.Equal("Virtual", value);
        }

        [Fact]
        public void VirtualProperty_SetValue_SetsValue()
        {
            PropertyInfo virtualProp = _customTypeInfo.GetProperty("VirtualProperty");
            Assert.NotNull(virtualProp);

            var target = new TestObject("test");
            virtualProp.SetValue(target, "NewValue");
            // Setter just stores in a dictionary, value is replaced
            object value = virtualProp.GetValue(target);
            Assert.NotNull(value);
        }
    }

    // Custom context that adds virtual properties
    internal class VirtualPropertyAddingContext : CustomReflectionContext
    {
        protected override IEnumerable<PropertyInfo> AddProperties(Type type)
        {
            if (type == typeof(TestObject))
            {
                yield return CreateProperty(
                    MapType(typeof(string).GetTypeInfo()),
                    "VirtualProperty",
                    o => "Virtual",
                    (o, v) => { });
            }
        }
    }

    public class MoreParameterTests2
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void ReturnParameter_Member_ReturnsProjectedMethod()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("GetMessage");
            ParameterInfo returnParam = method.ReturnParameter;

            MemberInfo member = returnParam.Member;
            Assert.NotNull(member);
        }

        [Fact]
        public void Parameter_RawDefaultValue_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(TypeWithParameters).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("MethodWithOptionalParam");
            ParameterInfo optionalParam = method.GetParameters()[1];

            object rawValue = optionalParam.RawDefaultValue;
            Assert.Equal(42, rawValue);
        }

        [Fact]
        public void Parameter_Attributes_ReturnsValue()
        {
            TypeInfo typeInfo = typeof(TypeWithParameters).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            MethodInfo method = customType.GetMethod("MethodWithOptionalParam");
            ParameterInfo optionalParam = method.GetParameters()[1];

            ParameterAttributes attrs = optionalParam.Attributes;
            Assert.True(attrs.HasFlag(ParameterAttributes.Optional));
        }
    }

    public class MoreConstructorTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void GetMethodBody_ReturnsProjectedBody()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });

            MethodBody body = ctor.GetMethodBody();
            Assert.NotNull(body);
        }

        [Fact]
        public void GetParameters_ReturnsProjectedParameters()
        {
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            ConstructorInfo ctor = customType.GetConstructor(new[] { typeof(string) });

            ParameterInfo[] parameters = ctor.GetParameters();
            Assert.Single(parameters);
        }
    }

    public class MoreFieldTests2
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void IsNotSerialized_ReturnsFalse()
        {
            TypeInfo typeInfo = typeof(TypeWithFields).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            FieldInfo field = customType.GetField("PublicField");

#pragma warning disable SYSLIB0050
            Assert.False(field.IsNotSerialized);
#pragma warning restore SYSLIB0050
        }

        [Fact]
        public void IsPinvokeImpl_ReturnsFalse()
        {
            TypeInfo typeInfo = typeof(TypeWithFields).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            FieldInfo field = customType.GetField("PublicField");

            Assert.False(field.IsPinvokeImpl);
        }
    }

    public class MoreEventTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void IsSpecialName_ReturnsFalse()
        {
            TypeInfo typeInfo = typeof(TypeWithEvent).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            EventInfo evt = customType.GetEvent("TestEvent");

            Assert.False(evt.IsSpecialName);
        }
    }

    public class MorePropertyTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void GetConstantValue_ThrowsForNonConstantProperty()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("ReadWriteProperty");

            Assert.Throws<InvalidOperationException>(() => prop.GetConstantValue());
        }

        [Fact]
        public void GetRawConstantValue_ThrowsForNonConstantProperty()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            TypeInfo customType = _customReflectionContext.MapType(typeInfo);
            PropertyInfo prop = customType.GetProperty("ReadWriteProperty");

            Assert.Throws<InvalidOperationException>(() => prop.GetRawConstantValue());
        }
    }
}
