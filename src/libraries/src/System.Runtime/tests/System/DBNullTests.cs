// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Tests
{
    public class DBNullTests
    {
        [Fact]
        public void ToString_Invoke_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, DBNull.Value.ToString());
        }

        [Fact]
        public void ToBoolean_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToBoolean(null));
        }

        [Fact]
        public void ToChar_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToChar(null));
        }

        [Fact]
        public void ToSByte_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToSByte(null));
        }

        [Fact]
        public void ToByte_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToByte(null));
        }

        [Fact]
        public void ToInt16_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToInt16(null));
        }

        [Fact]
        public void ToUInt16_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToUInt16(null));
        }

        [Fact]
        public void ToInt32_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToInt32(null));
        }

        [Fact]
        public void ToUInt32_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToUInt32(null));
        }

        [Fact]
        public void ToInt64_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToInt64(null));
        }

        [Fact]
        public void ToUInt64_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToUInt64(null));
        }

        [Fact]
        public void ToSingle_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToSingle(null));
        }

        [Fact]
        public void ToDouble_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToDouble(null));
        }

        [Fact]
        public void ToDecimal_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToDecimal(null));
        }

        [Fact]
        public void ToDateTime_Invoke_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToDateTime(null));
        }

        [Fact]
        public static void GetTypeCode_Invoke_ReturnsDBNull()
        {
            Assert.Equal(TypeCode.DBNull, DBNull.Value.GetTypeCode());
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { DBNull.Value, DBNull.Value, true };
            yield return new object[] { Convert.DBNull, DBNull.Value, true };
            yield return new object[] { DBNull.Value, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Other_ReturnsExpected(DBNull dbNull, object other, bool expected)
        {
            Assert.Equal(expected, dbNull.Equals(other));
        }

        [Fact]
        public void ToType_ValidType_ReturnsEmptyString()
        {
            Assert.Equal(string.Empty, ((IConvertible)DBNull.Value).ToType(typeof(string), null));
        }

        [Fact]
        public void ToType_InvalidType_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => ((IConvertible)DBNull.Value).ToType(typeof(int), null));
        }

        public static IEnumerable<object[]> IsDBNull_TestData()
        {
            yield return new object[] { DBNull.Value, true };
            yield return new object[] { 6689, false };
            yield return new object[] { new object(), false };
        }

        [Theory]
        [MemberData(nameof(IsDBNull_TestData))]
        public void Convert_IsDBNull_ReturnsExpected(object value, bool expected)
        {
            Assert.Equal(expected, Convert.IsDBNull(value));
        }
    }
}
