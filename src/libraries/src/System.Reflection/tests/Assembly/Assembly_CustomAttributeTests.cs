// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Int32Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of Type Int64Attr
        [Fact]
        public void Test_Int64Attr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Int64Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of TypeStringAttr
        [Fact]
        public void Test_StringAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.StringAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.StringAttr(\"hello\", name = \"StringAttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of type  EnumAttr
        [Fact]
        public void Test_EnumAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.EnumAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.EnumAttr((System.Reflection.CustomAttributesTests.Data.MyColorEnum)1, name = \"EnumAttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of type  TypeAttr
        [Fact]
        public void Test_TypeAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.TypeAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of Type CompilationRelaxationsAttribute
        [Fact]
        public void Test_CompilationRelaxationsAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Runtime.CompilerServices.CompilationRelaxationsAttribute);
            string attrstr = "[System.Runtime.CompilerServices.CompilationRelaxationsAttribute((Int32)8)]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of Type AssemblyTitleAttribute
        [Fact]
        public void Test_AssemblyIdentityAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.AssemblyTitleAttribute);
            string attrstr = "[System.Reflection.AssemblyTitleAttribute(\"System.Reflection.Tests\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of Type AssemblyDescriptionAttribute
        [Fact]
        public void Test_AssemblyDescriptionAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.AssemblyDescriptionAttribute);
            string attrstr = "[System.Reflection.AssemblyDescriptionAttribute(\"System.Reflection.Tests\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of Type AssemblyCompanyAttribute
        [Fact]
        public void Test_AssemblyCompanyAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.AssemblyCompanyAttribute);
            string attrstr = "[System.Reflection.AssemblyCompanyAttribute(\"Microsoft Corporation\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of Type CLSCompliantAttribute
        [Fact]
        public void Test_CLSCompliantAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.CLSCompliantAttribute);
            string attrstr = "[System.CLSCompliantAttribute((Boolean)True)]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of Type DebuggableAttribute
        [Fact]
        public void Test_DebuggableAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.Diagnostics.DebuggableAttribute);
            string attrstr = "[System.Diagnostics.DebuggableAttribute((System.Diagnostics.DebuggableAttribute+DebuggingModes)263)]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        //Test for custom Attribute of type Attribute
        [Fact]
        public void Test_SimpleAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Attr((Int32)77, name = \"AttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        private static bool VerifyCustomAttribute(Type type, String attributeStr)
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

            return result;
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

