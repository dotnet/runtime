// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class TypeEqual : TypeBinaryTests
    {
        [Fact]
        public void NullExpression()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.TypeEqual(null, typeof(int)));
        }

        [Fact]
        public void NullType()
        {
            Expression exp = Expression.Constant(0);
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.TypeEqual(exp, null));
        }

        [Fact]
        public void TypeByRef()
        {
            Expression exp = Expression.Constant(0);
            Type byRef = typeof(int).MakeByRefType();
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.TypeEqual(exp, byRef));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void TypePointer(bool useInterpreter)
        {
            Expression exp = Expression.Constant(0);
            Type pointer = typeof(int*);
            var test = Expression.TypeEqual(exp, pointer);
            var lambda = Expression.Lambda<Func<bool>>(test);
            var func = lambda.Compile(useInterpreter);
            Assert.False(func());
        }

        [Fact]
        public void UnreadableExpression()
        {
            Expression exp = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.TypeEqual(exp, typeof(int)));
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
        public void TypePropertyMatches(Expression expression, Type type)
        {
            Assert.Equal(type, Expression.TypeEqual(expression, type).TypeOperand);
        }

        [Theory]
        [MemberData(nameof(ExpressionAndTypeCombinations))]
        public void TypeIsBoolean(Expression expression, Type type)
        {
            Assert.Equal(typeof(bool), Expression.TypeEqual(expression, type).Type);
        }

        [Theory]
        [MemberData(nameof(ExpressionAndTypeCombinations))]
        public void NodeType(Expression expression, Type type)
        {
            Assert.Equal(ExpressionType.TypeEqual, Expression.TypeEqual(expression, type).NodeType);
        }

        [Theory]
        [MemberData(nameof(ExpressionAndTypeCombinations))]
        public void ExpressionIsThatPassed(Expression expression, Type type)
        {
            Assert.Same(expression, Expression.TypeEqual(expression, type).Expression);
        }

        [Theory]
        [PerCompilationType(nameof(ExpressionAndTypeCombinations))]
        public void ExpressionEvaluation(Expression expression, Type type, bool useInterpreter)
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

            Assert.Equal(expected, Expression.Lambda<Func<bool>>(Expression.TypeEqual(expression, type)).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ExpressionAndTypeCombinations))]
        public void ExpressionEvaluationWithParameter(Expression expression, Type type, bool useInterpreter)
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

            ParameterExpression param = Expression.Parameter(expression.Type);

            Func<bool> func = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    new[] { param },
                    Expression.Assign(param, expression),
                    Expression.TypeEqual(param, type)
                    )
                ).Compile(useInterpreter);

            Assert.Equal(expected, func());
        }

        [Fact]
        public void UpdateSameReturnsSame()
        {
            Expression expression = Expression.Constant(0);
            TypeBinaryExpression typeExp = Expression.TypeEqual(expression, typeof(int));
            Assert.Same(typeExp, typeExp.Update(expression));
            Assert.Same(typeExp, NoOpVisitor.Instance.Visit(typeExp));
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

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void VariantDelegateArgument(bool useInterpreter)
        {
            Action<object> ao = x => { };
            Action<string> a = x => { };
            Action<string> b = ao;

            ParameterExpression param = Expression.Parameter(typeof(Action<string>));

            Func<Action<string>, bool> isActStr = Expression.Lambda<Func<Action<string>, bool>>(
                Expression.TypeEqual(param, typeof(Action<string>)),
                param
            ).Compile(useInterpreter);

            Assert.False(isActStr(ao));
            Assert.True(isActStr(a));
            Assert.False(isActStr(b));
        }

        [Theory, PerCompilationType(nameof(TypeArguments))]
        public void TypeEqualConstant(Type type, bool useInterpreter)
        {
            Func<bool> isNullOfType = Expression.Lambda<Func<bool>>(
                Expression.TypeEqual(Expression.Constant(null), type)
                ).Compile(useInterpreter);
            Assert.False(isNullOfType());

            isNullOfType = Expression.Lambda<Func<bool>>(
                Expression.TypeEqual(Expression.Constant(null, typeof(string)), type)
                ).Compile(useInterpreter);

            Assert.False(isNullOfType());
        }

        [Fact]
        public void ToStringTest()
        {
            TypeBinaryExpression e = Expression.TypeEqual(Expression.Parameter(typeof(string), "s"), typeof(string));
            Assert.Equal("(s TypeEqual String)", e.ToString());
        }
    }
}
