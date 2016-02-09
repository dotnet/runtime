// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ElementInitTests
    {
        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        private class ParameterlessAdd
        {
            public void Add()
            {
            }
        }

        private class StaticAdd
        {
            public static void Add(int value)
            {
            }
        }

        private class ByRefAdd
        {
            public static void Add(ref int value)
            {
            }
        }

        private class GenericAdd
        {
            public static void Add<T>(T value)
            {
            }
        }

        [Fact]
        public void NullAddMethod()
        {
            Assert.Throws<ArgumentNullException>("addMethod", () => Expression.ElementInit(null, Expression.Constant(0)));
            Assert.Throws<ArgumentNullException>("addMethod", () => Expression.ElementInit(null, Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void NullArguments()
        {
            Assert.Throws<ArgumentNullException>("arguments", () => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), default(Expression[])));
            Assert.Throws<ArgumentNullException>("arguments", () => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), default(Expression[])));
        }

        [Fact]
        public void NoArguments()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Add")));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Enumerable.Empty<Expression>()));
        }

        [Fact]
        public void ArgumentCountWrong()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Expression.Constant(0), Expression.Constant(1)));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Enumerable.Repeat(Expression.Constant(0), 2)));
        }

        [Fact]
        public void ArgumentTypeMisMatch()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Expression.Constant("Hello")));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Enumerable.Repeat(Expression.Constant("Hello"), 1)));
        }

        [Fact]
        public void UnreadableArgument()
        {
            Expression argument = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), argument));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Enumerable.Repeat(argument, 1)));
        }

        [Fact]
        public void ParameterlessAddProhibited()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(ParameterlessAdd).GetMethod("Add"), Expression.Constant(0)));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(ParameterlessAdd).GetMethod("Add"), Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void StaticAddProhibited()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(StaticAdd).GetMethod("Add"), Expression.Constant(0)));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(StaticAdd).GetMethod("Add"), Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void ByRefAddProhibited()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(ByRefAdd).GetMethod("Add"), Expression.Constant(0)));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(ByRefAdd).GetMethod("Add"), Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void GenericAddProhibited()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(GenericAdd).GetMethod("Add"), Expression.Constant(0)));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(GenericAdd).GetMethod("Add"), Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void GenericParameterAddProhibited()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<>).GetMethod("Add"), Expression.Constant(0)));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<>).GetMethod("Add"), Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void AddMethodNotCalledAdd()
        {
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Remove"), Expression.Constant(0)));
            Assert.Throws<ArgumentException>(() => Expression.ElementInit(typeof(List<int>).GetMethod("Remove"), Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void UpdateSameArgumentsSameInstanceReturned()
        {
            ElementInit init = Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Expression.Constant(0));
            IEnumerable<Expression> arguments = init.Arguments;
            Assert.Same(init, init.Update(arguments));
        }

        [Fact]
        public void UpdateDifferentArgumentsDifferetInstanceReturned()
        {
            ElementInit init = Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Expression.Constant(0));
            Assert.NotSame(init, init.Update(Enumerable.Repeat(Expression.Constant(1), 1)));
        }

        [Fact]
        public void UpdateDifferentNumberArgumentsDifferetInstanceReturned()
        {
            ElementInit init = Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Expression.Constant(0));
            Assert.NotSame(init, init.Update(Enumerable.Repeat(Expression.Constant(1), 1)));
        }

        [Fact]
        public void CanRetrieveMethod()
        {
            ElementInit init = Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Expression.Constant(0));
            Assert.Equal(typeof(List<int>).GetMethod("Add"), init.AddMethod);
        }

        [Fact]
        public void CanAccessArguments()
        {
            Expression key = Expression.Constant("Key");
            Expression value = Expression.Constant(42);
            ElementInit init = Expression.ElementInit(typeof(Dictionary<string, int>).GetMethod("Add"), key, value);
            Assert.Equal(2, init.ArgumentCount);
            Assert.Same(key, init.GetArgument(0));
            Assert.Same(value, init.GetArgument(1));
            Assert.Equal(new[] { key, value }, init.Arguments);
        }

        [Fact]
        public void InvalidArgumentIndex()
        {
            Expression key = Expression.Constant("Key");
            Expression value = Expression.Constant(42);
            ElementInit init = Expression.ElementInit(typeof(Dictionary<string, int>).GetMethod("Add"), key, value);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => init.GetArgument(-1));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => init.GetArgument(2));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => init.GetArgument(3));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => init.GetArgument(int.MaxValue));
        }

        [Fact]
        public void ToStringShowsArguments()
        {
            ElementInit init = Expression.ElementInit(
                typeof(Dictionary<string, int>).GetMethod("Add"),
                Expression.Constant("Key"),
                Expression.Constant(42)
                );
            Assert.Equal(typeof(Dictionary<string, int>).GetMethod("Add").ToString() + "(\"Key\", 42)", init.ToString());
        }
    }
}
