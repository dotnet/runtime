// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if FEATURE_INTERPRET

using System.Linq.Expressions.Interpreter;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class InterpreterTests
    {
        private static readonly PropertyInfo s_debugView = typeof(LightLambda).GetPropertyAssert("DebugView");

        [Fact]
        public static void VerifyInstructions_Simple()
        {
            Expression<Func<string, bool>> f = s => s != null && s.Substring(1).Length * 2 > 0;

            f.VerifyInstructions(
                @"object lambda_method(object[])
                  {
                    .locals 1
                    .maxstack 2
                    .maxcontinuation 0

                    IP_0000: InitParameter(0)
                    IP_0001: LoadLocal(0)
                    IP_0002: LoadObject(null)
                    IP_0003: Call(Boolean op_Inequality(System.String, System.String))
                    IP_0004: BranchFalse(10) -> 14
                    IP_0005: LoadLocal(0)
                    IP_0006: LoadObject(1)
                    IP_0007: Call(System.String Substring(Int32))
                    IP_0008: Call(Int32 get_Length())
                    IP_0009: LoadObject(2)
                    IP_0010: Mul()
                    IP_0011: LoadObject(0)
                    IP_0012: GreaterThan()
                    IP_0013: Branch(2) -> 15
                    IP_0014: LoadObject(False)
                  }");
        }

        [Fact]
        public static void VerifyInstructions_Exceptions()
        {
            ParameterExpression x = Expression.Parameter(typeof(int), "x");
            Expression<Func<int, int>> f =
                Expression.Lambda<Func<int, int>>(
                    Expression.TryCatchFinally(
                        Expression.Call(
                            typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }),
                            Expression.Divide(
                                Expression.Constant(42),
                                x
                            )
                        ),
                        Expression.Empty(),
                        Expression.Catch(
                            typeof(DivideByZeroException),
                            Expression.Constant(-1)
                        )
                    ),
                    x
                );

            f.VerifyInstructions(
                @"object lambda_method(object[])
                  {
                    .locals 2
                    .maxstack 3
                    .maxcontinuation 1

                    IP_0000: InitParameter(0)
                    .try
                    {
                      IP_0001: EnterTryFinally[0] -> 11
                      IP_0002: LoadObject(42)
                      IP_0003: LoadLocal(0)
                      IP_0004: Div()
                      IP_0005: Call(Int32 Abs(Int32))
                      IP_0006: Goto[1] -> 13
                    }
                    catch(DivideByZeroException) [8->11]
                    {
                      IP_0007: EnterExceptionHandler()
                      IP_0008: StoreLocal(1)
                      IP_0009: LoadObject(-1)
                      IP_0010: LeaveExceptionHandler[3] -> 6
                    }
                    finally
                    {
                      IP_0011: EnterFinally[0] -> 11
                      IP_0012: LeaveFinally()
                    }
                  }");
        }

        public static void VerifyInstructions(this LambdaExpression expression, string expected)
        {
            var actual = expression.GetInstructions();

            var nExpected = Normalize(expected);
            var nActual = Normalize(actual);

            Assert.Equal(nExpected, nActual);
        }

        private static string Normalize(string s)
        {
            var lines =
                s
                .Replace("\r\n", "\n")
                .Split(new[] { '\n' })
                .Select(line => line.Trim())
                .Where(line => line != "" && !line.StartsWith("//"));

            return string.Join("\n", lines);
        }

        private static string GetInstructions(this LambdaExpression expression)
        {
            Delegate d = expression.Compile(true);
            var thunk = (Func<object[], object>)d.Target;
            var lambda = (LightLambda)thunk.Target;
            var debugView = (string)s_debugView.GetValue(lambda);
            return debugView;
        }
    }
}

#endif
