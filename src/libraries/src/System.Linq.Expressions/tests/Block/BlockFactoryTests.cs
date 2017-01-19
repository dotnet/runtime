// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BlockFactoryTests
    {
        [Fact]
        public static void CheckBlockFactoryOptimization2()
        {
            AssertBlockIsOptimized(2);
        }

        [Fact]
        public static void CheckBlockFactoryOptimization3()
        {
            AssertBlockIsOptimized(3);
        }

        [Fact]
        public static void CheckBlockFactoryOptimization4()
        {
            AssertBlockIsOptimized(4);
        }

        [Fact]
        public static void CheckBlockFactoryOptimization5()
        {
            AssertBlockIsOptimized(5);
        }

        private static void AssertBlockIsOptimized(int n)
        {
            // (Expression[]) overload
            {
                ConstantExpression[] args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToArray();
                BlockExpression expr = Expression.Block(args);

                AssertBlockIsOptimized(expr, args);
            }

            // (IEnumerable<Expression>) overload
            {
                List<ConstantExpression> args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToList();
                BlockExpression expr = Expression.Block(args);

                AssertBlockIsOptimized(expr, args);
            }

            // (Type, Expression[]) overload
            {
                ConstantExpression[] args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToArray();
                BlockExpression expr = Expression.Block(args.Last().Type, args);

                AssertBlockIsOptimized(expr, args);
            }

            // (Type, IEnumerable<Expression>) overload
            {
                List<ConstantExpression> args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToList();
                BlockExpression expr = Expression.Block(args.Last().Type, args);

                AssertBlockIsOptimized(expr, args);
            }

            // (IEnumerable<ParameterExpression>, Expression[]) overload
            {
                var vars = new ParameterExpression[0];
                ConstantExpression[] args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToArray();
                BlockExpression expr = Expression.Block(vars, args);

                AssertBlockIsOptimized(expr, args);
            }

            // (IEnumerable<ParameterExpression>, IEnumerable<Expression>) overload
            {
                var vars = new ParameterExpression[0];
                List<ConstantExpression> args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToList();
                BlockExpression expr = Expression.Block(vars, args);

                AssertBlockIsOptimized(expr, args);
            }

            // (Type, IEnumerable<ParameterExpression>, Expression[]) overload
            {
                var vars = new ParameterExpression[0];
                ConstantExpression[] args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToArray();
                BlockExpression expr = Expression.Block(args.Last().Type, vars, args);

                AssertBlockIsOptimized(expr, args);
            }

            // (Type, IEnumerable<ParameterExpression>, IEnumerable<Expression>) overload
            {
                var vars = new ParameterExpression[0];
                List<ConstantExpression> args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToList();
                BlockExpression expr = Expression.Block(args.Last().Type, vars, args);

                AssertBlockIsOptimized(expr, args);
            }
        }

        private static void AssertBlockIsOptimized(BlockExpression expr, IReadOnlyList<Expression> args)
        {
            int n = args.Count;

            BlockExpression updated = Update(expr);
            BlockExpression visited = Visit(expr);

            foreach (var node in new[] { expr, updated, visited })
            {
                AssertBlock(n, node);

                Assert.Equal(n, node.Expressions.Count);

                if (node != visited) // our visitor clones argument nodes
                {
                    for (var i = 0; i < n; i++)
                    {
                        Assert.Same(args[i], node.Expressions[i]);
                    }
                }
            }
        }

        private static void AssertBlock(int n, object obj)
        {
            AssertTypeName("Block" + n, obj);
        }

        private static void AssertTypeName(string expected, object obj)
        {
            Assert.Equal(expected, obj.GetType().Name);
        }

        private static BlockExpression Update(BlockExpression node)
        {
            // Tests the call of Update to Expression.Block factories.

            BlockExpression res = node.Update(node.Variables, node.Expressions.ToArray());

            Assert.Same(node, res);

            return res;
        }

        private static BlockExpression Visit(BlockExpression node)
        {
            // Tests dispatch of ExpressionVisitor into Rewrite method which calls Expression.Block factories.

            return (BlockExpression)new Visitor().Visit(node);
        }

        class Visitor : ExpressionVisitor
        {
            protected override Expression VisitConstant(ConstantExpression node)
            {
                return Expression.Constant(node.Value, node.Type); // clones
            }
        }
    }
}
