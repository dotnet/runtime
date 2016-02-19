// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class VariableTests : ParameterExpressionTests
    {
        [Theory]
        [MemberData(nameof(ValidTypeData))]
        public void CreateVariableForValidTypeNoName(Type type)
        {
            ParameterExpression variable = Expression.Variable(type);
            Assert.Equal(type, variable.Type);
            Assert.False(variable.IsByRef);
            Assert.Null(variable.Name);
        }

        [Theory]
        [MemberData(nameof(ValidTypeData))]
        public void CrateVariableForValidTypeWithName(Type type)
        {
            ParameterExpression variable = Expression.Variable(type, "name");
            Assert.Equal(type, variable.Type);
            Assert.False(variable.IsByRef);
            Assert.Equal("name", variable.Name);
        }

        [Fact]
        public void NameNeedNotBeCSharpValid()
        {
            ParameterExpression variable = Expression.Variable(typeof(int), "a name with charcters not allowed in C# <, >, !, =, \0, \uFFFF, &c.");
            Assert.Equal("a name with charcters not allowed in C# <, >, !, =, \0, \uFFFF, &c.", variable.Name);
        }

        [Fact]
        public void VariableCannotBeTypeVoid()
        {
            Assert.Throws<ArgumentException>(() => Expression.Variable(typeof(void)));
            Assert.Throws<ArgumentException>(() => Expression.Variable(typeof(void), "var"));
        }

        [Fact]
        public void NullType()
        {
            Assert.Throws<ArgumentNullException>("type", () => Expression.Variable(null));
            Assert.Throws<ArgumentNullException>("type", () => Expression.Variable(null, "var"));
        }

        [Theory]
        [MemberData(nameof(ByRefTypeData))]
        public void VariableCannotBeByRef(Type type)
        {
            Assert.Throws<ArgumentException>(() => Expression.Variable(type));
            Assert.Throws<ArgumentException>(() => Expression.Variable(type, "var"));
        }

        [Theory]
        [MemberData(nameof(ValueData))]
        public void CanWriteAndReadBack(object value)
        {
            Type type = value.GetType();
            ParameterExpression variable = Expression.Variable(type);
            Assert.True(
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(value),
                        Expression.Block(
                            type,
                            new[] { variable },
                            Expression.Assign(variable, Expression.Constant(value)),
                            variable
                            )
                        )
                    ).Compile()()
                );
        }

        [Fact]
        public void CanUseAsLambdaParameter()
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            Func<int, int> addOne = Expression.Lambda<Func<int, int>>(
                Expression.Add(variable, Expression.Constant(1)),
                variable
                ).Compile();
            Assert.Equal(3, addOne(2));
        }

        [Fact]
        public void CannotReduce()
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            Assert.False(variable.CanReduce);
            Assert.Same(variable, variable.Reduce());
            Assert.Throws<ArgumentException>(() => variable.ReduceAndCheck());
        }
    }
}
