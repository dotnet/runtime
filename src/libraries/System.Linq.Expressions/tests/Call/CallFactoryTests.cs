// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class CallFactoryTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public static void CheckCallIsOptimizedInstance(int arity)
        {
            AssertCallIsOptimizedInstance(arity);
        }

        [Fact]
        public static void CheckCallFactoryOptimisedInstanceNullArgumentList()
        {
            var instance = Expression.Constant(new MS());
            var expr = Expression.Call(instance, typeof(MS).GetMethod(nameof(MS.I0)), default(Expression[]));
            AssertInstanceMethodCall(0, expr);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationInstance2()
        {
            MethodCallExpression expr = Expression.Call(Expression.Parameter(typeof(MS)), typeof(MS).GetMethod("I2"), Expression.Constant(0), Expression.Constant(1));

            AssertInstanceMethodCall(2, expr);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationInstance3()
        {
            MethodCallExpression expr = Expression.Call(Expression.Parameter(typeof(MS)), typeof(MS).GetMethod("I3"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2));

            AssertInstanceMethodCall(3, expr);
        }

        [Fact]
        public static void CheckCallFactoryInstanceN()
        {
            const int N = 4;

            ParameterExpression obj = Expression.Parameter(typeof(MS));
            ConstantExpression[] args = Enumerable.Range(0, N).Select(i => Expression.Constant(i)).ToArray();

            MethodCallExpression expr = Expression.Call(obj, typeof(MS).GetMethod("I" + N), args);
            
            Assert.Equal("InstanceMethodCallExpressionN", expr.GetType().Name);
            Assert.Same(obj, expr.Object);

            Assert.Equal(N, expr.ArgumentCount);
            for (var i = 0; i < N; i++)
            {
                Assert.Same(args[i], expr.GetArgument(i));
            }

            Collections.ObjectModel.ReadOnlyCollection<Expression> arguments = expr.Arguments;
            Assert.Same(arguments, expr.Arguments);

            Assert.Equal(N, arguments.Count);
            for (var i = 0; i < N; i++)
            {
                Assert.Same(args[i], arguments[i]);
            }

            MethodCallExpression updated = expr.Update(obj, arguments.ToList());
            Assert.Same(expr, updated);

            var visited = (MethodCallExpression)new NopVisitor().Visit(expr);
            Assert.Same(expr, visited);

            var visitedObj = (MethodCallExpression)new VisitorObj().Visit(expr);
            Assert.NotSame(expr, visitedObj);
            Assert.NotSame(obj, visitedObj.Object);
            Assert.Same(arguments, visitedObj.Arguments);

            var visitedArgs = (MethodCallExpression)new VisitorArgs().Visit(expr);
            Assert.NotSame(expr, visitedArgs);
            Assert.Same(obj, visitedArgs.Object);
            Assert.NotSame(arguments, visitedArgs.Arguments);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public static void CheckCallIsOptimizedStatic(int arity)
        {
            AssertCallIsOptimizedStatic(arity);
        }

        [Fact]
        public static void CheckCallFactoryOptimisedStaticNullArgumentList() =>
            AssertStaticMethodCall(0, Expression.Call(typeof(MS).GetMethod(nameof(MS.S0)), default(Expression[])));

        [Fact]
        public static void CheckCallFactoryOptimizationStatic1()
        {
            MethodCallExpression expr = Expression.Call(typeof(MS).GetMethod("S1"), Expression.Constant(0));

            AssertStaticMethodCall(1, expr);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic2()
        {
            MethodCallExpression expr1 = Expression.Call(typeof(MS).GetMethod("S2"), Expression.Constant(0), Expression.Constant(1));
            MethodCallExpression expr2 = Expression.Call(null, typeof(MS).GetMethod("S2"), Expression.Constant(0), Expression.Constant(1));

            AssertStaticMethodCall(2, expr1);
            AssertStaticMethodCall(2, expr2);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic3()
        {
            MethodCallExpression expr1 = Expression.Call(typeof(MS).GetMethod("S3"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2));
            MethodCallExpression expr2 = Expression.Call(null, typeof(MS).GetMethod("S3"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2));

            AssertStaticMethodCall(3, expr1);
            AssertStaticMethodCall(3, expr2);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic4()
        {
            MethodCallExpression expr = Expression.Call(typeof(MS).GetMethod("S4"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3));

            AssertStaticMethodCall(4, expr);
        }

        [Fact]
        public static void CheckCallFactoryOptimizationStatic5()
        {
            MethodCallExpression expr = Expression.Call(typeof(MS).GetMethod("S5"), Expression.Constant(0), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3), Expression.Constant(4));

            AssertStaticMethodCall(5, expr);
        }

        [Fact]
        public static void CheckCallFactoryStaticN()
        {
            const int N = 6;

            ConstantExpression[] args = Enumerable.Range(0, N).Select(i => Expression.Constant(i)).ToArray();

            MethodCallExpression expr = Expression.Call(typeof(MS).GetMethod("S" + N), args);

            Assert.Equal("MethodCallExpressionN", expr.GetType().Name);
            Assert.Equal(N, expr.ArgumentCount);
            for (var i = 0; i < N; i++)
            {
                Assert.Same(args[i], expr.GetArgument(i));
            }

            Collections.ObjectModel.ReadOnlyCollection<Expression> arguments = expr.Arguments;
            Assert.Same(arguments, expr.Arguments);

            Assert.Equal(N, arguments.Count);
            for (var i = 0; i < N; i++)
            {
                Assert.Same(args[i], arguments[i]);
            }

            MethodCallExpression updated = expr.Update(null, arguments);
            Assert.Same(expr, updated);

            var visited = (MethodCallExpression)new NopVisitor().Visit(expr);
            Assert.Same(expr, visited);

            var visitedArgs = (MethodCallExpression)new VisitorArgs().Visit(expr);
            Assert.NotSame(expr, visitedArgs);
            Assert.Same(null, visitedArgs.Object);
            Assert.NotSame(arguments, visitedArgs.Arguments);
        }

        [Fact]
        public static void CheckArrayIndexOptimization1()
        {
            ParameterExpression instance = Expression.Parameter(typeof(int[]));
            ConstantExpression[] args = new[] { Expression.Constant(0) };
            MethodCallExpression expr = Expression.ArrayIndex(instance, args);
            MethodInfo method = typeof(int[]).GetMethod("Get", BindingFlags.Public | BindingFlags.Instance);

            AssertCallIsOptimized(expr, instance, method, args);
        }

        [Fact]
        public static void CheckArrayIndexOptimization2()
        {
            ParameterExpression instance = Expression.Parameter(typeof(int[,]));
            ConstantExpression[] args = new[] { Expression.Constant(0), Expression.Constant(0) };
            MethodCallExpression expr = Expression.ArrayIndex(instance, args);
            MethodInfo method = typeof(int[,]).GetMethod("Get", BindingFlags.Public | BindingFlags.Instance);

            AssertCallIsOptimized(expr, instance, method, args);
        }

        private static void AssertCallIsOptimizedInstance(int n)
        {
            MethodInfo method = typeof(MS).GetMethod("I" + n);

            AssertCallIsOptimized(method);
        }

        private static void AssertCallIsOptimizedStatic(int n)
        {
            MethodInfo method = typeof(MS).GetMethod("S" + n);

            AssertCallIsOptimized(method);
        }

        private static void AssertCallIsOptimized(MethodInfo method)
        {
            ParameterExpression instance = method.IsStatic ? null : Expression.Parameter(method.DeclaringType);

            int n = method.GetParameters().Length;

            // Expression[] overload
            {
                ConstantExpression[] args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToArray();
                MethodCallExpression expr = Expression.Call(instance, method, args);

                AssertCallIsOptimized(expr, instance, method, args);
            }

            // IEnumerable<Expression> overload
            {
                List<ConstantExpression> args = Enumerable.Range(0, n).Select(i => Expression.Constant(i)).ToList();
                MethodCallExpression expr = Expression.Call(instance, method, args);

                AssertCallIsOptimized(expr, instance, method, args);
            }
        }

        private static void AssertCallIsOptimized(MethodCallExpression expr, Expression instance, MethodInfo method, IReadOnlyList<Expression> args)
        {
            int n = method.GetParameters().Length;

            MethodCallExpression updatedArgs = UpdateArgs(expr);
            MethodCallExpression visitedArgs = VisitArgs(expr);
            var updatedObj = default(MethodCallExpression);
            var visitedObj = default(MethodCallExpression);

            MethodCallExpression[] nodes;

            if (instance == null)
            {
                nodes = new[] { expr, updatedArgs, visitedArgs };
            }
            else
            {
                updatedObj = UpdateObj(expr);
                visitedObj = VisitObj(expr);

                nodes = new[] { expr, updatedArgs, visitedArgs, updatedObj, visitedObj };
            }

            foreach (var node in nodes)
            {
                if (node != visitedObj && node != updatedObj)
                {
                    Assert.Same(instance, node.Object);
                }

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

                Assert.Throws<ArgumentOutOfRangeException>(() => argProvider.GetArgument(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => argProvider.GetArgument(n));

                if (node != visitedArgs) // our visitor clones argument nodes
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

        private static MethodCallExpression UpdateArgs(MethodCallExpression node)
        {
            // Tests the call of Update to Expression.Call factories.

            MethodCallExpression res = node.Update(node.Object, node.Arguments.ToArray());

            Assert.Same(node, res);

            return res;
        }

        [Fact]
        public static void UpdateStaticNull()
        {
            MethodCallExpression expr = Expression.Call(typeof(MS).GetMethod(nameof(MS.S0)));
            Assert.Same(expr, expr.Update(null, null));

            for (int argNum = 1; argNum != 7; ++argNum)
            {
                ConstantExpression[] args = Enumerable.Range(0, argNum).Select(i => Expression.Constant(i)).ToArray();

                expr = Expression.Call(typeof(MS).GetMethod("S" + argNum), args);

                // Should attempt to create new expression, and fail due to incorrect arguments.
                AssertExtensions.Throws<ArgumentException>("method", () => expr.Update(null, null));
            }
        }

        [Fact]
        public static void UpdateInstanceNull()
        {
            ConstantExpression instance = Expression.Constant(new MS());
            MethodCallExpression expr = Expression.Call(instance, typeof(MS).GetMethod(nameof(MS.I0)));
            Assert.Same(expr, expr.Update(instance, null));

            for (int argNum = 1; argNum != 6; ++argNum)
            {
                ConstantExpression[] args = Enumerable.Range(0, argNum).Select(i => Expression.Constant(i)).ToArray();

                expr = Expression.Call(instance, typeof(MS).GetMethod("I" + argNum), args);

                // Should attempt to create new expression, and fail due to incorrect arguments.
                AssertExtensions.Throws<ArgumentException>("method", () => expr.Update(instance, null));
            }
        }

        [Fact]
        public static void UpdateStaticExtraArguments()
        {
            for (int argNum = 0; argNum != 7; ++argNum)
            {
                ConstantExpression[] args = Enumerable.Range(0, argNum).Select(i => Expression.Constant(i)).ToArray();

                MethodCallExpression expr = Expression.Call(typeof(MS).GetMethod("S" + argNum), args);

                // Should attempt to create new expression, and fail due to incorrect arguments.
                AssertExtensions.Throws<ArgumentException>("method", () => expr.Update(null, args.Append(Expression.Constant(-1))));
            }
        }

        [Fact]
        public static void UpdateInstanceExtraArguments()
        {
            ConstantExpression instance = Expression.Constant(new MS());
            for (int argNum = 0; argNum != 6; ++argNum)
            {
                ConstantExpression[] args = Enumerable.Range(0, argNum).Select(i => Expression.Constant(i)).ToArray();

                MethodCallExpression expr = Expression.Call(instance, typeof(MS).GetMethod("I" + argNum), args);

                // Should attempt to create new expression, and fail due to incorrect arguments.
                AssertExtensions.Throws<ArgumentException>("method", () => expr.Update(instance, args.Append(Expression.Constant(-1))));
            }
        }

        [Fact]
        public static void UpdateStaticDifferentArguments()
        {
            for (int argNum = 1; argNum != 7; ++argNum)
            {
                ConstantExpression[] args = Enumerable.Range(0, argNum).Select(i => Expression.Constant(i)).ToArray();

                MethodCallExpression expr = Expression.Call(typeof(MS).GetMethod("S" + argNum), args);

                ConstantExpression[] newArgs = new ConstantExpression[argNum];
                for (int i = 0; i != argNum; ++i)
                {
                    args.CopyTo(newArgs, 0);
                    newArgs[i] = Expression.Constant(i);
                    Assert.NotSame(expr, expr.Update(null, newArgs));
                }
            }
        }

        [Fact]
        public static void UpdateInstanceDifferentArguments()
        {
            ConstantExpression instance = Expression.Constant(new MS());
            for (int argNum = 1; argNum != 6; ++argNum)
            {
                ConstantExpression[] args = Enumerable.Range(0, argNum).Select(i => Expression.Constant(i)).ToArray();

                MethodCallExpression expr = Expression.Call(instance, typeof(MS).GetMethod("I" + argNum), args);

                ConstantExpression[] newArgs = new ConstantExpression[argNum];
                for (int i = 0; i != argNum; ++i)
                {
                    args.CopyTo(newArgs, 0);
                    newArgs[i] = Expression.Constant(i);
                    Assert.NotSame(expr, expr.Update(instance, newArgs));
                }
            }
        }

        private static MethodCallExpression UpdateObj(MethodCallExpression node)
        {
            // Tests the call of Update to Expression.Call factories.

            MethodCallExpression res = node.Update(new VisitorObj().Visit(node.Object), node.Arguments);

            Assert.NotSame(node, res);

            return res;
        }

        private static MethodCallExpression VisitArgs(MethodCallExpression node)
        {
            // Tests dispatch of ExpressionVisitor into Rewrite method which calls Expression.Call factories.

            return (MethodCallExpression)new VisitorArgs().Visit(node);
        }

        private static MethodCallExpression VisitObj(MethodCallExpression node)
        {
            // Tests dispatch of ExpressionVisitor into Rewrite method which calls Expression.Call factories.

            return (MethodCallExpression)new VisitorObj().Visit(node);
        }

        class NopVisitor : ExpressionVisitor
        {
        }

        class VisitorArgs : ExpressionVisitor
        {
            protected override Expression VisitConstant(ConstantExpression node)
            {
                return Expression.Constant(node.Value, node.Type); // clones
            }
        }

        class VisitorObj : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return Expression.Parameter(node.Type, node.Name); // clones
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
        public static void S6(int a, int b, int c, int d, int e, int f) { }
    }
}
