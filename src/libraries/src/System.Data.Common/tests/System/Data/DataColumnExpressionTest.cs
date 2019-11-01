// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using Xunit;

namespace System.Data.Tests
{
    public class DataColumnExpressionTest
    {
        [Theory]
        [MemberData(nameof(Aggregations))]
        public void Aggregation(Type dataType, string expression, object expectedResult)
        {
            var parentTable = new DataTable
            {
                Columns =
                {
                    new DataColumn("ParentId", typeof(int)),
                    new DataColumn("ParentName", typeof(string)),
                },
            };
            var childTable = new DataTable
            {
                Columns =
                {
                    new DataColumn("ChildId", typeof(int)),
                    new DataColumn("ParentId", typeof(int)),
                    new DataColumn("Data", dataType),
                }
            };

            var dataSet = new DataSet()
            {
                Tables =
                {
                    parentTable,
                    childTable,
                },
                Relations =
                {
                    new DataRelation("relation", parentTable.Columns[0], childTable.Columns[1]),
                },
            };

            parentTable.Rows.Add(1, "parent1");
            for (var i = 1; i <= 10; i++)
            {
                childTable.Rows.Add(i, 1, ChangeType(i * 2, dataType));
            }
            childTable.Rows.Add(11, 1, DBNull.Value);

            parentTable.Columns.Add(new DataColumn("Aggregate", expectedResult.GetType(), expression));
            Assert.Equal(expectedResult, parentTable.Rows[0][2]);
        }

        [Theory]
        [MemberData(nameof(BinaryOperators))]
        public void BinaryOperator(Type operandType1, Type operandType2, Type resultType, string expression, object operand1, object operand2, object result)
        {
            var table = new DataTable();
            table.Columns.Add("Operand1", operandType1);
            table.Columns.Add("Operand2", operandType2);
            table.Columns.Add("Result", resultType, expression);

            table.Rows.Add(new object[] { operand1, operand2 });

            Assert.Equal(result, table.Rows[0][2]);
        }

        private static object ChangeType(int value, Type type)
        {
            if (type == typeof(SqlByte))
                return new SqlByte((byte)value);
            if (type == typeof(SqlInt16))
                return new SqlInt16((short) value);
            if (type == typeof(SqlInt32))
                return new SqlInt32(value);
            if (type == typeof(SqlInt64))
                return new SqlInt64(value);
            if (type == typeof(SqlSingle))
                return new SqlSingle(value);
            if (type == typeof(SqlDouble))
                return new SqlDouble(value);
            if (type == typeof(SqlDecimal))
                return new SqlDecimal(value);
            if (type == typeof(SqlMoney))
                return new SqlMoney(value);

            return Convert.ChangeType(value, type);
        }

        public static IEnumerable<object[]> Aggregations()
        {
            var aggregations = new (string Operator, int Result)[]
            {
                ("sum", 110),
                ("count", 10),
                ("avg", 11),
                ("min", 2),
                ("max", 20),
            };

            var types = new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) };
            var sqlTypes = new[] { typeof(SqlByte), typeof(SqlInt16), typeof(SqlInt32), typeof(SqlInt64), typeof(SqlSingle), typeof(SqlDouble), typeof(SqlDecimal), typeof(SqlMoney) };

            foreach (var type in types.Concat(sqlTypes))
            {
                foreach (var aggregation in aggregations)
                {
                    var resultType = type;

                    // BUG? sum(Column) is always promoted to SqlInt64
                    if (aggregation.Operator == "sum" && (type == typeof(SqlByte) || type == typeof(SqlInt16) || type == typeof(SqlInt32)))
                        resultType = typeof(SqlInt64);

                    if (aggregation.Operator == "count" && (type == typeof(SqlByte) || type == typeof(SqlInt16)))
                        resultType = typeof(SqlInt32);

                    // BUG? sum(SqlMoney) yields SqlDecimal, but SqlDecimal can't be converted to SqlMoney
                    if (aggregation.Operator == "sum" && type == typeof(SqlMoney))
                        resultType = typeof(SqlDecimal);

                    yield return new object[] { type, aggregation.Operator + "(Child.Data)", ChangeType(aggregation.Result, resultType) };
                }

                // BUG? Var() for SQL types can't convert to System.Double, but StDev can
                if (type.Namespace == "System.Data.SqlTypes")
                    yield return new object[] { type, "Var(Child.Data)", new SqlDouble(110.0 / 3) };
                else
                    yield return new object[] { type, "Var(Child.Data)", 110.0 / 3 };
                yield return new object[] { type, "StDev(Child.Data)", Math.Sqrt(110.0 / 3) };
            }
        }

        public static IEnumerable<object[]> BinaryOperators()
        {
            var arithmeticEquations = new (string Expression, int Result)[]
            {
                ("Operand1 + Operand2", 12),
                ("Operand1 - Operand2", 8),
                ("Operand1 * Operand2", 20),
                ("Operand1 / Operand2", 5),
            };
            var comparisonEquations = new (string Expression, bool Result)[]
            {
                ("Operand1 < Operand2", false),
                ("Operand1 <= Operand2", false),
                ("Operand1 > Operand2", true),
                ("Operand1 >= Operand2", true),
                ("Operand1 = Operand2", false),
                ("Operand1 <> Operand2", true),
            };

            var types = new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(float), typeof(double), typeof(decimal) };
            foreach (var type1 in types)
            {
                object operand1 = ChangeType(10, type1);

                foreach (var type2 in types)
                {
                    object operand2 = ChangeType(2, type2);
                    foreach (var type3 in types)
                    {
                        foreach (var equation in arithmeticEquations)
                        {
                            yield return new object[] { type1, type2, type3, equation.Expression, operand1, operand2, ChangeType(equation.Result, type3) };
                            yield return new object[] { type1, type2, type3, equation.Expression, operand1, DBNull.Value, DBNull.Value };
                        }
                    }

                    foreach (var equation in comparisonEquations)
                    {
                        yield return new object[] { type1, type2, typeof(bool), equation.Expression, operand1, operand2, equation.Result };
                        yield return new object[] { type1, type2, typeof(bool), equation.Expression, operand1, DBNull.Value, DBNull.Value };
                    }
                }
            }

            foreach (var equation in arithmeticEquations)
                yield return new object[] { typeof(ulong), typeof(ulong), typeof(ulong), equation.Expression, 10ul, 2ul, ChangeType(equation.Result, typeof(ulong)) };

            var sqlObjects = new object[] { new SqlByte(), new SqlInt16(), new SqlInt32(), new SqlInt64(), new SqlSingle(), new SqlDouble(), new SqlDecimal(), new SqlMoney() };
            foreach (var sqlNull in sqlObjects)
            {
                var type = sqlNull.GetType();
                object operand1 = ChangeType(10, type);
                object operand2 = ChangeType(2, type);
                var isIntegral = sqlNull is SqlByte || sqlNull is SqlInt16 || sqlNull is SqlInt32 || sqlNull is SqlInt64;

                foreach (var equation in arithmeticEquations)
                {
                    // BUG? Dividing two integral SQL types fails, even if the result is integral
                    if (!isIntegral || equation.Expression != "Operand1 / Operand2")
                        yield return new object[] { type, type, type, equation.Expression, operand1, operand2, ChangeType(equation.Result, type) };

                    yield return new object[] { type, type, type, equation.Expression, operand1, sqlNull, sqlNull };
                }

                foreach (var equation in comparisonEquations)
                {
                    yield return new object[] { type, type, typeof(SqlBoolean), equation.Expression, operand1, operand2,  new SqlBoolean(equation.Result) };

                    // BUG? Result type of comparison of two SQL types (when one operard is Null) is the type itself, not SqlBoolean.
                    yield return new object[] { type, type, type, equation.Expression, operand1, sqlNull, sqlNull };
                }
            }
        }
    }
}
