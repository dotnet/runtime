// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class LabelTargetTests
    {
        // The actual use of label targets when compiling, interpreting or otherwise acting upon an expression
        // that makes use of them is by necessity covered by testing those GotoExpressions that make use of them.
        // These tests focus on the LabelTarget class and the factory methods producing them, with compilation
        // only when some feature of a target itself (viz. a name that is not a valid C# name is still valid)
        // could perhaps have an effect in a regression error.

        private class CustomException : Exception
        {
            public CustomException()
                : base("This is a custom exception that exists just to distinguish throwing it in a test from any other cause of exceptions being thrown.")
            {
            }
        }

        [Fact]
        public void FactoryProducesUniqueTargets()
        {
            Assert.NotSame(Expression.Label("name"), Expression.Label("name"));
            Assert.NotSame(Expression.Label(), Expression.Label());
            Assert.NotSame(Expression.Label(typeof(int)), Expression.Label(typeof(int)));
            Assert.NotSame(Expression.Label(typeof(int), "name"), Expression.Label(typeof(int), "name"));
        }

        [Fact]
        public void TypeRetained()
        {
            Assert.Equal(typeof(void), Expression.Label().Type);
            Assert.Equal(typeof(void), Expression.Label("name").Type);
            Assert.Equal(typeof(int), Expression.Label(typeof(int)).Type);
            Assert.Equal(typeof(int), Expression.Label(typeof(int), "name").Type);
            Assert.Equal(typeof(void), Expression.Label(typeof(void)).Type);
            Assert.Equal(typeof(void), Expression.Label(typeof(void), "name").Type);
        }

        [Fact]
        public void NameRetained()
        {
            Assert.Null(Expression.Label().Name);
            Assert.Null(Expression.Label(typeof(int)).Name);
            Assert.Equal("name", Expression.Label("name").Name);
            Assert.Equal("name", Expression.Label(typeof(int), "name").Name);
        }

        [Fact]
        public void ExplicitNullName()
        {
            Assert.Null(Expression.Label(default(string)).Name);
            Assert.Null(Expression.Label(typeof(int), null).Name);
        }

        [Fact]
        public void NullType()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Label(default(Type)));
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Label(null, "name"));
        }

        [Fact]
        public void GenericType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Label(typeof(List<>)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Label(typeof(List<>), null));
        }

        [Fact]
        public void TypeWithGenericParamters()
        {
            Type listType = typeof(List<>);
            Type listListListType = listType.MakeGenericType(listType.MakeGenericType(listType));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Label(listListListType));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Label(listListListType, null));
        }

        [Fact]
        public void PointerType()
        {
            Type pointerType = typeof(int).MakePointerType();
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Label(pointerType));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Label(pointerType, null));
        }

        [Fact]
        public void ByRefType()
        {
            Type byRefType = typeof(int).MakeByRefType();
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Label(byRefType));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Label(byRefType, null));
        }

        [Fact]
        public void NameIsStringRepresentation()
        {
            Assert.Equal("name", Expression.Label("name").ToString());
            Assert.Equal("name", Expression.Label(typeof(int), "name").ToString());
            Assert.Equal(" ", Expression.Label(" ").ToString());
            Assert.Equal(" ", Expression.Label(typeof(int), " ").ToString());
        }

        [Fact]
        public void NameForUnnamedLabel()
        {
            Assert.Equal("UnamedLabel", Expression.Label().ToString());
            Assert.Equal("UnamedLabel", Expression.Label(typeof(int)).ToString());
            Assert.Equal("UnamedLabel", Expression.Label("").ToString());
            Assert.Equal("UnamedLabel", Expression.Label(typeof(int), "").ToString());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void LableNameNeedNotBeValidCSharpLabel(bool useInterpreter)
        {
            LabelTarget target = Expression.Label("1, 2, 3, 4. This is not a valid C♯ label!\"'<>.\uffff");
            Expression.Lambda<Action>(
                Expression.Block(
                    Expression.Goto(target),
                    Expression.Throw(Expression.Constant(new CustomException())),
                    Expression.Label(target)
                    )
                ).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void LableNameNeedNotBeValidCSharpLabelWithValue(bool useInterpreter)
        {
            LabelTarget target = Expression.Label(typeof(int), "1, 2, 3, 4. This is not a valid C♯ label!\"'<>.\uffff");
            Func<int> func = Expression.Lambda<Func<int>>(
                Expression.Block(
                    Expression.Return(target, Expression.Constant(42)),
                    Expression.Throw(Expression.Constant(new CustomException())),
                    Expression.Label(target, Expression.Default(typeof(int)))
                    )
                ).Compile(useInterpreter);
            Assert.Equal(42, func());
        }
    }
}
