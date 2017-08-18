// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
using System.Globalization;

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
            SqlBoolean SqlTrue2 = new SqlBoolean(1);
            SqlBoolean SqlFalse2 = new SqlBoolean(0);

            Assert.True(_sqlTrue.Value);
            Assert.True(SqlTrue2.Value);
            Assert.True(!_sqlFalse.Value);
            Assert.True(!SqlFalse2.Value);
        }

        ////
        // PUBLIC STATIC METHODS
        //

        // And
        [Fact]
        public void And()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            // One result value
            SqlBoolean sqlResult;

            // true && false
            sqlResult = SqlBoolean.And(_sqlTrue, _sqlFalse);
            Assert.True(!sqlResult.Value);
            sqlResult = SqlBoolean.And(_sqlFalse, _sqlTrue);
            Assert.True(!sqlResult.Value);

            // true && true
            sqlResult = SqlBoolean.And(_sqlTrue, SqlTrue2);
            Assert.True(sqlResult.Value);

            sqlResult = SqlBoolean.And(_sqlTrue, _sqlTrue);
            Assert.True(sqlResult.Value);

            // false && false
            sqlResult = SqlBoolean.And(_sqlFalse, SqlFalse2);
            Assert.True(!sqlResult.Value);
            sqlResult = SqlBoolean.And(_sqlFalse, _sqlFalse);
            Assert.True(!sqlResult.Value);
        }

        // NotEquals
        [Fact]
        public void NotEquals()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            SqlBoolean SqlResult;

            // true != false
            SqlResult = SqlBoolean.NotEquals(_sqlTrue, _sqlFalse);
            Assert.True(SqlResult.Value);
            SqlResult = SqlBoolean.NotEquals(_sqlFalse, _sqlTrue);
            Assert.True(SqlResult.Value);


            // true != true
            SqlResult = SqlBoolean.NotEquals(_sqlTrue, _sqlTrue);
            Assert.True(!SqlResult.Value);
            SqlResult = SqlBoolean.NotEquals(_sqlTrue, SqlTrue2);
            Assert.True(!SqlResult.Value);
            // false != false
            SqlResult = SqlBoolean.NotEquals(_sqlFalse, _sqlFalse);
            Assert.True(!SqlResult.Value);
            SqlResult = SqlBoolean.NotEquals(_sqlTrue, SqlTrue2);
            Assert.True(!SqlResult.Value);

            // If either instance of SqlBoolean is null, the Value of the SqlBoolean will be Null.
            SqlResult = SqlBoolean.NotEquals(SqlBoolean.Null, _sqlFalse);
            Assert.True(SqlResult.IsNull);
            SqlResult = SqlBoolean.NotEquals(_sqlTrue, SqlBoolean.Null);
            Assert.True(SqlResult.IsNull);
        }

        // OnesComplement
        [Fact]
        public void OnesComplement()
        {
            SqlBoolean SqlFalse2 = SqlBoolean.OnesComplement(_sqlTrue);
            Assert.True(!SqlFalse2.Value);

            SqlBoolean SqlTrue2 = SqlBoolean.OnesComplement(_sqlFalse);
            Assert.True(SqlTrue2.Value);
        }

        // Or
        [Fact]
        public void Or()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            SqlBoolean SqlResult;

            // true || false
            SqlResult = SqlBoolean.Or(_sqlTrue, _sqlFalse);
            Assert.True(SqlResult.Value);
            SqlResult = SqlBoolean.Or(_sqlFalse, _sqlTrue);
            Assert.True(SqlResult.Value);

            // true || true
            SqlResult = SqlBoolean.Or(_sqlTrue, _sqlTrue);
            Assert.True(SqlResult.Value);
            SqlResult = SqlBoolean.Or(_sqlTrue, SqlTrue2);
            Assert.True(SqlResult.Value);

            // false || false
            SqlResult = SqlBoolean.Or(_sqlFalse, _sqlFalse);
            Assert.True(!SqlResult.Value);
            SqlResult = SqlBoolean.Or(_sqlFalse, SqlFalse2);
            Assert.True(!SqlResult.Value);
        }


        //  Parse
        [Fact]
        public void Parse()
        {
            string error = "Parse method does not work correctly ";

            Assert.True(SqlBoolean.Parse("True").Value);
            Assert.True(SqlBoolean.Parse(" True").Value);
            Assert.True(SqlBoolean.Parse("True ").Value);
            Assert.True(SqlBoolean.Parse("tRuE").Value);
            Assert.True(!SqlBoolean.Parse("False").Value);
            Assert.True(!SqlBoolean.Parse(" False").Value);
            Assert.True(!SqlBoolean.Parse("False ").Value);
            Assert.True(!SqlBoolean.Parse("fAlSe").Value);
        }

        // Xor
        [Fact]
        public void Xor()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            SqlBoolean SqlResult;

            // true ^ false
            SqlResult = SqlBoolean.Xor(_sqlTrue, _sqlFalse);
            Assert.True(SqlResult.Value);
            SqlResult = SqlBoolean.Xor(_sqlFalse, _sqlTrue);
            Assert.True(SqlResult.Value);

            // true ^ true
            SqlResult = SqlBoolean.Xor(_sqlTrue, SqlTrue2);
            Assert.True(!SqlResult.Value);

            // false ^ false
            SqlResult = SqlBoolean.Xor(_sqlFalse, SqlFalse2);
            Assert.True(!SqlResult.Value);
        }

        // static Equals
        [Fact]
        public void StaticEquals()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);
            string error = "Static Equals method does not work correctly ";

            Assert.True(SqlBoolean.Equals(_sqlTrue, SqlTrue2).Value);
            Assert.True(SqlBoolean.Equals(_sqlFalse, SqlFalse2).Value);

            Assert.True(!SqlBoolean.Equals(_sqlTrue, _sqlFalse).Value);
            Assert.True(!SqlBoolean.Equals(_sqlFalse, _sqlTrue).Value);

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
            string error = "CompareTo method does not work correctly";

            Assert.True((_sqlTrue.CompareTo(SqlBoolean.Null) > 0));
            Assert.True((_sqlTrue.CompareTo(_sqlFalse) > 0));
            Assert.True((_sqlFalse.CompareTo(_sqlTrue) < 0));
            Assert.True((_sqlFalse.CompareTo(_sqlFalse) == 0));
        }

        // Equals
        [Fact]
        public void Equals()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            string error = "Equals method does not work correctly ";
            Assert.True(_sqlTrue.Equals(SqlTrue2));
            Assert.True(_sqlFalse.Equals(SqlFalse2));

            Assert.True(!_sqlTrue.Equals(_sqlFalse));
            Assert.True(!_sqlFalse.Equals(_sqlTrue));

            Assert.False(_sqlTrue.Equals(SqlBoolean.Null));
            Assert.False(_sqlFalse.Equals(SqlBoolean.Null));

            Assert.True(!_sqlTrue.Equals(null));
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

        // GetType
        [Fact]
        public void GetTypeTest()
        {
            Assert.Equal("System.Data.SqlTypes.SqlBoolean", _sqlTrue.GetType().ToString());
        }

        // ToSqlByte
        [Fact]
        public void ToSqlByte()
        {
            SqlByte SqlTestByte;

            string error = "ToSqlByte method does not work correctly ";

            SqlTestByte = _sqlTrue.ToSqlByte();
            Assert.Equal((byte)1, SqlTestByte.Value);

            SqlTestByte = _sqlFalse.ToSqlByte();
            Assert.Equal((byte)0, SqlTestByte.Value);
        }

        // ToSqlDecimal
        [Fact]
        public void ToSqlDecimal()
        {
            SqlDecimal SqlTestDecimal;

            string error = "ToSqlDecimal method does not work correctly ";
            SqlTestDecimal = _sqlTrue.ToSqlDecimal();

            Assert.Equal(1, SqlTestDecimal.Value);

            SqlTestDecimal = _sqlFalse.ToSqlDecimal();
            Assert.Equal(0, SqlTestDecimal.Value);
        }

        // ToSqlDouble
        [Fact]
        public void ToSqlDouble()
        {
            SqlDouble SqlTestDouble;

            string error = "ToSqlDouble method does not work correctly ";
            SqlTestDouble = _sqlTrue.ToSqlDouble();
            Assert.Equal(1, SqlTestDouble.Value);

            SqlTestDouble = _sqlFalse.ToSqlDouble();
            Assert.Equal(0, SqlTestDouble.Value);
        }

        // ToSqlInt16
        [Fact]
        public void ToSqlInt16()
        {
            SqlInt16 SqlTestInt16;

            string error = "ToSqlInt16 method does not work correctly ";
            SqlTestInt16 = _sqlTrue.ToSqlInt16();
            Assert.Equal((short)1, SqlTestInt16.Value);

            SqlTestInt16 = _sqlFalse.ToSqlInt16();
            Assert.Equal((short)0, SqlTestInt16.Value);
        }

        // ToSqlInt32
        [Fact]
        public void ToSqlInt32()
        {
            SqlInt32 SqlTestInt32;

            string error = "ToSqlInt32 method does not work correctly ";
            SqlTestInt32 = _sqlTrue.ToSqlInt32();
            Assert.Equal(1, SqlTestInt32.Value);

            SqlTestInt32 = _sqlFalse.ToSqlInt32();
            Assert.Equal(0, SqlTestInt32.Value);
        }

        // ToSqlInt64
        [Fact]
        public void ToSqlInt64()
        {
            SqlInt64 SqlTestInt64;

            string error = "ToSqlInt64 method does not work correctly ";

            SqlTestInt64 = _sqlTrue.ToSqlInt64();
            Assert.Equal(1, SqlTestInt64.Value);

            SqlTestInt64 = _sqlFalse.ToSqlInt64();
            Assert.Equal(0, SqlTestInt64.Value);
        }

        // ToSqlMoney
        [Fact]
        public void ToSqlMoney()
        {
            SqlMoney SqlTestMoney;

            string error = "ToSqlMoney method does not work correctly ";
            SqlTestMoney = _sqlTrue.ToSqlMoney();
            Assert.Equal(1.0000M, SqlTestMoney.Value);

            SqlTestMoney = _sqlFalse.ToSqlMoney();
            Assert.Equal(0, SqlTestMoney.Value);
        }

        // ToSqlSingle
        [Fact]
        public void ToSqlsingle()
        {
            SqlSingle SqlTestSingle;

            string error = "ToSqlSingle method does not work correctly ";
            SqlTestSingle = _sqlTrue.ToSqlSingle();
            Assert.Equal(1, SqlTestSingle.Value);

            SqlTestSingle = _sqlFalse.ToSqlSingle();
            Assert.Equal(0, SqlTestSingle.Value);
        }

        // ToSqlString
        [Fact]
        public void ToSqlString()
        {
            SqlString SqlTestString;

            string error = "ToSqlString method does not work correctly ";
            SqlTestString = _sqlTrue.ToSqlString();
            Assert.Equal("True", SqlTestString.Value);

            SqlTestString = _sqlFalse.ToSqlString();
            Assert.Equal("False", SqlTestString.Value);
        }

        // ToString
        [Fact]
        public void ToStringTest()
        {
            SqlString TestString;

            string error = "ToString method does not work correctly ";

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
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            SqlBoolean SqlResult;
            string error = "BitwiseAnd operator does not work correctly ";

            SqlResult = _sqlTrue & _sqlFalse;
            Assert.True(!SqlResult.Value);
            SqlResult = _sqlFalse & _sqlTrue;
            Assert.True(!SqlResult.Value);

            SqlResult = _sqlTrue & SqlTrue2;
            Assert.True(SqlResult.Value);

            SqlResult = _sqlFalse & SqlFalse2;
            Assert.True(!SqlResult.Value);
        }

        // BitwixeOr operator
        [Fact]
        public void BitwiseOrOperator()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            SqlBoolean SqlResult;
            string error = "BitwiseOr operator does not work correctly ";

            SqlResult = _sqlTrue | _sqlFalse;
            Assert.True(SqlResult.Value);
            SqlResult = _sqlFalse | _sqlTrue;

            Assert.True(SqlResult.Value);

            SqlResult = _sqlTrue | SqlTrue2;
            Assert.True(SqlResult.Value);

            SqlResult = _sqlFalse | SqlFalse2;
            Assert.True(!SqlResult.Value);
        }

        // Equality operator
        [Fact]
        public void EqualityOperator()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            SqlBoolean SqlResult;
            string error = "Equality operator does not work correctly ";

            SqlResult = _sqlTrue == _sqlFalse;
            Assert.True(!SqlResult.Value);
            SqlResult = _sqlFalse == _sqlTrue;
            Assert.True(!SqlResult.Value);

            SqlResult = _sqlTrue == SqlTrue2;
            Assert.True(SqlResult.Value);

            SqlResult = _sqlFalse == SqlFalse2;
            Assert.True(SqlResult.Value);

            SqlResult = _sqlFalse == SqlBoolean.Null;
            Assert.True(SqlResult.IsNull);
            //SqlResult = SqlBoolean.Null == SqlBoolean.Null;
            Assert.True(SqlResult.IsNull);
        }

        // ExlusiveOr operator
        [Fact]
        public void ExlusiveOrOperator()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            SqlBoolean SqlResult;
            string error = "ExclusiveOr operator does not work correctly ";

            SqlResult = _sqlTrue ^ _sqlFalse;
            Assert.True(SqlResult.Value);
            SqlResult = _sqlFalse | _sqlTrue;
            Assert.True(SqlResult.Value);

            SqlResult = _sqlTrue ^ SqlTrue2;
            Assert.True(!SqlResult.Value);

            SqlResult = _sqlFalse ^ SqlFalse2;
            Assert.True(!SqlResult.Value);
        }

        // false operator
        [Fact]
        public void FalseOperator()
        {
            string error = "false operator does not work correctly ";

            Assert.Equal(SqlBoolean.False, (!_sqlTrue));
            Assert.Equal(SqlBoolean.True, (!_sqlFalse));
        }

        // Inequality operator
        [Fact]
        public void InequalityOperator()
        {
            SqlBoolean SqlTrue2 = new SqlBoolean(true);
            SqlBoolean SqlFalse2 = new SqlBoolean(false);

            string error = "Inequality operator does not work correctly";

            Assert.Equal(SqlBoolean.False, _sqlTrue != true);
            Assert.Equal(SqlBoolean.False, _sqlTrue != SqlTrue2);
            Assert.Equal(SqlBoolean.False, _sqlFalse != false);
            Assert.Equal(SqlBoolean.False, _sqlFalse != SqlFalse2);
            Assert.Equal(SqlBoolean.True, _sqlTrue != _sqlFalse);
            Assert.Equal(SqlBoolean.True, _sqlFalse != _sqlTrue);
            Assert.Equal(SqlBoolean.Null, SqlBoolean.Null != _sqlTrue);
            Assert.Equal(SqlBoolean.Null, _sqlFalse != SqlBoolean.Null);
        }

        // Logical Not operator
        [Fact]
        public void LogicalNotOperator()
        {
            string error = "Logical Not operator does not work correctly";

            Assert.Equal(SqlBoolean.False, !_sqlTrue);
            Assert.Equal(SqlBoolean.True, !_sqlFalse);
        }

        // OnesComplement operator
        [Fact]
        public void OnesComplementOperator()
        {
            string error = "Ones complement operator does not work correctly";

            SqlBoolean SqlResult;

            SqlResult = ~_sqlTrue;
            Assert.True(!SqlResult.Value);
            SqlResult = ~_sqlFalse;
            Assert.True(SqlResult.Value);
        }


        // true operator
        [Fact]
        public void TrueOperator()
        {
            string error = "true operator does not work correctly ";

            Assert.Equal(SqlBoolean.True, (_sqlTrue));
            Assert.Equal(SqlBoolean.False, (_sqlFalse));
        }

        // SqlBoolean to Boolean
        [Fact]
        public void SqlBooleanToBoolean()
        {
            string error = "SqlBooleanToBoolean operator does not work correctly ";

            bool TestBoolean = (bool)_sqlTrue;
            Assert.True(TestBoolean);
            TestBoolean = (bool)_sqlFalse;
            Assert.True(!TestBoolean);
        }

        // SqlByte to SqlBoolean
        [Fact]
        public void SqlByteToSqlBoolean()
        {
            SqlByte SqlTestByte;
            SqlBoolean SqlTestBoolean;
            string error = "SqlByteToSqlBoolean operator does not work correctly ";

            SqlTestByte = new SqlByte(1);
            SqlTestBoolean = (SqlBoolean)SqlTestByte;
            Assert.True(SqlTestBoolean.Value);

            SqlTestByte = new SqlByte(2);
            SqlTestBoolean = (SqlBoolean)SqlTestByte;
            Assert.True(SqlTestBoolean.Value);

            SqlTestByte = new SqlByte(0);
            SqlTestBoolean = (SqlBoolean)SqlTestByte;
            Assert.True(!SqlTestBoolean.Value);
        }

        // SqlDecimal to SqlBoolean
        [Fact]
        public void SqlDecimalToSqlBoolean()
        {
            SqlDecimal SqlTest;
            SqlBoolean SqlTestBoolean;
            string error = "SqlDecimalToSqlBoolean operator does not work correctly ";

            SqlTest = new SqlDecimal(1);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlDecimal(19);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlDecimal(0);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(!SqlTestBoolean.Value);
        }

        // SqlDouble to SqlBoolean
        [Fact]
        public void SqlDoubleToSqlBoolean()
        {
            SqlDouble SqlTest;
            SqlBoolean SqlTestBoolean;
            string error = "SqlDoubleToSqlBoolean operator does not work correctly ";

            SqlTest = new SqlDouble(1);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlDouble(-19.8);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlDouble(0);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(!SqlTestBoolean.Value);
        }

        // SqlIn16 to SqlBoolean
        [Fact]
        public void SqlInt16ToSqlBoolean()
        {
            SqlInt16 SqlTest;
            SqlBoolean SqlTestBoolean;
            string error = "SqlInt16ToSqlBoolean operator does not work correctly ";

            SqlTest = new SqlInt16(1);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlInt16(-143);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlInt16(0);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(!SqlTestBoolean.Value);
        }

        // SqlInt32 to SqlBoolean
        [Fact]
        public void SqlInt32ToSqlBoolean()
        {
            SqlInt32 SqlTest;
            SqlBoolean SqlTestBoolean;
            string error = "SqlInt32ToSqlBoolean operator does not work correctly ";

            SqlTest = new SqlInt32(1);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlInt32(1430);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlInt32(0);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(!SqlTestBoolean.Value);
        }

        // SqlInt64 to SqlBoolean
        [Fact]
        public void SqlInt64ToSqlBoolean()
        {
            SqlInt64 SqlTest;
            SqlBoolean SqlTestBoolean;
            string error = "SqlInt64ToSqlBoolean operator does not work correctly ";

            SqlTest = new SqlInt64(1);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlInt64(-14305);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlInt64(0);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(!SqlTestBoolean.Value);
        }

        // SqlMoney to SqlBoolean
        [Fact]
        public void SqlMoneyToSqlBoolean()
        {
            SqlMoney SqlTest;
            SqlBoolean SqlTestBoolean;
            string error = "SqlMoneyToSqlBoolean operator does not work correctly ";

            SqlTest = new SqlMoney(1);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlMoney(1305);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlMoney(0);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(!SqlTestBoolean.Value);
        }

        // SqlSingle to SqlBoolean
        [Fact]
        public void SqlSingleToSqlBoolean()
        {
            SqlSingle SqlTest;
            SqlBoolean SqlTestBoolean;
            string error = "SqlSingleToSqlBoolean operator does not work correctly ";

            SqlTest = new SqlSingle(1);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlSingle(1305);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlSingle(-305.3);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlSingle(0);
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(!SqlTestBoolean.Value);
        }

        // SqlString to SqlBoolean
        [Fact]
        public void SqlStringToSqlBoolean()
        {
            SqlString SqlTest;
            SqlBoolean SqlTestBoolean;
            string error = "SqlSingleToSqlBoolean operator does not work correctly ";

            SqlTest = new SqlString("true");
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlString("TRUE");
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlString("True");
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(SqlTestBoolean.Value);

            SqlTest = new SqlString("false");
            SqlTestBoolean = (SqlBoolean)SqlTest;
            Assert.True(!SqlTestBoolean.Value);
        }

        // Boolean to SqlBoolean
        [Fact]
        public void BooleanToSqlBoolean()
        {
            SqlBoolean SqlTestBoolean;
            bool btrue = true;
            bool bfalse = false;
            string error = "BooleanToSqlBoolean operator does not work correctly ";

            bool SqlTest = true;
            SqlTestBoolean = SqlTest;
            Assert.True(SqlTestBoolean.Value);
            SqlTestBoolean = btrue;
            Assert.True(SqlTestBoolean.Value);


            SqlTest = false;
            SqlTestBoolean = SqlTest;
            Assert.True(!SqlTestBoolean.Value);
            SqlTestBoolean = bfalse;
            Assert.True(!SqlTestBoolean.Value);
        }

        // END OF OPERATORS
        ////

        ////
        // PROPERTIES

        // ByteValue property
        [Fact]
        public void ByteValueProperty()
        {
            string error = "ByteValue property does not work correctly ";

            Assert.Equal((byte)1, _sqlTrue.ByteValue);
            Assert.Equal((byte)0, _sqlFalse.ByteValue);
        }

        // IsFalse property
        [Fact]
        public void IsFalseProperty()
        {
            string error = "IsFalse property does not work correctly ";

            Assert.True(!_sqlTrue.IsFalse);
            Assert.True(_sqlFalse.IsFalse);
        }

        // IsNull property
        [Fact]
        public void IsNullProperty()
        {
            string error = "IsNull property does not work correctly ";

            Assert.True(!_sqlTrue.IsNull);
            Assert.True(!_sqlFalse.IsNull);
            Assert.True(SqlBoolean.Null.IsNull);
        }

        // IsTrue property
        [Fact]
        public void IsTrueProperty()
        {
            string error = "IsTrue property does not work correctly ";

            Assert.True(_sqlTrue.IsTrue);
            Assert.True(!_sqlFalse.IsTrue);
        }

        // Value property
        [Fact]
        public void ValueProperty()
        {
            string error = "Value property does not work correctly ";

            Assert.True(_sqlTrue.Value);
            Assert.True(!_sqlFalse.Value);
        }

        // END OF PROPERTIEs
        ////

        ////
        // FIELDS

        [Fact]
        public void FalseField()
        {
            Assert.True(!SqlBoolean.False.Value);
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

