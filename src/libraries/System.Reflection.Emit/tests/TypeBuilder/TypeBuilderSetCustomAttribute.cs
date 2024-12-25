// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderSetCustomAttribute
    {
        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);

            Type attributeType = typeof(TypeBuilderIntAttribute);
            ConstructorInfo attriubteConstructor = attributeType.GetConstructors()[0];
            FieldInfo attributeField = attributeType.GetField("Field12345");

            CustomAttributeBuilder attribute = new CustomAttributeBuilder(attriubteConstructor, new object[] { 4 }, new FieldInfo[] { attributeField }, new object[] { "hello" });
            type.SetCustomAttribute(attribute);
            type.CreateType();

            object[] attributes = type.GetCustomAttributes(false).ToArray();
            Assert.Equal(1, attributes.Length);

            TypeBuilderIntAttribute obj = (TypeBuilderIntAttribute)attributes[0];
            Assert.Equal("hello", obj.Field12345);
            Assert.Equal(4, obj.m_ctorType2);
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => type.SetCustomAttribute(null));
        }

        [Fact]
        public void SetCustomAttribute()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorInfo constructor = typeof(TypeBuilderStringAttribute).GetConstructor(new Type[] { typeof(string) });
            CustomAttributeBuilder cuatbu = new CustomAttributeBuilder(constructor, new object[] { "hello" });
            type.SetCustomAttribute(cuatbu);

            type.CreateType();

            object[] attributes = type.GetCustomAttributes(false).ToArray();
            Assert.Equal(1, attributes.Length);
            Assert.True(attributes[0] is TypeBuilderStringAttribute);
            Assert.Equal("hello", ((TypeBuilderStringAttribute)attributes[0]).Creator);
        }

        [Fact]
        public void GetterAndSetterAreAvailableOnSubTypeWhenOverridingOneButInheritingBothFromBaseClass()
        {
            // checks that a setter in inherited
            var pProperty = typeof(DerivedAttributeWithGetter).GetProperty("P", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            Assert.NotNull(pProperty);
            Assert.NotNull(pProperty.SetMethod);

            // check that reflected base setter works as intended when invoked
            object[] attributes = typeof(ClassWithDerivedAttr).GetCustomAttributes(true);
            Assert.Equal(2, attributes.Length);
            Assert.Contains(attributes,
                a => a is DerivedAttributeWithGetter derivedAttributeWithGetterAttr && derivedAttributeWithGetterAttr.P == 2);

            // checks that a getter in inherited
            pProperty = typeof(DerivedAttributeWithSetter).GetProperty("P", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            Assert.NotNull(pProperty);
            Assert.NotNull(pProperty.GetMethod);
        }

        public class TypeBuilderStringAttribute : Attribute
        {
            private string _creator;
            public string Creator { get { return _creator; } }

            public TypeBuilderStringAttribute(string name)
            {
                _creator = name;
            }
        }

        public class TypeBuilderIntAttribute : Attribute
        {
            public TypeBuilderIntAttribute(int mc)
            {
                m_ctorType2 = mc;
            }

            public string Field12345;
            public int m_ctorType2;
        }

        public class BaseAttributeWithGetterSetter : Attribute
        {
            protected int _p;

            public virtual int P
            {
                get => _p;
                set
                {
                    _p = value;
                }
            }
        }

        public class DerivedAttributeWithGetter : BaseAttributeWithGetterSetter
        {
            public override int P
            {
                get => _p;
            }
        }

        public class DerivedAttributeWithSetter : BaseAttributeWithGetterSetter
        {
            public override int P
            {
                set => _p = value;
            }
        }

        [DerivedAttributeWithGetter(P = 2), DerivedAttributeWithSetter(P = 2)]
        public class ClassWithDerivedAttr
        { }
    }
}
