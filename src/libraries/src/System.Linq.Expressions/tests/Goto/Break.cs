// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class Break : GotoExpressionTests
    {
        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void JustBreakValue(object value)
        {
            Type type = value.GetType();
            LabelTarget target = Expression.Label(type);
            Expression block = Expression.Block(
                Expression.Break(target, Expression.Constant(value)),
                Expression.Label(target, Expression.Default(type))
                );
            Expression equals = Expression.Equal(Expression.Constant(value), block);
            Assert.True(Expression.Lambda<Func<bool>>(equals).Compile()());
        }

        [Fact]
        public void BreakToMiddle()
        {
            // The behaviour is that return jumps to a label, but does not necessarily leave a block.
            LabelTarget target = Expression.Label(typeof(int));
            Expression block = Expression.Block(
                Expression.Break(target, Expression.Constant(1)),
                Expression.Label(target, Expression.Constant(2)),
                Expression.Constant(3)
                );
            Assert.Equal(3, Expression.Lambda<Func<int>>(block).Compile()());
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void BreakJumps(object value)
        {
            Type type = value.GetType();
            LabelTarget target = Expression.Label(type);
            Expression block = Expression.Block(
                Expression.Break(target, Expression.Constant(value)),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target, Expression.Default(type))
                );
            Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Constant(value), block)).Compile()());
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetBreakHasNoValue(Type type)
        {
            LabelTarget target = Expression.Label(type);
            Assert.Throws<ArgumentException>(() => Expression.Break(target));
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetBreakHasNoValueTypeExplicit(Type type)
        {
            LabelTarget target = Expression.Label(type);
            Assert.Throws<ArgumentException>(() => Expression.Break(target, type));
        }

        [Fact]
        public void BreakVoidNoValue()
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Break(target),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile()();
        }

        [Fact]
        public void BreakExplicitVoidNoValue()
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Break(target, typeof(void)),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile()();
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NullValueOnNonVoidBreak(Type type)
        {
            Assert.Throws<ArgumentException>(() => Expression.Break(Expression.Label(type)));
            Assert.Throws<ArgumentException>(() => Expression.Break(Expression.Label(type), default(Expression)));
            Assert.Throws<ArgumentException>(() => Expression.Break(Expression.Label(type), null, type));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void ExplicitNullTypeWithValue(object value)
        {
            Assert.Throws<ArgumentException>(() => Expression.Break(Expression.Label(value.GetType()), default(Type)));
        }

        [Fact]
        public void UnreadableLabel()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            LabelTarget target = Expression.Label(typeof(string));
            Assert.Throws<ArgumentException>("value", () => Expression.Break(target, value));
            Assert.Throws<ArgumentException>("value", () => Expression.Break(target, value, typeof(string)));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void CanAssignAnythingToVoid(object value)
        {
            LabelTarget target = Expression.Label();
            BlockExpression block = Expression.Block(
                Expression.Break(target, Expression.Constant(value)),
                Expression.Label(target)
                );
            Assert.Equal(typeof(void), block.Type);
            Expression.Lambda<Action>(block).Compile()();
        }

        [Theory]
        [MemberData(nameof(NonObjectAssignableConstantValueData))]
        public void CannotAssignValueTypesToObject(object value)
        {
            Assert.Throws<ArgumentException>(() => Expression.Break(Expression.Label(typeof(object)), Expression.Constant(value)));
        }

        [Theory]
        [MemberData(nameof(ObjectAssignableConstantValueData))]
        public void ExplicitTypeAssigned(object value)
        {
            LabelTarget target = Expression.Label(typeof(object));
            BlockExpression block = Expression.Block(
                Expression.Break(target, Expression.Constant(value), typeof(object)),
                Expression.Label(target, Expression.Default(typeof(object)))
                );
            Assert.Equal(typeof(object), block.Type);
            Assert.Equal(value, Expression.Lambda<Func<object>>(block).Compile()());
        }

        [Fact]
        public void BreakQuotesIfNecessary()
        {
            LabelTarget target = Expression.Label(typeof(Expression<Func<int>>));
            BlockExpression block = Expression.Block(
                Expression.Break(target, Expression.Lambda<Func<int>>(Expression.Constant(0))),
                Expression.Label(target, Expression.Default(typeof(Expression<Func<int>>)))
                );
            Assert.Equal(typeof(Expression<Func<int>>), block.Type);
        }

        [Fact]
        public void UpdateSameIsSame()
        {
            LabelTarget target = Expression.Label(typeof(int));
            Expression value = Expression.Constant(0);
            GotoExpression ret = Expression.Break(target, value);
            Assert.Same(ret, ret.Update(target, value));
        }

        [Fact]
        public void UpdateDiferentValueIsDifferent()
        {
            LabelTarget target = Expression.Label(typeof(int));
            GotoExpression ret = Expression.Break(target, Expression.Constant(0));
            Assert.NotSame(ret, ret.Update(target, Expression.Constant(0)));
        }

        [Fact]
        public void UpdateDifferentTargetIsDifferent()
        {
            Expression value = Expression.Constant(0);
            GotoExpression ret = Expression.Break(Expression.Label(typeof(int)), value);
            Assert.NotSame(ret, ret.Update(Expression.Label(typeof(int)), value));
        }
    }
}
