// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PreIncrementAssign()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    foreach (var e in GetAssignments(o, (op) => Expression.PreIncrementAssign(op)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PreIncrementAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PreIncrementAssign_Nullable()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    foreach (var e in GetAssignments(o, (op) => Expression.PreIncrementAssign(op)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PreIncrementAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PostIncrementAssign()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    foreach (var e in GetAssignments(o, (op) => Expression.PostIncrementAssign(op)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PostIncrementAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PostIncrementAssign_Nullable()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    foreach (var e in GetAssignments(o, (op) => Expression.PostIncrementAssign(op)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PostIncrementAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PreDecrementAssign()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    foreach (var e in GetAssignments(o, (op) => Expression.PreDecrementAssign(op)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PreDecrementAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PreDecrementAssign_Nullable()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    foreach (var e in GetAssignments(o, (op) => Expression.PreDecrementAssign(op)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PreDecrementAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PostDecrementAssign()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_exprs[t])
                {
                    foreach (var e in GetAssignments(o, (op) => Expression.PostDecrementAssign(op)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PostDecrementAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PostDecrementAssign_Nullable()
        {
            foreach (var t in s_unaryIncrDecrTypes)
            {
                foreach (var o in s_nullableExprs[t])
                {
                    foreach (var e in GetAssignments(o, (op) => Expression.PostDecrementAssign(op)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PostDecrementAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<Expression> GetAssignments(Expression initialValue, Func<Expression, Expression> createAssignment)
        {
            // Parameter
            {
                var p = Expression.Parameter(initialValue.Type);
                var a = Expression.Assign(p, initialValue);

                yield return Expression.Block(new[] { p }, a, createAssignment(p));
            }

            // Member
            {
                var p = Expression.Parameter(typeof(Holder<>).MakeGenericType(initialValue.Type));
                var a = Expression.Assign(p, Expression.New(p.Type));
                var v = Expression.Property(p, "Value");
                var b = Expression.Assign(v, initialValue);

                yield return Expression.Block(new[] { p }, a, b, createAssignment(v));
            }

            // Array
            {
                var p = Expression.Parameter(initialValue.Type.MakeArrayType());
                var a = Expression.Assign(p, Expression.NewArrayBounds(initialValue.Type, Expression.Constant(1)));
                var e = Expression.ArrayAccess(p, Expression.Constant(0));
                var b = Expression.Assign(e, initialValue);

                yield return Expression.Block(new[] { p }, a, b, createAssignment(e));
            }

            // Vector
            {
                var p = Expression.Parameter(typeof(Vector<>).MakeGenericType(initialValue.Type));
                var a = Expression.Assign(p, Expression.New(p.Type));
                var e = Expression.MakeIndex(p, p.Type.GetTypeInfo().GetDeclaredProperty("Item"), new[] { Expression.Constant(0) });
                var b = Expression.Assign(e, initialValue);

                yield return Expression.Block(new[] { p }, a, b, createAssignment(e));
            }
        }
    }
}