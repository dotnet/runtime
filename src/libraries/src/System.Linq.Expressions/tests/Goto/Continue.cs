// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class Continue : GotoExpressionTests
    {
        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetContinueHasNoValue(Type type)
        {
            LabelTarget target = Expression.Label(type);
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Continue(target));
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetContinueHasNoValueTypeExplicit(Type type)
        {
            LabelTarget target = Expression.Label(type);
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Continue(target, type));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ContinueVoidNoValue(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Continue(target),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ContinueExplicitVoidNoValue(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Continue(target, typeof(void)),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile(useInterpreter)();
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NullValueOnNonVoidContinue(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Continue(Expression.Label(type)));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void ExplicitNullTypeWithValue(object value)
        {
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Continue(Expression.Label(value.GetType()), default(Type)));
        }

        [Fact]
        public void OpenGenericType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(List<>)));
        }

        [Fact]
        public static void TypeContainsGenericParameters()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(List<>.Enumerator)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Fact]
        public void PointerType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(int).MakePointerType()));
        }

        [Fact]
        public void ByRefType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Continue(Expression.Label(typeof(void)), typeof(int).MakeByRefType()));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void UndefinedLabel(bool useInterpreter)
        {
            Expression<Action> continueNowhere = Expression.Lambda<Action>(Expression.Continue(Expression.Label()));
            Assert.Throws<InvalidOperationException>(() => continueNowhere.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AmbiguousJump(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Action> exp = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.Continue(target),
                    Expression.Block(Expression.Label(target)),
                    Expression.Block(Expression.Label(target))
                    )
                );
            Assert.Throws<InvalidOperationException>(() => exp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void MultipleDefinitionsInSeparateBlocks(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Func<int> add = Expression.Lambda<Func<int>>(
                Expression.Add(
                    Expression.Block(
                        Expression.Continue(target),
                        Expression.Throw(Expression.Constant(new Exception())),
                        Expression.Label(target),
                        Expression.Constant(10)
                    ),
                    Expression.Block(
                        Expression.Continue(target),
                        Expression.Throw(Expression.Constant(new Exception())),
                        Expression.Label(target),
                        Expression.Constant(5)
                    )
                )
            ).Compile(useInterpreter);
            Assert.Equal(15, add());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpIntoExpression(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Func<bool>> isInt = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    Expression.Continue(target),
                    Expression.TypeIs(Expression.Label(target), typeof(int))
                )
            );
            Assert.Throws<InvalidOperationException>(() => isInt.Compile(useInterpreter));
        }
    }
}
