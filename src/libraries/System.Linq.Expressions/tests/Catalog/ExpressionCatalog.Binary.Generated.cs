// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static readonly Type[] s_binaryArithTypes = new[] { typeof(S1), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long), typeof(float), typeof(double) };
        private static readonly Type[] s_binaryShiftTypes = new[] { typeof(S1), typeof(byte), typeof(sbyte), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long) };
        private static readonly Type[] s_binaryLogicTypes = new[] { typeof(S1), typeof(bool), typeof(byte), typeof(sbyte), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long) };
        private static readonly Type[] s_binaryComprTypes = new[] { typeof(S1), typeof(byte), typeof(sbyte), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long), typeof(float), typeof(double) };
        private static readonly Type[] s_binaryEqualTypes = new[] { typeof(S1), typeof(string), typeof(byte), typeof(sbyte), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long), typeof(float), typeof(double) };
        private static readonly Type[] s_binaryShCirTypes = new[] { typeof(S2), typeof(bool) };
        private static readonly Type[] s_binaryPowerTypes = new[] { typeof(double) };

        private static IEnumerable<Expression> Add()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.Add(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Add_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.Add(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> AddChecked()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.AddChecked(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> AddChecked_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.AddChecked(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Subtract()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.Subtract(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Subtract_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.Subtract(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> SubtractChecked()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.SubtractChecked(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> SubtractChecked_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.SubtractChecked(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Multiply()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.Multiply(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Multiply_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.Multiply(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> MultiplyChecked()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.MultiplyChecked(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> MultiplyChecked_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.MultiplyChecked(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Divide()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.Divide(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Divide_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.Divide(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Modulo()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.Modulo(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Modulo_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.Modulo(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> LeftShift()
        {
            foreach (var t in s_binaryShiftTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[typeof(int)])
                    {
                        yield return Expression.LeftShift(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> LeftShift_Nullable()
        {
            foreach (var t in s_binaryShiftTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[typeof(int)])
                    {
                        yield return Expression.LeftShift(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> RightShift()
        {
            foreach (var t in s_binaryShiftTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[typeof(int)])
                    {
                        yield return Expression.RightShift(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> RightShift_Nullable()
        {
            foreach (var t in s_binaryShiftTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[typeof(int)])
                    {
                        yield return Expression.RightShift(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> And()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.And(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> And_Nullable()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.And(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Or()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.Or(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Or_Nullable()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.Or(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> ExclusiveOr()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.ExclusiveOr(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> ExclusiveOr_Nullable()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.ExclusiveOr(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> LessThan()
        {
            foreach (var t in s_binaryComprTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.LessThan(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> LessThan_Nullable()
        {
            foreach (var t in s_binaryComprTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.LessThan(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> LessThanOrEqual()
        {
            foreach (var t in s_binaryComprTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.LessThanOrEqual(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> LessThanOrEqual_Nullable()
        {
            foreach (var t in s_binaryComprTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.LessThanOrEqual(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> GreaterThan()
        {
            foreach (var t in s_binaryComprTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.GreaterThan(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> GreaterThan_Nullable()
        {
            foreach (var t in s_binaryComprTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.GreaterThan(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> GreaterThanOrEqual()
        {
            foreach (var t in s_binaryComprTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.GreaterThanOrEqual(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> GreaterThanOrEqual_Nullable()
        {
            foreach (var t in s_binaryComprTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.GreaterThanOrEqual(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Equal()
        {
            foreach (var t in s_binaryEqualTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.Equal(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Equal_Nullable()
        {
            foreach (var t in s_binaryEqualTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.Equal(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> NotEqual()
        {
            foreach (var t in s_binaryEqualTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.NotEqual(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> NotEqual_Nullable()
        {
            foreach (var t in s_binaryEqualTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.NotEqual(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> AndAlso()
        {
            foreach (var t in s_binaryShCirTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.AndAlso(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> AndAlso_Nullable()
        {
            foreach (var t in s_binaryShCirTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.AndAlso(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> OrElse()
        {
            foreach (var t in s_binaryShCirTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.OrElse(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> OrElse_Nullable()
        {
            foreach (var t in s_binaryShCirTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.OrElse(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Power()
        {
            foreach (var t in s_binaryPowerTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        yield return Expression.Power(l, r);
                    }
                }
            }
        }

        private static IEnumerable<Expression> Power_Nullable()
        {
            foreach (var t in s_binaryPowerTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        yield return Expression.Power(l, r);
                    }
                }
            }
        }

    }
}