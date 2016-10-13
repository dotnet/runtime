// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
            byte[] b1 = new byte[2];
            byte[] b2 = new byte[3];
            byte[] b3 = new byte[2];

            b1[0] = 240;
            b1[1] = 15;
            b2[0] = 10;
            b2[1] = 10;
            b2[2] = 10;
            b3[0] = 240;
            b3[1] = 15;

            _test1 = new SqlBinary(b1);
            _test2 = new SqlBinary(b2);
            _test3 = new SqlBinary(b3);
        }

        // Test constructor
        [Fact]
        public void Create()
        {
            byte[] b = new byte[3];
            SqlBinary Test = new SqlBinary(b);
            Assert.True(!(Test.IsNull));
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
            byte[] b = new byte[2];
            b[0] = 64;
            b[1] = 128;

            SqlBinary TestBinary = new SqlBinary(b);

            // IsNull
            Assert.True(SqlBinary.Null.IsNull);

            // Item
            Assert.Equal((byte)128, TestBinary[1]);
            Assert.Equal((byte)64, TestBinary[0]);

            // FIXME: MSDN says that should throw SqlNullValueException
            // but throws IndexOutOfRangeException
            try
            {
                byte test = TestBinary[TestBinary.Length];
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
            }

            try
            {
                byte test = SqlBinary.Null[2];
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(SqlNullValueException), e.GetType());
            }

            // Length
            Assert.Equal(2, TestBinary.Length);

            try
            {
                int test = SqlBinary.Null.Length;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(SqlNullValueException), e.GetType());
            }

            // Value
            Assert.Equal((byte)128, TestBinary[1]);
            Assert.Equal((byte)64, TestBinary[0]);

            try
            {
                byte[] test = SqlBinary.Null.Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(SqlNullValueException), e.GetType());
            }
        }

        // Methods 
        [Fact]
        public void ComparisonMethods()
        {
            // GreaterThan
            Assert.True(SqlBinary.GreaterThan(_test1, _test2).Value);
            Assert.True(SqlBinary.GreaterThan(_test3, _test2).Value);
            Assert.True(!SqlBinary.GreaterThan(_test2, _test1).Value);

            // GreaterThanOrEqual
            Assert.True(SqlBinary.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlBinary.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(!SqlBinary.GreaterThanOrEqual(_test2, _test1).Value);

            // LessThan
            Assert.True(!SqlBinary.LessThan(_test1, _test2).Value);
            Assert.True(!SqlBinary.LessThan(_test3, _test2).Value);
            Assert.True(SqlBinary.LessThan(_test2, _test1).Value);

            // LessThanOrEqual
            Assert.True(!SqlBinary.LessThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlBinary.LessThanOrEqual(_test3, _test1).Value);
            Assert.True(SqlBinary.LessThanOrEqual(_test2, _test1).Value);

            // Equals
            Assert.True(!_test1.Equals(_test2));
            Assert.True(!_test3.Equals(_test2));
            Assert.True(_test3.Equals(_test1));

            // NotEquals
            Assert.True(SqlBinary.NotEquals(_test1, _test2).Value);
            Assert.True(!SqlBinary.NotEquals(_test3, _test1).Value);
            Assert.True(SqlBinary.NotEquals(_test2, _test1).Value);
        }

        [Fact]
        public void CompareTo()
        {
            SqlString TestString = new SqlString("This is a test");

            Assert.True(_test1.CompareTo(_test2) > 0);
            Assert.True(_test2.CompareTo(_test1) < 0);
            Assert.True(_test1.CompareTo(_test3) == 0);

            try
            {
                _test1.CompareTo(TestString);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
            }
        }

        [Fact]
        public void GetHashCodeTest()
        {
            Assert.Equal(_test1.GetHashCode(), _test1.GetHashCode());
            Assert.True(_test2.GetHashCode() != _test1.GetHashCode());
        }

        [Fact]
        public void GetTypeTest()
        {
            Assert.Equal("System.Data.SqlTypes.SqlBinary", _test1.GetType().ToString());
        }

        [Fact]
        public void Concat()
        {
            SqlBinary TestBinary;

            TestBinary = SqlBinary.Concat(_test2, _test3);
            Assert.Equal((byte)15, TestBinary[4]);

            TestBinary = SqlBinary.Concat(_test1, _test2);
            Assert.Equal((byte)240, TestBinary[0]);
            Assert.Equal((byte)15, TestBinary[1]);
        }

        [Fact]
        public void ToSqlGuid()
        {
            SqlBinary TestBinary = new SqlBinary(new byte[16]);
            SqlGuid TestGuid = TestBinary.ToSqlGuid();
            Assert.True(!TestGuid.IsNull);
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
            SqlBinary TestBinary = _test1 + _test2;
            Assert.Equal((byte)240, TestBinary[0]);
            Assert.Equal((byte)15, TestBinary[1]);
        }

        [Fact]
        public void ComparisonOperators()
        {
            // Equality
            Assert.True(!(_test1 == _test2).Value);
            Assert.True((_test3 == _test1).Value);

            // Greater than
            Assert.True((_test1 > _test2).Value);
            Assert.True(!(_test3 > _test1).Value);

            // Greater than or equal
            Assert.True((_test1 >= _test2).Value);
            Assert.True((_test3 >= _test2).Value);

            // Inequality
            Assert.True((_test1 != _test2).Value);
            Assert.True(!(_test3 != _test1).Value);

            // Less than
            Assert.True(!(_test1 < _test2).Value);
            Assert.True(!(_test3 < _test2).Value);

            // Less than or equal
            Assert.True(!(_test1 <= _test2).Value);
            Assert.True((_test3 <= _test1).Value);
        }

        [Fact]
        public void SqlBinaryToByteArray()
        {
            byte[] TestByteArray = (byte[])_test1;
            Assert.Equal((byte)240, TestByteArray[0]);
        }

        [Fact]
        public void SqlGuidToSqlBinary()
        {
            byte[] TestByteArray = new byte[16];
            TestByteArray[0] = 15;
            TestByteArray[1] = 200;
            SqlGuid TestGuid = new SqlGuid(TestByteArray);

            SqlBinary TestBinary = (SqlBinary)TestGuid;
            Assert.Equal((byte)15, TestBinary[0]);
        }

        [Fact]
        public void ByteArrayToSqlBinary()
        {
            byte[] TestByteArray = new byte[2];
            TestByteArray[0] = 15;
            TestByteArray[1] = 200;
            SqlBinary TestBinary = TestByteArray;
            Assert.Equal((byte)15, TestBinary[0]);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlBinary.GetXsdType(null);
            Assert.Equal("base64Binary", qualifiedName.Name);
        }
    }
}

