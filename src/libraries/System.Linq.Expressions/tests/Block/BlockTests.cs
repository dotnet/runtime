// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BlockTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBlockClosureVariableInitializationTest(bool useInterpreter)
        {
            foreach (var kv in BlockClosureVariableInitialization())
            {
                VerifyBlockClosureVariableInitialization(kv.Key, kv.Value, useInterpreter);
            }
        }

        private static IEnumerable<KeyValuePair<Expression, object>> BlockClosureVariableInitialization()
        {
            {
                ParameterExpression p = Expression.Parameter(typeof(int));
                ParameterExpression q = Expression.Parameter(typeof(Func<int>));
                Expression<Func<int>> l = Expression.Lambda<Func<int>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(int));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(int));
                ParameterExpression q = Expression.Parameter(typeof(Action<int>));
                ParameterExpression x = Expression.Parameter(typeof(int));
                Expression<Action<int>> l = Expression.Lambda<Action<int>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(int));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(TimeSpan));
                ParameterExpression q = Expression.Parameter(typeof(Func<TimeSpan>));
                Expression<Func<TimeSpan>> l = Expression.Lambda<Func<TimeSpan>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(TimeSpan));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(TimeSpan));
                ParameterExpression q = Expression.Parameter(typeof(Action<TimeSpan>));
                ParameterExpression x = Expression.Parameter(typeof(TimeSpan));
                Expression<Action<TimeSpan>> l = Expression.Lambda<Action<TimeSpan>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(TimeSpan));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(string));
                ParameterExpression q = Expression.Parameter(typeof(Func<string>));
                Expression<Func<string>> l = Expression.Lambda<Func<string>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(string));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(string));
                ParameterExpression q = Expression.Parameter(typeof(Action<string>));
                ParameterExpression x = Expression.Parameter(typeof(string));
                Expression<Action<string>> l = Expression.Lambda<Action<string>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(string));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(int?));
                ParameterExpression q = Expression.Parameter(typeof(Func<int?>));
                Expression<Func<int?>> l = Expression.Lambda<Func<int?>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(int?));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(int?));
                ParameterExpression q = Expression.Parameter(typeof(Action<int?>));
                ParameterExpression x = Expression.Parameter(typeof(int?));
                Expression<Action<int?>> l = Expression.Lambda<Action<int?>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(int?));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(TimeSpan?));
                ParameterExpression q = Expression.Parameter(typeof(Func<TimeSpan?>));
                Expression<Func<TimeSpan?>> l = Expression.Lambda<Func<TimeSpan?>>(p);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(TimeSpan?));
            }

            {
                ParameterExpression p = Expression.Parameter(typeof(TimeSpan?));
                ParameterExpression q = Expression.Parameter(typeof(Action<TimeSpan?>));
                ParameterExpression x = Expression.Parameter(typeof(TimeSpan?));
                Expression<Action<TimeSpan?>> l = Expression.Lambda<Action<TimeSpan?>>(Expression.Assign(p, x), x);
                yield return new KeyValuePair<Expression, object>(Expression.Block(new[] { p, q }, Expression.Assign(q, l), p), default(TimeSpan?));
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBlockClosureVariableInitialization(Expression e, object o, bool useInterpreter)
        {
            Expression<Func<object>> f =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(e, typeof(object)));

            Func<object> c = f.Compile(useInterpreter);
            Assert.Equal(o, c());
        }

        #endregion

        private class ParameterChangingVisitor : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return Expression.Parameter(node.IsByRef ? node.Type.MakeByRefType() : node.Type, node.Name);
            }
        }

        [Fact]
        public static void VisitChangingOnlyParameters()
        {
            BlockExpression block = Expression.Block(
                new[] { Expression.Parameter(typeof(int)), Expression.Parameter(typeof(string)) },
                Expression.Empty()
                );
            Assert.NotSame(block, new ParameterChangingVisitor().Visit(block));
        }

        [Fact]
        public static void VisitChangingOnlyParametersMultiStatementBody()
        {
            BlockExpression block = Expression.Block(
                new[] { Expression.Parameter(typeof(int)), Expression.Parameter(typeof(string)) },
                Expression.Empty(),
                Expression.Empty()
                );
            Assert.NotSame(block, new ParameterChangingVisitor().Visit(block));
        }

        [Fact]
        public static void VisitChangingOnlyParametersTyped()
        {
            BlockExpression block = Expression.Block(
                typeof(object),
                new[] { Expression.Parameter(typeof(int)), Expression.Parameter(typeof(string)) },
                Expression.Constant("")
                );
            Assert.NotSame(block, new ParameterChangingVisitor().Visit(block));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void EmptyBlock(bool useInterpreter)
        {
            BlockExpression block = Expression.Block();
            Assert.Equal(typeof(void), block.Type);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => block.Result);
            Action nop = Expression.Lambda<Action>(block).Compile(useInterpreter);
            nop();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void EmptyBlockExplicitType(bool useInterpreter)
        {
            BlockExpression block = Expression.Block(typeof(void));
            Assert.Equal(typeof(void), block.Type);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => block.Result);
            Action nop = Expression.Lambda<Action>(block).Compile(useInterpreter);
            nop();
        }

        [Fact]
        public static void EmptyBlockWrongExplicitType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Block(typeof(int)));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void EmptyScope(bool useInterpreter)
        {
            BlockExpression scope = Expression.Block(new[] { Expression.Parameter(typeof(int), "x") }, new Expression[0]);
            Assert.Equal(typeof(void), scope.Type);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => scope.Result);
            Action nop = Expression.Lambda<Action>(scope).Compile(useInterpreter);
            nop();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void EmptyScopeExplicitType(bool useInterpreter)
        {
            BlockExpression scope = Expression.Block(typeof(void), new[] { Expression.Parameter(typeof(int), "x") }, new Expression[0]);
            Assert.Equal(typeof(void), scope.Type);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => scope.Result);
            Action nop = Expression.Lambda<Action>(scope).Compile(useInterpreter);
            nop();
        }

        [Fact]
        public static void EmptyScopeExplicitWrongType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Block(
                typeof(int),
                new[] { Expression.Parameter(typeof(int), "x") },
                new Expression[0]));
        }

        [Fact]
        public static void ToStringTest()
        {
            BlockExpression e1 = Expression.Block(Expression.Empty());
            Assert.Equal("{ ... }", e1.ToString());

            BlockExpression e2 = Expression.Block(new[] { Expression.Parameter(typeof(int), "x") }, Expression.Empty());
            Assert.Equal("{var x; ... }", e2.ToString());

            BlockExpression e3 = Expression.Block(new[] { Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y") }, Expression.Empty());
            Assert.Equal("{var x;var y; ... }", e3.ToString());
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/14919", TestRuntimes.Mono)]
        public static void InsignificantBlock()
        {
            Expression<Action> nop = Expression.Lambda<Action>(
                    Expression.Block(
                        Expression.Block(Expression.Empty(), Expression.Default(typeof(void))),
                        Expression.Block(Expression.Empty(), Expression.Default(typeof(void))),
                        Expression.Block(Expression.Empty(), Expression.Default(typeof(void))),
                        Expression.Block(Expression.Empty(), Expression.Default(typeof(void)))));

            nop.Verify(
@".method void ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
{
  .maxstack 0
  IL_0000: ret
}",
@"object lambda_method(object[])
{
  .locals 0
  .maxstack 0
  .maxcontinuation 0
}");
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MutableValue(bool useInterpreter)
        {
            // f = () =>
            //     {
            //         ValueTypeWithParameterlessConstructorThatThrows p;
            //         p = new ValueTypeWithParameterlessConstructorThatThrows(42);
            //         return p.Value;
            //     };
            const int value = 42;
            ParameterExpression p = Expression.Variable(typeof(ValueTypeWithParameterlessConstructorThatThrows));
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(p, Expression.Constant(new ValueTypeWithParameterlessConstructorThatThrows(value), typeof(ValueTypeWithParameterlessConstructorThatThrows))),
                        Expression.Field(p, typeof(ValueTypeWithParameterlessConstructorThatThrows).GetField("Value"))));

            Func<object> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MutableValue_Nullable(bool useInterpreter)
        {
            // f = () =>
            //     {
            //         ValueTypeWithParameterlessConstructorThatThrows? p;
            //         p = p.HasValue : new ValueTypeWithParameterlessConstructorThatThrows(42) : null;
            //         return p;
            //     };
            const int value = 42;
            ParameterExpression p = Expression.Variable(typeof(ValueTypeWithParameterlessConstructorThatThrows?));
            Expression<Func<ValueTypeWithParameterlessConstructorThatThrows?>> e =
                Expression.Lambda<Func<ValueTypeWithParameterlessConstructorThatThrows?>>(
                    Expression.Block(
                        new[] { p },
                        Expression.Assign(p,
                            Expression.Condition(
                                Expression.Property(p, typeof(ValueTypeWithParameterlessConstructorThatThrows?).GetProperty("HasValue")),
                                Expression.Constant((ValueTypeWithParameterlessConstructorThatThrows?)new ValueTypeWithParameterlessConstructorThatThrows(value), typeof(ValueTypeWithParameterlessConstructorThatThrows?)),
                                Expression.Constant(null, typeof(ValueTypeWithParameterlessConstructorThatThrows?)))),
                        p));

            Func<ValueTypeWithParameterlessConstructorThatThrows?> f = e.Compile(useInterpreter);
            Assert.Null(f());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void MutableBox(bool useInterpreter)
        {
            // f = () =>
            //     {
            //         ValueTypeWithParameterlessConstructorThatThrows p;
            //         Func<ValueTypeWithParameterlessConstructorThatThrows> q;
            //         q = () => p;
            //         p = new ValueTypeWithParameterlessConstructorThatThrows(42);
            //         return q().Value;
            //     };
            const int value = 42;
            ParameterExpression p = Expression.Parameter(typeof(ValueTypeWithParameterlessConstructorThatThrows));
            ParameterExpression q = Expression.Parameter(typeof(Func<ValueTypeWithParameterlessConstructorThatThrows>));
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Block(
                        new[] { p, q },
                        Expression.Assign(q, Expression.Lambda<Func<ValueTypeWithParameterlessConstructorThatThrows>>(p)),
                        Expression.Assign(p, Expression.Constant(new ValueTypeWithParameterlessConstructorThatThrows(value), typeof(ValueTypeWithParameterlessConstructorThatThrows))),
                        Expression.Field(Expression.Invoke(q), typeof(ValueTypeWithParameterlessConstructorThatThrows).GetField("Value"))));

            Func<object> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void MutableBox_Nullable(bool useInterpreter)
        {
            // f = () =>
            //     {
            //         ValueTypeWithParameterlessConstructorThatThrows? p;
            //         Func<ValueTypeWithParameterlessConstructorThatThrows?> q;
            //         q = () => p;
            //         return q();
            //     };
            ParameterExpression p = Expression.Parameter(typeof(ValueTypeWithParameterlessConstructorThatThrows?));
            ParameterExpression q = Expression.Parameter(typeof(Func<ValueTypeWithParameterlessConstructorThatThrows?>));
            Expression<Func<ValueTypeWithParameterlessConstructorThatThrows?>> e =
                Expression.Lambda<Func<ValueTypeWithParameterlessConstructorThatThrows?>>(
                    Expression.Block(
                        new[] { p, q },
                        Expression.Assign(q, Expression.Lambda<Func<ValueTypeWithParameterlessConstructorThatThrows?>>(p)),
                        Expression.Invoke(q)));

            Func<ValueTypeWithParameterlessConstructorThatThrows?> f = e.Compile(useInterpreter);
            Assert.Null(f());
        }
    }
}
