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
    }
}
