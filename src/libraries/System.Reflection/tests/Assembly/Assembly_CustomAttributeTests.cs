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

[assembly: System.Reflection.CustomAttributesTests.Data.Attr(77, name = "AttrSimple")]
[assembly: System.Reflection.CustomAttributesTests.Data.Int32Attr(77, name = "Int32AttrSimple"),
System.Reflection.CustomAttributesTests.Data.Int64Attr((Int64)77, name = "Int64AttrSimple"),
System.Reflection.CustomAttributesTests.Data.StringAttr("hello", name = "StringAttrSimple"),
System.Reflection.CustomAttributesTests.Data.EnumAttr(System.Reflection.CustomAttributesTests.Data.MyColorEnum.RED, name = "EnumAttrSimple"),
System.Reflection.CustomAttributesTests.Data.TypeAttr(typeof(Object), name = "TypeAttrSimple")]

[assembly: System.Runtime.CompilerServices.CompilationRelaxationsAttribute((Int32)8)]
[assembly: System.Diagnostics.Debuggable((System.Diagnostics.DebuggableAttribute.DebuggingModes)263)]
[assembly: System.CLSCompliant(false)]

namespace System.Reflection.Tests
{
    public class AssemblyCustomAttributeTest
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

        //Test for custom Attribute of Type CompilationRelaxationsAttribute
        [Fact]
        public void Test_CompilationRelaxationsAttr()
        { 
            Type attrType = typeof(System.Runtime.CompilerServices.CompilationRelaxationsAttribute);
            string attrstr = "[System.Runtime.CompilerServices.CompilationRelaxationsAttribute((Int32)8)]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of Type AssemblyTitleAttribute
        [Fact]
        public void Test_AssemblyIdentityAttr()
        {

            Type attrType = typeof(System.Reflection.AssemblyTitleAttribute);
            string attrstr = "[System.Reflection.AssemblyTitleAttribute(\"System.Reflection.Tests\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of Type AssemblyDescriptionAttribute
        [Fact]
        public void Test_AssemblyDescriptionAttribute()
        {

            Type attrType = typeof(System.Reflection.AssemblyDescriptionAttribute);
            string attrstr = "[System.Reflection.AssemblyDescriptionAttribute(\"System.Reflection.Tests\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of Type AssemblyCompanyAttribute
        [Fact]
        public void Test_AssemblyCompanyAttribute()
        {

            Type attrType = typeof(System.Reflection.AssemblyCompanyAttribute);
            string attrstr = "[System.Reflection.AssemblyCompanyAttribute(\"Microsoft Corporation\")]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of Type CLSCompliantAttribute
        [Fact]
        public void Test_CLSCompliantAttribute()
        {

            Type attrType = typeof(System.CLSCompliantAttribute);
            string attrstr = "[System.CLSCompliantAttribute((Boolean)True)]";
            VerifyCustomAttribute(attrType, attrstr);
        }

        //Test for custom Attribute of Type DebuggableAttribute
        [Fact]
        public void Test_DebuggableAttribute()
        {

            Type attrType = typeof(System.Diagnostics.DebuggableAttribute);
            string attrstr = "[System.Diagnostics.DebuggableAttribute((System.Diagnostics.DebuggableAttribute+DebuggingModes)263)]";
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
            Assembly asm = GetExecutingAssembly();
            IEnumerator<CustomAttributeData> customAttrs = asm.CustomAttributes.GetEnumerator();
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

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", type));

            result = false;
            // Also double check that we can get these values via ICustomAttributeProvider
            ICustomAttributeProvider prov = asm as ICustomAttributeProvider;
            Assert.NotNull(prov.GetCustomAttributes(type, false));
            Assert.Equal(1, prov.GetCustomAttributes(type, false).Length);
            Assert.True(prov.IsDefined(type, false));

            // Check that there exists a custom attribute with the same type.
            object[] atrs = prov.GetCustomAttributes(false);
            for (int i = 0; i < atrs.Length; i++)
            {
                if (atrs[i].GetType().Equals(type))
                {
                    result = true;
                    break;
                }
            }

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", type));
        }

        public static Assembly GetExecutingAssembly()
        {
            Assembly asm = null;
            Type t = typeof(AssemblyCustomAttributeTest);
            TypeInfo ti = t.GetTypeInfo();
            asm = ti.Assembly;

            return asm;
        }
    }
}

