// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryLogicalTests
    {
        //TODO: Need tests on the short-circuit and non-short-circuit nature of the two forms.

        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolAndTest(bool useInterpreter)
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolAnd(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolAndAlsoTest(bool useInterpreter)
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolAndAlso(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolOrTest(bool useInterpreter)
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolOr(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolOrElseTest(bool useInterpreter)
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolOrElse(array[i], array[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBoolAnd(bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.And(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBoolAndAlso(bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.AndAlso(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a && b, f());
        }

        private static void VerifyBoolOr(bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a | b, f());
        }

        private static void VerifyBoolOrElse(bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.OrElse(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a || b, f());
        }

        #endregion

        [Fact]
        public static void CannotReduceAndAlso()
        {
            Expression exp = Expression.AndAlso(Expression.Constant(true), Expression.Constant(false));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void CannotReduceOrElse()
        {
            Expression exp = Expression.OrElse(Expression.Constant(true), Expression.Constant(false));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void AndAlsoThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.AndAlso(null, Expression.Constant(true)));
        }

        [Fact]
        public static void AndAlsoThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.AndAlso(Expression.Constant(true), null));
        }

        [Fact]
        public static void OrElseThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.OrElse(null, Expression.Constant(true)));
        }

        [Fact]
        public static void OrElseThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.OrElse(Expression.Constant(true), null));
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public static void AndAlsoThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.AndAlso(value, Expression.Constant(true)));
        }

        [Fact]
        public static void AndAlsoThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.AndAlso(Expression.Constant(true), value));
        }

        [Fact]
        public static void OrElseThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.OrElse(value, Expression.Constant(true)));
        }

        [Fact]
        public static void OrElseThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.OrElse(Expression.Constant(false), value));
        }

        [Fact]
        public static void ToStringTest()
        {
            // NB: These were && and || in .NET 3.5 but shipped as AndAlso and OrElse in .NET 4.0; we kept the latter.

            var e1 = Expression.AndAlso(Expression.Parameter(typeof(bool), "a"), Expression.Parameter(typeof(bool), "b"));
            Assert.Equal("(a AndAlso b)", e1.ToString());

            var e2 = Expression.OrElse(Expression.Parameter(typeof(bool), "a"), Expression.Parameter(typeof(bool), "b"));
            Assert.Equal("(a OrElse b)", e2.ToString());
        }
    }
}
