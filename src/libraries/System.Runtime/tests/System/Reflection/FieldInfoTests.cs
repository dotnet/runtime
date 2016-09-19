// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Reflection.Tests
{
    public class FieldInfoTests
    {
        [Theory]
        [InlineData("nonstatic_strField", "nonstatic_strField", true)]
        [InlineData("nonstatic_strField", "intFieldStatic", false)]
        public static void TestEquality_EqualAndNotEqualFields(string fieldName1, string fieldName2, bool expected)
        {
            FieldInfo info1 = GetField(fieldName1);
            FieldInfo info2 = GetField(fieldName2);

            Assert.NotNull(info1);
            Assert.NotNull(info2);
            Assert.Equal(expected, info1 == info2);
            Assert.NotEqual(expected, info1 != info2);
        }

        [Fact]
        public static void Test_SecurityAttributes()
        {
            FieldInfo info = GetField("intFieldStatic");

            Assert.True(info.IsSecurityCritical);
            Assert.False(info.IsSecuritySafeCritical);
            Assert.False(info.IsSecurityTransparent);
        }

        // Helper method to get field from Type type
        private static FieldInfo GetField(string fieldName)
        {
            Type t = typeof(FieldInfoTests);
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
    }
}