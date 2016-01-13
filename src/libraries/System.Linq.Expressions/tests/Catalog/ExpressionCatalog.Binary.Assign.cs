// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> AddAssign()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AddAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AddAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Add(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AddAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AddAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AddAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AddAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> AddAssign_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AddAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AddAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> AddAssignChecked()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AddAssignChecked(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AddAssignChecked, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Add(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AddAssignChecked(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AddAssignChecked, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AddAssignChecked(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AddAssignChecked, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> AddAssignChecked_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AddAssignChecked(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AddAssignChecked, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> SubtractAssign()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.SubtractAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.SubtractAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Subtract(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.SubtractAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.SubtractAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.SubtractAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.SubtractAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> SubtractAssign_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.SubtractAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.SubtractAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> SubtractAssignChecked()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.SubtractAssignChecked(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.SubtractAssignChecked, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Subtract(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.SubtractAssignChecked(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.SubtractAssignChecked, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.SubtractAssignChecked(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.SubtractAssignChecked, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> SubtractAssignChecked_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.SubtractAssignChecked(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.SubtractAssignChecked, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> MultiplyAssign()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.MultiplyAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.MultiplyAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Multiply(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.MultiplyAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.MultiplyAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.MultiplyAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.MultiplyAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> MultiplyAssign_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.MultiplyAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.MultiplyAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> MultiplyAssignChecked()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.MultiplyAssignChecked(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.MultiplyAssignChecked, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Multiply(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.MultiplyAssignChecked(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.MultiplyAssignChecked, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.MultiplyAssignChecked(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.MultiplyAssignChecked, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> MultiplyAssignChecked_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.MultiplyAssignChecked(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.MultiplyAssignChecked, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> DivideAssign()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.DivideAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.DivideAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Divide(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.DivideAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.DivideAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.DivideAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.DivideAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> DivideAssign_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.DivideAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.DivideAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> ModuloAssign()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.ModuloAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ModuloAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Modulo(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.ModuloAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ModuloAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.ModuloAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ModuloAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> ModuloAssign_Nullable()
        {
            foreach (var t in s_binaryArithTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.ModuloAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ModuloAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> LeftShiftAssign()
        {
            foreach (var t in s_binaryShiftTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[typeof(int)])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.LeftShiftAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.LeftShiftAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.LeftShift(Expression.Default(typeof(S1)), Expression.Default(typeof(int))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(int)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.LeftShiftAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.LeftShiftAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.LeftShiftAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.LeftShiftAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> LeftShiftAssign_Nullable()
        {
            foreach (var t in s_binaryShiftTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[typeof(int)])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.LeftShiftAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.LeftShiftAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> RightShiftAssign()
        {
            foreach (var t in s_binaryShiftTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[typeof(int)])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.RightShiftAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.RightShiftAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.RightShift(Expression.Default(typeof(S1)), Expression.Default(typeof(int))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(int)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.RightShiftAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.RightShiftAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.RightShiftAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.RightShiftAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> RightShiftAssign_Nullable()
        {
            foreach (var t in s_binaryShiftTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[typeof(int)])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.RightShiftAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.RightShiftAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> AndAssign()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AndAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AndAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.And(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AndAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AndAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AndAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AndAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> AndAssign_Nullable()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.AndAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.AndAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> OrAssign()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.OrAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.OrAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.Or(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.OrAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.OrAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.OrAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.OrAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> OrAssign_Nullable()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.OrAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.OrAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> ExclusiveOrAssign()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.ExclusiveOrAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ExclusiveOrAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = Expression.ExclusiveOr(Expression.Default(typeof(S1)), Expression.Default(typeof(S1))).Method;

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.ExclusiveOrAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ExclusiveOrAssign, e);
                    }

                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.ExclusiveOrAssign(lhs, rhs, null, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ExclusiveOrAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> ExclusiveOrAssign_Nullable()
        {
            foreach (var t in s_binaryLogicTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.ExclusiveOrAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.ExclusiveOrAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PowerAssign()
        {
            foreach (var t in s_binaryPowerTypes)
            {
                foreach (var l in s_exprs[t])
                {
                    foreach (var r in s_exprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.PowerAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PowerAssign, e);
                        }
                    }
                }
            }

            var x = Expression.Parameter(typeof(S1));
            var c = Expression.Lambda(Expression.Negate(x), x);
            var m = typeof(S1).GetTypeInfo().GetDeclaredMethod("Pow");

            foreach (var l in s_exprs[typeof(S1)])
            {
                foreach (var r in s_exprs[typeof(S1)])
                {
                    foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.PowerAssign(lhs, rhs, m, c)))
                    {
                        yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PowerAssign, e);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> PowerAssign_Nullable()
        {
            foreach (var t in s_binaryPowerTypes)
            {
                foreach (var l in s_nullableExprs[t])
                {
                    foreach (var r in s_nullableExprs[t])
                    {
                        foreach (var e in GetAssignments(l, r, (lhs, rhs) => Expression.PowerAssign(lhs, rhs)))
                        {
                            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.PowerAssign, e);
                        }
                    }
                }
            }

            // TODO: conversion
        }

        private static IEnumerable<Expression> GetAssignments(Expression initialValue, Expression value, Func<Expression, Expression, Expression> createAssignment)
        {
            // Parameter
            {
                var p = Expression.Parameter(initialValue.Type);
                var a = Expression.Assign(p, initialValue);

                yield return Expression.Block(new[] { p }, a, createAssignment(p, value));
            }

            // Member
            {
                var p = Expression.Parameter(typeof(Holder<>).MakeGenericType(initialValue.Type));
                var a = Expression.Assign(p, Expression.New(p.Type));
                var v = Expression.Property(p, "Value");
                var b = Expression.Assign(v, initialValue);

                yield return Expression.Block(new[] { p }, a, b, createAssignment(v, value));
            }

            // Array
            {
                var p = Expression.Parameter(initialValue.Type.MakeArrayType());
                var a = Expression.Assign(p, Expression.NewArrayBounds(initialValue.Type, Expression.Constant(1)));
                var e = Expression.ArrayAccess(p, Expression.Constant(0));
                var b = Expression.Assign(e, initialValue);

                yield return Expression.Block(new[] { p }, a, b, createAssignment(e, value));
            }

            // Vector
            {
                var p = Expression.Parameter(typeof(Vector<>).MakeGenericType(initialValue.Type));
                var a = Expression.Assign(p, Expression.New(p.Type));
                var e = Expression.MakeIndex(p, p.Type.GetTypeInfo().GetDeclaredProperty("Item"), new[] { Expression.Constant(0) });
                var b = Expression.Assign(e, initialValue);

                yield return Expression.Block(new[] { p }, a, b, createAssignment(e, value));
            }
        }

        // TODO: conversion lambda
    }
}