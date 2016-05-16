// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Reflection.CustomAttributesTests.Data;

// Need to disable warning related to CLS Compliance as using Array as custom attribute is not CLS compliant
#pragma warning disable 3016

namespace System.Reflection.Tests
{
    public class FieldInfoTestClass
    {
        public FieldInfoTestClass()
        {
        }

        [Attr(77, name = "AttrSimple"),
        Int32Attr(77, name = "Int32AttrSimple"),
        Int64Attr(77, name = "Int64AttrSimple"),
        StringAttr("hello", name = "StringAttrSimple"),
        EnumAttr(MyColorEnum.RED, name = "EnumAttrSimple"),
        TypeAttr(typeof(object), name = "TypeAttrSimple")]
        public string MyField = "MyField";

        public int Prop
        {
            get { return 0; }
            set { }
        }
    }

    // Test class for Custom Attribute Test
    public class FieldInfoCustomAttributeTests
    {
        public static IEnumerable<object[]> FieldInfoCustomAttributes_TestData()
        {
            yield return new object[] { typeof(Int32Attr), 
                "[System.Reflection.CustomAttributesTests.Data.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]" }; 
            yield return new object[] { typeof(Int64Attr), 
                "[System.Reflection.CustomAttributesTests.Data.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]" };
            yield return new object[] { typeof(StringAttr),
                "[System.Reflection.CustomAttributesTests.Data.StringAttr(\"hello\", name = \"StringAttrSimple\")]" };
            yield return new object[] { typeof(EnumAttr),
                "[System.Reflection.CustomAttributesTests.Data.EnumAttr((System.Reflection.CustomAttributesTests.Data.MyColorEnum)1, name = \"EnumAttrSimple\")]" };
            yield return new object[] { typeof(TypeAttr),
                "[System.Reflection.CustomAttributesTests.Data.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]" };
            yield return new object[] { typeof(Attr),
                "[System.Reflection.CustomAttributesTests.Data.Attr((Int32)77, name = \"AttrSimple\")]" };
        }

        // Test that custom attributes are correct
        [Theory]
        [MemberData(nameof(FieldInfoCustomAttributes_TestData))]
        private static void verifyCustomAttribute(Type type, string attributeStr)
        {
            FieldInfo fi = getField("MyField");
            IEnumerator<CustomAttributeData> customAttrs = fi.CustomAttributes.GetEnumerator();
            CustomAttributeData current = null;
            bool result = false;
            while (customAttrs.MoveNext())
            {
                current = customAttrs.Current;
                if (current.AttributeType.Equals(type) && current.ToString().Equals(attributeStr))
                {
                    result = true;
                    break;
                }
            }

            Assert.True(result);
        }

        // Helper method to get field from Type type
        private static FieldInfo getField(string field)
        {
            Type t = typeof(FieldInfoTestClass);
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<FieldInfo> alldefinedFields = ti.DeclaredFields.GetEnumerator();
            FieldInfo fi = null, found = null;

            while (alldefinedFields.MoveNext())
            {
                fi = alldefinedFields.Current;
                if (fi.Name.Equals(field))
                {
                    //found type
                    found = fi;
                    break;
                }
            }
            return found;
        }
    }
}

