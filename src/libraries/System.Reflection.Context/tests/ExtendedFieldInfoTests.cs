// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Test types for extended field coverage
    internal class TypeWithFields
    {
        public int PublicField = 1;
        public readonly int ReadOnlyField = 2;
        public static int StaticField = 3;
        public const int ConstField = 4;
        private int _privateField = 5;

        public int GetPrivateField() => _privateField;
        public void SetPrivateField(int value) => _privateField = value;
    }

    public class ExtendedFieldInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;
        private readonly FieldInfo _publicField;
        private readonly FieldInfo _readOnlyField;
        private readonly FieldInfo _staticField;
        private readonly FieldInfo _constField;

        public ExtendedFieldInfoTests()
        {
            TypeInfo typeInfo = typeof(TypeWithFields).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _publicField = _customTypeInfo.GetField("PublicField");
            _readOnlyField = _customTypeInfo.GetField("ReadOnlyField");
            _staticField = _customTypeInfo.GetField("StaticField");
            _constField = _customTypeInfo.GetField("ConstField");
        }

        [Fact]
        public void Attributes_ReturnsValue()
        {
            FieldAttributes attrs = _publicField.Attributes;
            Assert.True(attrs.HasFlag(FieldAttributes.Public));
        }

        [Fact]
        public void DeclaringType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _publicField.DeclaringType.GetType().FullName);
        }

        [Fact]
        public void FieldHandle_ReturnsValue()
        {
            RuntimeFieldHandle handle = _publicField.FieldHandle;
            Assert.NotEqual(default, handle);
        }

        [Fact]
        public void FieldType_ReturnsProjectedType()
        {
            Type fieldType = _publicField.FieldType;
            Assert.NotNull(fieldType);
            Assert.Equal(ProjectionConstants.CustomType, fieldType.GetType().FullName);
        }

        [Fact]
        public void IsAssembly_ReturnsFalse()
        {
            Assert.False(_publicField.IsAssembly);
        }

        [Fact]
        public void IsFamily_ReturnsFalse()
        {
            Assert.False(_publicField.IsFamily);
        }

        [Fact]
        public void IsFamilyAndAssembly_ReturnsFalse()
        {
            Assert.False(_publicField.IsFamilyAndAssembly);
        }

        [Fact]
        public void IsFamilyOrAssembly_ReturnsFalse()
        {
            Assert.False(_publicField.IsFamilyOrAssembly);
        }

        [Fact]
        public void IsInitOnly_ReturnsTrue_ForReadOnly()
        {
            Assert.True(_readOnlyField.IsInitOnly);
            Assert.False(_publicField.IsInitOnly);
        }

        [Fact]
        public void IsLiteral_ReturnsTrue_ForConst()
        {
            Assert.True(_constField.IsLiteral);
            Assert.False(_publicField.IsLiteral);
        }

        [Fact]
        public void IsPrivate_ReturnsFalse()
        {
            Assert.False(_publicField.IsPrivate);
        }

        [Fact]
        public void IsPublic_ReturnsTrue()
        {
            Assert.True(_publicField.IsPublic);
        }

        [Fact]
        public void IsSecurityCritical_ReturnsValue()
        {
            bool value = _publicField.IsSecurityCritical;
            Assert.True(value);
        }

        [Fact]
        public void IsSecuritySafeCritical_ReturnsValue()
        {
            bool value = _publicField.IsSecuritySafeCritical;
            Assert.False(value);
        }

        [Fact]
        public void IsSecurityTransparent_ReturnsValue()
        {
            bool value = _publicField.IsSecurityTransparent;
            Assert.False(value);
        }

        [Fact]
        public void IsSpecialName_ReturnsFalse()
        {
            Assert.False(_publicField.IsSpecialName);
        }

        [Fact]
        public void IsStatic_ReturnsTrue_ForStatic()
        {
            Assert.True(_staticField.IsStatic);
            Assert.False(_publicField.IsStatic);
        }

        [Fact]
        public void MetadataToken_ReturnsValue()
        {
            Assert.True(_publicField.MetadataToken > 0);
        }

        [Fact]
        public void Module_ReturnsCustomModule()
        {
            Assert.Equal(ProjectionConstants.CustomModule, _publicField.Module.GetType().FullName);
        }

        [Fact]
        public void Name_ReturnsValue()
        {
            Assert.Equal("PublicField", _publicField.Name);
        }

        [Fact]
        public void ReflectedType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.CustomType, _publicField.ReflectedType.GetType().FullName);
        }

        [Fact]
        public void GetValue_ReturnsValue()
        {
            var target = new TypeWithFields { PublicField = 42 };
            object value = _publicField.GetValue(target);
            Assert.Equal(42, value);
        }

        [Fact]
        public void SetValue_SetsValue()
        {
            var target = new TypeWithFields();
            _publicField.SetValue(target, 99);
            Assert.Equal(99, target.PublicField);
        }

        [Fact]
        public void GetRawConstantValue_ReturnsValue_ForConst()
        {
            object value = _constField.GetRawConstantValue();
            Assert.Equal(4, value);
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsEmptyForUnattributedField()
        {
            object[] attributes = _publicField.GetCustomAttributes(typeof(Attribute), true);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributes_NoType_ReturnsEmptyForUnattributedField()
        {
            object[] attributes = _publicField.GetCustomAttributes(false);
            Assert.Empty(attributes);
        }

        [Fact]
        public void GetCustomAttributesData_ReturnsEmptyForUnattributedField()
        {
            IList<CustomAttributeData> data = _publicField.GetCustomAttributesData();
            Assert.Empty(data);
        }

        [Fact]
        public void IsDefined_ReturnsValue()
        {
            bool isDefined = _publicField.IsDefined(typeof(Attribute), true);
            Assert.False(isDefined);
        }

        [Fact]
        public void GetOptionalCustomModifiers_ReturnsEmpty()
        {
            Type[] modifiers = _publicField.GetOptionalCustomModifiers();
            Assert.Empty(modifiers);
        }

        [Fact]
        public void GetRequiredCustomModifiers_ReturnsEmpty()
        {
            Type[] modifiers = _publicField.GetRequiredCustomModifiers();
            Assert.Empty(modifiers);
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            string str = _publicField.ToString();
            Assert.Contains("PublicField", str);
        }

        [Fact]
        public void Equals_SameField_ReturnsTrue()
        {
            FieldInfo sameField = _customTypeInfo.GetField("PublicField");
            Assert.True(_publicField.Equals(sameField));
        }

        [Fact]
        public void GetHashCode_IsIdempotent()
        {
            int hashCode1 = _publicField.GetHashCode();
            int hashCode2 = _publicField.GetHashCode();
            Assert.Equal(hashCode1, hashCode2);
        }
    }
}
