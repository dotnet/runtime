// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class FieldInfoMethodTests
    {
        //Verify GetValue method for static int FieldType
        [Fact]
        public void TestGetValue_StaticIntField()
        {
            String fieldname = "intFieldStatic";
            int expectedfieldValue = 100;
            FieldInfo fi = GetField(fieldname);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();
            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.True(((int)fi.GetValue(myInstance)).Equals(expectedfieldValue));
        }

        //Verify GetValue method for non-static int FieldType
        [Fact]
        public void TestGetValue_NonStaticIntField()
        {
            String fieldname = "intFieldNonStatic";
            int expectedfieldValue = 101;
            FieldInfo fi = GetField(fieldname);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();
            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.True(((int)fi.GetValue(myInstance)).Equals(expectedfieldValue));
        }

        //Verify GetValue method for static string FieldType
        [Fact]
        public void TestGetValue_StaticStrField()
        {
            String fieldname = "static_strField";
            String expectedfieldValue = "Static string field";
            FieldInfo fi = GetField(fieldname);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.True(((String)fi.GetValue(myInstance)).Equals(expectedfieldValue));
        }

        //Verify GetValue method for non-static string FieldType
        [Fact]
        public void TestGetValue_NonStaticStrField()
        {
            String fieldname = "nonstatic_strField";
            String expectedfieldValue = "NonStatic string field";
            FieldInfo fi = GetField(fieldname);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.True(((String)fi.GetValue(myInstance)).Equals(expectedfieldValue));
        }

        //Verify GetValue method throws Exception when null is passed for non-static fields
        [Fact]
        public void TestGetValue_Exception1()
        {
            String fieldname = "nonstatic_strField";
            FieldInfo fi = GetField(fieldname);

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            // In Win8p Instead of TargetException , generic Exception is thrown.
            // Refer http://msdn.microsoft.com/en-us/library/system.reflection.fieldinfo.getvalue.aspx

            try
            {
                string retValue = (String)fi.GetValue(null);
                Assert.False(true, "Exception expected.");
            }
            catch (Exception) { }
        }

        //Verify GetValue method does not throw Exception when null is passed for static fields
        [Fact]
        public void TestGetValue_Exception2()
        {
            String fieldname = "static_strField";
            FieldInfo fi = GetField(fieldname);

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            string retValue = (String)fi.GetValue(null);
            Assert.True(retValue.Equals("Static string field"));
        }

        //Verify GetValue method throws Exception when invalid object is passed for non-static fields
        [Fact]
        public void TestGetValue_Exception3()
        {
            String fieldname = "nonstatic_strField";
            FieldInfo fi = GetField(fieldname);
            Object objInstance = new Object();
            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.Throws<System.ArgumentException>(() => { string retValue = (String)fi.GetValue(objInstance); });
        }

        //Verify SetValue method for static int FieldType
        [Fact]
        public void TestSetValue_StaticIntField()
        {
            String fieldname = "intFieldStatic";
            int origfieldvalue = 0;
            int newfieldValue = 1000;
            FieldInfo fi = GetField(fieldname);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            origfieldvalue = (int)fi.GetValue(myInstance);
            fi.SetValue(myInstance, (object)newfieldValue);
            Assert.True(((int)fi.GetValue(myInstance)).Equals(newfieldValue));

            //reset static field value to its original value
            FieldInfoMethodTests.intFieldStatic = origfieldvalue;
        }

        //Verify SetValue method for non-static int FieldType
        [Fact]
        public void TestSetValue_IntField()
        {
            String fieldname = "intFieldNonStatic";
            int origfieldvalue = 0;
            int newfieldValue = 1000;
            FieldInfo fi = GetField(fieldname);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            origfieldvalue = (int)fi.GetValue(myInstance);
            fi.SetValue(myInstance, (object)newfieldValue);

            Assert.True(((int)fi.GetValue(myInstance)).Equals(newfieldValue));
        }

        //Verify SetValue method for static String FieldType
        [Fact]
        public void TestSetValue_StaticStrField()
        {
            String fieldname = "static_strField";
            String origfieldvalue;
            String newfieldValue = "new";
            FieldInfo fi = GetField(fieldname);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            origfieldvalue = (String)fi.GetValue(myInstance);
            fi.SetValue(myInstance, (object)newfieldValue);

            Assert.True(((String)fi.GetValue(myInstance)).Equals(newfieldValue));

            //reset static field value to its original value
            FieldInfoMethodTests.static_strField = origfieldvalue;
        }

        //Verify SetValue method for non-static String FieldType
        [Fact]
        public void TestSetValue_StrField()
        {
            String fieldname = "nonstatic_strField";
            String newfieldValue = "new";
            FieldInfo fi = GetField(fieldname);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            fi.SetValue(myInstance, (object)newfieldValue);
            Assert.True(((String)fi.GetValue(myInstance)).Equals(newfieldValue));
        }

        //Verify SetValue method throws Exception when null is passed for non-static fields
        [Fact]
        public void TestSetValue_Exception1()
        {
            String fieldname = "nonstatic_strField";
            String newfieldValue = "new";
            FieldInfo fi = GetField(fieldname);

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            // In Win8p Instead of TargetException , generic Exception is thrown.
            // Refer http://msdn.microsoft.com/en-us/library/system.reflection.fieldinfo.getvalue.aspx
            try
            {
                fi.SetValue(null, (object)newfieldValue);
                Assert.False(true, "Exception expected.");
            }
            catch (Exception) { }
        }

        //Verify SetValue method does not throw Exception when null is passed for static fields
        [Fact]
        public void TestSetValue_Exception2()
        {
            String fieldname = "static_strField";
            String origfieldvalue = null;
            String newfieldValue = "new";
            FieldInfo fi = GetField(fieldname);

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            origfieldvalue = (String)fi.GetValue(null);
            fi.SetValue(null, (object)newfieldValue);

            String curValue = (String)fi.GetValue(null);
            Assert.Equal(newfieldValue, curValue);

            //reset static field value to its original value
            FieldInfoMethodTests.static_strField = origfieldvalue;
        }



        //Verify SetValue method throws ArgumentException when value type can not be converted to field type
        [Fact]
        public void TestSetValue_ArgumentException1()
        {
            String fieldname = "nonstatic_strField";
            int newfieldValue = 100;
            FieldInfo fi = GetField(fieldname);

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));

            // In Win8p Instead of TargetException, generic Exception is thrown.
            // Refer http://msdn.microsoft.com/en-us/library/system.reflection.fieldinfo.setvalue.aspx

            try
            {
                fi.SetValue(null, (object)newfieldValue);
                Assert.False(true, "Exception expected");
            }
            catch (Exception) { }
        }

        //Verify Equals method when two Fieldinfo objects are equal
        [Fact]
        public void TestEquals1()
        {
            String fieldname = "nonstatic_strField";
            FieldInfo info1 = GetField(fieldname);
            FieldInfo info2 = GetField(fieldname);

            Assert.False((info1 == null) || (info2 == null));
            Assert.True(info1.Equals(info2));
        }

        //Verify Equals method when two Fieldinfo objects are not equal
        [Fact]
        public void TestEquals2()
        {
            String fieldname1 = "nonstatic_strField";
            String fieldname2 = "intFieldStatic";
            FieldInfo info1 = GetField(fieldname1);
            FieldInfo info2 = GetField(fieldname2);

            Assert.False((info1 == null) || (info2 == null));
            Assert.False(info1.Equals(info2));
        }

        //Verify GetHashCode method returns HashCode
        [Fact]
        public void TestGetHashCode()
        {
            String fieldname = "nonstatic_strField";
            FieldInfo info = GetField(fieldname);
            Assert.NotNull(info);
            Assert.False(info.GetHashCode().Equals(0));
        }

        private static FieldInfo GetField(string field)
        {
            Type t = typeof(FieldInfoMethodTests);
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


        public static int intFieldStatic = 100;    // Field for Reflection
        public int intFieldNonStatic = 101; //Field for Reflection
        public static string static_strField = "Static string field";   // Field for Reflection
        public string nonstatic_strField = "NonStatic string field";   // Field for Reflection
        private int _privateInt = 1; // Field for Reflection
        private string _privateStr = "privateStr"; // Field for Reflection

        private static Object s_field_Assembly1 = null;				    // without keyword
        private static Object s_field_Assembly2 = null;			// with private keyword
        protected static Object Field_Assembly3 = null;			// with protected keyword
        public static Object Field_Assembly4 = null;			// with public keyword
        internal static Object Field_Assembly5 = null;			// with internal keyword

        private static Object s_field_FamilyAndAssembly1 = null;						// without keyword
        private static Object s_field_FamilyAndAssembly2 = null;			    // with private keyword
        protected static Object Field_FamilyAndAssembly3 = null;			// with protected keyword
        public static Object Field_FamilyAndAssembly4 = null;				// with public keyword
        internal static Object Field_FamilyAndAssembly5 = null;				// with internal keyword

        private static Object s_field_FamilyOrAssembly1 = null;				    // without keyword
        private static Object s_field_FamilyOrAssembly2 = null;			// with private keyword
        protected static Object Field_FamilyOrAssembly3 = null;			// with protected keyword
        public static Object Field_FamilyOrAssembly4 = null;			// with public keyword
        internal static Object Field_FamilyOrAssembly5 = null;			// with internal keyword

        public readonly int rointField = 1;

        public const int constIntField = 1222;
    }
}
