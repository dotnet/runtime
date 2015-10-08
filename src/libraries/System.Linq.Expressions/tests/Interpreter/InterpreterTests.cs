// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if FEATURE_INTERPRET
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.ExpressionInterpreter
{
    public static unsafe class InterpreterTests
    {
        #region Test methods

        [Fact]
        public static void InterpretCompileCrossChecks()
        {
            Verify(Expression.Constant(1));
            Verify(Expression.Constant("bar"));

            Verify(Expression.Add(Expression.Constant(1), Expression.Constant(2)));
        }

        #endregion

        #region Test verifiers

        private static void Verify(Expression expr)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(expr, typeof(object)),
                    Enumerable.Empty<ParameterExpression>());

            Func<object> c = e.Compile();
            Func<object> i = e.Compile(preferInterpretation: true);

            // compute the value with the compiler
            object cResult = null;
            Exception cException = null;
            try
            {
                cResult = c();
            }
            catch (Exception ex)
            {
                cException = ex;
            }

            // compute the value with the interpreter
            object iResult = default(object);
            Exception iException = null;
            try
            {
                iResult = i();
            }
            catch (Exception ex)
            {
                iException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (cException != null || iException != null)
            {
                Assert.NotNull(cException);
                Assert.NotNull(iException);
                Assert.Equal(iException.GetType(), cException.GetType());
            }
            else
            {
                Assert.Equal(iResult, cResult);
            }
        }

        #endregion
    }
}
#endif