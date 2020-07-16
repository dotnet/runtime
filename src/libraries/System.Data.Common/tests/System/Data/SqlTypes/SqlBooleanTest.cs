// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// (C) Ville Palo

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
    public class SqlBooleanTest
    {
        private SqlBoolean _sqlTrue;
        private SqlBoolean _sqlFalse;

        public SqlBooleanTest()
        {
            _sqlTrue = new SqlBoolean(true);
            _sqlFalse = new SqlBoolean(false);
        }

        [Fact]
        public void Create()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(1);
            SqlBoolean sqlFalse2 = new SqlBoolean(0);

            Assert.True(_sqlTrue.Value);
            Assert.True(sqlTrue2.Value);
            Assert.False(_sqlFalse.Value);
            Assert.False(sqlFalse2.Value);
        }

        ////
        // PUBLIC STATIC METHODS
        //

        // And
        [Fact]
        public void And()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            // One result value
            SqlBoolean sqlResult;

            // true && false
            sqlResult = SqlBoolean.And(_sqlTrue, _sqlFalse);
            Assert.False(sqlResult.Value);
            sqlResult = SqlBoolean.And(_sqlFalse, _sqlTrue);
            Assert.False(sqlResult.Value);

            // true && true
            sqlResult = SqlBoolean.And(_sqlTrue, sqlTrue2);
            Assert.True(sqlResult.Value);

            sqlResult = SqlBoolean.And(_sqlTrue, _sqlTrue);
            Assert.True(sqlResult.Value);

            // false && false
            sqlResult = SqlBoolean.And(_sqlFalse, sqlFalse2);
            Assert.False(sqlResult.Value);
            sqlResult = SqlBoolean.And(_sqlFalse, _sqlFalse);
            Assert.False(sqlResult.Value);
        }

        // NotEquals
        [Fact]
        public void NotEquals()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);

            SqlBoolean sqlResult;

            // true != false
            sqlResult = SqlBoolean.NotEquals(_sqlTrue, _sqlFalse);
            Assert.True(sqlResult.Value);
            sqlResult = SqlBoolean.NotEquals(_sqlFalse, _sqlTrue);
            Assert.True(sqlResult.Value);


            // true != true
            sqlResult = SqlBoolean.NotEquals(_sqlTrue, _sqlTrue);
            Assert.False(sqlResult.Value);
            sqlResult = SqlBoolean.NotEquals(_sqlTrue, sqlTrue2);
            Assert.False(sqlResult.Value);
            // false != false
            sqlResult = SqlBoolean.NotEquals(_sqlFalse, _sqlFalse);
            Assert.False(sqlResult.Value);
            sqlResult = SqlBoolean.NotEquals(_sqlTrue, sqlTrue2);
            Assert.False(sqlResult.Value);

            // If either instance of SqlBoolean is null, the Value of the SqlBoolean will be Null.
            sqlResult = SqlBoolean.NotEquals(SqlBoolean.Null, _sqlFalse);
            Assert.True(sqlResult.IsNull);
            sqlResult = SqlBoolean.NotEquals(_sqlTrue, SqlBoolean.Null);
            Assert.True(sqlResult.IsNull);
        }

        // OnesComplement
        [Fact]
        public void OnesComplement()
        {
            SqlBoolean sqlFalse2 = SqlBoolean.OnesComplement(_sqlTrue);
            Assert.False(sqlFalse2.Value);

            SqlBoolean sqlTrue2 = SqlBoolean.OnesComplement(_sqlFalse);
            Assert.True(sqlTrue2.Value);
        }

        // Or
        [Fact]
        public void Or()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            SqlBoolean sqlResult;

            // true || false
            sqlResult = SqlBoolean.Or(_sqlTrue, _sqlFalse);
            Assert.True(sqlResult.Value);
            sqlResult = SqlBoolean.Or(_sqlFalse, _sqlTrue);
            Assert.True(sqlResult.Value);

            // true || true
            sqlResult = SqlBoolean.Or(_sqlTrue, _sqlTrue);
            Assert.True(sqlResult.Value);
            sqlResult = SqlBoolean.Or(_sqlTrue, sqlTrue2);
            Assert.True(sqlResult.Value);

            // false || false
            sqlResult = SqlBoolean.Or(_sqlFalse, _sqlFalse);
            Assert.False(sqlResult.Value);
            sqlResult = SqlBoolean.Or(_sqlFalse, sqlFalse2);
            Assert.False(sqlResult.Value);
        }


        //  Parse
        [Fact]
        public void Parse()
        {
            Assert.True(SqlBoolean.Parse("True").Value);
            Assert.True(SqlBoolean.Parse(" True").Value);
            Assert.True(SqlBoolean.Parse("True ").Value);
            Assert.True(SqlBoolean.Parse("tRuE").Value);
            Assert.False(SqlBoolean.Parse("False").Value);
            Assert.False(SqlBoolean.Parse(" False").Value);
            Assert.False(SqlBoolean.Parse("False ").Value);
            Assert.False(SqlBoolean.Parse("fAlSe").Value);
        }

        // Xor
        [Fact]
        public void Xor()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            SqlBoolean sqlResult;

            // true ^ false
            sqlResult = SqlBoolean.Xor(_sqlTrue, _sqlFalse);
            Assert.True(sqlResult.Value);
            sqlResult = SqlBoolean.Xor(_sqlFalse, _sqlTrue);
            Assert.True(sqlResult.Value);

            // true ^ true
            sqlResult = SqlBoolean.Xor(_sqlTrue, sqlTrue2);
            Assert.False(sqlResult.Value);

            // false ^ false
            sqlResult = SqlBoolean.Xor(_sqlFalse, sqlFalse2);
            Assert.False(sqlResult.Value);
        }

        // static Equals
        [Fact]
        public void StaticEquals()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            Assert.True(SqlBoolean.Equals(_sqlTrue, sqlTrue2).Value);
            Assert.True(SqlBoolean.Equals(_sqlFalse, sqlFalse2).Value);

            Assert.False(SqlBoolean.Equals(_sqlTrue, _sqlFalse).Value);
            Assert.False(SqlBoolean.Equals(_sqlFalse, _sqlTrue).Value);

            Assert.Equal(SqlBoolean.Null, SqlBoolean.Equals(SqlBoolean.Null, _sqlFalse));
            Assert.Equal(SqlBoolean.Null, SqlBoolean.Equals(_sqlTrue, SqlBoolean.Null));
        }

        //
        // END OF STATIC METHODS
        ////

        ////
        // PUBLIC METHODS
        //

        // CompareTo
        [Fact]
        public void CompareTo()
        {

            Assert.True((_sqlTrue.CompareTo(SqlBoolean.Null) > 0));
            Assert.True((_sqlTrue.CompareTo(_sqlFalse) > 0));
            Assert.True((_sqlFalse.CompareTo(_sqlTrue) < 0));
            Assert.Equal(0, _sqlFalse.CompareTo(_sqlFalse));
        }

        // Equals
        [Fact]
        public void EqualsTest()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            Assert.True(_sqlTrue.Equals(sqlTrue2));
            Assert.True(_sqlFalse.Equals(sqlFalse2));

            Assert.False(_sqlTrue.Equals(_sqlFalse));
            Assert.False(_sqlFalse.Equals(_sqlTrue));

            Assert.False(_sqlTrue.Equals(SqlBoolean.Null));
            Assert.False(_sqlFalse.Equals(SqlBoolean.Null));

            Assert.False(_sqlTrue.Equals(null));
            Assert.True(SqlBoolean.Null.Equals(SqlBoolean.Null));
            Assert.False(SqlBoolean.Null.Equals(_sqlTrue));
            Assert.False(SqlBoolean.Null.Equals(_sqlFalse));
        }

        [Fact]
        public void GetHashCodeTest()
        {
            Assert.Equal(1, _sqlTrue.GetHashCode());

            Assert.Equal(0, _sqlFalse.GetHashCode());
        }

        // ToSqlByte
        [Fact]
        public void ToSqlByte()
        {
            SqlByte sqlTestByte;

            sqlTestByte = _sqlTrue.ToSqlByte();
            Assert.Equal((byte)1, sqlTestByte.Value);

            sqlTestByte = _sqlFalse.ToSqlByte();
            Assert.Equal((byte)0, sqlTestByte.Value);
        }

        // ToSqlDecimal
        [Fact]
        public void ToSqlDecimal()
        {
            SqlDecimal sqlTestDecimal;

            sqlTestDecimal = _sqlTrue.ToSqlDecimal();

            Assert.Equal(1, sqlTestDecimal.Value);

            sqlTestDecimal = _sqlFalse.ToSqlDecimal();
            Assert.Equal(0, sqlTestDecimal.Value);
        }

        // ToSqlDouble
        [Fact]
        public void ToSqlDouble()
        {
            SqlDouble sqlTestDouble;

            sqlTestDouble = _sqlTrue.ToSqlDouble();
            Assert.Equal(1, sqlTestDouble.Value);

            sqlTestDouble = _sqlFalse.ToSqlDouble();
            Assert.Equal(0, sqlTestDouble.Value);
        }

        // ToSqlInt16
        [Fact]
        public void ToSqlInt16()
        {
            SqlInt16 sqlTestInt16;

            sqlTestInt16 = _sqlTrue.ToSqlInt16();
            Assert.Equal((short)1, sqlTestInt16.Value);

            sqlTestInt16 = _sqlFalse.ToSqlInt16();
            Assert.Equal((short)0, sqlTestInt16.Value);
        }

        // ToSqlInt32
        [Fact]
        public void ToSqlInt32()
        {
            SqlInt32 sqlTestInt32;

            sqlTestInt32 = _sqlTrue.ToSqlInt32();
            Assert.Equal(1, sqlTestInt32.Value);

            sqlTestInt32 = _sqlFalse.ToSqlInt32();
            Assert.Equal(0, sqlTestInt32.Value);
        }

        // ToSqlInt64
        [Fact]
        public void ToSqlInt64()
        {
            SqlInt64 sqlTestInt64;

            sqlTestInt64 = _sqlTrue.ToSqlInt64();
            Assert.Equal(1, sqlTestInt64.Value);

            sqlTestInt64 = _sqlFalse.ToSqlInt64();
            Assert.Equal(0, sqlTestInt64.Value);
        }

        // ToSqlMoney
        [Fact]
        public void ToSqlMoney()
        {
            SqlMoney sqlTestMoney;

            sqlTestMoney = _sqlTrue.ToSqlMoney();
            Assert.Equal(1M, sqlTestMoney.Value);

            sqlTestMoney = _sqlFalse.ToSqlMoney();
            Assert.Equal(0, sqlTestMoney.Value);
        }

        // ToSqlSingle
        [Fact]
        public void ToSqlsingle()
        {
            SqlSingle sqlTestSingle;

            sqlTestSingle = _sqlTrue.ToSqlSingle();
            Assert.Equal(1, sqlTestSingle.Value);

            sqlTestSingle = _sqlFalse.ToSqlSingle();
            Assert.Equal(0, sqlTestSingle.Value);
        }

        // ToSqlString
        [Fact]
        public void ToSqlString()
        {
            SqlString sqlTestString;

            sqlTestString = _sqlTrue.ToSqlString();
            Assert.Equal("True", sqlTestString.Value);

            sqlTestString = _sqlFalse.ToSqlString();
            Assert.Equal("False", sqlTestString.Value);
        }

        // ToString
        [Fact]
        public void ToStringTest()
        {
            SqlString TestString;

            TestString = _sqlTrue.ToString();
            Assert.Equal("True", TestString.Value);

            TestString = _sqlFalse.ToSqlString();
            Assert.Equal("False", TestString.Value);
        }

        // END OF PUBLIC METHODS
        ////

        ////
        // OPERATORS

        // BitwixeAnd operator
        [Fact]
        public void BitwiseAndOperator()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            SqlBoolean sqlResult;

            sqlResult = _sqlTrue & _sqlFalse;
            Assert.False(sqlResult.Value);
            sqlResult = _sqlFalse & _sqlTrue;
            Assert.False(sqlResult.Value);

            sqlResult = _sqlTrue & sqlTrue2;
            Assert.True(sqlResult.Value);

            sqlResult = _sqlFalse & sqlFalse2;
            Assert.False(sqlResult.Value);
        }

        // BitwixeOr operator
        [Fact]
        public void BitwiseOrOperator()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            SqlBoolean sqlResult;

            sqlResult = _sqlTrue | _sqlFalse;
            Assert.True(sqlResult.Value);
            sqlResult = _sqlFalse | _sqlTrue;

            Assert.True(sqlResult.Value);

            sqlResult = _sqlTrue | sqlTrue2;
            Assert.True(sqlResult.Value);

            sqlResult = _sqlFalse | sqlFalse2;
            Assert.False(sqlResult.Value);
        }

        // Equality operator
        [Fact]
        public void EqualityOperator()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            SqlBoolean sqlResult;

            sqlResult = _sqlTrue == _sqlFalse;
            Assert.False(sqlResult.Value);
            sqlResult = _sqlFalse == _sqlTrue;
            Assert.False(sqlResult.Value);

            sqlResult = _sqlTrue == sqlTrue2;
            Assert.True(sqlResult.Value);

            sqlResult = _sqlFalse == sqlFalse2;
            Assert.True(sqlResult.Value);

            sqlResult = _sqlFalse == SqlBoolean.Null;
            Assert.True(sqlResult.IsNull);
            //sqlResult = SqlBoolean.Null == SqlBoolean.Null;
            //Assert.True(sqlResult.IsNull);
        }

        // ExlusiveOr operator
        [Fact]
        public void ExlusiveOrOperator()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            SqlBoolean sqlResult;

            sqlResult = _sqlTrue ^ _sqlFalse;
            Assert.True(sqlResult.Value);
            sqlResult = _sqlFalse | _sqlTrue;
            Assert.True(sqlResult.Value);

            sqlResult = _sqlTrue ^ sqlTrue2;
            Assert.False(sqlResult.Value);

            sqlResult = _sqlFalse ^ sqlFalse2;
            Assert.False(sqlResult.Value);
        }

        // false operator
        [Fact]
        public void FalseOperator()
        {
            Assert.Equal(SqlBoolean.False, !_sqlTrue);
            Assert.Equal(SqlBoolean.True, !_sqlFalse);
        }

        // Inequality operator
        [Fact]
        public void InequalityOperator()
        {
            SqlBoolean sqlTrue2 = new SqlBoolean(true);
            SqlBoolean sqlFalse2 = new SqlBoolean(false);

            Assert.Equal(SqlBoolean.False, _sqlTrue != true);
            Assert.Equal(SqlBoolean.False, _sqlTrue != sqlTrue2);
            Assert.Equal(SqlBoolean.False, _sqlFalse != false);
            Assert.Equal(SqlBoolean.False, _sqlFalse != sqlFalse2);
            Assert.Equal(SqlBoolean.True, _sqlTrue != _sqlFalse);
            Assert.Equal(SqlBoolean.True, _sqlFalse != _sqlTrue);
            Assert.Equal(SqlBoolean.Null, SqlBoolean.Null != _sqlTrue);
            Assert.Equal(SqlBoolean.Null, _sqlFalse != SqlBoolean.Null);
        }

        // Logical Not operator
        [Fact]
        public void LogicalNotOperator()
        {
            Assert.Equal(SqlBoolean.False, !_sqlTrue);
            Assert.Equal(SqlBoolean.True, !_sqlFalse);
        }

        // OnesComplement operator
        [Fact]
        public void OnesComplementOperator()
        {
            SqlBoolean sqlResult;

            sqlResult = ~_sqlTrue;
            Assert.False(sqlResult.Value);
            sqlResult = ~_sqlFalse;
            Assert.True(sqlResult.Value);
        }


        // true operator
        [Fact]
        public void TrueOperator()
        {
            Assert.Equal(SqlBoolean.True, _sqlTrue);
            Assert.Equal(SqlBoolean.False, _sqlFalse);
        }

        // SqlBoolean to Boolean
        [Fact]
        public void SqlBooleanToBoolean()
        {
            bool TestBoolean = (bool)_sqlTrue;
            Assert.True(TestBoolean);
            TestBoolean = (bool)_sqlFalse;
            Assert.False(TestBoolean);
        }

        // SqlByte to SqlBoolean
        [Fact]
        public void SqlByteToSqlBoolean()
        {
            SqlByte sqlTestByte;
            SqlBoolean sqlTestBoolean;
            sqlTestByte = new SqlByte(1);
            sqlTestBoolean = (SqlBoolean)sqlTestByte;
            Assert.True(sqlTestBoolean.Value);

            sqlTestByte = new SqlByte(2);
            sqlTestBoolean = (SqlBoolean)sqlTestByte;
            Assert.True(sqlTestBoolean.Value);

            sqlTestByte = new SqlByte(0);
            sqlTestBoolean = (SqlBoolean)sqlTestByte;
            Assert.False(sqlTestBoolean.Value);
        }

        // SqlDecimal to SqlBoolean
        [Fact]
        public void SqlDecimalToSqlBoolean()
        {
            SqlDecimal sqlTest;
            SqlBoolean sqlTestBoolean;

            sqlTest = new SqlDecimal(1);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlDecimal(19);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlDecimal(0);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.False(sqlTestBoolean.Value);
        }

        // SqlDouble to SqlBoolean
        [Fact]
        public void SqlDoubleToSqlBoolean()
        {
            SqlDouble sqlTest;
            SqlBoolean sqlTestBoolean;

            sqlTest = new SqlDouble(1);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlDouble(-19.8);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlDouble(0);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.False(sqlTestBoolean.Value);
        }

        // SqlIn16 to SqlBoolean
        [Fact]
        public void SqlInt16ToSqlBoolean()
        {
            SqlInt16 sqlTest;
            SqlBoolean sqlTestBoolean;

            sqlTest = new SqlInt16(1);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlInt16(-143);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlInt16(0);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.False(sqlTestBoolean.Value);
        }

        // SqlInt32 to SqlBoolean
        [Fact]
        public void SqlInt32ToSqlBoolean()
        {
            SqlInt32 sqlTest;
            SqlBoolean sqlTestBoolean;

            sqlTest = new SqlInt32(1);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlInt32(1430);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlInt32(0);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.False(sqlTestBoolean.Value);
        }

        // SqlInt64 to SqlBoolean
        [Fact]
        public void SqlInt64ToSqlBoolean()
        {
            SqlInt64 sqlTest;
            SqlBoolean sqlTestBoolean;

            sqlTest = new SqlInt64(1);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlInt64(-14305);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlInt64(0);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.False(sqlTestBoolean.Value);
        }

        // SqlMoney to SqlBoolean
        [Fact]
        public void SqlMoneyToSqlBoolean()
        {
            SqlMoney sqlTest;
            SqlBoolean sqlTestBoolean;

            sqlTest = new SqlMoney(1);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlMoney(1305);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlMoney(0);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.False(sqlTestBoolean.Value);
        }

        // SqlSingle to SqlBoolean
        [Fact]
        public void SqlSingleToSqlBoolean()
        {
            SqlSingle sqlTest;
            SqlBoolean sqlTestBoolean;

            sqlTest = new SqlSingle(1);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlSingle(1305);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlSingle(-305.3);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlSingle(0);
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.False(sqlTestBoolean.Value);
        }

        // SqlString to SqlBoolean
        [Fact]
        public void SqlStringToSqlBoolean()
        {
            SqlString sqlTest;
            SqlBoolean sqlTestBoolean;

            sqlTest = new SqlString("true");
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlString("TRUE");
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlString("True");
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = new SqlString("false");
            sqlTestBoolean = (SqlBoolean)sqlTest;
            Assert.False(sqlTestBoolean.Value);
        }

        // Boolean to SqlBoolean
        [Fact]
        public void BooleanToSqlBoolean()
        {
            SqlBoolean sqlTestBoolean;
            bool btrue = true;
            bool bfalse = false;

            bool sqlTest = true;
            sqlTestBoolean = sqlTest;
            Assert.True(sqlTestBoolean.Value);
            sqlTestBoolean = btrue;
            Assert.True(sqlTestBoolean.Value);

            sqlTest = false;
            sqlTestBoolean = sqlTest;
            Assert.False(sqlTestBoolean.Value);
            sqlTestBoolean = bfalse;
            Assert.False(sqlTestBoolean.Value);
        }

        // END OF OPERATORS
        ////

        ////
        // PROPERTIES

        // ByteValue property
        [Fact]
        public void ByteValueProperty()
        {
            Assert.Equal((byte)1, _sqlTrue.ByteValue);
            Assert.Equal((byte)0, _sqlFalse.ByteValue);
        }

        // IsFalse property
        [Fact]
        public void IsFalseProperty()
        {
            Assert.False(_sqlTrue.IsFalse);
            Assert.True(_sqlFalse.IsFalse);
        }

        // IsNull property
        [Fact]
        public void IsNullProperty()
        {
            Assert.False(_sqlTrue.IsNull);
            Assert.False(_sqlFalse.IsNull);
            Assert.True(SqlBoolean.Null.IsNull);
        }

        // IsTrue property
        [Fact]
        public void IsTrueProperty()
        {
            Assert.True(_sqlTrue.IsTrue);
            Assert.False(_sqlFalse.IsTrue);
        }

        // Value property
        [Fact]
        public void ValueProperty()
        {
            Assert.True(_sqlTrue.Value);
            Assert.False(_sqlFalse.Value);
        }

        // END OF PROPERTIES
        ////

        ////
        // FIELDS

        [Fact]
        public void FalseField()
        {
            Assert.False(SqlBoolean.False.Value);
        }

        [Fact]
        public void NullField()
        {
            Assert.True(SqlBoolean.Null.IsNull);
        }

        [Fact]
        public void OneField()
        {
            Assert.Equal((byte)1, SqlBoolean.One.ByteValue);
        }

        [Fact]
        public void TrueField()
        {
            Assert.True(SqlBoolean.True.Value);
        }

        [Fact]
        public void ZeroField()
        {
            Assert.Equal((byte)0, SqlBoolean.Zero.ByteValue);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlBoolean.GetXsdType(null);
            Assert.Equal("boolean", qualifiedName.Name);
        }

        [Fact]
        public void GreaterThanTest()
        {
            SqlBoolean x = new SqlBoolean(-1);
            SqlBoolean y = new SqlBoolean(true);
            SqlBoolean z = new SqlBoolean();
            SqlBoolean z1 = new SqlBoolean(0);

            Assert.False((x > y).Value);
            Assert.Equal(x > z, SqlBoolean.Null);
            Assert.True((x > z1).Value);
            Assert.Equal(y > z, SqlBoolean.Null);
            Assert.False((y > x).Value);
            Assert.True((y > z1).Value);
            Assert.Equal(z > z1, SqlBoolean.Null);
            Assert.Equal(z > x, SqlBoolean.Null);
            Assert.Equal(z > y, SqlBoolean.Null);
            Assert.Equal(z1 > z, SqlBoolean.Null);
            Assert.False((z1 > x).Value);
            Assert.False((z1 > y).Value);
        }

        [Fact]
        public void GreaterThanOrEqualTest()
        {
            SqlBoolean x = new SqlBoolean(-1);
            SqlBoolean y = new SqlBoolean(true);
            SqlBoolean z = new SqlBoolean();
            SqlBoolean z1 = new SqlBoolean(0);

            Assert.True((x >= y).Value);
            Assert.Equal(x >= z, SqlBoolean.Null);
            Assert.True((x >= z1).Value);
            Assert.Equal(y >= z, SqlBoolean.Null);
            Assert.True((y >= x).Value);
            Assert.True((y >= z1).Value);
            Assert.Equal(z >= z1, SqlBoolean.Null);
            Assert.Equal(z >= x, SqlBoolean.Null);
            Assert.Equal(z >= y, SqlBoolean.Null);
            Assert.Equal(z1 >= z, SqlBoolean.Null);
            Assert.False((z1 >= x).Value);
            Assert.False((z1 >= y).Value);
        }

        [Fact]
        public void LessThanTest()
        {
            SqlBoolean x = new SqlBoolean(-1);
            SqlBoolean y = new SqlBoolean(true);
            SqlBoolean z = new SqlBoolean();
            SqlBoolean z1 = new SqlBoolean(0);

            Assert.False((x < y).Value);
            Assert.Equal(x < z, SqlBoolean.Null);
            Assert.False((x < z1).Value);
            Assert.Equal(y < z, SqlBoolean.Null);
            Assert.False((y < x).Value);
            Assert.False((y < z1).Value);
            Assert.Equal(z < z1, SqlBoolean.Null);
            Assert.Equal(z < x, SqlBoolean.Null);
            Assert.Equal(z < y, SqlBoolean.Null);
            Assert.Equal(z1 < z, SqlBoolean.Null);
            Assert.True((z1 < x).Value);
            Assert.True((z1 < y).Value);
        }

        [Fact]
        public void LessThanOrEqualTest()
        {
            SqlBoolean x = new SqlBoolean(-1);
            SqlBoolean y = new SqlBoolean(true);
            SqlBoolean z = new SqlBoolean();
            SqlBoolean z1 = new SqlBoolean(0);

            Assert.True((x <= y).Value);
            Assert.Equal(x <= z, SqlBoolean.Null);
            Assert.False((x <= z1).Value);
            Assert.Equal(y <= z, SqlBoolean.Null);
            Assert.True((y <= x).Value);
            Assert.False((y <= z1).Value);
            Assert.Equal(z <= z1, SqlBoolean.Null);
            Assert.Equal(z <= x, SqlBoolean.Null);
            Assert.Equal(z <= y, SqlBoolean.Null);
            Assert.Equal(z1 <= z, SqlBoolean.Null);
            Assert.True((z1 <= x).Value);
            Assert.True((z1 <= y).Value);
        }
    }
}
