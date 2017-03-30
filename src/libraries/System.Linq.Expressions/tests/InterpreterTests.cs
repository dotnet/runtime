// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// FEATURE_COMPILE is not directly required, 
// but this functionality relies on private reflection and that would not work with AOT
#if FEATURE_INTERPRET && FEATURE_COMPILE

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
            // Using an unchecked multiplication to ensure that a mul instruction is emitted (and not mul.ovf)
            Expression<Func<string, bool>> f = s => s != null && unchecked(s.Substring(1).Length * 2) > 0;

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

        [Fact]
        public static void ConstructorThrows_StackTrace()
        {
            Expression<Func<Thrower>> e = () => new Thrower(true);
            Func<Thrower> f = e.Compile(preferInterpretation: true);
            AssertStackTrace(() => f(), "Thrower..ctor");
        }

        [Fact]
        public static void PropertyGetterThrows_StackTrace()
        {
            Expression<Func<Thrower, int>> e = t => t.Bar;
            Func<Thrower, int> f = e.Compile(preferInterpretation: true);
            AssertStackTrace(() => f(new Thrower(error: false)), "Thrower.get_Bar");
        }

        [Fact]
        public static void PropertySetterThrows_StackTrace()
        {
            ParameterExpression t = Expression.Parameter(typeof(Thrower), "t");
            Expression<Action<Thrower>> e = Expression.Lambda<Action<Thrower>>(Expression.Assign(Expression.Property(t, nameof(Thrower.Bar)), Expression.Constant(0)), t);
            Action<Thrower> f = e.Compile(preferInterpretation: true);
            AssertStackTrace(() => f(new Thrower(error: false)), "Thrower.set_Bar");
        }

        [Fact]
        public static void IndexerGetterThrows_StackTrace()
        {
            ParameterExpression t = Expression.Parameter(typeof(Thrower), "t");
            Expression<Func<Thrower, int>> e = Expression.Lambda<Func<Thrower, int>>(Expression.MakeIndex(t, typeof(Thrower).GetProperty("Item"), new[] { Expression.Constant(0) }), t);
            Func<Thrower, int> f = e.Compile(preferInterpretation: true);
            AssertStackTrace(() => f(new Thrower(error: false)), "Thrower.get_Item");
        }

        [Fact]
        public static void IndexerSetterThrows_StackTrace()
        {
            ParameterExpression t = Expression.Parameter(typeof(Thrower), "t");
            Expression<Action<Thrower>> e = Expression.Lambda<Action<Thrower>>(Expression.Assign(Expression.MakeIndex(t, typeof(Thrower).GetProperty("Item"), new[] { Expression.Constant(0) }), Expression.Constant(0)), t);
            Action<Thrower> f = e.Compile(preferInterpretation: true);
            AssertStackTrace(() => f(new Thrower(error: false)), "Thrower.set_Item");
        }

        [Fact]
        public static void MethodThrows_StackTrace()
        {
            Expression<Action<Thrower>> e = t => t.Foo();
            Action<Thrower> f = e.Compile(preferInterpretation: true);
            AssertStackTrace(() => f(new Thrower(error: false)), "Thrower.Foo");
        }

        public static void VerifyInstructions(this LambdaExpression expression, string expected)
        {
            string actual = expression.GetInstructions();

            string nExpected = Normalize(expected);
            string nActual = Normalize(actual);

            Assert.Equal(nExpected, nActual);
        }

        private static string Normalize(string s)
        {
            Collections.Generic.IEnumerable<string> lines =
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

        private static void AssertStackTrace(Action a, string searchTerm)
        {
            bool hasThrown = false;
            try
            {
                a();
            }
            catch (Exception ex)
            {
                AssertStackTrace(ex, searchTerm);
                hasThrown = true;
            }

            Assert.True(hasThrown);
        }

        private static void AssertStackTrace(Exception ex, string searchTerm)
        {
            Assert.True(ex.StackTrace.Contains(searchTerm));
        }

        private sealed class Thrower
        {
            public Thrower(bool error)
            {
                if (error)
                    throw new Exception();
            }

            public int this[int x]
            {
                get { throw new Exception(); }
                set { throw new Exception(); }
            }

            public int Bar
            {
                get { throw new Exception(); }
                set { throw new Exception(); }
            }

            public void Foo()
            {
                throw new Exception();
            }
        }
    }
}

#endif
