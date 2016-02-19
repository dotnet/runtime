// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class NoParameterBlockTests : SharedBlockTests
    {
        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void SingleElementBlock(object value)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void DoubleElementBlock(object value)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                Expression.Empty(),
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Fact]
        public void DoubleElementBlockNullArgument()
        {
            Assert.Throws<ArgumentNullException>("arg0", () => Expression.Block(default(Expression), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg1", () => Expression.Block(Expression.Constant(1), default(Expression)));
        }

        [Fact]
        public void DoubleElementBlockUnreadable()
        {
            Assert.Throws<ArgumentException>("arg0", () => Expression.Block(UnreadableExpression, Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg1", () => Expression.Block(Expression.Constant(1), UnreadableExpression));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void TripleElementBlock(object value)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                Expression.Empty(),
                Expression.Empty(),
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Fact]
        public void TripleElementBlockNullArgument()
        {
            Assert.Throws<ArgumentNullException>("arg0", () => Expression.Block(default(Expression), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg1", () => Expression.Block(Expression.Constant(1), default(Expression), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), default(Expression)));
        }

        [Fact]
        public void TripleElementBlockUnreadable()
        {
            Assert.Throws<ArgumentException>("arg0", () => Expression.Block(UnreadableExpression, Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg1", () => Expression.Block(Expression.Constant(1), UnreadableExpression, Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), UnreadableExpression));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void QuadrupleElementBlock(object value)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                Expression.Empty(),
                Expression.Empty(),
                Expression.Empty(),
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Fact]
        public void QuadrupleElementBlockNullArgument()
        {
            Assert.Throws<ArgumentNullException>("arg0", () => Expression.Block(default(Expression), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg1", () => Expression.Block(Expression.Constant(1), default(Expression), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), default(Expression), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg3", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), default(Expression)));
        }

        [Fact]
        public void QuadrupleElementBlockUnreadable()
        {
            Assert.Throws<ArgumentException>("arg0", () => Expression.Block(UnreadableExpression, Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg1", () => Expression.Block(Expression.Constant(1), UnreadableExpression, Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), UnreadableExpression, Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg3", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), UnreadableExpression));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void QuintupleElementBlock(object value)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                Expression.Empty(),
                Expression.Empty(),
                Expression.Empty(),
                Expression.Empty(),
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Fact]
        public void QuintupleElementBlockNullArgument()
        {
            Assert.Throws<ArgumentNullException>("arg0", () => Expression.Block(default(Expression), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg1", () => Expression.Block(Expression.Constant(1), default(Expression), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), default(Expression), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg3", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), default(Expression), Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("arg4", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), default(Expression)));
        }

        [Fact]
        public void QuintupleElementBlockUnreadable()
        {
            Assert.Throws<ArgumentException>("arg0", () => Expression.Block(UnreadableExpression, Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg1", () => Expression.Block(Expression.Constant(1), UnreadableExpression, Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), UnreadableExpression, Expression.Constant(1), Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg3", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), UnreadableExpression, Expression.Constant(1)));
            Assert.Throws<ArgumentException>("arg4", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), UnreadableExpression));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void SextupleElementBlock(object value)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                Expression.Empty(),
                Expression.Empty(),
                Expression.Empty(),
                Expression.Empty(),
                Expression.Empty(),
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Fact]
        public void NullExpicitType()
        {
            Assert.Throws<ArgumentNullException>("type", () => Expression.Block(default(Type), default(IEnumerable<ParameterExpression>), Expression.Constant(0)));
            Assert.Throws<ArgumentNullException>("type", () => Expression.Block(default(Type), null, Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void NullExpressionList()
        {
            Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(default(Expression[])));
            Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(default(IEnumerable<Expression>)));
        }

        [Theory]
        [MemberData(nameof(BlockSizes))]
        public void NullExpressionInExpressionList(int size)
        {
            List<Expression> expressionList = Enumerable.Range(0, size).Select(i => (Expression)Expression.Constant(1)).ToList();
            for (int i = 0; i != expressionList.Count; ++i)
            {
                Expression[] expressions = expressionList.ToArray();
                expressions[i] = null;
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(expressions));
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(expressions.Skip(0)));
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(typeof(int), expressions));
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(typeof(int), expressions.Skip(0)));
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(typeof(int), null, expressions));
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(typeof(int), null, expressions.Skip(0)));
            }
        }

        [Theory]
        [MemberData(nameof(BlockSizes))]
        public void UnreadableExpressionInExpressionList(int size)
        {
            List<Expression> expressionList = Enumerable.Range(0, size).Select(i => (Expression)Expression.Constant(1)).ToList();
            for (int i = 0; i != expressionList.Count; ++i)
            {
                Expression[] expressions = expressionList.ToArray();
                expressions[i] = UnreadableExpression;
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(expressions));
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(expressions.Skip(0)));
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(int), expressions));
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(int), expressions.Skip(0)));
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(int), null, expressions));
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(int), null, expressions.Skip(0)));
            }
        }

        [Theory]
        [MemberData(nameof(ObjectAssignableConstantValuesAndSizes))]
        public void BlockExplicitType(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            BlockExpression block = Expression.Block(typeof(object), PadBlock(blockSize - 1, constant));

            Assert.Equal(typeof(object), block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Theory]
        [MemberData(nameof(BlockSizes))]
        public void BlockInvalidExplicitType(int blockSize)
        {
            ConstantExpression constant = Expression.Constant(0);
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, Expression.Constant(0));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(string), expressions));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(string), expressions.ToArray()));
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void InvalidExpressionIndex(object value, int blockSize)
        {
            BlockExpression block = Expression.Block(PadBlock(blockSize - 1, Expression.Constant(value, value.GetType())));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => block.Expressions[-1]);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => block.Expressions[blockSize]);
        }

        [Fact]
        public void EmptyBlockWithNonVoidTypeNotAllowed()
        {
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(int)));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(int), Enumerable.Empty<Expression>()));
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void ResultPropertyFromParams(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(expressions.ToArray());

            Assert.Same(constant, block.Result);
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void ResultPropertyFromEnumerable(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(expressions);

            Assert.Same(constant, block.Result);
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void VariableCountZeroOnNonVariableAcceptingForms(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(expressions);

            Assert.Empty(block.Variables);
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        [ActiveIssue(3883)]
        public void RewriteToSameWithSameValues(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant).ToArray();

            BlockExpression block = Expression.Block(expressions);
            Assert.Same(block, block.Update(null, expressions));
            Assert.Same(block, block.Update(Enumerable.Empty<ParameterExpression>(), expressions));
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void CanFindItems(object value, int blockSize)
        {
            ConstantExpression[] values = new ConstantExpression[blockSize];
            for (int i = 0; i != values.Length; ++i)
                values[i] = Expression.Constant(value);

            BlockExpression block = Expression.Block(values);

            IList<Expression> expressions = block.Expressions;

            for (int i = 0; i != values.Length; ++i)
                Assert.Equal(i, expressions.IndexOf(values[i]));
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void IdentifyNonAbsentItemAsAbsent(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(expressions);

            Assert.Equal(-1, block.Expressions.IndexOf(Expression.Default(typeof(long))));
            Assert.False(block.Expressions.Contains(null));
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void ExpressionsEnumerable(object value, int blockSize)
        {
            ConstantExpression[] values = new ConstantExpression[blockSize];
            for (int i = 0; i != values.Length; ++i)
                values[i] = Expression.Constant(value);

            BlockExpression block = Expression.Block(values);

            Assert.True(values.SequenceEqual(block.Expressions));
            int index = 0;
            foreach (Expression exp in ((IEnumerable)block.Expressions))
                Assert.Same(exp, values[index++]);
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void UpdateWithExpressionsReturnsSame(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(expressions);

            Assert.Same(block, block.Update(block.Variables, block.Expressions));
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void Visit(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(expressions);

            Assert.NotSame(block, new TestVistor().Visit(block));
        }

        [Theory]
        [MemberData(nameof(ObjectAssignableConstantValuesAndSizes))]
        public void VisitTyped(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(typeof(object), expressions);

            Assert.NotSame(block, new TestVistor().Visit(block));
        }
    }
}
