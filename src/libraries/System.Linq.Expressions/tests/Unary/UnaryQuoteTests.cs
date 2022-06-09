// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

using static System.Linq.Expressions.Expression;

namespace System.Linq.Expressions.Tests
{
    public class UnaryQuoteTests
    {
        [Theory, ClassData(typeof(CompilationTypes))]
        public void QuotePreservesTypingOfBlock(bool useInterpreter)
        {
            ParameterExpression x = Parameter(typeof(int));

            Expression<Func<int, Type>> f1 =
                Lambda<Func<int, Type>>(
                    Call(
                        typeof(UnaryQuoteTests).GetMethod(nameof(Quote1)),
                        Lambda(
                            Block(typeof(void), x)
                        )
                    ),
                    x
                );

            Assert.Equal(typeof(void), f1.Compile(useInterpreter)(42));

            ParameterExpression s = Parameter(typeof(string));

            Expression<Func<string, Type>> f2 =
                Lambda<Func<string, Type>>(
                    Call(
                        typeof(UnaryQuoteTests).GetMethod(nameof(Quote2)),
                        Lambda(
                            Block(typeof(object), s)
                        )
                    ),
                    s
                );

            Assert.Equal(typeof(object), f2.Compile(useInterpreter)("bar"));
        }

        public static Type Quote1(Expression<Action> e) => e.Body.Type;
        public static Type Quote2(Expression<Func<object>> e) => e.Body.Type;

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Lambda_Action(bool useInterpreter)
        {
            Expression<Func<LambdaExpression>> f = () => GetQuote<Action>(() => Nop());

            var quote = f.Compile(useInterpreter)();

            Assert.Equal(0, quote.Parameters.Count);
            Assert.Equal(ExpressionType.Call, quote.Body.NodeType);

            var call = (MethodCallExpression)quote.Body;
            Assert.Equal(typeof(UnaryQuoteTests).GetMethod(nameof(Nop)), call.Method);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Lambda_Action_MakeUnary(bool useInterpreter)
        {
            Expression<Action> e = () => Nop();
            UnaryExpression q = MakeUnary(ExpressionType.Quote, e, null);
            Expression<Func<LambdaExpression>> f = Lambda<Func<LambdaExpression>>(q);

            var quote = f.Compile(useInterpreter)();

            Assert.Equal(0, quote.Parameters.Count);
            Assert.Equal(ExpressionType.Call, quote.Body.NodeType);

            var call = (MethodCallExpression)quote.Body;
            Assert.Equal(typeof(UnaryQuoteTests).GetMethod(nameof(Nop)), call.Method);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Lambda_IdentityFunc(bool useInterpreter)
        {
            Expression<Func<LambdaExpression>> f = () => GetQuote<Func<int, int>>(x => x);

            var quote = f.Compile(useInterpreter)();

            Assert.Equal(1, quote.Parameters.Count);
            Assert.Same(quote.Body, quote.Parameters[0]);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Lambda_Closure1(bool useInterpreter)
        {
            Expression<Func<int, LambdaExpression>> f = x => GetQuote<Func<int>>(() => x);

            var quote = f.Compile(useInterpreter)(42);

            Assert.Equal(0, quote.Parameters.Count);
            AssertIsBox(quote.Body, 42, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Lambda_Closure2(bool useInterpreter)
        {
            // Using an unchecked addition to ensure that an Add expression is used (and not AddChecked)
            Expression<Func<int, Func<int, LambdaExpression>>> f = x => y => GetQuote<Func<int>>(() => unchecked(x + y));

            var quote = f.Compile(useInterpreter)(1)(2);

            Assert.Equal(0, quote.Parameters.Count);

            Assert.Equal(ExpressionType.Add, quote.Body.NodeType);

            var add = (BinaryExpression)quote.Body;
            AssertIsBox(add.Left, 1, useInterpreter);
            AssertIsBox(add.Right, 2, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Block_Action(bool useInterpreter)
        {
            var expr =
                Block(
                    Call(typeof(UnaryQuoteTests).GetMethod(nameof(Nop)))
                );

            var f = BuildQuote<Func<LambdaExpression>, Action>(expr);

            var quote = f.Compile(useInterpreter)();

            Assert.Equal(0, quote.Parameters.Count);
            Assert.Same(expr, quote.Body);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Block_Local(bool useInterpreter)
        {
            var x = Parameter(typeof(int));

            var expr =
                Block(
                    new[] { x },
                    Assign(x, Constant(42)),
                    x
                );

            var f = BuildQuote<Func<LambdaExpression>, Func<int>>(expr);

            var quote = f.Compile(useInterpreter)();

            Assert.Equal(0, quote.Parameters.Count);
            Assert.Same(expr, quote.Body);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Block_Local_Shadow(bool useInterpreter)
        {
            var x = Parameter(typeof(int));

            var expr =
                Block(
                    new[] { x },
                    Assign(x, Constant(42)),
                    x
                );

            var f = BuildQuote<Func<int, LambdaExpression>, Func<int>>(expr, x);

            var quote = f.Compile(useInterpreter)(43);

            Assert.Equal(0, quote.Parameters.Count);
            Assert.Same(expr, quote.Body);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Block_Closure(bool useInterpreter)
        {
            var x = Parameter(typeof(int));

            var expr =
                Block(
                    x
                );

            var f = BuildQuote<Func<int, LambdaExpression>, Func<int>>(expr, x);

            var quote = f.Compile(useInterpreter)(42);

            Assert.Equal(0, quote.Parameters.Count);

            var block = quote.Body as BlockExpression;
            Assert.NotNull(block);
            Assert.Equal(0, block.Variables.Count);
            Assert.Equal(1, block.Expressions.Count);

            AssertIsBox(block.Expressions[0], 42, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_Block_LocalAndClosure(bool useInterpreter)
        {
            var x = Parameter(typeof(int));
            var y = Parameter(typeof(int));

            var expr =
                Block(
                    new[] { y },
                    Assign(y, Constant(2)),
                    Add(
                        x,
                        y
                    )
                );

            var f = BuildQuote<Func<int, LambdaExpression>, Func<int>>(expr, x);

            var quote = f.Compile(useInterpreter)(1);

            Assert.Equal(0, quote.Parameters.Count);

            var block = quote.Body as BlockExpression;
            Assert.NotNull(block);

            Assert.Equal(1, block.Variables.Count);
            Assert.Same(y, block.Variables[0]);

            Assert.Equal(2, block.Expressions.Count);
            Assert.Same(block.Expressions[0], expr.Expressions[0]);

            var expr1 = block.Expressions[1];
            Assert.Equal(ExpressionType.Add, expr1.NodeType);

            var add = (BinaryExpression)expr1;

            AssertIsBox(add.Left, 1, useInterpreter);
            Assert.Same(y, add.Right);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_CatchBlock_Local(bool useInterpreter)
        {
            var ex = Parameter(typeof(Exception));

            var expr =
                TryCatch(
                    Empty(),
                    Catch(
                        ex,
                        Empty()
                    )
                );

            var f = BuildQuote<Func<LambdaExpression>, Action>(expr);

            var quote = f.Compile(useInterpreter)();

            Assert.Equal(0, quote.Parameters.Count);
            Assert.Same(expr, quote.Body);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_CatchBlock_Variable_Closure1(bool useInterpreter)
        {
            var x = Parameter(typeof(int));
            var ex = Parameter(typeof(Exception));

            var expr =
                TryCatch(
                    x,
                    Catch(
                        ex,
                        Constant(0)
                    )
                );

            var f = BuildQuote<Func<int, LambdaExpression>, Func<int>>(expr, x);

            var quote = f.Compile(useInterpreter)(42);

            Assert.Equal(0, quote.Parameters.Count);

            var @try = quote.Body as TryExpression;
            Assert.NotNull(@try);

            AssertIsBox(@try.Body, 42, useInterpreter);
            Assert.Null(@try.Fault);
            Assert.Null(@try.Finally);

            Assert.NotNull(@try.Handlers);
            Assert.Equal(1, @try.Handlers.Count);

            var handler = @try.Handlers[0];
            Assert.Same(expr.Handlers[0], handler);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_CatchBlock_Variable_Closure2(bool useInterpreter)
        {
            var x = Parameter(typeof(int));
            var ex = Parameter(typeof(Exception));

            var expr =
                TryCatch(
                    Constant(0),
                    Catch(
                        ex,
                        x
                    )
                );

            var f = BuildQuote<Func<int, LambdaExpression>, Func<int>>(expr, x);

            var quote = f.Compile(useInterpreter)(42);

            Assert.Equal(0, quote.Parameters.Count);

            var @try = quote.Body as TryExpression;
            Assert.NotNull(@try);

            Assert.Same(expr.Body, @try.Body);
            Assert.Null(@try.Fault);
            Assert.Null(@try.Finally);

            Assert.NotNull(@try.Handlers);
            Assert.Equal(1, @try.Handlers.Count);

            var handler = @try.Handlers[0];
            Assert.Null(handler.Filter);
            Assert.Same(ex, handler.Variable);
            AssertIsBox(@handler.Body, 42, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_CatchBlock_NoVariable_Closure1(bool useInterpreter)
        {
            var x = Parameter(typeof(int));

            var expr =
                TryCatch(
                    x,
                    Catch(
                        typeof(Exception),
                        Constant(0)
                    )
                );

            var f = BuildQuote<Func<int, LambdaExpression>, Func<int>>(expr, x);

            var quote = f.Compile(useInterpreter)(42);

            Assert.Equal(0, quote.Parameters.Count);

            var @try = quote.Body as TryExpression;
            Assert.NotNull(@try);

            AssertIsBox(@try.Body, 42, useInterpreter);
            Assert.Null(@try.Fault);
            Assert.Null(@try.Finally);

            Assert.NotNull(@try.Handlers);
            Assert.Equal(1, @try.Handlers.Count);

            var handler = @try.Handlers[0];
            Assert.Same(expr.Handlers[0], handler);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_CatchBlock_NoVariable_Closure2(bool useInterpreter)
        {
            var x = Parameter(typeof(int));

            var expr =
                TryCatch(
                    Constant(0),
                    Catch(
                        typeof(Exception),
                        x
                    )
                );

            var f = BuildQuote<Func<int, LambdaExpression>, Func<int>>(expr, x);

            var quote = f.Compile(useInterpreter)(42);

            Assert.Equal(0, quote.Parameters.Count);

            var @try = quote.Body as TryExpression;
            Assert.NotNull(@try);

            Assert.Same(expr.Body, @try.Body);
            Assert.Null(@try.Fault);
            Assert.Null(@try.Finally);

            Assert.NotNull(@try.Handlers);
            Assert.Equal(1, @try.Handlers.Count);

            var handler = @try.Handlers[0];
            Assert.Null(handler.Filter);
            Assert.Null(handler.Variable);
            AssertIsBox(@handler.Body, 42, useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_RuntimeVariables_Closure(bool useInterpreter)
        {
            var x = Parameter(typeof(int));

            var expr =
                RuntimeVariables(
                    x
                );

            var f = BuildQuote<Func<int, Expression<Func<IRuntimeVariables>>>, Func<IRuntimeVariables>>(expr, x);

            var quote = f.Compile(useInterpreter)(42);

            var vars = quote.Compile(useInterpreter)();
            Assert.Equal(1, vars.Count);
            Assert.Equal(42, vars[0]);

            vars[0] = 43;
            Assert.Equal(43, vars[0]);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void Quote_RuntimeVariables_Local(bool useInterpreter)
        {
            var x = Parameter(typeof(int));

            var expr =
                Block(
                    new[] { x },
                    Assign(x, Constant(42)),
                    RuntimeVariables(
                        x
                    )
                );

            var f = BuildQuote<Func<Expression<Func<IRuntimeVariables>>>, Func<IRuntimeVariables>>(expr);

            var quote = f.Compile(useInterpreter)();

            var vars = quote.Compile(useInterpreter)();
            Assert.Equal(1, vars.Count);
            Assert.Equal(42, vars[0]);

            vars[0] = 43;
            Assert.Equal(43, vars[0]);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69944", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void Quote_RuntimeVariables_ClosureAndLocal(bool useInterpreter)
        {
            var x = Parameter(typeof(int));
            var y = Parameter(typeof(int));

            var expr =
                Block(
                    new[] { y },
                    Assign(y, Constant(2)),
                    RuntimeVariables(
                        x,
                        y
                    )
                );

            var f = BuildQuote<Func<int, Expression<Func<IRuntimeVariables>>>, Func<IRuntimeVariables>>(expr, x);

            var quote = f.Compile(useInterpreter)(1);

            var vars = quote.Compile(useInterpreter)();
            Assert.Equal(2, vars.Count);
            Assert.Equal(1, vars[0]);
            Assert.Equal(2, vars[1]);

            vars[0] = 3;
            vars[1] = 4;
            Assert.Equal(3, vars[0]);
            Assert.Equal(4, vars[1]);
        }

        [Fact]
        public void NullLambda()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Quote(null));
        }

        [Fact]
        public void QuoteNonLamdba()
        {
            Func<int> zero = () => 0;
            Expression funcConst = Constant(zero);
            AssertExtensions.Throws<ArgumentException>("expression", () => Quote(funcConst));
        }

        [Fact]
        public void CannotReduce()
        {
            Expression<Func<int>> exp = () => 2;
            Expression q = Expression.Quote(exp);
            Assert.False(q.CanReduce);
            Assert.Same(q, q.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => q.ReduceAndCheck());
        }

        [Fact]
        public void TypeExplicitWithGeneralLambdaArgument()
        {
            LambdaExpression lambda = Lambda(Constant(2));
            Expression q = Quote(lambda);
            Assert.Equal(typeof(Expression<Func<int>>), q.Type);
        }

        private void AssertIsBox<T>(Expression expression, T value, bool isInterpreted)
        {
            if (isInterpreted)
            {
                // See https://github.com/dotnet/runtime/issues/18278 for the difference between
                // runtime expression quoting in the compiler and the interpreter.

                Assert.Equal(ExpressionType.Convert, expression.NodeType);

                var convert = (UnaryExpression)expression;
                Assert.Equal(typeof(T), convert.Type);

                AssertBox<object>(convert.Operand, value);
            }
            else
            {
                AssertBox(expression, value);
            }
        }

        private void AssertBox<T>(Expression expression, T value)
        {
            Assert.Equal(ExpressionType.MemberAccess, expression.NodeType);

            var member = (MemberExpression)expression;

            var field = member.Member as FieldInfo;
            Assert.NotNull(field);
            Assert.Equal(typeof(StrongBox<T>).GetField(nameof(StrongBox<T>.Value)), field);

            var constant = member.Expression as ConstantExpression;
            Assert.NotNull(constant);

            var box = constant.Value as StrongBox<T>;
            Assert.NotNull(box);
            Assert.Equal(value, box.Value);
        }

        private static Expression<TDelegate> BuildQuote<TDelegate, TQuoteType>(Expression body, params ParameterExpression[] parameters)
        {
            var expr =
                Lambda<TDelegate>(
                    Call(
                        typeof(UnaryQuoteTests).GetMethod(nameof(GetQuote)).MakeGenericMethod(typeof(TQuoteType)),
                        Quote(
                            Lambda<TQuoteType>(body)
                        )
                    ),
                    parameters
                );

            return expr;
        }

        public static Expression<T> GetQuote<T>(Expression<T> e) => e;

#pragma warning disable xUnit1013 // needs to be public for reflection-based test
        public static void Nop() { }
#pragma warning restore xUnit1013
    }
}
