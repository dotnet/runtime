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
    public class SqlGuidTest
    {
        // 00000a01-0000-0000-0000-000000000000
        private SqlGuid _test1;

        // 00000f64-0000-0000-0000-000000000000
        private SqlGuid _test2;
        private SqlGuid _test3;

        // 0000fafa-0000-0000-0000-000000000000
        private SqlGuid _test4;

        public SqlGuidTest()
        {
            byte[] b1 = new byte[16];
            byte[] b2 = new byte[16];
            byte[] b3 = new byte[16];
            byte[] b4 = new byte[16];

            b1[0] = 1;
            b1[1] = 10;
            b2[0] = 100;
            b2[1] = 15;
            b3[0] = 100;
            b3[1] = 15;
            b4[0] = 250;
            b4[1] = 250;

            _test1 = new SqlGuid(b1);
            _test2 = new SqlGuid(b2);
            _test3 = new SqlGuid(b3);
            _test4 = new SqlGuid(b4);
        }

        // Test constructor
        [Fact]
        public void Create()
        {
            // SqlGuid (Byte[])
            byte[] b = new byte[16];
            b[0] = 100;
            b[1] = 200;

            _ =  new SqlGuid(b);

            // SqlGuid (Guid)
            Guid TestGuid = new Guid(b);
            _ = new SqlGuid(TestGuid);

            // SqlGuid (string)
            _ = new SqlGuid("12345678-1234-1234-1234-123456789012");

            // SqlGuid (int, short, short, byte, byte, byte, byte, byte, byte, byte, byte)
            _ = new SqlGuid(10, 1, 2, 13, 14, 15, 16, 17, 19, 20, 21);
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            Assert.True(SqlGuid.Null.IsNull);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            Guid ResultGuid = new Guid("00000f64-0000-0000-0000-000000000000");
            Assert.False(_test1.IsNull);
            Assert.True(SqlGuid.Null.IsNull);
            Assert.Equal(ResultGuid, _test2.Value);
        }

        // PUBLIC METHODS
        [Fact]
        public void CompareTo()
        {
            string testString = "This is a test string";
            SqlGuid test1 = new SqlGuid("1AAAAAAA-BBBB-CCCC-DDDD-3EEEEEEEEEEE");
            SqlGuid test2 = new SqlGuid("1AAAAAAA-BBBB-CCCC-DDDD-2EEEEEEEEEEE");
            SqlGuid test3 = new SqlGuid("1AAAAAAA-BBBB-CCCC-DDDD-1EEEEEEEEEEE");
            Assert.True(_test1.CompareTo(_test3) < 0);
            Assert.True(_test4.CompareTo(_test1) > 0);
            Assert.Equal(0, _test3.CompareTo(_test2));
            Assert.True(_test4.CompareTo(SqlGuid.Null) > 0);
            Assert.True(test1.CompareTo(test2) > 0);
            Assert.True(test3.CompareTo(test2) < 0);

            Assert.Throws<ArgumentException>(() => _test1.CompareTo(testString));
        }

        [Fact]
        public void EqualsMethods()
        {
            Assert.False(_test1.Equals(_test2));
            Assert.False(_test1.Equals((object)_test2));
            Assert.False(_test2.Equals(_test4));
            Assert.False(_test2.Equals((object)_test4));
            Assert.False(_test2.Equals(new SqlString("TEST")));
            Assert.True(_test2.Equals(_test3));
            Assert.True(_test2.Equals((object)_test3));

            // Static Equals()-method
            Assert.True(SqlGuid.Equals(_test2, _test3).Value);
            Assert.False(SqlGuid.Equals(_test1, _test2).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            Assert.Equal(_test1.GetHashCode(), _test1.GetHashCode());
            Assert.NotEqual(_test1.GetHashCode(), _test2.GetHashCode());
            Assert.Equal(_test3.GetHashCode(), _test2.GetHashCode());
        }

        [Fact]
        public void Greaters()
        {
            // GreateThan ()
            Assert.False(SqlGuid.GreaterThan(_test1, _test2).Value);
            Assert.True(SqlGuid.GreaterThan(_test2, _test1).Value);
            Assert.False(SqlGuid.GreaterThan(_test2, _test3).Value);
            // GreaterTharOrEqual ()
            Assert.False(SqlGuid.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlGuid.GreaterThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlGuid.GreaterThanOrEqual(_test2, _test3).Value);
        }

        [Fact]
        public void Lessers()
        {
            // LessThan()
            Assert.False(SqlGuid.LessThan(_test2, _test3).Value);
            Assert.False(SqlGuid.LessThan(_test2, _test1).Value);
            Assert.True(SqlGuid.LessThan(_test1, _test2).Value);

            // LessThanOrEqual ()
            Assert.True(SqlGuid.LessThanOrEqual(_test1, _test2).Value);
            Assert.False(SqlGuid.LessThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlGuid.LessThanOrEqual(_test2, _test3).Value);
            Assert.True(SqlGuid.LessThanOrEqual(_test4, SqlGuid.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            Assert.True(SqlGuid.NotEquals(_test1, _test2).Value);
            Assert.True(SqlGuid.NotEquals(_test2, _test1).Value);
            Assert.True(SqlGuid.NotEquals(_test3, _test1).Value);
            Assert.False(SqlGuid.NotEquals(_test3, _test2).Value);
            Assert.True(SqlGuid.NotEquals(SqlGuid.Null, _test2).IsNull);
        }

        [Fact]
        public void Parse()
        {
            Assert.Throws<ArgumentNullException>(() => SqlGuid.Parse(null));

            Assert.Throws<FormatException>(() => SqlGuid.Parse("not-a-number"));

            Assert.Throws<FormatException>(() => SqlGuid.Parse("9e400"));

            Assert.Equal(new Guid("87654321-0000-0000-0000-000000000000"), SqlGuid.Parse("87654321-0000-0000-0000-000000000000").Value);
        }

        [Fact]
        public void Conversions()
        {
            // ToByteArray ()
            Assert.Equal((byte)1, _test1.ToByteArray()[0]);
            Assert.Equal((byte)15, _test2.ToByteArray()[1]);

            // ToSqlBinary ()
            byte[] b = new byte[2] { 100, 15 };

            Assert.Equal(new SqlBinary(b), _test3.ToSqlBinary());

            // ToSqlString ()
            Assert.Equal("00000a01-0000-0000-0000-000000000000", _test1.ToSqlString().Value);
            Assert.Equal("0000fafa-0000-0000-0000-000000000000", _test4.ToSqlString().Value);

            // ToString ()
            Assert.Equal("00000a01-0000-0000-0000-000000000000", _test1.ToString());
            Assert.Equal("0000fafa-0000-0000-0000-000000000000", _test4.ToString());
        }

        // OPERATORS

        [Fact]
        public void ThanOrEqualOperators()
        {
            // == -operator
            Assert.True((_test3 == _test2).Value);
            Assert.False((_test1 == _test2).Value);
            Assert.True((_test1 == SqlGuid.Null).IsNull);

            // != -operator
            Assert.False((_test2 != _test3).Value);
            Assert.True((_test1 != _test3).Value);
            Assert.True((_test1 != SqlGuid.Null).IsNull);

            // > -operator
            Assert.True((_test2 > _test1).Value);
            Assert.False((_test1 > _test3).Value);
            Assert.False((_test3 > _test2).Value);
            Assert.True((_test1 > SqlGuid.Null).IsNull);

            // >=  -operator
            Assert.False((_test1 >= _test3).Value);
            Assert.True((_test3 >= _test1).Value);
            Assert.True((_test3 >= _test2).Value);
            Assert.True((_test1 >= SqlGuid.Null).IsNull);

            // < -operator
            Assert.False((_test2 < _test1).Value);
            Assert.True((_test1 < _test3).Value);
            Assert.False((_test2 < _test3).Value);
            Assert.True((_test1 < SqlGuid.Null).IsNull);

            // <= -operator
            Assert.True((_test1 <= _test3).Value);
            Assert.False((_test3 <= _test1).Value);
            Assert.True((_test2 <= _test3).Value);
            Assert.True((_test1 <= SqlGuid.Null).IsNull);
        }

        [Fact]
        public void SqlBinaryToSqlGuid()
        {
            byte[] b = new byte[16];
            b[0] = 100;
            b[1] = 200;
            SqlBinary testBinary = new SqlBinary(b);

            Assert.Equal(new Guid("0000c864-0000-0000-0000-000000000000"), ((SqlGuid)testBinary).Value);
        }

        [Fact]
        public void SqlGuidToGuid()
        {
            Assert.Equal(new Guid("00000a01-0000-0000-0000-000000000000"), (Guid)_test1);
            Assert.Equal(new Guid("00000f64-0000-0000-0000-000000000000"), (Guid)_test2);
        }

        [Fact]
        public void SqlStringToSqlGuid()
        {
            SqlString testString = new SqlString("Test string");
            SqlString testString100 = new SqlString("0000c864-0000-0000-0000-000000000000");

            Assert.Equal(new Guid("0000c864-0000-0000-0000-000000000000"), ((SqlGuid)testString100).Value);

            Assert.Throws<FormatException>(() => (SqlGuid)testString);
        }

        [Fact]
        public void GuidToSqlGuid()
        {
            Guid testGuid = new Guid("0000c864-0000-0000-0000-000007650000");
            Assert.Equal(new SqlGuid("0000c864-0000-0000-0000-000007650000"), testGuid);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlGuid.GetXsdType(null);
            Assert.Equal("string", qualifiedName.Name);
        }
    }
}
