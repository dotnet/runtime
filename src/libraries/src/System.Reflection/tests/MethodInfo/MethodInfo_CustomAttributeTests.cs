// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.CustomAttributesTests.Data;

// Need to disable warning related to CLS Compliance as using Array as cusom attribute is not CLS compliant
#pragma warning disable 3016

namespace System.Reflection.Tests
{
    public class MethodInfoTestClass
    {
        public MethodInfoTestClass()
        {
        }

        [Attr(77, name = "AttrSimple"),
        Int32Attr(77, name = "Int32AttrSimple"),
        Int64Attr((Int64)77, name = "Int64AttrSimple"),
        StringAttr("hello", name = "StringAttrSimple"),
        EnumAttr(MyColorEnum.RED, name = "EnumAttrSimple"),
        TypeAttr(typeof(Object), name = "TypeAttrSimple")]
        [return:Attr(77, name = "AttrSimple"),
        Int32Attr(77, name = "Int32AttrSimple"),
        Int64Attr((Int64)77, name = "Int64AttrSimple"),
        StringAttr("hello", name = "StringAttrSimple"),
        EnumAttr(MyColorEnum.RED, name = "EnumAttrSimple"),
        TypeAttr(typeof(Object), name = "TypeAttrSimple")]
        public void MyMethod() { }

        public int Prop
        {
            get { return 0; }
            set { }
        }
    }

    public class AssemblyMethodInfoCustomAttributeTest
    {
        //Test for custom Attribute of type  Int32AttrSimple
        [Fact]
        public void Test_Int32AttrSimple()
        {
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Int32Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of Type Int64Attr
        [Fact]
        public void Test_Int64Attr()
        {
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Int64Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of TypeStringAttr
        [Fact]
        public void Test_StringAttr()
        {
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.StringAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.StringAttr(\"hello\", name = \"StringAttrSimple\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of type  EnumAttr
        [Fact]
        public void Test_EnumAttr()
        {
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.EnumAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.EnumAttr((System.Reflection.CustomAttributesTests.Data.MyColorEnum)1, name = \"EnumAttrSimple\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of type  TypeAttr
        [Fact]
        public void Test_TypeAttr()
        {
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.TypeAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of type Attribute
        [Fact]
        public void Test_SimpleAttribute()
        {
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Attr((Int32)77, name = \"AttrSimple\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        private static void VerifyCustomAttribute(Type type, String attributeStr)
        {
            MethodInfo mi = GetMethod(typeof(MethodInfoTestClass), "MyMethod");
            IEnumerator<CustomAttributeData> customAttrs = mi.CustomAttributes.GetEnumerator();
            CustomAttributeData current = null;
            bool result = false;
            while (customAttrs.MoveNext())
            {
                current = customAttrs.Current;
                if (current.AttributeType.Equals(type))
                {
                    result = true;
                    break;
                }
            }

            Assert.True(result);

            // Also check the custom attribute on the return type.
            VerifyReturnTypeCustomAttribute(type, attributeStr);
        }

        private static void VerifyReturnTypeCustomAttribute(Type type, String attributeStr)
        {
            MethodInfo mi = GetMethod(typeof(MethodInfoTestClass), "MyMethod");

            //Get all the customAttributes of the type.
            object[] atrs = mi.ReturnTypeCustomAttributes.GetCustomAttributes(type, false);
            Assert.Equal(1, atrs.Length);
        }

        private static MethodInfo GetMethod(Type t, string method)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<MethodInfo> alldefinedMethods = ti.DeclaredMethods.GetEnumerator();
            MethodInfo mi = null;

            while (alldefinedMethods.MoveNext())
            {
                if (alldefinedMethods.Current.Name.Equals(method))
                {
                    //found method
                    mi = alldefinedMethods.Current;
                    break;
                }
            }
            return mi;
        }

        private static Assembly GetExecutingAssembly(Type t)
        {
            Assembly asm = null;
            TypeInfo ti = t.GetTypeInfo();
            asm = ti.Assembly;

            return asm;
        }
    }
}

