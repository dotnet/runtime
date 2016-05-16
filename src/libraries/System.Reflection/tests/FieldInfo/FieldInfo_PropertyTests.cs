// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Linq;

using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class FieldInfoPropertyTests
    {
        public static IEnumerable<object[]> TestFieldType_TestData()
        {
            yield return new object[] { "intFieldStatic", true };
            yield return new object[] { "intFieldNonStatic", false };
            yield return new object[] { "static_strField", true };
            yield return new object[] { "nonstatic_strField", false };
            yield return new object[] { "_privateInt", false };
        }

        public static IEnumerable<object[]> TestIsAssemblyAndIsFamily_TestData()
        {
            yield return new object[] { "s_field_Assembly1", false, false };
            yield return new object[] { "s_field_Assembly2", false, false };
            yield return new object[] { "Field_Assembly3", false, true };
            yield return new object[] { "Field_Assembly4", false, false };
            yield return new object[] { "Field_Assembly5", true, false };
        }

        public static IEnumerable<object[]> TestIsFamilyAndAssembly_TestData()
        {
            yield return new object[] { "s_field_FamilyAndAssembly1", false };
            yield return new object[] { "s_field_FamilyAndAssembly2", false };
            yield return new object[] { "Field_FamilyAndAssembly3", false };
            yield return new object[] { "Field_FamilyAndAssembly4", false };
            yield return new object[] { "Field_FamilyAndAssembly5", false };
        }

        public static IEnumerable<object[]> TestIsFamilyOrAssembly_TestData()
        {
            yield return new object[] { "s_field_FamilyOrAssembly1", false };
            yield return new object[] { "s_field_FamilyOrAssembly2", false };
            yield return new object[] { "Field_FamilyOrAssembly3", false };
            yield return new object[] { "Field_FamilyOrAssembly4", false };
            yield return new object[] { "Field_FamilyOrAssembly5", false };
        }

        public static IEnumerable<object[]> TestIsPublicAndIsPrivate_TestData()
        {
            yield return new object[] { "nonstatic_strField", true };
            yield return new object[] { "intFieldStatic", true };
            yield return new object[] { "_privateInt", false };
            yield return new object[] { "_privateStr", false };
        }

        public static IEnumerable<object[]> TestFieldAttributes_TestData()
        {
            yield return new object[] { "intFieldNonStatic", FieldAttributes.Public };
            yield return new object[] { "intFieldStatic", FieldAttributes.Public | FieldAttributes.Static };
            yield return new object[] { "_privateInt", FieldAttributes.Private };
            yield return new object[] { "roIntField", FieldAttributes.Public | FieldAttributes.InitOnly };
        }

        // Verify static and non-static FieldTypes
        [Theory]
        [MemberData(nameof(TestFieldType_TestData))]
        public void TestFieldTypeAndIsStatic(string fieldName, bool expectedIsStatic)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(fieldName, fi.Name);
            Assert.Equal(expectedIsStatic, fi.IsStatic);
        }

        // Verify IsAssembly and IsFamily for static and non-static objects
        [Theory]
        [MemberData(nameof(TestIsAssemblyAndIsFamily_TestData))]
        public void TestIsAssemblyAndIsFamily(string fieldName, bool expectedIsAssembly, bool expectedIsFamily)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsAssembly, fi.IsAssembly);
            Assert.Equal(expectedIsFamily, fi.IsFamily);
        }

        //Verify IsFamilyAndAssembly for static and non-static objects
        [Theory]
        [MemberData(nameof(TestIsFamilyAndAssembly_TestData))]
        public void TestIsFamilyAndAssembly(string fieldName, bool expectedIsFamilyAndAssembly)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsFamilyAndAssembly, fi.IsFamilyAndAssembly);
        }

        //Verify IsFamilyOrAssembly for static and non-static objects
        [Theory]
        [MemberData(nameof(TestIsFamilyOrAssembly_TestData))]
        public void TestIsFamilyOrAssembly(string fieldName, bool expectedIsFamilyOrAssembly)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsFamilyOrAssembly, fi.IsFamilyOrAssembly);
        }

        // Verify IsPublic and IsPrivate for public and private FieldTypes
        [Theory]
        [MemberData(nameof(TestIsPublicAndIsPrivate_TestData))]
        public void TestIsPublicAndIsPrivate(string fieldName, bool expectedIsPublic)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsPublic, fi.IsPublic);
            Assert.Equal(!expectedIsPublic, fi.IsPrivate);
        }

        // Verify IsInitOnly for readonly and non-readonly fields
        [Theory]
        [InlineData("roIntField", true)]
        [InlineData("intFieldNonStatic", false)]
        public void TestIsInitOnly(string fieldName, bool expectedIsInitOnly)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsInitOnly, fi.IsInitOnly);
        }

        //Verify IsLiteral for constant and non-constant fields 
        [Theory]
        [InlineData("constIntField", true)]
        [InlineData("intFieldNonStatic", false)]
        public void TestIsLiteral(string fieldName, bool expectedIsLiteral)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsLiteral, fi.IsLiteral);
        }

        // Verify FieldType for a field
        [Theory]
        [InlineData("intFieldNonStatic", "System.Int32")]
        [InlineData("nonstatic_strField", "System.String")]
        [InlineData("s_field_Assembly1", "System.Object")]
        public void TestFieldType(string fieldName, string expectedFieldType)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedFieldType, fi.FieldType.ToString());
        }

        // Verify FieldAttributes
        [Theory]
        [MemberData(nameof(TestFieldAttributes_TestData))]
        public void TestFieldAttributes(string fieldName, FieldAttributes expectedAttributes)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedAttributes, fi.Attributes);
        }

        //Verify IsSpecialName for FieldInfo
        [Fact]
        public void TestIsSpecialName()
        {
            string fieldname = "intFieldNonStatic";
            FieldInfo fi = GetField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsSpecialName, "Failed: FieldInfo IsSpecialName returned True for field: " + fieldname);
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(string field)
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

        public readonly int roIntField = 1;

        public const int constIntField = 1222;
    }
}
