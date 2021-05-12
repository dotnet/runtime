// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class NoParameterBlockTests : SharedBlockTests
    {
        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void SingleElementBlock(object value, bool useInterpreter)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void DoubleElementBlock(object value, bool useInterpreter)
        {
            Type type = value.GetType();
            ConstantExpression constant = Expression.Constant(value, type);
            BlockExpression block = Expression.Block(
                Expression.Empty(),
                constant
               );

            Assert.Equal(type, block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile(useInterpreter)());
        }

        [Fact]
        public void DoubleElementBlockNullArgument()
        {
            AssertExtensions.Throws<ArgumentNullException>("arg0", () => Expression.Block(default(Expression), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg1", () => Expression.Block(Expression.Constant(1), default(Expression)));
        }

        [Fact]
        public void DoubleElementBlockUnreadable()
        {
            AssertExtensions.Throws<ArgumentException>("arg0", () => Expression.Block(UnreadableExpression, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg1", () => Expression.Block(Expression.Constant(1), UnreadableExpression));
        }

        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void TripleElementBlock(object value, bool useInterpreter)
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
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile(useInterpreter)());
        }

        [Fact]
        public void TripleElementBlockNullArgument()
        {
            AssertExtensions.Throws<ArgumentNullException>("arg0", () => Expression.Block(default(Expression), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg1", () => Expression.Block(Expression.Constant(1), default(Expression), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), default(Expression)));
        }

        [Fact]
        public void TripleElementBlockUnreadable()
        {
            AssertExtensions.Throws<ArgumentException>("arg0", () => Expression.Block(UnreadableExpression, Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg1", () => Expression.Block(Expression.Constant(1), UnreadableExpression, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), UnreadableExpression));
        }

        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void QuadrupleElementBlock(object value, bool useInterpreter)
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
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile(useInterpreter)());
        }

        [Fact]
        public void QuadrupleElementBlockNullArgument()
        {
            AssertExtensions.Throws<ArgumentNullException>("arg0", () => Expression.Block(default(Expression), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg1", () => Expression.Block(Expression.Constant(1), default(Expression), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), default(Expression), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg3", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), default(Expression)));
        }

        [Fact]
        public void QuadrupleElementBlockUnreadable()
        {
            AssertExtensions.Throws<ArgumentException>("arg0", () => Expression.Block(UnreadableExpression, Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg1", () => Expression.Block(Expression.Constant(1), UnreadableExpression, Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), UnreadableExpression, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg3", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), UnreadableExpression));
        }

        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void QuintupleElementBlock(object value, bool useInterpreter)
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
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile(useInterpreter)());
        }

        [Fact]
        public void QuintupleElementBlockNullArgument()
        {
            AssertExtensions.Throws<ArgumentNullException>("arg0", () => Expression.Block(default(Expression), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg1", () => Expression.Block(Expression.Constant(1), default(Expression), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), default(Expression), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg3", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), default(Expression), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("arg4", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), default(Expression)));
        }

        [Fact]
        public void QuintupleElementBlockUnreadable()
        {
            AssertExtensions.Throws<ArgumentException>("arg0", () => Expression.Block(UnreadableExpression, Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg1", () => Expression.Block(Expression.Constant(1), UnreadableExpression, Expression.Constant(1), Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg2", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), UnreadableExpression, Expression.Constant(1), Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg3", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), UnreadableExpression, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("arg4", () => Expression.Block(Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), Expression.Constant(1), UnreadableExpression));
        }

        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void SextupleElementBlock(object value, bool useInterpreter)
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
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile(useInterpreter)());
        }

        [Fact]
        public void NullExpicitType()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Block(default(Type), default(IEnumerable<ParameterExpression>), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Block(default(Type), null, Enumerable.Repeat(Expression.Constant(0), 1)));
        }

        [Fact]
        public void NullExpressionList()
        {
            AssertExtensions.Throws<ArgumentNullException>("expressions", () => Expression.Block(default(Expression[])));
            AssertExtensions.Throws<ArgumentNullException>("expressions", () => Expression.Block(default(IEnumerable<Expression>)));
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
                AssertExtensions.Throws<ArgumentNullException>($"expressions[{i}]", () => Expression.Block(expressions));
                AssertExtensions.Throws<ArgumentNullException>($"expressions[{i}]", () => Expression.Block(expressions.Skip(0)));
                AssertExtensions.Throws<ArgumentNullException>($"expressions[{i}]", () => Expression.Block(typeof(int), expressions));
                AssertExtensions.Throws<ArgumentNullException>($"expressions[{i}]", () => Expression.Block(typeof(int), expressions.Skip(0)));
                AssertExtensions.Throws<ArgumentNullException>($"expressions[{i}]", () => Expression.Block(typeof(int), null, expressions));
                AssertExtensions.Throws<ArgumentNullException>($"expressions[{i}]", () => Expression.Block(typeof(int), null, expressions.Skip(0)));
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
                AssertExtensions.Throws<ArgumentException>($"expressions[{i}]", () => Expression.Block(expressions));
                AssertExtensions.Throws<ArgumentException>($"expressions[{i}]", () => Expression.Block(expressions.Skip(0)));
                AssertExtensions.Throws<ArgumentException>($"expressions[{i}]", () => Expression.Block(typeof(int), expressions));
                AssertExtensions.Throws<ArgumentException>($"expressions[{i}]", () => Expression.Block(typeof(int), expressions.Skip(0)));
                AssertExtensions.Throws<ArgumentException>($"expressions[{i}]", () => Expression.Block(typeof(int), null, expressions));
                AssertExtensions.Throws<ArgumentException>($"expressions[{i}]", () => Expression.Block(typeof(int), null, expressions.Skip(0)));
            }
        }

        [Theory]
        [PerCompilationType(nameof(ObjectAssignableConstantValuesAndSizes))]
        public void BlockExplicitType(object value, int blockSize, bool useInterpreter)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            BlockExpression block = Expression.Block(typeof(object), PadBlock(blockSize - 1, constant));

            Assert.Equal(typeof(object), block.Type);

            Expression equal = Expression.Equal(constant, block);
            Assert.True(Expression.Lambda<Func<bool>>(equal).Compile(useInterpreter)());
        }

        [Theory]
        [MemberData(nameof(BlockSizes))]
        public void BlockInvalidExplicitType(int blockSize)
        {
            ConstantExpression constant = Expression.Constant(0);
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, Expression.Constant(0));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Block(typeof(string), expressions));
            AssertExtensions.Throws<ArgumentException>(null, (() => Expression.Block(typeof(string), expressions.ToArray())));
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void InvalidExpressionIndex(object value, int blockSize)
        {
            BlockExpression block = Expression.Block(PadBlock(blockSize - 1, Expression.Constant(value, value.GetType())));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => block.Expressions[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => block.Expressions[blockSize]);
        }

        [Fact]
        public void EmptyBlockWithNonVoidTypeNotAllowed()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Block(typeof(int)));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Block(typeof(int), Enumerable.Empty<Expression>()));
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51952", TestPlatforms.tvOS)]
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
        public void RewriteToSameWithSameValues(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant).ToArray();

            BlockExpression block = Expression.Block(expressions);
            Assert.Same(block, block.Update(null, expressions));
            Assert.Same(block, block.Update(Enumerable.Empty<ParameterExpression>(), expressions));
            Assert.Same(block, NoOpVisitor.Instance.Visit(block));
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

        [Theory, MemberData(nameof(BlockSizes))]
        public void ExpressionListBehavior(int parCount)
        {
            // This method contains a lot of assertions, which amount to one large assertion that
            // the result of the Expressions property behaves correctly.
            Expression[] exps = Enumerable.Range(0, parCount).Select(_ => Expression.Constant(0)).ToArray();
            BlockExpression block = Expression.Block(exps);
            ReadOnlyCollection<Expression> children = block.Expressions;
            Assert.Equal(parCount, children.Count);
            using (var en = children.GetEnumerator())
            {
                IEnumerator nonGenEn = ((IEnumerable)children).GetEnumerator();
                for (int i = 0; i != parCount; ++i)
                {
                    Assert.True(en.MoveNext());
                    Assert.True(nonGenEn.MoveNext());
                    Assert.Same(exps[i], children[i]);
                    Assert.Same(exps[i], en.Current);
                    Assert.Same(exps[i], nonGenEn.Current);
                    Assert.Equal(i, children.IndexOf(exps[i]));
                    Assert.Contains(exps[i], children);
                }

                Assert.False(en.MoveNext());
                Assert.False(nonGenEn.MoveNext());
                (nonGenEn as IDisposable)?.Dispose();
            }

            Expression[] copyToTest = new Expression[parCount + 1];
            Assert.Throws<ArgumentNullException>(() => children.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => children.CopyTo(copyToTest, -1));
            Assert.All(copyToTest, Assert.Null); // assert partial copy didn't happen before exception
            AssertExtensions.Throws<ArgumentException>(parCount >= 2 && parCount <= 5 ? null : "destinationArray", () => children.CopyTo(copyToTest, 2));
            Assert.All(copyToTest, Assert.Null);
            children.CopyTo(copyToTest, 1);
            Assert.Equal(copyToTest, exps.Prepend(null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => children[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => children[parCount]);
            Assert.Equal(-1, children.IndexOf(Expression.Parameter(typeof(int))));
            Assert.DoesNotContain(Expression.Parameter(typeof(int)), children);
        }

        [Theory]
        [MemberData(nameof(ConstantValuesAndSizes))]
        public void IdentifyNonAbsentItemAsAbsent(object value, int blockSize)
        {
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(expressions);

            Assert.Equal(-1, block.Expressions.IndexOf(Expression.Default(typeof(long))));
            Assert.DoesNotContain(null, block.Expressions);
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
            Assert.Same(block, NoOpVisitor.Instance.Visit(block));
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

        [Theory, MemberData(nameof(BlockSizes))]
        public void UpdateToNullArguments(int blockSize)
        {
            ConstantExpression constant = Expression.Constant(0);
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant);

            BlockExpression block = Expression.Block(expressions);

            AssertExtensions.Throws<ArgumentNullException>("expressions", () => block.Update(null, null));
        }

        [Theory, MemberData(nameof(BlockSizes))]
        public void UpdateDoesntRepeatEnumeration(int blockSize)
        {
            ConstantExpression constant = Expression.Constant(0);
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant).ToArray();

            BlockExpression block = Expression.Block(expressions);

            Assert.Same(block, block.Update(null, new RunOnceEnumerable<Expression>(expressions)));
            Assert.NotSame(
                block,
                block.Update(null, new RunOnceEnumerable<Expression>(PadBlock(blockSize - 1, Expression.Constant(1)))));
        }

        [Theory, MemberData(nameof(BlockSizes))]
        public void UpdateDifferentSizeReturnsDifferent(int blockSize)
        {
            ConstantExpression constant = Expression.Constant(0);
            IEnumerable<Expression> expressions = PadBlock(blockSize - 1, constant).ToArray();

            BlockExpression block = Expression.Block(expressions);

            Assert.NotSame(block, block.Update(null, block.Expressions.Prepend(Expression.Empty())));
        }

        [Theory, MemberData(nameof(BlockSizes))]
        public void UpdateAnyExpressionDifferentReturnsDifferent(int blockSize)
        {
            ConstantExpression constant = Expression.Constant(0);
            Expression[] expressions = PadBlock(blockSize - 1, constant).ToArray();

            BlockExpression block = Expression.Block(expressions);

            for (int i = 0; i != expressions.Length; ++i)
            {
                Expression[] newExps = new Expression[expressions.Length];
                expressions.CopyTo(newExps, 0);
                newExps[i] = Expression.Constant(1);
                Assert.NotSame(block, block.Update(null, newExps));
            }
        }
    }
}
