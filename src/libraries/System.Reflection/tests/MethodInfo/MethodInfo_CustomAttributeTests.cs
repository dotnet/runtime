// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

// Need to disable warning related to CLS Compliance as using Array as custom attribute is not CLS compliant
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
        Int64Attr(77, name = "Int64AttrSimple"),
        StringAttr("hello", name = "StringAttrSimple"),
        EnumAttr(PublicEnum.Case1, name = "EnumAttrSimple"),
        TypeAttr(typeof(object), name = "TypeAttrSimple")]
        [return:Attr(77, name = "AttrSimple"),
        Int32Attr(77, name = "Int32AttrSimple"),
        Int64Attr(77, name = "Int64AttrSimple"),
        StringAttr("hello", name = "StringAttrSimple"),
        EnumAttr(PublicEnum.Case1, name = "EnumAttrSimple"),
        TypeAttr(typeof(object), name = "TypeAttrSimple")]

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
        [Theory]
        [InlineData(typeof(Int32Attr), "[System.Reflection.Tests.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]")]
        [InlineData(typeof(Int64Attr), "[System.Reflection.Tests.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]")]
        [InlineData(typeof(StringAttr), "[System.Reflection.Tests.StringAttr(\"hello\", name = \"StringAttrSimple\")]")]
        [InlineData(typeof(EnumAttr), "[System.Reflection.Tests.EnumAttr((System.Reflection.Tests.MyColorEnum)1, name = \"EnumAttrSimple\")]")]
        [InlineData(typeof(TypeAttr), "[System.Reflection.Tests.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]")]
        [InlineData(typeof(Attr), "[System.Reflection.Tests.Attr((Int32)77, name = \"AttrSimple\")]")]


        private void Test_Attr(Type type, string attributeStr)
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

        private static void VerifyReturnTypeCustomAttribute(Type type, string attributeStr)
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

