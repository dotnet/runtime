// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            ParameterExpression variable = Expression.Variable(typeof(int), "a name with characters not allowed in C# <, >, !, =, \0, \uFFFF, &c.");
            Assert.Equal("a name with characters not allowed in C# <, >, !, =, \0, \uFFFF, &c.", variable.Name);
        }

        [Fact]
        public void VariableCannotBeTypeVoid()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Variable(typeof(void)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Variable(typeof(void), "var"));
        }

        [Fact]
        public void NullType()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Variable(null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Variable(null, "var"));
        }

        [Theory]
        [MemberData(nameof(ByRefTypeData))]
        public void VariableCannotBeByRef(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Variable(type));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Variable(type, "var"));
        }

        [Theory]
        [PerCompilationType(nameof(ValueData))]
        public void CanWriteAndReadBack(object value, bool useInterpreter)
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
                    ).Compile(useInterpreter)()
                );
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanUseAsLambdaParameter(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            Func<int, int> addOne = Expression.Lambda<Func<int, int>>(
                Expression.Add(variable, Expression.Constant(1)),
                variable
                ).Compile(useInterpreter);
            Assert.Equal(3, addOne(2));
        }

        [Fact]
        public void CannotReduce()
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            Assert.False(variable.CanReduce);
            Assert.Same(variable, variable.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => variable.ReduceAndCheck());
        }

        [Theory]
        [ClassData(typeof(InvalidTypesData))]
        public void OpenGenericType_ThrowsArgumentException(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Variable(type));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Variable(type, "name"));
        }

        [Fact]
        public void CannotBePointerType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Variable(typeof(int*)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Variable(typeof(int*), "pointer"));
        }
    }
}
