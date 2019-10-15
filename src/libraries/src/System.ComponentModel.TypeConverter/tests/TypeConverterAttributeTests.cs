// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeConverterAttributeTests
    {
        // TypeConverterAttribute does not impose any restrictions on the type arguments.
        private static TypeConverterAttribute s_attributeFromType = new TypeConverterAttribute(typeof(int));
        private static TypeConverterAttribute s_attributeFromString = new TypeConverterAttribute("System.Int32");

        [Fact]
        public static void Get_ConverterTypeName()
        {
            Assert.Equal(typeof(int).AssemblyQualifiedName, TypeConverterAttributeTests.s_attributeFromType.ConverterTypeName);
            Assert.Equal("System.Int32", TypeConverterAttributeTests.s_attributeFromString.ConverterTypeName);
        }

        [Fact]
        public static void Equals_Negative()
        {
            Assert.False(TypeConverterAttributeTests.s_attributeFromType.Equals(TypeConverterAttributeTests.s_attributeFromString));
            Assert.False(TypeConverterAttributeTests.s_attributeFromType.Equals(null));
        }

        [Fact]
        public static void Equals_Positive()
        {
            Assert.True(new TypeConverterAttribute("System.Int32").Equals(TypeConverterAttributeTests.s_attributeFromString));
        }

        [Fact]
        public static void GetHashCode_Positive()
        {
            Assert.Equal("System.Int32".GetHashCode(), TypeConverterAttributeTests.s_attributeFromString.GetHashCode());
        }
    }
}
