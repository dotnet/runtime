// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class FieldInfoMethodTests
    {
        // Verify GetValue method for fields
        [Theory]
        [InlineData("intFieldStatic", 100)]
        [InlineData("intFieldNonStatic", 101)]
        [InlineData("static_strField", "Static string field")]
        [InlineData("nonstatic_strField", "NonStatic string field")]
        public void TestGetValue(string fieldName, object expectedFieldValue)
        {
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();
            FieldInfo fi = GetAndTestFieldInfo(fieldName);
            Assert.Equal(expectedFieldValue, fi.GetValue(myInstance));
        }

        //Verify GetValue method throws Exception when null is passed for non-static fields
        [Fact]
        public void TestGetValue_Exception1()
        {
            FieldInfo fi = GetAndTestFieldInfo("nonstatic_strField");

            // In Win8p Instead of TargetException , generic Exception is thrown.
            // Refer http://msdn.microsoft.com/en-us/library/system.reflection.fieldinfo.getvalue.aspx
            Assert.ThrowsAny<Exception>(() => fi.GetValue(null));
        }

        //Verify GetValue method does not throw Exception when null is passed for static fields
        [Fact]
        public void TestGetValue_Exception2()
        {
            FieldInfo fi = GetAndTestFieldInfo("static_strField");
            string retValue = (string)fi.GetValue(null);
            Assert.Equal("Static string field", retValue);
        }

        //Verify GetValue method throws Exception when invalid object is passed for non-static fields
        [Fact]
        public void TestGetValue_Exception3()
        {
            FieldInfo fi = GetAndTestFieldInfo("nonstatic_strField");
            object objInstance = new object();
            Assert.Throws<ArgumentException>(() => { string retValue = (string)fi.GetValue(objInstance); });
        }

        // Verify SetValue method for fields
        [Theory]
        [InlineData("intFieldStatic", 1000)]
        [InlineData("intFieldNonStatic", 1000)]
        [InlineData("static_strField", "new")]
        [InlineData("nonstatic_strField", "new")]
        public void TestSetValue(string fieldName, object newFieldValue)
        {
            FieldInfo fi = GetAndTestFieldInfo(fieldName);
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();

            object origFieldValue = fi.GetValue(myInstance);
            fi.SetValue(myInstance, newFieldValue);
            Assert.Equal(newFieldValue, fi.GetValue(myInstance));

            //reset static field values to their original value
            switch (fieldName)
            {
                case "intFieldStatic":
                    intFieldStatic = (int)origFieldValue;
                    break;
                case "static_strField":
                    static_strField = (string)origFieldValue;
                    break;
            }
        }

        //Verify SetValue method throws Exception when null is passed for non-static fields
        [Fact]
        public void TestSetValue_Exception1()
        {
            FieldInfo fi = GetAndTestFieldInfo("nonstatic_strField");
            string newFieldValue = "new";

            // In Win8p Instead of TargetException , generic Exception is thrown.
            // Refer http://msdn.microsoft.com/en-us/library/system.reflection.fieldinfo.getvalue.aspx
            Assert.ThrowsAny<Exception>(() => fi.SetValue(null, newFieldValue));
        }

        //Verify SetValue method does not throw Exception when null is passed for static fields
        [Fact]
        public void TestSetValue_Exception2()
        {
            FieldInfo fi = GetAndTestFieldInfo("static_strField");
            string newFieldValue = "new";

            string origFieldValue = (string)fi.GetValue(null);
            fi.SetValue(null, newFieldValue);

            string curValue = (string)fi.GetValue(null);
            Assert.Equal(newFieldValue, curValue);

            //reset static field value to its original value
            static_strField = origFieldValue;
        }

        //Verify SetValue method throws ArgumentException when value type can not be converted to field type
        [Fact]
        public void TestSetValue_ArgumentException1()
        {
            string fieldName = "nonstatic_strField";
            int newfieldValue = 100;
            FieldInfo fi = GetAndTestFieldInfo(fieldName);

            // In Win8p Instead of TargetException, generic Exception is thrown.
            // Refer http://msdn.microsoft.com/en-us/library/system.reflection.fieldinfo.setvalue.aspx
            Assert.ThrowsAny<Exception>(() => fi.SetValue(null, newfieldValue));
        }

        // Verifiy Equals method when two Fieldinfo objects are equal and are not equal
        [Theory]
        [InlineData("nonstatic_strField", "nonstatic_strField", true)]
        [InlineData("nonstatic_strField", "intFieldStatic", false)]
        public void TestEquals(string fieldName1, string fieldName2, bool expectedResult)
        {
            FieldInfo info1 = getField(fieldName1);
            FieldInfo info2 = getField(fieldName2);

            Assert.NotNull(info1);
            Assert.NotNull(info2);
            Assert.Equal(expectedResult, info1.Equals(info2));
        }

        //Verify GetHashCode method returns HashCode
        [Fact]
        public void TestGetHashCode()
        {
            string fieldName = "nonstatic_strField";
            FieldInfo info = getField(fieldName);
            Assert.NotNull(info);
            Assert.NotEqual(0, info.GetHashCode());
        }

        // Helper method to test correctness of FieldInfo, and returns it 
        public FieldInfo GetAndTestFieldInfo(string fieldName)
        {
            FieldInfo fi = getField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(fieldName, fi.Name);
            return fi;
        }

        // Helper method to get field from Type type
        private static FieldInfo getField(string field)
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

        private static object s_field_Assembly1 = null;				    // without keyword
        private static object s_field_Assembly2 = null;			// with private keyword
        protected static object Field_Assembly3 = null;			// with protected keyword
        public static object Field_Assembly4 = null;			// with public keyword
        internal static object Field_Assembly5 = null;			// with internal keyword

        private static object s_field_FamilyAndAssembly1 = null;						// without keyword
        private static object s_field_FamilyAndAssembly2 = null;			    // with private keyword
        protected static object Field_FamilyAndAssembly3 = null;			// with protected keyword
        public static object Field_FamilyAndAssembly4 = null;				// with public keyword
        internal static object Field_FamilyAndAssembly5 = null;				// with internal keyword

        private static object s_field_FamilyOrAssembly1 = null;				    // without keyword
        private static object s_field_FamilyOrAssembly2 = null;			// with private keyword
        protected static object Field_FamilyOrAssembly3 = null;			// with protected keyword
        public static object Field_FamilyOrAssembly4 = null;			// with public keyword
        internal static object Field_FamilyOrAssembly5 = null;			// with internal keyword

        public readonly int rointField = 1;

        public const int constIntField = 1222;
    }
}
