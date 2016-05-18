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
        [Theory]
        [InlineData("intFieldStatic", true)]
        [InlineData("intFieldNonStatic", false)]
        [InlineData("static_strField", true)]
        [InlineData("nonstatic_strField", false)]
        [InlineData("_privateInt", false)]
        public void TestFieldTypeAndIsStatic(string fieldName, bool expectedIsStatic)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(fieldName, fi.Name);
            Assert.Equal(expectedIsStatic, fi.IsStatic);
        }

        [Theory]
        [InlineData("private_Field_Assembly", false, false)]
        [InlineData("protected_Field_Assembly", false, true)]
        [InlineData("public_Field_Assembly", false, false)]
        [InlineData("internal_Field_Assembly", true, false)]
        public void TestIsAssemblyAndIsFamily(string fieldName, bool expectedIsAssembly, bool expectedIsFamily)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsAssembly, fi.IsAssembly);
            Assert.Equal(expectedIsFamily, fi.IsFamily);
        }

        [Theory]
        [InlineData("private_Field_FamilyAndAssembly", false)]
        [InlineData("protected_Field_FamilyAndAssembly", false)]
        [InlineData("public_Field_FamilyAndAssembly", false)]
        [InlineData("internal_Field_FamilyAndAssembly", false)]
        public void TestIsFamilyAndAssembly(string fieldName, bool expectedIsFamilyAndAssembly)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsFamilyAndAssembly, fi.IsFamilyAndAssembly);
        }

        [Theory]
        [InlineData("private_Field_FamilyOrAssembly", false)]
        [InlineData("protected_Field_FamilyOrAssembly", false)]
        [InlineData("public_Field_FamilyOrAssembly", false)]
        [InlineData("internal_Field_FamilyOrAssembly", false)]
        public void TestIsFamilyOrAssembly(string fieldName, bool expectedIsFamilyOrAssembly)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsFamilyOrAssembly, fi.IsFamilyOrAssembly);
        }

        [Theory]
        [InlineData("nonstatic_strField", true)]
        [InlineData("intFieldStatic", true)]
        [InlineData("_privateInt", false)]
        [InlineData("_privateStr", false)]
        public void TestIsPublicAndIsPrivate(string fieldName, bool expectedIsPublic)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsPublic, fi.IsPublic);
            Assert.Equal(!expectedIsPublic, fi.IsPrivate);
        }

        [Theory]
        [InlineData("readOnlyIntField", true)]
        [InlineData("intFieldNonStatic", false)]
        public void TestIsInitOnly(string fieldName, bool expectedIsInitOnly)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsInitOnly, fi.IsInitOnly);
        }

        [Theory]
        [InlineData("constIntField", true)]
        [InlineData("intFieldNonStatic", false)]
        public void TestIsLiteral(string fieldName, bool expectedIsLiteral)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedIsLiteral, fi.IsLiteral);
        }

        [Theory]
        [InlineData("intFieldNonStatic", "System.Int32")]
        [InlineData("nonstatic_strField", "System.String")]
        [InlineData("private_Field_Assembly", "System.Object")]
        public void TestFieldType(string fieldName, string expectedFieldType)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedFieldType, fi.FieldType.ToString());
        }

        [Theory]
        [InlineData("intFieldNonStatic", FieldAttributes.Public)]
        [InlineData("intFieldStatic", FieldAttributes.Public | FieldAttributes.Static)]
        [InlineData("_privateInt", FieldAttributes.Private)]
        [InlineData("readOnlyIntField", FieldAttributes.Public | FieldAttributes.InitOnly)]
        public void TestFieldAttributes(string fieldName, FieldAttributes expectedAttributes)
        {
            FieldInfo fi = GetField(fieldName);
            Assert.NotNull(fi);
            Assert.Equal(expectedAttributes, fi.Attributes);
        }

        [Fact]
        public void TestIsSpecialName()
        {
            string fieldname = "intFieldNonStatic";
            FieldInfo fi = GetField(fieldname);
            Assert.NotNull(fi);
            Assert.False(fi.IsSpecialName, "Failed: FieldInfo IsSpecialName returned True for field: " + fieldname);
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(string fieldName)
        {
            Type t = typeof(FieldInfoPropertyTests);
            TypeInfo ti = t.GetTypeInfo();
            FieldInfo fi = null;
            fi = ti.DeclaredFields.Single(x => x.Name == fieldName);

            return fi;
        }

        // Fields for Reflection Metadata

        public static int intFieldStatic = 100;                             // Field for Reflection
        public int intFieldNonStatic = 101;                                 // Field for Reflection
        public static string static_strField = "Static string field";       // Field for Reflection
        public string nonstatic_strField = "NonStatic string field";        // Field for Reflection

        private int _privateInt = 1;                                        // Field for Reflection
        private string _privateStr = "_privateStr";                         // Field for Reflection

        private static object private_Field_Assembly = null;			    // with private keyword
        protected static object protected_Field_Assembly = null;			// with protected keyword
        public static object public_Field_Assembly = null;			        // with public keyword
        internal static object internal_Field_Assembly = null;		        // with internal keyword

        private static object private_Field_FamilyAndAssembly = null;	    // with private keyword
        protected static object protected_Field_FamilyAndAssembly = null;	// with protected keyword
        public static object public_Field_FamilyAndAssembly = null;		    // with public keyword
        internal static object internal_Field_FamilyAndAssembly = null;		// with internal keyword

        private static object private_Field_FamilyOrAssembly = null;	    // with private keyword
        protected static object protected_Field_FamilyOrAssembly = null;    // with protected keyword
        public static object public_Field_FamilyOrAssembly = null;		    // with public keyword
        internal static object internal_Field_FamilyOrAssembly = null;	    // with internal keyword

        public readonly int readOnlyIntField = 1;                           // with readonly keyword
        public const int constIntField = 1222;                              // with constant keyword
    }
}
