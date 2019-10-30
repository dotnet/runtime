// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ConditionalTests
    {
        [Fact]
        public void VisitIfThenDoesNotCloneTree()
        {
            Expression ifTrue = ((Expression<Action>)(() => Nop())).Body;

            ConditionalExpression e = Expression.IfThen(Expression.Constant(true), ifTrue);

            Expression r = new Visitor().Visit(e);

            Assert.Same(e, r);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void Conditional(bool useInterpreter)
        {
            Expression<Func<int, int, int>> f = (x, y) => x > 5 ? x : y;
            Func<int, int, int> d = f.Compile(useInterpreter);
            Assert.Equal(7, d(7, 4));
            Assert.Equal(6, d(3, 6));
        }

        [Fact]
        public void NullTest()
        {
            AssertExtensions.Throws<ArgumentNullException>("test", () => Expression.IfThen(null, Expression.Empty()));
            AssertExtensions.Throws<ArgumentNullException>("test", () => Expression.IfThenElse(null, Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentNullException>("test", () => Expression.Condition(null, Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentNullException>("test", () => Expression.Condition(null, Expression.Empty(), Expression.Empty(), typeof(void)));
        }

        [Fact]
        public void UnreadableTest()
        {
            Expression test = Expression.Property(null, typeof(Unreadable<bool>), nameof(Unreadable<bool>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThen(test, Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThenElse(test, Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(test, Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(test, Expression.Empty(), Expression.Empty(), typeof(void)));
        }

        [Fact]
        public void NullIfTrue()
        {
            AssertExtensions.Throws<ArgumentNullException>("ifTrue", () => Expression.IfThen(Expression.Constant(true), null));
            AssertExtensions.Throws<ArgumentNullException>("ifTrue", () => Expression.IfThenElse(Expression.Constant(true), null, Expression.Empty()));
            AssertExtensions.Throws<ArgumentNullException>("ifTrue", () => Expression.Condition(Expression.Constant(true), null, Expression.Empty()));
            AssertExtensions.Throws<ArgumentNullException>("ifTrue", () => Expression.Condition(Expression.Constant(true), null, Expression.Empty(), typeof(void)));
        }

        [Fact]
        public void UnreadableIfTrue()
        {
            Expression ifTrue = Expression.Property(null, typeof(Unreadable<int>), nameof(Unreadable<int>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("ifTrue", () => Expression.IfThen(Expression.Constant(true), ifTrue));
            AssertExtensions.Throws<ArgumentException>("ifTrue", () => Expression.IfThenElse(Expression.Constant(true), ifTrue, Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("ifTrue", () => Expression.Condition(Expression.Constant(true), ifTrue, Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("ifTrue", () => Expression.Condition(Expression.Constant(true), ifTrue, Expression.Empty(), typeof(void)));
        }

        [Fact]
        public void NullIfFalse()
        {
            AssertExtensions.Throws<ArgumentNullException>("ifFalse", () => Expression.IfThenElse(Expression.Constant(true), Expression.Empty(), null));
            AssertExtensions.Throws<ArgumentNullException>("ifFalse", () => Expression.Condition(Expression.Constant(true), Expression.Empty(), null));
            AssertExtensions.Throws<ArgumentNullException>("ifFalse", () => Expression.Condition(Expression.Constant(true), Expression.Empty(), null, typeof(void)));
        }

        [Fact]
        public void UnreadbleIfFalse()
        {
            Expression ifFalse = Expression.Property(null, typeof(Unreadable<int>), nameof(Unreadable<int>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("ifFalse", () => Expression.IfThenElse(Expression.Constant(true), Expression.Empty(), ifFalse));
            AssertExtensions.Throws<ArgumentException>("ifFalse", () => Expression.Condition(Expression.Constant(true), Expression.Constant(0), ifFalse));
            AssertExtensions.Throws<ArgumentException>("ifFalse", () => Expression.Condition(Expression.Constant(true), Expression.Empty(), ifFalse, typeof(void)));
        }

        [Fact]
        public void NullType()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Condition(Expression.Constant(true), Expression.Empty(), Expression.Empty(), null));
        }

        [Fact]
        public void NonBooleanTest()
        {
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThen(Expression.Constant(0), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThenElse(Expression.Constant(0), Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(Expression.Constant(0), Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(Expression.Constant(0), Expression.Empty(), Expression.Empty(), typeof(void)));

            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThen(Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThenElse(Expression.Empty(), Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(Expression.Empty(), Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(Expression.Empty(), Expression.Empty(), Expression.Empty(), typeof(void)));

            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThen(Expression.Constant(true, typeof(bool?)), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThenElse(Expression.Constant(true, typeof(bool?)), Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(Expression.Constant(true, typeof(bool?)), Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(Expression.Constant(true, typeof(bool?)), Expression.Empty(), Expression.Empty(), typeof(void)));

            ConstantExpression truthyConstant = Expression.Constant(new Truthiness(true));

            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThen(Expression.Constant(truthyConstant), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.IfThenElse(Expression.Constant(truthyConstant), Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(Expression.Constant(truthyConstant), Expression.Empty(), Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("test", () => Expression.Condition(Expression.Constant(truthyConstant), Expression.Empty(), Expression.Empty(), typeof(void)));
        }

        [Fact]
        public void IncompatibleImplicitTypes()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant(0), Expression.Constant(0L)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant(0L), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant("hello"), Expression.Constant(new object())));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant(new object()), Expression.Constant("hello")));
        }

        [Fact]
        public void IncompatibleExplicitTypes()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant(0), Expression.Constant(0L), typeof(int)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant(0L), Expression.Constant(0), typeof(int)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant(0), Expression.Constant(0L), typeof(long)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant(0L), Expression.Constant(0), typeof(long)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant(0), Expression.Constant("hello"), typeof(object)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(Expression.Constant(true), Expression.Constant("hello"), Expression.Constant(0), typeof(object)));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AnyTypesAllowedWithExplicitVoid(bool useInterpreter)
        {
            Action act = Expression.Lambda<Action>(
                Expression.Condition(Expression.Constant(true), Expression.Constant(0), Expression.Constant(0L), typeof(void))
                ).Compile(useInterpreter);
            act();

            act = Expression.Lambda<Action>(
                Expression.Condition(Expression.Constant(true), Expression.Constant(0L), Expression.Constant(0), typeof(void))
                ).Compile(useInterpreter);
            act();
        }

        [Theory, PerCompilationType(nameof(ConditionalValues))]
        public void ConditionalSelectsCorrectExpression(bool test, object ifTrue, object ifFalse, object expected, bool useInterpreter)
        {
            Func<object> func = Expression.Lambda<Func<object>>(
                Expression.Convert(
                    Expression.Condition(
                        Expression.Constant(test),
                        Expression.Constant(ifTrue),
                        Expression.Constant(ifFalse)
                        ),
                        typeof(object)
                    )
                ).Compile(useInterpreter);

            Assert.Equal(expected, func());
        }

        [Theory, PerCompilationType(nameof(ConditionalValues))]
        public void InvertedConditionalSelectsCorrectExpression(bool test, object ifTrue, object ifFalse, object expected, bool useInterpreter)
        {
            Func<object> func = Expression.Lambda<Func<object>>(
                Expression.Convert(
                    Expression.Condition(
                        Expression.Not(Expression.Constant(test)),
                        Expression.Constant(ifFalse),
                        Expression.Constant(ifTrue)
                    ),
                    typeof(object)
                )
            ).Compile(useInterpreter);

            Assert.Equal(expected, func());
        }


        [Theory, PerCompilationType(nameof(ConditionalValues))]
        public void ConditionalWithMethodSelectsCorrectExpression(bool test, object ifTrue, object ifFalse, object expected, bool useInterpreter)
        {
            Func<object> func = Expression.Lambda<Func<object>>(
                Expression.Convert(
                    Expression.Condition(
                        Expression.Not(Expression.Constant(test), GetType().GetMethod(nameof(NotNot))),
                        Expression.Constant(ifTrue),
                        Expression.Constant(ifFalse)
                    ),
                    typeof(object)
                )
            ).Compile(useInterpreter);

            Assert.Equal(expected, func());
        }

        [Theory, PerCompilationType(nameof(ConditionalValuesWithTypes))]
        public void ConditionalSelectsCorrectExpressionWithType(bool test, object ifTrue, object ifFalse, object expected, Type type, bool useInterpreter)
        {
            Func<object> func = Expression.Lambda<Func<object>>(
                Expression.Condition(
                    Expression.Constant(test),
                    Expression.Constant(ifTrue),
                    Expression.Constant(ifFalse),
                    type
                    )
                ).Compile(useInterpreter);

            Assert.Same(expected, func());
        }

        [Fact]
        public void ByRefType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(
                Expression.Constant(true),
                Expression.Constant(null),
                Expression.Constant(null),
                typeof(string).MakeByRefType()));
        }

        [Fact]
        public void PointerType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(
                Expression.Constant(true),
                Expression.Constant(null),
                Expression.Constant(null),
                typeof(string).MakePointerType()));
        }

        [Fact]
        public void GenericType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(
                Expression.Constant(true),
                Expression.Constant(null),
                Expression.Constant(null),
                typeof(List<>)));
        }

        [Fact]
        public void TypeContainsGenericParameters()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(
                Expression.Constant(true),
                Expression.Constant(null),
                Expression.Constant(null),
                typeof(List<>.Enumerator)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Condition(
                Expression.Constant(true),
                Expression.Constant(null),
                Expression.Constant(null),
                typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Fact]
        public static void ToStringTest()
        {
            ConditionalExpression e1 = Expression.Condition(Expression.Parameter(typeof(bool), "a"), Expression.Parameter(typeof(int), "b"), Expression.Parameter(typeof(int), "c"));
            Assert.Equal("IIF(a, b, c)", e1.ToString());

            ConditionalExpression e2 = Expression.IfThen(Expression.Parameter(typeof(bool), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("IIF(a, b, default(Void))", e2.ToString());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void TurnOnNullableComparedWithConstantNull(bool useInterpreter)
        {
            Func<int> func = Expression.Lambda<Func<int>>(
                    Expression.Condition(
                        Expression.Equal(Expression.Constant(2, typeof(int?)), Expression.Default(typeof(int?))),
                        Expression.Constant(1), Expression.Constant(2)))
                .Compile(useInterpreter);
            Assert.Equal(2, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void TurnOnReferenceComparedWithConstantNull(bool useInterpreter)
        {
            Func<int> func = Expression.Lambda<Func<int>>(
                    Expression.Condition(
                        Expression.Equal(Expression.Constant(new object()), Expression.Default(typeof(object))),
                        Expression.Constant(1), Expression.Constant(2)))
                .Compile(useInterpreter);
            Assert.Equal(2, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void TurnOnConstantNullComparedWithNullable(bool useInterpreter)
        {
            Func<int> func = Expression.Lambda<Func<int>>(
                    Expression.Condition(
                        Expression.Equal(Expression.Default(typeof(int?)), Expression.Constant(2, typeof(int?))),
                        Expression.Constant(1), Expression.Constant(2)))
                .Compile(useInterpreter);
            Assert.Equal(2, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void TurnOnConstantNullComparedWithReference(bool useInterpreter)
        {
            Func<int> func = Expression.Lambda<Func<int>>(
                    Expression.Condition(
                        Expression.Equal(Expression.Default(typeof(object)), Expression.Constant(new object())),
                        Expression.Constant(1), Expression.Constant(2)))
                .Compile(useInterpreter);
            Assert.Equal(2, func());
        }

        public static IEnumerable<object[]> ConditionalValues()
        {
            yield return new object[] { true, "yes", "no", "yes" };
            yield return new object[] { false, "yes", "no", "no" };
            yield return new object[] { true, 42, 12, 42 };
            yield return new object[] { false, 42L, 12L, 12L };
        }

        public static IEnumerable<object[]> ConditionalValuesWithTypes()
        {
            ConstantExpression ce = Expression.Constant(98);
            BinaryExpression be = Expression.And(Expression.Constant(2), Expression.Constant(3));
            yield return new object[] { true, ce, be, ce, typeof(Expression) };
            yield return new object[] { false, ce, be, be, typeof(Expression) };
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        private static void Nop()
        {
        }

        private class Visitor : ExpressionVisitor
        {
        }

        public static bool NotNot(bool value) => value;
    }
}
