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
        [Theory]
        [InlineData(typeof(Int32Attr), "[System.Reflection.CustomAttributesTests.Data.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]")]
        [InlineData(typeof(Int64Attr), "[System.Reflection.CustomAttributesTests.Data.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]")]
        [InlineData(typeof(StringAttr), "[System.Reflection.CustomAttributesTests.Data.StringAttr(\"hello\", name = \"StringAttrSimple\")]")]
        [InlineData(typeof(EnumAttr), "[System.Reflection.CustomAttributesTests.Data.EnumAttr((System.Reflection.CustomAttributesTests.Data.MyColorEnum)1, name = \"EnumAttrSimple\")]")]
        [InlineData(typeof(TypeAttr), "[System.Reflection.CustomAttributesTests.Data.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]")]
        [InlineData(typeof(Attr), "[System.Reflection.CustomAttributesTests.Data.Attr((Int32)77, name = \"AttrSimple\")]")]
        public static void TestCustomAttributeDetails(Type type, string attributeStr)
        {
            FieldInfo fi = GetField("MyField");
            Assert.Contains(fi.CustomAttributes, attr => attr.AttributeType.Equals(type) && attr.ToString().Equals(attributeStr));
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(string fieldName)
        {
            Type t = typeof(FieldInfoTestClass);
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<FieldInfo> alldefinedFields = ti.DeclaredFields.GetEnumerator();
            FieldInfo fi = null, found = null;

            while (alldefinedFields.MoveNext())
            {
                fi = alldefinedFields.Current;
                if (fi.Name.Equals(fieldName))
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

