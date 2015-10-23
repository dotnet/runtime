// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static readonly Type[] s_unaryArithTypesUnaryPlus = new[] { typeof(S1), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long), typeof(float), typeof(double) };
        private static readonly Type[] s_unaryArithTypesOnesComplement = new[] { typeof(S1), typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(ulong), typeof(long) };
        private static readonly Type[] s_unaryArithTypesNegate = new[] { typeof(S1), typeof(short), typeof(int), typeof(long), typeof(float), typeof(double) };
        private static readonly Type[] s_unaryLogicTypes = new[] { typeof(S2), typeof(bool) };
        private static readonly Type[] s_unaryIncrDecrTypes = new[] { typeof(S1), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double) };

        private static IEnumerable<Expression> UnaryPlus()
        {
            foreach (var t in s_unaryArithTypesUnaryPlus)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.UnaryPlus(o);
                }
            }
        }

        private static IEnumerable<Expression> UnaryPlus_Nullable()
        {
            foreach (var t in s_unaryArithTypesUnaryPlus)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.UnaryPlus(o);
                }
            }
        }

        private static IEnumerable<Expression> Negate()
        {
            foreach (var t in s_unaryArithTypesNegate)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.Negate(o);
                }
            }
        }

        private static IEnumerable<Expression> Negate_Nullable()
        {
            foreach (var t in s_unaryArithTypesNegate)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.Negate(o);
                }
            }
        }

        private static IEnumerable<Expression> NegateChecked()
        {
            foreach (var t in s_unaryArithTypesNegate)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.NegateChecked(o);
                }
            }
        }

        private static IEnumerable<Expression> NegateChecked_Nullable()
        {
            foreach (var t in s_unaryArithTypesNegate)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.NegateChecked(o);
                }
            }
        }

        private static IEnumerable<Expression> OnesComplement()
        {
            foreach (var t in s_unaryArithTypesOnesComplement)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.OnesComplement(o);
                }
            }
        }

        private static IEnumerable<Expression> OnesComplement_Nullable()
        {
            foreach (var t in s_unaryArithTypesOnesComplement)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.OnesComplement(o);
                }
            }
        }

        private static IEnumerable<Expression> IsTrue()
        {
            foreach (var t in s_unaryLogicTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.IsTrue(o);
                }
            }
        }

        private static IEnumerable<Expression> IsTrue_Nullable()
        {
            foreach (var t in s_unaryLogicTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.IsTrue(o);
                }
            }
        }

        private static IEnumerable<Expression> IsFalse()
        {
            foreach (var t in s_unaryLogicTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.IsFalse(o);
                }
            }
        }

        private static IEnumerable<Expression> IsFalse_Nullable()
        {
            foreach (var t in s_unaryLogicTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.IsFalse(o);
                }
            }
        }

        private static IEnumerable<Expression> Not()
        {
            foreach (var t in s_unaryLogicTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.Not(o);
                }
            }
        }

        private static IEnumerable<Expression> Not_Nullable()
        {
            foreach (var t in s_unaryLogicTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.Not(o);
                }
            }
        }

        private static IEnumerable<Expression> Increment()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.Increment(o);
                }
            }
        }

        private static IEnumerable<Expression> Increment_Nullable()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.Increment(o);
                }
            }
        }

        private static IEnumerable<Expression> Decrement()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    yield return Expression.Decrement(o);
                }
            }
        }

        private static IEnumerable<Expression> Decrement_Nullable()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    yield return Expression.Decrement(o);
                }
            }
        }

    }
}