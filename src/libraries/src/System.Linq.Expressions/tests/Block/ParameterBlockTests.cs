// Copyright (c) Jon Hanna. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Test
{
    public class ParameterBlockTests : SharedBlockTests
    {
        private static IEnumerable<ParameterExpression> SingleParameter
        {
            get { return Enumerable.Repeat(Expression.Variable(typeof(int)), 1); }
        }

        [Theory]
        [MemberData("ConstantValueData")]
        public void SingleElementBlock(object value)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                SingleParameter,
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Theory]
        [MemberData("ConstantValueData")]
        public void DoubleElementBlock(object value)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                SingleParameter,
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
            Assert.Throws<ArgumentNullException>("type", () => Expression.Block(null, SingleParameter, Expression.Constant(0)));
            Assert.Throws<ArgumentNullException>("type", () => Expression.Block(null, SingleParameter, Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void NullExpressionList()
        {
            Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(SingleParameter, default(Expression[])));
            Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(SingleParameter, default(IEnumerable<Expression>)));
            Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(typeof(int), SingleParameter, default(Expression[])));
            Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(typeof(int), SingleParameter, default(IEnumerable<Expression>)));
        }

        [Theory]
        [MemberData("BlockSizes")]
        public void NullExpressionInExpressionList(int size)
        {
            List<Expression> expressionList = Enumerable.Range(0, size).Select(i => (Expression)Expression.Constant(1)).ToList();
            for (int i = 0; i < expressionList.Count; ++i)
            {
                Expression[] expressions = expressionList.ToArray();
                expressions[i] = null;
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(SingleParameter, expressions));
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(SingleParameter, expressions.Skip(0)));
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(typeof(int), SingleParameter, expressions));
                Assert.Throws<ArgumentNullException>("expressions", () => Expression.Block(typeof(int), SingleParameter, expressions.Skip(0)));
            }
        }

        [Theory]
        [MemberData("BlockSizes")]
        public void UnreadableExpressionInExpressionList(int size)
        {
            List<Expression> expressionList = Enumerable.Range(0, size).Select(i => (Expression)Expression.Constant(1)).ToList();
            for (int i = 0; i != expressionList.Count; ++i)
            {
                Expression[] expressions = expressionList.ToArray();
                expressions[i] = UnreadableExpression;
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(SingleParameter, expressions));
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(SingleParameter, expressions.Skip(0)));
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(int), SingleParameter, expressions));
                Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(int), SingleParameter, expressions.Skip(0)));
            }
        }

        [Theory]
        [MemberData("ObjectAssignableConstantValuesAndSizes")]
        public void BlockExplicitType(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            BlockExpression block = Expression.Block(typeof(object), SingleParameter, PadBlock(blockSize - 1, constant));

            Assert.Equal(typeof(object), block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile()());
        }

        [Theory]
        [MemberData("BlockSizes")]
        public void BlockInvalidExplicitType(int blockSize)
        {
            ConstantExpression constant = Expression.Constant(0);
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, Expression.Constant(0));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(string), SingleParameter, expressions));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(string), SingleParameter, expressions.ToArray()));
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void BlockFromEmptyParametersSameAsFromParams(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression fromParamsBlock = Expression.Block(SingleParameter, expressions.ToArray());
            BlockExpression fromEnumBlock = Expression.Block(SingleParameter, expressions);

            Assert.Equal(fromParamsBlock.GetType(), fromEnumBlock.GetType());

            Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(constant, fromParamsBlock)).Compile()());
            Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(constant, fromEnumBlock)).Compile()());
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        [ActiveIssue(3881)]
        public void InvalidExpressionIndex(object value, int blockSize)
        {
            BlockExpression block = Expression.Block(SingleParameter, PadBlock(blockSize - 1, Expression.Constant(value, value.GetType())));
            Assert.Throws<ArgumentOutOfRangeException>(() => block.Expressions[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => block.Expressions[blockSize]);
        }

        // Remove below if issue blocking above is fixed.
        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void InvalidExpressionIndexVaryingExceptin(object value, int blockSize)
        {
            BlockExpression block = Expression.Block(SingleParameter, PadBlock(blockSize - 1, Expression.Constant(value, value.GetType())));
            Assert.ThrowsAny<Exception>(() => block.Expressions[-1]);
            Assert.ThrowsAny<Exception>(() => block.Expressions[blockSize]);
        }

        // See https://github.com/dotnet/corefx/issues/3043
        [Fact]
        public void EmptyBlockWithParametersNotAllowed()
        {
            Assert.Throws<ArgumentException>("expressions", () => Expression.Block(SingleParameter));
            Assert.Throws<ArgumentException>("expressions", () => Expression.Block(SingleParameter, Enumerable.Empty<Expression>()));
            Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(void), SingleParameter));
            Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(void), SingleParameter, Enumerable.Empty<Expression>()));
        }

        // If https://github.com/dotnet/corefx/issues/3043 is ever actioned, this case would still be prohibited.
        [Fact]
        public void EmptyBlockWithParametersAndNonVoidTypeNotAllowed()
        {
            Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(int), SingleParameter));
            Assert.Throws<ArgumentException>("expressions", () => Expression.Block(typeof(int), SingleParameter, Enumerable.Empty<Expression>()));
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void ResultPropertyFromParams(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(SingleParameter, expressions.ToArray());

            Assert.Same(constant, block.Result);
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void ResultPropertyFromEnumerable(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(SingleParameter, expressions);

            Assert.Same(constant, block.Result);
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void VariableCountCorrect(object value, int blockSize)
        {
            IEnumerable<ParameterExpression> vars = Enumerable.Range(0, blockSize).Select(i => Expression.Variable(value.GetType()));
            BlockExpression block = Expression.Block(vars, Expression.Constant(value, value.GetType()));

            Assert.Equal(blockSize, block.Variables.Count);
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        [ActiveIssue(3883)]
        public void RewriteToSameWithSameValues(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant).ToArray();

            BlockExpression block = Expression.Block(SingleParameter, expressions);
            Assert.Same(block, block.Update(block.Variables.ToArray(), expressions));
            Assert.Same(block, block.Update(block.Variables.ToArray(), expressions));
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void CanFindItems(object value, int blockSize)
        {
            ConstantExpression[] values = new ConstantExpression[blockSize];
            for (int i = 0; i != values.Length; ++i)
                values[i] = Expression.Constant(value);

            BlockExpression block = Expression.Block(SingleParameter, values);

            IList<Expression> expressions = block.Expressions;

            for (int i = 0; i != values.Length; ++i)
                Assert.Equal(i, expressions.IndexOf(values[i]));
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void IdentifyNonAbsentItemAsAbsent(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(SingleParameter, expressions);

            Assert.Equal(-1, block.Expressions.IndexOf(Expression.Default(typeof(long))));
            Assert.False(block.Expressions.Contains(null));
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void ExpressionsEnumerable(object value, int blockSize)
        {
            ConstantExpression[] values = new ConstantExpression[blockSize];
            for (int i = 0; i != values.Length; ++i)
                values[i] = Expression.Constant(value);

            BlockExpression block = Expression.Block(SingleParameter, values);

            Assert.True(values.SequenceEqual(block.Expressions));
            int index = 0;
            foreach (Expression exp in ((IEnumerable)block.Expressions))
                Assert.Same(exp, values[index++]);
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void UpdateWithExpressionsReturnsSame(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(SingleParameter, expressions);

            Assert.Same(block, block.Update(block.Variables, block.Expressions));
        }

        [Theory]
        [MemberData("ConstantValuesAndSizes")]
        public void Visit(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(SingleParameter, expressions);

            Assert.NotSame(block, new TestVistor().Visit(block));
        }

        [Theory]
        [MemberData("ObjectAssignableConstantValuesAndSizes")]
        public void VisitTyped(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(typeof(object), SingleParameter, expressions);

            Assert.NotSame(block, new TestVistor().Visit(block));
        }

        [Theory]
        [MemberData("BlockSizes")]
        public void NullVariables(int blockSize)
        {
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, Expression.Constant(0));
            IEnumerable<ParameterExpression> vars = Enumerable.Repeat(default(ParameterExpression), 1);

            Assert.Throws<ArgumentNullException>(() => Expression.Block(vars, expressions));
            Assert.Throws<ArgumentNullException>(() => Expression.Block(vars, expressions.ToArray()));
            Assert.Throws<ArgumentNullException>(() => Expression.Block(typeof(object), vars, expressions));
            Assert.Throws<ArgumentNullException>(() => Expression.Block(typeof(object), vars, expressions.ToArray()));
        }

        [Theory]
        [MemberData("BlockSizes")]
        public void ByRefVariables(int blockSize)
        {
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, Expression.Constant(0));
            IEnumerable<ParameterExpression> vars = Enumerable.Repeat(Expression.Parameter(typeof(int).MakeByRefType()), 1);

            Assert.Throws<ArgumentException>(() => Expression.Block(vars, expressions));
            Assert.Throws<ArgumentException>(() => Expression.Block(vars, expressions.ToArray()));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(object), vars, expressions));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(object), vars, expressions.ToArray()));
        }

        [Theory]
        [MemberData("BlockSizes")]
        public void RepeatedVariables(int blockSize)
        {
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, Expression.Constant(0));
            ParameterExpression variable = Expression.Variable(typeof(int));
            IEnumerable<ParameterExpression> vars = Enumerable.Repeat(variable, 2);

            Assert.Throws<ArgumentException>(() => Expression.Block(vars, expressions));
            Assert.Throws<ArgumentException>(() => Expression.Block(vars, expressions.ToArray()));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(object), vars, expressions));
            Assert.Throws<ArgumentException>(() => Expression.Block(typeof(object), vars, expressions.ToArray()));
        }
    }
}
