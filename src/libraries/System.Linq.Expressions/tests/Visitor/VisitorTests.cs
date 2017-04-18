// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class VisitorTests
    {
        private class UsingExpression : Expression
        {
            public UsingExpression(Expression disposable, Expression body)
            {
                if (!typeof(IDisposable).IsAssignableFrom(disposable.Type))
                {
                    throw new ArgumentException();
                }
                // Omit other exception handling as this is just an example for testing purposes.

                Disposable = disposable;
                Body = body;
            }

            private Expression Disposable { get; }

            private Expression Body { get; }

            public override bool CanReduce => true;

            public override Expression Reduce()
            {
                if (Disposable.Type.IsValueType)
                {
                    return TryFinally(
                        Body,
                        Call(
                            Disposable,
                            typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))
                            )
                        );
                }

                return TryFinally(
                    Body,
                    IfThen(
                        ReferenceNotEqual(Disposable, Constant(null)),
                        Call(
                            Disposable,
                            typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose))
                            )
                        )
                    );
            }

            public override Type Type => Body.Type;

            public override ExpressionType NodeType => ExpressionType.Extension;
        }

        private class ForEachExpression : Expression
        {
            public ForEachExpression(ParameterExpression item, Expression enumerable, Expression body)
            {
                ItemVariable = item;
                Enumerable = enumerable;
                Body = body;
            }

            public ParameterExpression ItemVariable { get; }

            public Expression Enumerable { get; }

            public Expression Body { get; }

            public override bool CanReduce => true;

            public override Type Type => typeof(void);

            public override ExpressionType NodeType => ExpressionType.Extension;

            public override Expression Reduce()
            {
                Type enType = typeof(IEnumerator<>).MakeGenericType(ItemVariable.Type);
                ParameterExpression enumerator = Variable(enType);
                LabelTarget breakLabel = Label();
                return Block(
                    new[] {ItemVariable, enumerator},
                    Assign(enumerator, Call(Enumerable, typeof(IEnumerable<>).MakeGenericType(ItemVariable.Type).GetMethod(nameof(IEnumerable.GetEnumerator)))),
                    new UsingExpression(
                        enumerator,
                        Loop(
                            Block(
                                IfThen(
                                    IsFalse(
                                        Call(enumerator, typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext)))
                                        ),
                                    Break(breakLabel)
                                    ),
                                Assign(ItemVariable, Property(enumerator, enType.GetProperty(nameof(IEnumerator.Current)))),
                                Body
                                ),
                            breakLabel
                            )
                        )
                    );
            }
        }

        private class DefaultVisitor : ExpressionVisitor
        {
        }

        private class ConstantRefreshingVisitor : ExpressionVisitor
        {
            protected override Expression VisitConstant(ConstantExpression node)
                => Expression.Constant(node.Value, node.Type);
        }

        private class ResultExpression : Expression
        {
        }

        private class SourceExpression : Expression
        {
            protected override Expression Accept(ExpressionVisitor visitor) => new ResultExpression();
        }

        private class NullBecomingExpression : Expression
        {
            protected override Expression Accept(ExpressionVisitor visitor) => null;
        }

        private static string UpperCaseIfNotAlready(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string upper = value.ToUpperInvariant();
            return upper == value ? value : upper;
        }

        [Fact]
        public void IsAbstract()
        {
            Assert.True(typeof(ExpressionVisitor).IsAbstract);
        }

        [Fact]
        public void VisitNullDefaultsToReturnNull()
        {
            Assert.Null(new DefaultVisitor().Visit(default(Expression)));
        }

        [Fact]
        public void VisitExpressionDefaultsCallAccept()
        {
            Assert.IsType<ResultExpression>(new DefaultVisitor().Visit(new SourceExpression()));
        }

        [Fact]
        public void VisitNullCollection()
        {
            AssertExtensions.Throws<ArgumentNullException>("nodes", () => new DefaultVisitor().Visit(default(ReadOnlyCollection<Expression>)));
        }

        [Fact]
        public void VisitNullCollectionWithVisitorFunction()
        {
            AssertExtensions.Throws<ArgumentNullException>("nodes", () => ExpressionVisitor.Visit(null, (Expression i) => i));
        }

        [Fact]
        public void VisitCollectionVisitorWithNullFunction()
        {
            AssertExtensions.Throws<ArgumentNullException>("elementVisitor", () => ExpressionVisitor.Visit(new List<Expression> { Expression.Empty() }.AsReadOnly(), null));
        }

        [Fact]
        public void VisitAndConvertNullNode()
        {
            Assert.Null(new DefaultVisitor().VisitAndConvert(default(Expression), ""));
        }

        [Fact]
        public void VisitAndConvertNullCollection()
        {
            AssertExtensions.Throws<ArgumentNullException>("nodes", () => new DefaultVisitor().VisitAndConvert(default(ReadOnlyCollection<Expression>), ""));
        }

        [Fact]
        public void VisitCollectionReturnSameIfChildrenUnchanged()
        {
            ReadOnlyCollection<Expression> collection = new List<Expression> { Expression.Constant(0), Expression.Constant(2), Expression.DebugInfo(Expression.SymbolDocument("fileName"), 1, 1, 1, 1) }.AsReadOnly();
            Assert.Same(collection, new DefaultVisitor().Visit(collection));
        }

        [Fact]
        public void VisitCollectionDifferOnFirst()
        {
            string value = new string(new[] { 'a', 'b', 'c' });
            ReadOnlyCollection<Expression> collection = new List<Expression> { Expression.Constant(value) }.AsReadOnly();
            ReadOnlyCollection<Expression> visited = new ConstantRefreshingVisitor().Visit(collection);
            Assert.NotSame(collection, visited);
            Assert.NotSame(collection[0], visited[0]);
            Assert.Same(value, ((ConstantExpression)visited[0]).Value);
        }

        [Fact]
        public void VisitCollectionDifferOnLater()
        {
            string value = new string(new[] { 'a', 'b', 'c' });
            ReadOnlyCollection<Expression> collection = new List<Expression> { Expression.Empty(), Expression.Constant(value) }.AsReadOnly();
            ReadOnlyCollection<Expression> visited = new ConstantRefreshingVisitor().Visit(collection);
            Assert.NotSame(collection, visited);
            Assert.Same(collection[0], visited[0]);
            Assert.NotSame(collection[1], visited[1]);
            Assert.Same(value, ((ConstantExpression)visited[1]).Value);
        }

        [Fact]
        public void VisitCollectionNullNodes()
        {
            ReadOnlyCollection<Expression> collection = new List<Expression> { null, null, null }.AsReadOnly();
            Assert.Same(collection, new DefaultVisitor().Visit(collection));
        }

        [Fact]
        public void VisitCollectionWithElementVisitorReturnSameIfChildrenUnchanged()
        {
            ReadOnlyCollection<string> collection = new List<string> { "ABC", "DEF" }.AsReadOnly();
            Assert.Same(collection, ExpressionVisitor.Visit(collection, UpperCaseIfNotAlready));
        }

        [Fact]
        public void VisitCollectionWithElementVisitorDifferOnFirst()
        {
            ReadOnlyCollection<string> collection = new List<string> { "abc" }.AsReadOnly();
            ReadOnlyCollection<string> visited = ExpressionVisitor.Visit(collection, UpperCaseIfNotAlready);
            Assert.NotSame(collection, visited);
            Assert.NotSame(collection[0], visited[0]);
        }

        [Fact]
        public void VisitCollectionWithElementVisitorDifferOnLater()
        {
            ReadOnlyCollection<string> collection = new List<string> { "ABC", "def", "GHI", "jkl" }.AsReadOnly();
            ReadOnlyCollection<string> visited = ExpressionVisitor.Visit(collection, UpperCaseIfNotAlready);
            Assert.NotSame(collection, visited);
            Assert.Same(collection[0], visited[0]);
            Assert.NotSame(collection[1], visited[1]);
            Assert.Same(collection[2], visited[2]);
            Assert.NotSame(collection[3], visited[3]);
        }

        [Fact]
        public void VisitCollectionWithElementVisitorNullNodes()
        {
            ReadOnlyCollection<string> collection = new List<string> { null, null, null }.AsReadOnly();
            Assert.Same(collection, ExpressionVisitor.Visit(collection, UpperCaseIfNotAlready));
        }

        [Fact]
        public void VisitAndConvertReturnsSameIfVisitDoes()
        {
            ConstantExpression constant = Expression.Constant(0);
            Assert.Same(constant, new DefaultVisitor().Visit(constant));
            Assert.Same(constant, new DefaultVisitor().VisitAndConvert(constant, "foo"));
        }

        [Fact]
        public void VisitAndConvertThrowsIfVisitReturnsNull()
        {
            string slug = "Won't be found by chance 3f8d0006-32f9-4622-9ff4-c88e95c9babc";
            string errMsg = Assert.Throws<InvalidOperationException>(() => new DefaultVisitor().VisitAndConvert(new NullBecomingExpression(), slug)).Message;
            Assert.Contains(slug, errMsg);
        }

        [Fact]
        public void VisitAndConvertThrowsIfVisitChangesType()
        {
            string slug = "Won't be found by chance 5154E15C-A475-49B0-B596-8F822D7ACFC4";
            string errMsg = Assert.Throws<InvalidOperationException>(() => new DefaultVisitor().VisitAndConvert(new SourceExpression(), slug)).Message;
            Assert.Contains(slug, errMsg);
        }

        [Fact]
        public void VisitAndConvertNullName()
        {
            new DefaultVisitor().VisitAndConvert(Expression.Constant(0), null);
            Assert.Throws<InvalidOperationException>(() => new DefaultVisitor().VisitAndConvert(new SourceExpression(), null));
        }

        [Fact]
        public void VisitAndConvertReturnsIfForcedToCommonBase()
        {
            Assert.IsNotType<SourceExpression>(new DefaultVisitor().VisitAndConvert<Expression>(new SourceExpression(), ""));
        }

        [Fact]
        public void VisitAndConvertSameResultAsVisit()
        {
            ConstantExpression constant = Expression.Constant(0);
            ConstantExpression visited = new ConstantRefreshingVisitor().VisitAndConvert(constant, "");
            Assert.NotSame(constant, visited);
            Assert.Equal(0, visited.Value);
        }

        [Fact]
        public void ReduceChildrenCascades()
        {
            ParameterExpression intVar = Expression.Variable(typeof(int));
            List<int> list = new List<int>();
            var foreachExp = new ForEachExpression(
                intVar,
                Expression.Constant(Enumerable.Range(5, 4)),
                Expression.Call(
                    Expression.Constant(list),
                    typeof(List<int>).GetMethod(nameof(List<int>.Insert)),
                    Expression.Constant(0),
                    intVar
                    )
                );

            // Check that not only has the visitor reduced the foreach into a block
            // but the using within that block into a try…finally.
            Expression reduced = new DefaultVisitor().Visit(foreachExp);
            var block = (BlockExpression)reduced;
            var tryExp = (TryExpression)block.Expressions[1];
            var loop = (LoopExpression)tryExp.Body;
            var innerBlock = (BlockExpression)loop.Body;
            var call = (MethodCallExpression)innerBlock.Expressions.Last();
            var instance = (ConstantExpression)call.Object;
            Assert.Same(list, instance.Value);
        }

        [Fact]
        public void Visit_DebugInfoExpression_DoesNothing()
        {
            DebugInfoExpression expression = Expression.DebugInfo(Expression.SymbolDocument("fileName"), 1, 1, 1, 1);
            Assert.Same(expression, new DefaultVisitor().Visit(expression));
        }
    }
}
