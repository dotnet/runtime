// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ParameterTests : ParameterExpressionTests
    {
        [Theory]
        [MemberData(nameof(ValidTypeData))]
        public void CreateParameterForValidTypeNoName(Type type)
        {
            ParameterExpression param = Expression.Parameter(type);
            Assert.Equal(type, param.Type);
            Assert.False(param.IsByRef);
            Assert.Null(param.Name);
        }

        [Theory]
        [MemberData(nameof(ValidTypeData))]
        public void CrateParamForValidTypeWithName(Type type)
        {
            ParameterExpression param = Expression.Parameter(type, "name");
            Assert.Equal(type, param.Type);
            Assert.False(param.IsByRef);
            Assert.Equal("name", param.Name);
        }

        [Fact]
        public void NameNeedNotBeCSharpValid()
        {
            ParameterExpression param = Expression.Parameter(typeof(int), "a name with charcters not allowed in C# <, >, !, =, \0, \uFFFF, &c.");
            Assert.Equal("a name with charcters not allowed in C# <, >, !, =, \0, \uFFFF, &c.", param.Name);
        }

        [Fact]
        public void ParameterCannotBeTypeVoid()
        {
            Assert.Throws<ArgumentException>(() => Expression.Parameter(typeof(void)));
            Assert.Throws<ArgumentException>(() => Expression.Parameter(typeof(void), "var"));
        }

        [Fact]
        public void NullType()
        {
            Assert.Throws<ArgumentNullException>("type", () => Expression.Parameter(null));
            Assert.Throws<ArgumentNullException>("type", () => Expression.Parameter(null, "var"));
        }

        [Theory]
        [MemberData(nameof(ByRefTypeData))]
        public void ParameterCanBeByRef(Type type)
        {
            ParameterExpression param = Expression.Parameter(type);
            Assert.Equal(type.GetElementType(), param.Type);
            Assert.True(param.IsByRef);
            Assert.Null(param.Name);
        }

        [Theory]
        [MemberData(nameof(ByRefTypeData))]
        public void NamedParameterCanBeByRef(Type type)
        {
            ParameterExpression param = Expression.Parameter(type, "name");
            Assert.Equal(type.GetElementType(), param.Type);
            Assert.True(param.IsByRef);
            Assert.Equal("name", param.Name);
        }

        [Theory]
        [MemberData(nameof(ValueData))]
        public void CanWriteAndReadBack(object value)
        {
            Type type = value.GetType();
            ParameterExpression param = Expression.Parameter(type);
            Assert.True(
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(value),
                        Expression.Block(
                            type,
                            new[] { param },
                            Expression.Assign(param, Expression.Constant(value)),
                            param
                            )
                        )
                    ).Compile()()
                );
        }

        [Fact]
        public void CanUseAsLambdaParameter()
        {
            ParameterExpression param = Expression.Parameter(typeof(int));
            Func<int, int> addOne = Expression.Lambda<Func<int, int>>(
                Expression.Add(param, Expression.Constant(1)),
                param
                ).Compile();
            Assert.Equal(3, addOne(2));
        }

        public delegate void ByRefFunc<T>(ref T arg);

        [Fact]
        public void CanUseAsLambdaByRefParameter()
        {
            ParameterExpression param = Expression.Parameter(typeof(int).MakeByRefType());
            ByRefFunc<int> addOneInPlace = Expression.Lambda<ByRefFunc<int>>(
                Expression.PreIncrementAssign(param),
                param
                ).Compile();
            int argument = 5;
            addOneInPlace(ref argument);
            Assert.Equal(6, argument);
        }

        [Fact]
        public void CannotReduce()
        {
            ParameterExpression param = Expression.Parameter(typeof(int));
            Assert.False(param.CanReduce);
            Assert.Same(param, param.Reduce());
            Assert.Throws<ArgumentException>(() => param.ReduceAndCheck());
        }
    }
}
