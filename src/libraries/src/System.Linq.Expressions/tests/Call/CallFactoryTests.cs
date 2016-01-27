// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class CallFactoryTests
    {
        [Fact]
        public static void CheckCallFactoryOptimizationInstance1()
        {
            AssertCallIsOptimizedInstance(1);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationInstance2()
        {
            var expr = Expression.Call(Expression.Parameter(typeof(MS)), typeof(MS).GetMethod("I2"), Expression.Constant(0), Expression.Constant(1));

            AssertInstanceMethodCall(2, expr);

            AssertCallIsOptimizedInstance(2);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationInstance3()
        {
            var expr = Expression.Call(Expression.Parameter(typeof(MS)), typeof(MS).GetMethod("I3"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2));

            AssertInstanceMethodCall(3, expr);

            AssertCallIsOptimizedInstance(3);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic1()
        {
            var expr = Expression.Call(typeof(MS).GetMethod("S1"), Expression.Constant(0));

            AssertStaticMethodCall(1, expr);

            AssertCallIsOptimizedStatic(1);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic2()
        {
            var expr1 = Expression.Call(typeof(MS).GetMethod("S2"), Expression.Constant(0), Expression.Constant(1));
            var expr2 = Expression.Call(null, typeof(MS).GetMethod("S2"), Expression.Constant(0), Expression.Constant(1));

            AssertStaticMethodCall(2, expr1);
            AssertStaticMethodCall(2, expr2);

            AssertCallIsOptimizedStatic(2);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic3()
        {
            var expr1 = Expression.Call(typeof(MS).GetMethod("S3"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2));
            var expr2 = Expression.Call(null, typeof(MS).GetMethod("S3"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2));

            AssertStaticMethodCall(3, expr1);
            AssertStaticMethodCall(3, expr2);

            AssertCallIsOptimizedStatic(3);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic4()
        {
            var expr = Expression.Call(typeof(MS).GetMethod("S4"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));

            AssertStaticMethodCall(4, expr);

            AssertCallIsOptimizedStatic(4);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic5()
        {
            var expr = Expression.Call(typeof(MS).GetMethod("S5"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3), Expression.Constant(4));

            AssertStaticMethodCall(5, expr);

            AssertCallIsOptimizedStatic(5);
        }

        [Fact]
        public static void CheckArrayIndexOptimization1()
        {
            var instance = Expression.Parameter(typeof(int[]));
            var args = new[] { Expression.Constant(0) };
            var expr = Expression.ArrayIndex(instance, args);
            var method = typeof(int[]).GetMethod("Get", BindingFlags.Public | BindingFlags.Instance);

            AssertCallIsOptimized(expr, instance, method, args);
        }

        [Fact]
        public static void CheckArrayIndexOptimization2()
        {
            var instance = Expression.Parameter(typeof(int[,]));
            var args = new[] { Expression.Constant(0), Expression.Constant(0) };
            var expr = Expression.ArrayIndex(instance, args);
            var method = typeof(int[,]).GetMethod("Get", BindingFlags.Public | BindingFlags.Instance);

            AssertCallIsOptimized(expr, instance, method, args);
        }

        private static void AssertCallIsOptimizedInstance(int n)
        {
            var method = typeof(MS).GetMethod("I" + n);

            AssertCallIsOptimized(method);
        }

        private static void AssertCallIsOptimizedStatic(int n)
        {
            var method = typeof(MS).GetMethod("S" + n);

            AssertCallIsOptimized(method);
        }

        private static void AssertCallIsOptimized(MethodInfo method)
        {
            var instance = method.IsStatic ? null : Expression.Parameter(method.DeclaringType);

            var n = method.GetParameters().Length;

            // Expression[] overload
            {
                var args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToArray();
                var expr = Expression.Call(instance, method, args);

                AssertCallIsOptimized(expr, instance, method, args);
            }

            // IEnumerable<Expression> overload
            {
                var args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToList();
                var expr = Expression.Call(instance, method, args);

                AssertCallIsOptimized(expr, instance, method, args);
            }
        }

        private static void AssertCallIsOptimized(MethodCallExpression expr, Expression instance, MethodInfo method, IReadOnlyList<Expression> args)
        {
            var n = method.GetParameters().Length;

            var updated = Update(expr);
            var visited = Visit(expr);

            foreach (var node in new[] { expr, updated, visited })
            {
                Assert.Same(instance, node.Object);
                Assert.Same(method, node.Method);

                if (method.IsStatic)
                {
                    AssertStaticMethodCall(n, node);
                }
                else
                {
                    AssertInstanceMethodCall(n, node);
                }

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

        private static void AssertStaticMethodCall(int n, object obj)
        {
            AssertTypeName("MethodCallExpression" + n, obj);
        }

        private static void AssertInstanceMethodCall(int n, object obj)
        {
            AssertTypeName("InstanceMethodCallExpression" + n, obj);
        }

        private static void AssertTypeName(string expected, object obj)
        {
            Assert.Equal(expected, obj.GetType().Name);
        }

        private static MethodCallExpression Update(MethodCallExpression node)
        {
            // Tests the call of Update to Expression.Call factories.

            var res = node.Update(node.Object, node.Arguments.ToArray());

            Assert.NotSame(node, res);

            return res;
        }

        private static MethodCallExpression Visit(MethodCallExpression node)
        {
            // Tests dispatch of ExpressionVisitor into Rewrite method which calls Expression.Call factories.

            return (MethodCallExpression)new Visitor().Visit(node);
        }

        class Visitor : ExpressionVisitor
        {
            protected override Expression VisitConstant(ConstantExpression node)
            {
                return Expression.Constant(node.Value, node.Type); // clones
            }
        }
    }

    public class MS
    {
        public void I0() { }
        public void I1(int a) { }
        public void I2(int a, int b) { }
        public void I3(int a, int b, int c) { }
        public void I4(int a, int b, int c, int d) { }
        public void I5(int a, int b, int c, int d, int e) { }

        public static void S0() { }
        public static void S1(int a) { }
        public static void S2(int a, int b) { }
        public static void S3(int a, int b, int c) { }
        public static void S4(int a, int b, int c, int d) { }
        public static void S5(int a, int b, int c, int d, int e) { }
    }
}
