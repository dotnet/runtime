// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public class CustomAttributeTests
    {
        private class SameTypesAttribute : Attribute
        {
            public object[] ObjectArray1 { get; set; }
            public object[] ObjectArray2 { get; set; }
        }

        [SameTypes(ObjectArray1 = null, ObjectArray2 = new object[] { "" })]
        private class SameTypesClass1 { }

        [SameTypes(ObjectArray1 = new object[] { "" }, ObjectArray2 = null)]
        private class SameTypesClass2 { }

        [Fact]
        public void AttributeWithSamePropertyTypes()
        {
            SameTypesAttribute attr;

            attr = typeof(SameTypesClass1)
                .GetCustomAttributes(typeof(SameTypesAttribute), true)
                .Cast<SameTypesAttribute>()
                .Single();

            Assert.Null(attr.ObjectArray1);
            Assert.Equal(1, attr.ObjectArray2.Length);

            attr = typeof(SameTypesClass2)
                .GetCustomAttributes(typeof(SameTypesAttribute), true)
                .Cast<SameTypesAttribute>()
                .Single();

            Assert.Equal(1, attr.ObjectArray1.Length);
            Assert.Null(attr.ObjectArray2);
        }

        private class DifferentTypesAttribute : Attribute
        {
            public object[] ObjectArray { get; set; }
            public string[] StringArray { get; set; }
        }

        [DifferentTypes(ObjectArray = null, StringArray = new[] { "" })]
        private class DifferentTypesClass1 { }

        [DifferentTypes(ObjectArray = new object[] { "" }, StringArray = null)]
        private class DifferentTypesClass2 { }

        [Fact]
        public void AttributeWithDifferentPropertyTypes()
        {
            DifferentTypesAttribute attr;

            attr = typeof(DifferentTypesClass1)
                .GetCustomAttributes(typeof(DifferentTypesAttribute), true)
                .Cast<DifferentTypesAttribute>()
                .Single();

            Assert.Null(attr.ObjectArray);
            Assert.Equal(1, attr.StringArray.Length);

            attr = typeof(DifferentTypesClass2)
                .GetCustomAttributes(typeof(DifferentTypesAttribute), true)
                .Cast<DifferentTypesAttribute>()
                .Single();

            Assert.Equal(1, attr.ObjectArray.Length);
            Assert.Null(attr.StringArray);
        }

        public class StringValuedAttribute : Attribute
        {
            public StringValuedAttribute (string s)
            {
                NamedField = s;
            }
            public StringValuedAttribute () {}
            public string NamedProperty
            {
                get => NamedField;
                set { NamedField = value; }
            }
            public string NamedField;
        }

        internal class ClassWithAttrs
        {
            [StringValuedAttribute("")]
            public void M1() {}

            [StringValuedAttribute(NamedProperty = "")]
            public void M2() {}

            [StringValuedAttribute(NamedField = "")]
            public void M3() {}
        }

        [Fact]
        public void StringAttributeValueRefEqualsStringEmpty () {
            StringValuedAttribute attr;
            attr = typeof (ClassWithAttrs).GetMethod("M1")
                .GetCustomAttributes(typeof(StringValuedAttribute), true)
                .Cast<StringValuedAttribute>()
                .Single();

            Assert.Same(string.Empty, attr.NamedField);

            attr = typeof (ClassWithAttrs).GetMethod("M2")
                .GetCustomAttributes(typeof(StringValuedAttribute), true)
                .Cast<StringValuedAttribute>()
                .Single();
            
            Assert.Same(string.Empty, attr.NamedField);


            attr = typeof (ClassWithAttrs).GetMethod("M3")
                .GetCustomAttributes(typeof(StringValuedAttribute), true)
                .Cast<StringValuedAttribute>()
                .Single();
            
            Assert.Same(string.Empty, attr.NamedField);
        }

        [AttributeUsage(AttributeTargets.Parameter)]
        internal class MyParameterAttribute : Attribute {}

        [AttributeUsage(AttributeTargets.Property)]
        internal class MyPropertyAttribute : Attribute {}

        internal sealed class PropertyAsParameterInfo : ParameterInfo
        {
            private readonly PropertyInfo _underlyingProperty;
            private readonly ParameterInfo? _constructionParameterInfo;

            public PropertyAsParameterInfo(PropertyInfo property, ParameterInfo parameterInfo)
            {
                _underlyingProperty = property;
                _constructionParameterInfo = parameterInfo;
                MemberImpl = _underlyingProperty;
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                var constructorAttributes = _constructionParameterInfo?.GetCustomAttributes(attributeType, inherit);

                if (constructorAttributes == null || constructorAttributes is { Length: 0 })
                {
                    return _underlyingProperty.GetCustomAttributes(attributeType, inherit);
                }

                var propertyAttributes = _underlyingProperty.GetCustomAttributes(attributeType, inherit);

                var mergedAttributes = new Attribute[constructorAttributes.Length + propertyAttributes.Length];
                Array.Copy(constructorAttributes, mergedAttributes, constructorAttributes.Length);
                Array.Copy(propertyAttributes, 0, mergedAttributes, constructorAttributes.Length, propertyAttributes.Length);

                return mergedAttributes;
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                var constructorAttributes = _constructionParameterInfo?.GetCustomAttributes(inherit);

                if (constructorAttributes == null || constructorAttributes is { Length: 0 })
                {
                    return _underlyingProperty.GetCustomAttributes(inherit);
                }

                var propertyAttributes = _underlyingProperty.GetCustomAttributes(inherit);

                var mergedAttributes = new object[constructorAttributes.Length + propertyAttributes.Length];
                Array.Copy(constructorAttributes, mergedAttributes, constructorAttributes.Length);
                Array.Copy(propertyAttributes, 0, mergedAttributes, constructorAttributes.Length, propertyAttributes.Length);

                return mergedAttributes;
            }

            public override IList<CustomAttributeData> GetCustomAttributesData()
            {
                var attributes = new List<CustomAttributeData>(
                    _constructionParameterInfo?.GetCustomAttributesData() ?? Array.Empty<CustomAttributeData>());
                attributes.AddRange(_underlyingProperty.GetCustomAttributesData());

                return attributes.AsReadOnly();
            }
        }

        internal class CustomAttributeProviderTestClass
        {
            public CustomAttributeProviderTestClass([MyParameter] int integerProperty)
            {
                IntegerProperty = integerProperty;
            }

            [MyProperty]
            public int IntegerProperty { get; set; }
        }

        [Fact]
        public void CustomAttributeProvider ()
        {
            var type = typeof(CustomAttributeProviderTestClass);
            var propertyInfo = type.GetProperty(nameof(CustomAttributeProviderTestClass.IntegerProperty));
            var ctorInfo = type.GetConstructor(new Type[] { typeof(int) });
            var ctorParamInfo = ctorInfo.GetParameters()[0];
            var propertyAndParamInfo = new PropertyAsParameterInfo(propertyInfo, ctorParamInfo);

            // check GetCustomAttribute API
            var cattrObjects = propertyAndParamInfo.GetCustomAttributes(true);
            Assert.Equal(2, cattrObjects.Length);
            Assert.Equal(typeof(MyParameterAttribute), cattrObjects[0].GetType());
            Assert.Equal(typeof(MyPropertyAttribute), cattrObjects[1].GetType());

            cattrObjects = propertyAndParamInfo.GetCustomAttributes(typeof(Attribute), true);
            Assert.Equal(2, cattrObjects.Length);
            Assert.Equal(typeof(MyParameterAttribute), cattrObjects[0].GetType());
            Assert.Equal(typeof(MyPropertyAttribute), cattrObjects[1].GetType());

            var cattrsEnumerable = propertyAndParamInfo.GetCustomAttributes();
            Attribute[] cattrs = cattrsEnumerable.Cast<Attribute>().ToArray();
            Assert.Equal(2, cattrs.Length);
            Assert.Equal(typeof(MyParameterAttribute), cattrs[0].GetType());
            Assert.Equal(typeof(MyPropertyAttribute), cattrs[1].GetType());

            // check GetCustomAttributeData API
            var customAttributesData = propertyAndParamInfo.GetCustomAttributesData();
            Assert.Equal(2, customAttributesData.Count);
            Assert.Equal(typeof(MyParameterAttribute), customAttributesData[0].AttributeType);
            Assert.Equal(typeof(MyPropertyAttribute), customAttributesData[1].AttributeType);
        }
    }
}
