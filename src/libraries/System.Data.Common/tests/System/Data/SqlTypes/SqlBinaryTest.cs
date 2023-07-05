// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// (C) 2002 Ville Palo
// (C) 2003 Martin Willemoes Hansen

// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Xunit;
using System.Xml;
using System.Data.SqlTypes;

namespace System.Data.Tests.SqlTypes
{
    public class SqlBinaryTest
    {
        private SqlBinary _test1;
        private SqlBinary _test2;
        private SqlBinary _test3;

        public SqlBinaryTest()
        {
            byte[] b1 = new byte[2] { 240, 15 };
            byte[] b2 = new byte[3] { 10, 10, 10 };
            byte[] b3 = new byte[2] { 240, 15 };

            _test1 = new SqlBinary(b1);
            _test2 = new SqlBinary(b2);
            _test3 = new SqlBinary(b3);
        }

        // Test constructor
        [Fact]
        public void Create()
        {
            byte[] b = new byte[3];
            SqlBinary test = new SqlBinary(b);
            Assert.False(test.IsNull);
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            Assert.True(SqlBinary.Null.IsNull);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            byte[] b = new byte[2] { 64, 128 };

            SqlBinary testBinary = new SqlBinary(b);

            // IsNull
            Assert.True(SqlBinary.Null.IsNull);

            // Item
            Assert.Equal((byte)128, testBinary[1]);
            Assert.Equal((byte)64, testBinary[0]);

            // See https://github.com/dotnet/runtime/issues/30416
            Assert.Throws<IndexOutOfRangeException>(() => testBinary[testBinary.Length]);

            Assert.Throws<SqlNullValueException>(() => SqlBinary.Null[2]);

            // Length
            Assert.Equal(2, testBinary.Length);

            Assert.Throws<SqlNullValueException>(() => SqlBinary.Null.Length);

            // Value
            Assert.Equal((byte)128, testBinary[1]);
            Assert.Equal((byte)64, testBinary[0]);

            Assert.Throws<SqlNullValueException>(() => SqlBinary.Null.Value);
        }

        // Methods
        [Fact]
        public void ComparisonMethods()
        {
            // GreaterThan
            Assert.True(SqlBinary.GreaterThan(_test1, _test2).Value);
            Assert.True(SqlBinary.GreaterThan(_test3, _test2).Value);
            Assert.False(SqlBinary.GreaterThan(_test2, _test1).Value);

            // GreaterThanOrEqual
            Assert.True(SqlBinary.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlBinary.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.False(SqlBinary.GreaterThanOrEqual(_test2, _test1).Value);

            // LessThan
            Assert.False(SqlBinary.LessThan(_test1, _test2).Value);
            Assert.False(SqlBinary.LessThan(_test3, _test2).Value);
            Assert.True(SqlBinary.LessThan(_test2, _test1).Value);

            // LessThanOrEqual
            Assert.False(SqlBinary.LessThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlBinary.LessThanOrEqual(_test3, _test1).Value);
            Assert.True(SqlBinary.LessThanOrEqual(_test2, _test1).Value);

            // Equals
            Assert.False(_test1.Equals(_test2));
            Assert.False(_test1.Equals((object)_test2));
            Assert.False(_test3.Equals(_test2));
            Assert.False(_test3.Equals((object)_test2));
            Assert.True(_test3.Equals(_test1));
            Assert.True(_test3.Equals((object)_test1));

            // NotEquals
            Assert.True(SqlBinary.NotEquals(_test1, _test2).Value);
            Assert.False(SqlBinary.NotEquals(_test3, _test1).Value);
            Assert.True(SqlBinary.NotEquals(_test2, _test1).Value);
        }

        [Fact]
        public void CompareTo()
        {
            SqlString testString = new SqlString("This is a test");

            Assert.True(_test1.CompareTo(_test2) > 0);
            Assert.True(_test2.CompareTo(_test1) < 0);
            Assert.Equal(0, _test1.CompareTo(_test3));

            Assert.Throws<ArgumentException>(() => _test1.CompareTo(testString));
        }

        [Fact]
        public void GetHashCodeTest()
        {
            Assert.Equal(_test1.GetHashCode(), _test1.GetHashCode());
            Assert.NotEqual(_test2.GetHashCode(), _test1.GetHashCode());
        }

        [Fact]
        public void Concat()
        {
            SqlBinary testBinary;

            testBinary = SqlBinary.Concat(_test2, _test3);
            Assert.Equal((byte)15, testBinary[4]);

            testBinary = SqlBinary.Concat(_test1, _test2);
            Assert.Equal((byte)240, testBinary[0]);
            Assert.Equal((byte)15, testBinary[1]);
        }

        [Fact]
        public void ToSqlGuid()
        {
            SqlBinary testBinary = new SqlBinary(new byte[16]);
            SqlGuid testGuid = testBinary.ToSqlGuid();
            Assert.False(testGuid.IsNull);
        }

        [Fact]
        public void ToStringTest()
        {
            Assert.Equal("SqlBinary(3)", _test2.ToString());
            Assert.Equal("SqlBinary(2)", _test1.ToString());
        }

        // OPERATORS
        [Fact]
        public void AdditionOperator()
        {
            SqlBinary testBinary = _test1 + _test2;
            Assert.Equal((byte)240, testBinary[0]);
            Assert.Equal((byte)15, testBinary[1]);
        }

        [Fact]
        public void ComparisonOperators()
        {
            // Equality
            Assert.False((_test1 == _test2).Value);
            Assert.True((_test3 == _test1).Value);

            // Greater than
            Assert.True((_test1 > _test2).Value);
            Assert.False((_test3 > _test1).Value);

            // Greater than or equal
            Assert.True((_test1 >= _test2).Value);
            Assert.True((_test3 >= _test2).Value);

            // Inequality
            Assert.True((_test1 != _test2).Value);
            Assert.False((_test3 != _test1).Value);

            // Less than
            Assert.False((_test1 < _test2).Value);
            Assert.False((_test3 < _test2).Value);

            // Less than or equal
            Assert.False((_test1 <= _test2).Value);
            Assert.True((_test3 <= _test1).Value);
        }

        [Fact]
        public void SqlBinaryToByteArray()
        {
            byte[] testByteArray = (byte[])_test1;
            Assert.Equal((byte)240, testByteArray[0]);
        }

        [Fact]
        public void SqlGuidToSqlBinary()
        {
            byte[] testByteArray = new byte[16];
            testByteArray[0] = 15;
            testByteArray[1] = 200;
            SqlGuid testGuid = new SqlGuid(testByteArray);

            SqlBinary testBinary = (SqlBinary)testGuid;
            Assert.Equal((byte)15, testBinary[0]);
        }

        [Fact]
        public void ByteArrayToSqlBinary()
        {
            byte[] testByteArray = new byte[2] { 15, 200 };
            SqlBinary testBinary = testByteArray;
            Assert.Equal((byte)15, testBinary[0]);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlBinary.GetXsdType(null);
            Assert.Equal("base64Binary", qualifiedName.Name);
        }

        [Fact]
        public void WrapBytes()
        {
            byte[] bytes = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 };

            SqlBinary binary = SqlBinary.WrapBytes(bytes);

            Assert.Equal(bytes[5], binary[5]);

            // verify that changing the byte[] that was passed in also changes the value in the SqlBinary instance
            bytes[5] = 0xFF;
            Assert.Equal(bytes[5], binary[5]);

            SqlBinary nullBinary = SqlBinary.WrapBytes(null);

            Assert.True(nullBinary.IsNull);
        }
    }
}
