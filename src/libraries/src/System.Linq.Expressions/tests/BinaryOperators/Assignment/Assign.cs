// Copyright (c) Jon Hanna. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class Assign
    {
        [Fact]
        public void SimpleAssignment()
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            LabelTarget target = Expression.Label(typeof(int));
            Expression exp = Expression.Block(
                new ParameterExpression[] { variable },
                Expression.Assign(variable, Expression.Constant(42)),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(int)))
                );
            Assert.Equal(42, Expression.Lambda<Func<int>>(exp).Compile()());
        }

        [Fact]
        public void AssignmentHasValueItself()
        {
            ParameterExpression variable = Expression.Variable(typeof(int));
            Expression exp = Expression.Block(
                new ParameterExpression[] { variable },
                Expression.Assign(variable, Expression.Constant(42))
                );
            Assert.Equal(42, Expression.Lambda<Func<int>>(exp).Compile()());
        }

        [Fact]
        public void CannotReduce()
        {
            Expression exp = Expression.Assign(Expression.Variable(typeof(int)), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public void ThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.Assign(null, Expression.Constant("")));
        }

        [Fact]
        public void ThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.Assign(Expression.Variable(typeof(int)), null));
        }

        [Fact]
        public void LeftMustBeWritable()
        {
            Assert.Throws<ArgumentException>(() => Expression.Assign(Expression.Constant(0), Expression.Constant(1)));
        }

        [Fact]
        public void MismatchTypes()
        {
            Assert.Throws<ArgumentException>(() => Expression.Assign(Expression.Variable(typeof(int)), Expression.Constant("Hello")));
        }

        [Fact]
        public void AssignableButOnlyWithConversion()
        {
            Assert.Throws<ArgumentException>(() => Expression.Assign(Expression.Variable(typeof(long)), Expression.Constant(1)));
        }

        [Fact]
        public void ReferenceAssignable()
        {
            ParameterExpression variable = Expression.Variable(typeof(object));
            LabelTarget target = Expression.Label(typeof(object));
            Expression exp = Expression.Block(
                new ParameterExpression[] { variable },
                Expression.Assign(variable, Expression.Constant("Hello")),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(object)))
                );
            Assert.Equal("Hello", Expression.Lambda<Func<object>>(exp).Compile()());
        }

        [Fact]
        public void AttemptToAssignToNonWritable()
        {
            Assert.Throws<ArgumentException>(() => Expression.Assign(Expression.Default(typeof(int)), Expression.Default(typeof(int))));
        }
        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public static void AttemptToAssignFromNonReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            ParameterExpression variable = Expression.Variable(typeof(int));
            Assert.Throws<ArgumentException>("right", () => Expression.Assign(variable, value));
        }
    }
}
