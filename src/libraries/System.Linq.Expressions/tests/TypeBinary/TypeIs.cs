// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class TypeIs : TypeBinaryTests
    {
        [Fact]
        public void NullExpression()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.TypeIs(null, typeof(int)));
        }

        [Fact]
        public void NullType()
        {
            Expression exp = Expression.Constant(0);
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.TypeIs(exp, null));
        }

        [Fact]
        public void TypeByRef()
        {
            Expression exp = Expression.Constant(0);
            Type byRef = typeof(int).MakeByRefType();
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.TypeIs(exp, byRef));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void TypePointer(bool useInterpreter)
        {
            Expression exp = Expression.Constant(0);
            Type pointer = typeof(int*);
            var test = Expression.TypeIs(exp, pointer);
            var lambda = Expression.Lambda<Func<bool>>(test);
            var func = lambda.Compile(useInterpreter);
            Assert.False(func());
        }

        [Fact]
        public void UnreadableExpression()
        {
            Expression exp = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.TypeIs(exp, typeof(int)));
        }

        [Fact]
        public void CannotReduce()
        {
            Expression exp = Expression.TypeIs(Expression.Constant(0), typeof(int));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Theory]
        [MemberData(nameof(ExpressionAndTypeCombinations))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51952", TestPlatforms.tvOS)]
        public void TypePropertyMatches(Expression expression, Type type)
        {
            Assert.Equal(type, Expression.TypeIs(expression, type).TypeOperand);
        }

        [Theory]
        [MemberData(nameof(ExpressionAndTypeCombinations))]
        public void TypeIsBoolean(Expression expression, Type type)
        {
            Assert.Equal(typeof(bool), Expression.TypeIs(expression, type).Type);
        }

        [Theory]
        [MemberData(nameof(ExpressionAndTypeCombinations))]
        public void NodeType(Expression expression, Type type)
        {
            Assert.Equal(ExpressionType.TypeIs, Expression.TypeIs(expression, type).NodeType);
        }

        [Theory]
        [MemberData(nameof(ExpressionAndTypeCombinations))]
        public void ExpressionIsThatPassed(Expression expression, Type type)
        {
            Assert.Same(expression, Expression.TypeIs(expression, type).Expression);
        }

        [Theory]
        [PerCompilationType(nameof(ExpressionAndTypeCombinations))]
        public void ExpressionEvaluationCompiled(Expression expression, Type type, bool useInterpreter)
        {
            bool expected = expression.Type == typeof(void)
                ? type == typeof(void)
                : type.IsInstanceOfType(Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()());

            Assert.Equal(expected, Expression.Lambda<Func<bool>>(Expression.TypeIs(expression, type)).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ExpressionAndTypeCombinations))]
        public void ExpressionEvaluationWithParameter(Expression expression, Type type, bool useInterpreter)
        {
            if (expression.Type == typeof(void))
                return; // Can't have void parameter.

            bool expected = expression.Type == typeof(void)
                ? type == typeof(void)
                : type.IsInstanceOfType(Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object))).Compile()());

            ParameterExpression param = Expression.Parameter(expression.Type);

            Func<bool> func = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    new[] { param },
                    Expression.Assign(param, expression),
                    Expression.TypeIs(param, type)
                    )
                ).Compile(useInterpreter);

            Assert.Equal(expected, func());
        }

        [Fact]
        public void UpdateSameReturnsSame()
        {
            Expression expression = Expression.Constant(0);
            TypeBinaryExpression typeExp = Expression.TypeIs(expression, typeof(int));
            Assert.Same(typeExp, typeExp.Update(expression));
            Assert.Same(typeExp, NoOpVisitor.Instance.Visit(typeExp));
        }

        [Fact]
        public void UpdateNotSameReturnsNotSame()
        {
            Expression expression = Expression.Constant(0);
            TypeBinaryExpression typeExp = Expression.TypeIs(expression, typeof(int));
            Assert.NotSame(typeExp, typeExp.Update(Expression.Constant(0)));
        }

        [Fact]
        public void VisitHitsVisitTypeBinary()
        {
            TypeBinaryExpression expression = Expression.TypeIs(Expression.Constant(0), typeof(int));
            TypeBinaryVisitCheckingVisitor visitor = new TypeBinaryVisitCheckingVisitor();
            visitor.Visit(expression);
            Assert.Same(expression, visitor.LastTypeBinaryVisited);
        }

        [Fact]
        public void ToStringTest()
        {
            TypeBinaryExpression e = Expression.TypeIs(Expression.Parameter(typeof(object), "o"), typeof(string));
            Assert.Equal("(o Is String)", e.ToString());
        }
    }
}
