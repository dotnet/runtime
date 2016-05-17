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
        [Theory]
        [InlineData("intFieldStatic", 100)]
        [InlineData("intFieldNonStatic", 101)]
        [InlineData("static_strField", "Static string field")]
        [InlineData("nonstatic_strField", "NonStatic string field")]
        public void GetValue_InstanceAndStaticFields_Succeeds(string fieldName, object expectedFieldValue)
        {
            FieldInfoMethodTests myInstance = new FieldInfoMethodTests();
            FieldInfo fi = GetAndTestFieldInfo(fieldName);
            Assert.Equal(expectedFieldValue, fi.GetValue(myInstance));
        }

        [Fact]
        public void GetValue_NullInstanceField_TargetException()
        {
            FieldInfo fi = GetAndTestFieldInfo("nonstatic_strField");
            Assert.ThrowsAny<TargetException>(() => fi.GetValue(null));
        }

        [Fact]
        public void GetValue_NullStaticField_Succeeds()
        {
            FieldInfo fi = GetAndTestFieldInfo("static_strField");
            Assert.Equal("Static string field", (string)fi.GetValue(null));
        }

        [Fact]
        public void GetValue_InvalidObject_ArgumentException()
        {
            FieldInfo fi = GetAndTestFieldInfo("nonstatic_strField");
            object objInstance = new object();
            Assert.Throws<ArgumentException>(null, () => (string)fi.GetValue(objInstance));
        }

        [Theory]
        [InlineData("intFieldStatic", 1000)]
        [InlineData("intFieldNonStatic", 1000)]
        [InlineData("static_strField", "new")]
        [InlineData("nonstatic_strField", "new")]
        public void SetValueAndReset_InstanceAndStaticFields(string fieldName, object newFieldValue)
        {
            FieldInfo fi = GetAndTestFieldInfo(fieldName);
            var myInstance = new FieldInfoMethodTests();
            object origFieldValue = fi.GetValue(myInstance);

            fi.SetValue(myInstance, newFieldValue);
            Assert.Equal(newFieldValue, fi.GetValue(myInstance));

            fi.SetValue(myInstance, origFieldValue);
            Assert.Equal(origFieldValue, fi.GetValue(myInstance));
        }

        [Fact]
        public void SetValue_NullInstanceField_TargetException()
        {
            FieldInfo fi = GetAndTestFieldInfo("nonstatic_strField");
            string newFieldValue = "new";
            Assert.ThrowsAny<TargetException>(() => fi.SetValue(null, newFieldValue));
        }

        [Fact]
        public void SetValue_NullStaticField_Succeeds()
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

        [Fact]
        public void SetValue_InvalidFieldType_ArgumentException()
        {
            string fieldName = "nonstatic_strField";
            int newfieldValue = 100;
            FieldInfo fi = GetAndTestFieldInfo(fieldName);
            var e = Assert.Throws<ArgumentException>(null, () => fi.SetValue(fieldName, newfieldValue));
        }

        [Theory]
        [InlineData("nonstatic_strField", "nonstatic_strField", true)]
        [InlineData("nonstatic_strField", "intFieldStatic", false)]
        public void TestEquals_EqualAndNotEqualFields(string fieldName1, string fieldName2, bool expectedResult)
        {
            FieldInfo info1 = GetField(fieldName1);
            FieldInfo info2 = GetField(fieldName2);

            Assert.NotNull(info1);
            Assert.NotNull(info2);
            Assert.Equal(expectedResult, info1.Equals(info2));
        }

        [Fact]
        public void GetHashCode_Succeeds()
        {
            string fieldName = "nonstatic_strField";
            FieldInfo info = GetField(fieldName);
            Assert.NotNull(info);
            Assert.NotEqual(0, info.GetHashCode());
        }

        // Helper method to test correctness of FieldInfo, and returns it 
        public FieldInfo GetAndTestFieldInfo(string fieldName)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(fieldName, fi.Name);
            return fi;
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(string fieldName)
        {
            Type t = typeof(FieldInfoMethodTests);
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

        // Fields for Reflection

        public static int intFieldStatic = 100;        
        public int intFieldNonStatic = 101;             
        public static string static_strField = "Static string field";  
        public string nonstatic_strField = "NonStatic string field";    
        private int _privateInt = 1;                                    
        private string _privateStr = "privateStr";                      
    }
}
