// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class InformationTests
    {
        [Theory]
        [InlineData(new int[0], true)]
        [InlineData(null, false)]
        public void IsArray(object value, bool expected)
        {
            Assert.Equal(expected, Information.IsArray(value));
        }

        [Fact]
        public void IsDate()
        {
            Assert.True(Information.IsDate(DateTime.MinValue));
            Assert.False(Information.IsDate(null));
            Assert.True(Information.IsDate("2018-01-01"));
            Assert.False(Information.IsDate("abc"));
            Assert.False(Information.IsDate(Guid.Empty));
        }

        [Fact]
        public void IsDBNull()
        {
            Assert.True(Information.IsDBNull(DBNull.Value));
            Assert.False(Information.IsDBNull(null));
            Assert.False(Information.IsDBNull("abc"));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("abc", false)]
        public void IsNothing(object value, bool expected)
        {
            Assert.Equal(expected, Information.IsNothing(value));
        }

        [Fact]
        public void IsError()
        {
            Assert.False(Information.IsError(null));
            Assert.True(Information.IsError(new Exception()));
            Assert.False(Information.IsError("abc"));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("abc", true)]
        [InlineData(1, false)]
        public void IsReference(object value, bool expected)
        {
            Assert.Equal(expected, Information.IsReference(value));
        }

        [Fact]
        public void LBound()
        {
            Assert.Equal(0, Information.LBound(new int[1]));
            Assert.Equal(5, Information.LBound(Array.CreateInstance("abc".GetType(), new int[] { 1 }, new int[] { 5 })));
            Assert.Equal(6, Information.LBound(Array.CreateInstance("abc".GetType(), new int[] { 1, 1 }, new int[] { 5, 6 }), 2));
        }

        [Fact]
        public void LBound_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => Information.LBound(null));
            Assert.Throws<RankException>(() => Information.LBound(new int[0], 0));
            Assert.Throws<RankException>(() => Information.LBound(new int[0], 2));
        }

        [Fact]
        public void UBound()
        {
            Assert.Equal(-1, Information.UBound(new int[0]));
            Assert.Equal(0, Information.UBound(new int[1]));
            Assert.Equal(5, Information.UBound(Array.CreateInstance("abc".GetType(), new int[] { 1 }, new int[] { 5 })));
            Assert.Equal(6, Information.UBound(Array.CreateInstance("abc".GetType(), new int[] { 1, 1 }, new int[] { 5, 6 }), 2));
        }

        [Fact]
        public void UBound_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => Information.UBound(null));
            Assert.Throws<RankException>(() => Information.UBound(new int[0], 0));
            Assert.Throws<RankException>(() => Information.UBound(new int[0], 2));
        }

        [Theory]
        [InlineData(4, 128)]
        [InlineData(0, 0)]
        [InlineData(15, 16777215)]
        public void QBColor(int value, int expected)
        {
            Assert.Equal(expected, Information.QBColor(value));
        }

        [Fact]
        public void QBColor_Invalid()
        {
            Assert.Throws<ArgumentException>(() => Information.QBColor(-1));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(255, 255, 255, 16777215)]
        [InlineData(300, 400, 500, 16777215)]
        public void RGB(int red, int green, int blue, int expected)
        {
            Assert.Equal(expected, Information.RGB(red, green, blue));
        }

        [Fact]
        public void RGB_Invalid()
        {
            Assert.Throws<ArgumentException>(() => Information.RGB(-1, -1, -1));
            Assert.Throws<ArgumentException>(() => Information.RGB(1, -1, -1));
            Assert.Throws<ArgumentException>(() => Information.RGB(1, 1, -1));
        }

        [Theory]
        [InlineData(null, VariantType.Object)]
        [InlineData(new int[0], VariantType.Array | VariantType.Integer)]
        [InlineData(StringComparison.Ordinal, VariantType.Integer)]
        [InlineData("abc", VariantType.String)]
        [InlineData((short)1, VariantType.Short)]
        [InlineData((long)1, VariantType.Long)]
        [InlineData((float)1, VariantType.Single)]
        [InlineData((double)1, VariantType.Double)]
        [InlineData(true, VariantType.Boolean)]
        [InlineData((byte)1, VariantType.Byte)]
        [InlineData('a', VariantType.Char)]
        public void VarType(object value, VariantType expected)
        {
            Assert.Equal(expected, Information.VarType(value));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData('a', false)]
        [InlineData(1, true)]
        [InlineData("12x", false)]
        [InlineData("123", true)]
        [InlineData('1', true)]
        [InlineData('a', false)]
        [InlineData("&O123", true)]
        [InlineData("&H123", true)]
        public void IsNumeric(object value, bool expected)
        {
            Assert.Equal(expected, Information.IsNumeric(value));
        }

        [Fact]
        public void IsNumeric_Invalid()
        {
            Assert.Throws<NullReferenceException>(() => Information.IsNumeric(new char[] { '1', '2', '3' })); // Bug compatible
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("OBJECT", "System.Object")]
        [InlineData(" OBJECT ", "System.Object")]
        [InlineData("object", "System.Object")]
        [InlineData("custom", null)]
        public void SystemTypeName(string value, string expected)
        {
            Assert.Equal(expected, Information.SystemTypeName(value));
        }

        [Theory]
        [MemberData(nameof(TypeName_TestData))]
        public void TypeName(object expression, string expected)
        {
            Assert.Equal(expected, Information.TypeName(expression));
        }

        private static IEnumerable<object[]> TypeName_TestData()
        {
            yield return new object[] { null, "Nothing" };
            yield return new object[] { new object(), "Object" };
            yield return new object[] { default(bool), "Boolean" };
            yield return new object[] { default(char), "Char" };
            yield return new object[] { default(sbyte), "SByte" };
            yield return new object[] { default(byte), "Byte" };
            yield return new object[] { default(short), "Short" };
            yield return new object[] { default(ushort), "UInt16" };
            yield return new object[] { default(int), "Integer" };
            yield return new object[] { default(uint), "UInt32" };
            yield return new object[] { default(long), "Long" };
            yield return new object[] { default(ulong), "UInt64" };
            yield return new object[] { default(float), "Single" };
            yield return new object[] { default(double), "Double" };
            yield return new object[] { default(decimal), "Decimal" };
            yield return new object[] { default(DateTime), "Date" };
            yield return new object[] { "", "String" };
            yield return new object[] { default(object[]), "Nothing" };
            yield return new object[] { new object[0], "Object()" };
            yield return new object[] { new char[0, 0], "Char(,)" };
            yield return new object[] { default(int?), "Nothing" };
            yield return new object[] { (int?)0, "Integer" };
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("System.Object", "Object")]
        [InlineData("Object", "Object")]
        [InlineData(" object ", "Object")]
        [InlineData("custom", null)]
        public void VbTypeName(string value, string expected)
        {
            Assert.Equal(expected, Information.VbTypeName(value));
        }
    }
}
