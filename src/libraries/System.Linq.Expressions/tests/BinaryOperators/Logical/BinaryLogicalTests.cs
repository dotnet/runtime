// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Binary
{
    public static unsafe class BinaryLogicalTests
    {
        #region Test methods

        [Fact]
        public static void CheckBoolAndTest()
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolAnd(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckBoolAndAlsoTest()
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolAndAlso(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckBoolOrTest()
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolOr(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckBoolOrElseTest()
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolOrElse(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBoolAnd(bool a, bool b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.And(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a & b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyBoolAndAlso(bool a, bool b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.AndAlso(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a && b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyBoolOr(bool a, bool b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a | b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyBoolOrElse(bool a, bool b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.OrElse(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a || b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        #endregion
    }
}
