// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if FEATURE_INTERPRET
using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static partial class InterpreterTests
    {
        #region Test methods

        [Fact]
        public static void InterpretCompileCrossChecks()
        {
            Verify(Expression.Constant(1));
            Verify(Expression.Constant("bar"));

            Verify(Expression.Add(Expression.Constant(1), Expression.Constant(2)));
        }

        static partial void MissingTest(ExpressionType nodeType)
        {
            Assert.True(false);
        }

        #endregion

        #region Test verifiers

        private static void Verify(Expression expr)
        {
            Expression res;

            if (expr.Type == typeof(void))
            {
                res = Expression.Block(expr, Expression.Constant(null));
            }
            else
            {
                res = Expression.Convert(expr, typeof(object));
            }

            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    res,
                    Enumerable.Empty<ParameterExpression>());

            try
            {
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
                    if (cException == null)
                    {
                        Assert.True(false, string.Format("Interpreter threw '{0}' but compiler did not.", iException));
                    }

                    if (iException == null)
                    {
                        Assert.True(false, string.Format("Compiler threw '{0}' but interpreter did not.", cException));
                    }

                    Assert.Equal(iException.GetType(), cException.GetType());
                }
                else
                {
                    Assert.Equal(iResult, cResult);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Error for expression '{0}':\r\n\r\n{1}", typeof(Expression).GetProperty("DebugView", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(expr), ex.ToString()));
            }
        }

        #endregion
    }
}
#endif