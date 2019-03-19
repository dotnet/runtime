// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class InvokeFactoryTests
    {
        [Fact]
        public static void CheckInvokeFactoryOptimization0()
        {
            AssertInvokeIsOptimized(0);
        }

        [Fact]
        public static void CheckInvokeFactoryOptimization1()
        {
            AssertInvokeIsOptimized(1);
        }

        [Fact]
        public static void CheckInvokeFactoryOptimization2()
        {
            AssertInvokeIsOptimized(2);
        }

        [Fact]
        public static void CheckInvokeFactoryOptimization3()
        {
            AssertInvokeIsOptimized(3);
        }

        [Fact]
        public static void CheckInvokeFactoryOptimization4()
        {
            AssertInvokeIsOptimized(4);
        }

        [Fact]
        public static void CheckInvokeFactoryOptimization5()
        {
            AssertInvokeIsOptimized(5);
        }

        private static void AssertInvokeIsOptimized(int n)
        {
            Type[] genArgs = Enumerable.Repeat(typeof(int), n + 1).ToArray();
            Type delegateType = Expression.GetFuncType(genArgs);

            ParameterExpression instance = Expression.Parameter(delegateType);

            // Expression[] overload
            {
                ConstantExpression[] args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToArray();
                InvocationExpression expr = Expression.Invoke(instance, args);

                AssertInvokeIsOptimized(expr, instance, args);
            }

            // IEnumerable<Expression> overload
            {
                List<ConstantExpression> args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToList();
                InvocationExpression expr = Expression.Invoke(instance, args);

                AssertInvokeIsOptimized(expr, instance, args);
            }
        }

        private static void AssertInvokeIsOptimized(InvocationExpression expr, Expression expression, IReadOnlyList<Expression> args)
        {
            int n = args.Count;

            InvocationExpression updated = Update(expr);
            InvocationExpression visited = Visit(expr);

            foreach (var node in new[] { expr, updated, visited })
            {
                Assert.Same(expression, node.Expression);

                AssertInvocation(n, node);

                var argProvider = node as IArgumentProvider;
                Assert.NotNull(argProvider);

                Assert.Equal(n, argProvider.ArgumentCount);

                if (node != visited) // our visitor clones argument nodes
                {
                    for (var i = 0; i < n; i++)
                    {
                        Assert.Same(args[i], argProvider.GetArgument(i));
                        Assert.Same(args[i], node.Arguments[i]);
                    }
                }
            }
        }

        private static void AssertInvocation(int n, object obj)
        {
            if (!PlatformDetection.IsNetNative)  // .NET Native blocks internal framework reflection.
            {
                AssertTypeName("InvocationExpression" + n, obj);
            }
        }

        private static void AssertTypeName(string expected, object obj)
        {
            Assert.Equal(expected, obj.GetType().Name);
        }

        private static InvocationExpression Update(InvocationExpression node)
        {
            // Tests the call of Update to Expression.Invoke factories.

            InvocationExpression res = node.Update(node.Expression, node.Arguments.ToArray());

            Assert.Same(node, res);

            return res;
        }

        private static InvocationExpression Visit(InvocationExpression node)
        {
            // Tests dispatch of ExpressionVisitor into Rewrite method which calls Expression.Invoke factories.

            return (InvocationExpression)new Visitor().Visit(node);
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
