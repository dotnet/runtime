// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class TypeEqual : TypeBinaryTests
    {
        [Fact]
        public void NullExpression()
        {
            Assert.Throws<ArgumentNullException>("expression", () => Expression.TypeEqual(null, typeof(int)));
        }

        [Fact]
        public void NullType()
        {
            Expression exp = Expression.Constant(0);
            Assert.Throws<ArgumentNullException>("type", () => Expression.TypeEqual(exp, null));
        }

        [Fact]
        public void TypeByRef()
        {
            Expression exp = Expression.Constant(0);
            Type byRef = typeof(int).MakeByRefType();
            Assert.Throws<ArgumentException>(() => Expression.TypeEqual(exp, byRef));
        }

        [Fact]
        public void UnreadableExpression()
        {
            Expression exp = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("expression", () => Expression.TypeEqual(exp, typeof(int)));
        }

        [Fact]
        public void CannotReduce()
        {
            Expression exp = Expression.TypeIs(Expression.Constant(0), typeof(int));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(() => exp.ReduceAndCheck());
        }

        [Theory]
        [MemberData("ExpressionAndTypeCombinations")]
        public void TypePropertyMatches(Expression expression, Type type)
        {
            Assert.Equal(type, Expression.TypeEqual(expression, type).TypeOperand);
        }

        [Theory]
        [MemberData("ExpressionAndTypeCombinations")]
        public void TypeIsBoolean(Expression expression, Type type)
        {
            Assert.Equal(typeof(bool), Expression.TypeEqual(expression, type).Type);
        }

        [Theory]
        [MemberData("ExpressionAndTypeCombinations")]
        public void NodeType(Expression expression, Type type)
        {
            Assert.Equal(ExpressionType.TypeEqual, Expression.TypeEqual(expression, type).NodeType);
        }

        [Theory]
        [MemberData("ExpressionAndTypeCombinations")]
        public void ExpressionIsThatPassed(Expression expression, Type type)
        {
            Assert.Same(expression, Expression.TypeEqual(expression, type).Expression);
        }

        [Theory]
        [MemberData("ExpressionAndTypeCombinations")]
        public void ExpressionEvaluationCompiled(Expression expression, Type type)
        {
            bool expected;
            if (type == typeof(void))
                expected = expression.Type == typeof(void);
            else if (expression.Type == typeof(void))
                expected = false;
            else
            {
                Type nonNullable = type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? type.GetGenericArguments()[0]
                    : type;
                object value = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()();
                expected = value != null && value.GetType() == nonNullable;
            }

            Assert.Equal(expected, Expression.Lambda<Func<bool>>(Expression.TypeEqual(expression, type)).Compile(false)());
        }

        [Theory]
        [MemberData("ExpressionAndTypeCombinations")]
        public void ExpressionEvaluationInterpretted(Expression expression, Type type)
        {
            bool expected;
            if (type == typeof(void))
                expected = expression.Type == typeof(void);
            else if (expression.Type == typeof(void))
                expected = false;
            else
            {
                Type nonNullable = type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? type.GetGenericArguments()[0]
                    : type;
                object value = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()();
                expected = value != null && value.GetType() == nonNullable;
            }

            Assert.Equal(expected, Expression.Lambda<Func<bool>>(Expression.TypeEqual(expression, type)).Compile(true)());
        }

        [Theory]
        [MemberData("ExpressionAndTypeCombinations")]
        public void ExpressionEvaluationWithParameterCompiled(Expression expression, Type type)
        {
            if (expression.Type == typeof(void))
                return; // Can't have void parameter.
            bool expected;
            if (type == typeof(void))
                expected = false;
            else
            {
                Type nonNullable = type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? type.GetGenericArguments()[0]
                    : type;
                object value = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()();
                expected = value != null && value.GetType() == nonNullable;
            }

            var param = Expression.Parameter(expression.Type);

            Func<bool> func = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    new[] { param },
                    Expression.Assign(param, expression),
                    Expression.TypeEqual(param, type)
                    )
                ).Compile(false);

            Assert.Equal(expected, func());
        }

        [Theory]
        [MemberData("ExpressionAndTypeCombinations")]
        public void ExpressionEvaluationWithParameterInterpretted(Expression expression, Type type)
        {
            if (expression.Type == typeof(void))
                return; // Can't have void parameter.
            bool expected;
            if (type == typeof(void))
                expected = false;
            else
            {
                Type nonNullable = type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    ? type.GetGenericArguments()[0]
                    : type;
                object value = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()();
                expected = value != null && value.GetType() == nonNullable;
            }

            var param = Expression.Parameter(expression.Type);

            Func<bool> func = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    new[] { param },
                    Expression.Assign(param, expression),
                    Expression.TypeEqual(param, type)
                    )
                ).Compile(true);

            Assert.Equal(expected, func());
        }

        [Fact]
        public void UpdateSameReturnsSame()
        {
            Expression expression = Expression.Constant(0);
            TypeBinaryExpression typeExp = Expression.TypeEqual(expression, typeof(int));
            Assert.Same(typeExp, typeExp.Update(expression));
        }

        [Fact]
        public void UpdateNotSameReturnsNotSame()
        {
            Expression expression = Expression.Constant(0);
            TypeBinaryExpression typeExp = Expression.TypeEqual(expression, typeof(int));
            Assert.NotSame(typeExp, typeExp.Update(Expression.Constant(0)));
        }

        [Fact]
        public void VisitHitsVisitTypeBinary()
        {
            TypeBinaryExpression expression = Expression.TypeEqual(Expression.Constant(0), typeof(int));
            TypeBinaryVisitCheckingVisitor visitor = new TypeBinaryVisitCheckingVisitor();
            visitor.Visit(expression);
            Assert.Same(expression, visitor.LastTypeBinaryVisited);
        }
    }
}
