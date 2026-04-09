// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class RuntimeVariablesTests
    {
        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ReadAndReturnVars(bool useInterpreter)
        {
            ParameterExpression x = Expression.Variable(typeof(int));
            ParameterExpression y = Expression.Variable(typeof(decimal));
            ParameterExpression z = Expression.Variable(typeof(string));
            IRuntimeVariables vars = Expression.Lambda<Func<IRuntimeVariables>>(
                Expression.Block(
                    new[] { x, y, z },
                    Expression.Assign(x, Expression.Constant(12)),
                    Expression.Assign(y, Expression.Constant(34m)),
                    Expression.Assign(z, Expression.Constant("hello")),
                    Expression.RuntimeVariables(x, y, z)
                    )
                ).Compile(useInterpreter)();
            Assert.Equal(3, vars.Count);
            Assert.Equal(12, vars[0]);
            Assert.Equal(34m, vars[1]);
            Assert.Equal("hello", vars[2]);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void IRuntimeVariablesListChecksBounds(bool useInterpreter)
        {
            ParameterExpression x = Expression.Variable(typeof(int));
            ParameterExpression y = Expression.Variable(typeof(int));
            IRuntimeVariables vars = Expression.Lambda<Func<IRuntimeVariables>>(
                Expression.Block(
                    new[] { x, y },
                    Expression.RuntimeVariables(x, y)
                    )
                ).Compile(useInterpreter)();
            Assert.Equal(2, vars.Count);
            Assert.Throws<IndexOutOfRangeException>(() => vars[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => vars[-1] = null);
            Assert.Throws<IndexOutOfRangeException>(() => vars[2]);
            Assert.Throws<IndexOutOfRangeException>(() => vars[2] = null);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ReadAndWriteVars(bool useInterpreter)
        {
            ParameterExpression x = Expression.Variable(typeof(int));
            ParameterExpression y = Expression.Variable(typeof(decimal));
            ParameterExpression z = Expression.Variable(typeof(string));
            ParameterExpression r = Expression.Variable(typeof(IRuntimeVariables));
            ParameterExpression b = Expression.Variable(typeof(bool));
            Assert.True(Expression.Lambda<Func<bool>>(
                Expression.Block(
                    new[] { x, y, z, r, b },
                    Expression.Assign(x, Expression.Constant(45)),
                    Expression.Assign(y, Expression.Constant(98.01m)),
                    Expression.Assign(z, Expression.Constant("In fair Verona, where we lay our scene,")),
                    Expression.Assign(r, Expression.RuntimeVariables(x, y, z)),
                    Expression.Assign(b, Expression.Equal(Expression.Constant(45), Expression.Convert(Expression.Property(r, "Item", Expression.Constant(0)), typeof(int)))),
                    Expression.AndAssign(b, Expression.Equal(Expression.Constant(98.01m), Expression.Convert(Expression.Property(r, "Item", Expression.Constant(1)), typeof(decimal)))),
                    Expression.AndAssign(b, Expression.Equal(Expression.Constant("In fair Verona, where we lay our scene,"), Expression.Convert(Expression.Property(r, "Item", Expression.Constant(2)), typeof(string)))),
                    Expression.Assign(Expression.Property(r, "Item", Expression.Constant(0)), Expression.Constant(988, typeof(object))),
                    Expression.Assign(Expression.Property(r, "Item", Expression.Constant(1)), Expression.Constant(0.01m, typeof(object))),
                    Expression.Assign(Expression.Property(r, "Item", Expression.Constant(2)), Expression.Constant("Where civil blood makes civil hands unclean.", typeof(object))),
                    Expression.AndAssign(b, Expression.Equal(Expression.Convert(x, typeof(int)), Expression.Constant(988))),
                    Expression.AndAssign(b, Expression.Equal(Expression.Convert(y, typeof(decimal)), Expression.Constant(0.01m))),
                    Expression.AndAssign(b, Expression.Equal(Expression.Convert(z, typeof(string)), Expression.Constant("Where civil blood makes civil hands unclean."))),
                    b
                    )
                ).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void AliasingAllowed(bool useInterpreter)
        {
            ParameterExpression x = Expression.Variable(typeof(int));
            ParameterExpression r = Expression.Variable(typeof(IRuntimeVariables));
            Assert.Equal(15, Expression.Lambda<Func<int>>(
                Expression.Block(
                    new[] { x, r },
                    Expression.Assign(x, Expression.Constant(8)),
                    Expression.Assign(r, Expression.RuntimeVariables(x, x)),
                    Expression.Assign(
                        Expression.Property(r, "Item", Expression.Constant(1)),
                        Expression.Convert(
                            Expression.Add(
                                Expression.Constant(7),
                                Expression.Convert(Expression.Property(r, "Item", Expression.Constant(0)), typeof(int))
                                ),
                            typeof(object))
                        ),
                    x
                    )
                ).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void MixedScope(bool useInterpreter)
        {
            ParameterExpression x = Expression.Variable(typeof(int));
            ParameterExpression y = Expression.Variable(typeof(int));
            IRuntimeVariables vars = Expression.Lambda<Func<IRuntimeVariables>>(
                Expression.Block(
                    new[] { x },
                    Expression.Assign(x, Expression.Constant(3)),
                    Expression.Block(
                        new[] { y },
                        Expression.Assign(y, Expression.Constant(19)),
                        Expression.RuntimeVariables(x, y)
                        )
                    )
                ).Compile(useInterpreter)();
            Assert.Equal(3, vars[0]);
            Assert.Equal(19, vars[1]);
        }

        [Fact]
        public void NullVariableList()
        {
            AssertExtensions.Throws<ArgumentNullException>("variables", () => Expression.RuntimeVariables(default(ParameterExpression[])));
            AssertExtensions.Throws<ArgumentNullException>("variables", () => Expression.RuntimeVariables(default(IEnumerable<ParameterExpression>)));
        }

        [Fact]
        public void NullVariableInList()
        {
            AssertExtensions.Throws<ArgumentNullException>("variables[1]", () => Expression.RuntimeVariables(Expression.Variable(typeof(int)), null));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ZeroVariables(bool useInterpreter)
        {
            IRuntimeVariables vars = Expression.Lambda<Func<IRuntimeVariables>>(Expression.RuntimeVariables()).Compile(useInterpreter)();
            Assert.Equal(0, vars.Count);
            Assert.Throws<IndexOutOfRangeException>(() => vars[0]);
            Assert.Throws<IndexOutOfRangeException>(() => vars[0] = null);
        }

        [Fact]
        public void CannotReduce()
        {
            RuntimeVariablesExpression vars = Expression.RuntimeVariables(Expression.Variable(typeof(int)));
            Assert.False(vars.CanReduce);
            Assert.Same(vars, vars.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => vars.ReduceAndCheck());
        }

        [Fact]
        public void UpdateSameCollectionSameNode()
        {
            ParameterExpression[] variables = {Expression.Variable(typeof(RuntimeVariablesTests))};
            RuntimeVariablesExpression varExp = Expression.RuntimeVariables(variables);
            Assert.Same(varExp, varExp.Update(variables));
            Assert.Same(varExp, varExp.Update(varExp.Variables));
            Assert.Same(varExp, NoOpVisitor.Instance.Visit(varExp));
        }

        [Fact]
        public void UpdateDiffVarsDiffNode()
        {
            RuntimeVariablesExpression varExp = Expression.RuntimeVariables(Enumerable.Repeat(Expression.Variable(typeof(RuntimeVariablesTests)), 1));
            Assert.NotSame(varExp, varExp.Update(new[] { Expression.Variable(typeof(RuntimeVariablesTests)) }));
        }


        [Fact]
        public void UpdateDoesntRepeatEnumeration()
        {
            RuntimeVariablesExpression varExp = Expression.RuntimeVariables(Enumerable.Repeat(Expression.Variable(typeof(RuntimeVariablesTests)), 1));
            Assert.NotSame(varExp, varExp.Update(new RunOnceEnumerable<ParameterExpression>(new[] { Expression.Variable(typeof(RuntimeVariablesTests)) })));
        }

        [Fact]
        public void UpdateNullThrows()
        {
            RuntimeVariablesExpression varExp = Expression.RuntimeVariables(Enumerable.Repeat(Expression.Variable(typeof(RuntimeVariablesTests)), 0));
            AssertExtensions.Throws<ArgumentNullException>("variables", () => varExp.Update(null));
        }

        [Fact]
        public void ToStringTest()
        {
            RuntimeVariablesExpression e1 = Expression.RuntimeVariables();
            Assert.Equal("()", e1.ToString());

            RuntimeVariablesExpression e2 = Expression.RuntimeVariables(Expression.Parameter(typeof(int), "x"));
            Assert.Equal("(x)", e2.ToString());

            RuntimeVariablesExpression e3 = Expression.RuntimeVariables(Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"));
            Assert.Equal("(x, y)", e3.ToString());
        }
    }
}
