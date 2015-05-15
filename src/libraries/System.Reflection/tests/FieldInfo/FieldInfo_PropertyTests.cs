// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class FieldInfoPropertyTests
    {
        //Verify Static int FieldType
        [Fact]
        public void TestFieldType_intstatic()
        {
            String fieldname = "intFieldStatic";
            FieldInfo fi = getField(fieldname);
            FieldInfoPropertyTests myInstance = new FieldInfoPropertyTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.True(fi.IsStatic);
        }

        //Verify Non Static int FieldType
        [Fact]
        public void TestFieldType_intnonstatic()
        {
            String fieldname = "intFieldNonStatic";
            FieldInfo fi = getField(fieldname);
            FieldInfoPropertyTests myInstance = new FieldInfoPropertyTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.False(fi.IsStatic);
        }

        //Verify Static String FieldType
        [Fact]
        public void TestFieldType_strstatic()
        {
            String fieldname = "static_strField";
            FieldInfo fi = getField(fieldname);
            FieldInfoPropertyTests myInstance = new FieldInfoPropertyTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.True(fi.IsStatic);
        }

        //Verify Non Static String FieldType
        [Fact]
        public void TestFieldType_strnonstatic()
        {
            String fieldname = "nonstatic_strField";
            FieldInfo fi = getField(fieldname);
            FieldInfoPropertyTests myInstance = new FieldInfoPropertyTests();

            Assert.NotNull(fi);
            Assert.True(fi.Name.Equals(fieldname));
            Assert.False(fi.IsStatic);
        }

        //Verify Public String FieldType using IsPublic
        [Fact]
        public void TestIsPublic1()
        {
            String fieldname = "nonstatic_strField";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.IsPublic);
        }

        //Verify Public int FieldType using IsPublic
        [Fact]
        public void TestIsPublic2()
        {
            String fieldname = "intFieldStatic";
            FieldInfo fi = getField(fieldname);

            Assert.NotNull(fi);
            Assert.True(fi.IsPublic);
        }

        //Verify Private int FieldType using IsPublic
        [Fact]
        public void TestIsPublic3()
        {
            String fieldname = "_privateInt";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsPublic);
        }

        //Verify Private String FieldType using IsPublic
        [Fact]
        public void TestIsPublic4()
        {
            String fieldname = "_privateStr";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsPublic);
        }

        //Verify Private int FieldType using IsPrivate
        [Fact]
        public void TestIsPrivate1()
        {
            String fieldname = "_privateInt";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.IsPrivate);
        }

        //Verify Private String FieldType using IsPrivate
        [Fact]
        public void TestIsPrivate2()
        {
            String fieldname = "_privateStr";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.IsPrivate);
        }

        //Verify Public String FieldType using IsPrivate
        [Fact]
        public void TestIsPrivate3()
        {
            String fieldname = "nonstatic_strField";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsPrivate);
        }

        //Verify Public int FieldType using IsPrivate
        [Fact]
        public void TestIsPrivate4()
        {
            String fieldname = "intFieldStatic";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsPrivate);
        }

        //Verify Private int FieldType using IsStatic
        [Fact]
        public void TestIsStatic1()
        {
            String fieldname = "_privateInt";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsStatic);
        }

        //Verify public static int FieldType using IsStatic
        [Fact]
        public void TestIsStatic2()
        {
            String fieldname = "intFieldStatic";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.IsStatic);
        }

        //Verify IsAssembly for static object
        [Fact]
        public void TestIsAssembly1()
        {
            String fieldname = "s_field_Assembly1";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsAssembly);
        }

        //Verify IsAssembly for private static object
        [Fact]
        public void TestIsAssembly2()
        {
            String fieldname = "s_field_Assembly2";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsAssembly);
        }

        //Verify IsAssembly for protected static object
        [Fact]
        public void TestIsAssembly3()
        {
            String fieldname = "Field_Assembly3";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsAssembly);
        }

        //Verify IsAssembly for public static object
        [Fact]
        public void TestIsAssembly4()
        {
            String fieldname = "Field_Assembly4";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsAssembly);
        }

        //Verify IsAssembly for internal static object
        [Fact]
        public void TestIsAssembly5()
        {
            String fieldname = "Field_Assembly5";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.IsAssembly);
        }

        //Verify IsFamily for static object
        [Fact]
        public void TestIsFamily1()
        {
            String fieldname = "s_field_Assembly1";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamily);
        }

        //Verify IsFamily for private static object
        [Fact]
        public void TestIsFamily2()
        {
            String fieldname = "s_field_Assembly2";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamily);
        }

        //Verify IsFamily for protected static object
        [Fact]
        public void TestIsFamily3()
        {
            String fieldname = "Field_Assembly3";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.IsFamily);
        }

        //Verify IsFamily for public static object
        [Fact]
        public void TestIsFamily4()
        {
            String fieldname = "Field_Assembly4";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamily);
        }

        //Verify IsFamily for internal static object
        [Fact]
        public void TestIsFamily5()
        {
            String fieldname = "Field_Assembly5";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamily);
        }

        //Verify IsFamilyAndAssembly for s_field_FamilyAndAssembly1
        [Fact]
        public void TestIsFamilyAndAssembly1()
        {
            String fieldname = "s_field_FamilyAndAssembly1";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyAndAssembly);
        }

        //Verify IsFamilyAndAssembly for s_field_FamilyAndAssembly2
        [Fact]
        public void TestIsFamilyAndAssembly2()
        {
            String fieldname = "s_field_FamilyAndAssembly2";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyAndAssembly);
        }

        //Verify IsFamilyAndAssembly for Field_FamilyAndAssembly3
        [Fact]
        public void TestIsFamilyAndAssembly3()
        {
            String fieldname = "Field_FamilyAndAssembly3";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyAndAssembly);
        }

        //Verify IsFamilyAndAssembly for Field_FamilyAndAssembly4
        [Fact]
        public void TestIsFamilyAndAssembly4()
        {
            String fieldname = "Field_FamilyAndAssembly4";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyAndAssembly);
        }

        //Verify IsFamilyAndAssembly for Field_FamilyAndAssembly5
        [Fact]
        public void TestIsFamilyAndAssembly5()
        {
            String fieldname = "Field_FamilyAndAssembly5";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyAndAssembly);
        }

        //Verify IsFamilyOrAssembly for s_field_FamilyOrAssembly1
        [Fact]
        public void TestIsFamilyOrAssembly1()
        {
            String fieldname = "s_field_FamilyOrAssembly1";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyOrAssembly);
        }

        //Verify IsFamilyOrAssembly for s_field_FamilyOrAssembly2
        [Fact]
        public void TestIsFamilyOrAssembly2()
        {
            String fieldname = "s_field_FamilyOrAssembly2";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyOrAssembly);
        }

        //Verify IsFamilyOrAssembly for Field_FamilyOrAssembly3
        [Fact]
        public void TestIsFamilyOrAssembly3()
        {
            String fieldname = "Field_FamilyOrAssembly3";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyOrAssembly);
        }

        //Verify IsFamilyOrAssembly for Field_FamilyOrAssembly4
        [Fact]
        public void TestIsFamilyOrAssembly4()
        {
            String fieldname = "Field_FamilyOrAssembly4";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyOrAssembly);
        }

        //Verify IsFamilyOrAssembly for Field_FamilyOrAssembly5
        [Fact]
        public void TestIsFamilyOrAssembly5()
        {
            String fieldname = "Field_FamilyOrAssembly5";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsFamilyOrAssembly);
        }

        //Verify IsInitOnly for readonly field
        [Fact]
        public void TestIsInitOnly1()
        {
            String fieldname = "rointField";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.IsInitOnly);
        }

        //Verify IsInitOnly for non- readonly field
        [Fact]
        public void TestIsInitOnly2()
        {
            String fieldname = "intFieldNonStatic";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsInitOnly);
        }

        //Verify IsLiteral for literal fields like constant
        [Fact]
        public void TestIsLiteral1()
        {
            String fieldname = "constIntField";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.IsLiteral);
        }

        //Verify IsLiteral for non constant fields
        [Fact]
        public void TestIsLiteral2()
        {
            String fieldname = "intFieldNonStatic";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsLiteral);
        }

        //Verify FieldType for int field
        [Fact]
        public void TestFieldType1()
        {
            String fieldname = "intFieldNonStatic";
            FieldInfo fi = getField(fieldname);
            string typeStr = "System.Int32";
            Assert.NotNull(fi);
            Assert.True(fi.FieldType.ToString().Equals(typeStr));
        }

        //Verify FieldType for string field
        [Fact]
        public void TestFieldType2()
        {
            String fieldname = "nonstatic_strField";
            FieldInfo fi = getField(fieldname);
            string typeStr = "System.String";
            Assert.NotNull(fi);
            Assert.True(fi.FieldType.ToString().Equals(typeStr), "Failed!! Expected FieldType to return " + typeStr);
        }


        //Verify FieldType for Object field
        [Fact]
        public void TestFieldType3()
        {
            String fieldname = "s_field_Assembly1";
            FieldInfo fi = getField(fieldname);
            string typeStr = "System.Object";


            Assert.NotNull(fi);
            Assert.True(fi.FieldType.ToString().Equals(typeStr), "Failed!! Expected FieldType to return " + typeStr);
        }


        //Verify FieldAttributes
        [Fact]
        public void TestFieldAttribute1()
        {
            String fieldname = "intFieldNonStatic";
            FieldInfo fi = getField(fieldname);

            Assert.NotNull(fi);
            Assert.True(fi.Attributes.Equals(FieldAttributes.Public), "Failed!! Expected Field Attribute to be of type Public");
        }


        //Verify FieldAttributes
        [Fact]
        public void TestFieldAttribute2()
        {
            String fieldname = "intFieldStatic";
            FieldInfo fi = getField(fieldname);

            Assert.NotNull(fi);
            Assert.True(fi.Attributes.Equals(FieldAttributes.Public | FieldAttributes.Static), "Failed!! Expected Field Attribute to be of type Public and static");
        }


        //Verify FieldAttributes
        [Fact]
        public void TestFieldAttribute3()
        {
            String fieldname = "_privateInt";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.Attributes.Equals(FieldAttributes.Private), "Failed!! Expected Field Attribute to be of type Private");
        }



        //Verify FieldAttributes
        [Fact]
        public void TestFieldAttribute4()
        {
            String fieldname = "rointField";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.True(fi.Attributes.Equals(FieldAttributes.Public | FieldAttributes.InitOnly), "Failed!! Expected Field Attribute to be of type InitOnly");
        }

        //Verify IsSpecialName for FieldInfo
        [Fact]
        public void TestIsSpecialName()
        {
            String fieldname = "intFieldNonStatic";
            FieldInfo fi = getField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsSpecialName, "Failed: FieldInfo IsSpecialName returned True for field: " + fieldname);
        }

        private static FieldInfo getField(string field)
        {
            Type t = typeof(FieldInfoPropertyTests);
            TypeInfo ti = t.GetTypeInfo();
            FieldInfo fi = null;
            fi = ti.DeclaredFields.Single(x => x.Name == field);

            return fi;
        }


        //Fields for Reflection Metadata


        public static int intFieldStatic = 100;    // Field for Reflection
        public int intFieldNonStatic = 101; //Field for Reflection
        public static string static_strField = "Static string field";   // Field for Reflection
        public string nonstatic_strField = "NonStatic string field";   // Field for Reflection

        private int _privateInt = 1; // Field for Reflection

        private string _privateStr = "_privateStr";        // Field for Reflection

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
