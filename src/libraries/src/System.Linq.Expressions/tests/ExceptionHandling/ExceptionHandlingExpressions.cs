// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ExceptionHandlingExpressions
    {
        // As this class is only used here, it is distinguished from an exception
        // being thrown due to an actual error.
        protected class TestException : Exception
        {
            public TestException()
                : base("This is a test exception")
            {
            }
        }

        protected class DerivedTestException : TestException
        {
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ThrowNullSameAsRethrow(bool useInterpreter)
        {
            UnaryExpression rethrow = Expression.Rethrow();
            UnaryExpression nullThrow = Expression.Throw(null);
            Assert.Equal(rethrow.GetType(), nullThrow.GetType());
            TryExpression rethrowTwice = Expression.TryCatch(
                Expression.TryCatch(
                        Expression.Throw(Expression.Constant(new TestException())),
                        Expression.Catch(typeof(TestException), rethrow)
                    ),
                    Expression.Catch(typeof(TestException), nullThrow)
                );
            Action doRethrowTwice = Expression.Lambda<Action>(rethrowTwice).Compile(useInterpreter);
            Assert.Throws<TestException>(doRethrowTwice);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void TypedThrowNullSameAsRethrow(bool useInterpreter)
        {
            UnaryExpression rethrow = Expression.Rethrow(typeof(int));
            UnaryExpression nullThrow = Expression.Throw(null, typeof(int));
            Assert.Equal(rethrow.GetType(), nullThrow.GetType());
            TryExpression rethrowTwice = Expression.TryCatch(
                Expression.TryCatch(
                        Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                        Expression.Catch(typeof(TestException), rethrow)
                    ),
                    Expression.Catch(typeof(TestException), nullThrow)
                );
            Action doRethrowTwice = Expression.Lambda<Action>(rethrowTwice).Compile(useInterpreter);
            Assert.Throws<TestException>(() => doRethrowTwice());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CannotRethrowOutsideCatch(bool useInterpreter)
        {
            LambdaExpression rethrowNothing = Expression.Lambda<Action>(Expression.Rethrow());
            Assert.Throws<InvalidOperationException>(() => rethrowNothing.Compile(useInterpreter));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CannotRethrowWithinFinallyWithinCatch(bool useInterpreter)
        {
            LambdaExpression rethrowFinally = Expression.Lambda<Action>(
                Expression.TryCatch(
                    Expression.Empty(),
                    Expression.Catch(
                        typeof(Exception),
                        Expression.TryFinally(
                            Expression.Empty(),
                            Expression.Rethrow()
                            )
                        )
                    )
                );
            Assert.Throws<InvalidOperationException>(() => rethrowFinally.Compile(useInterpreter));
        }

        [Theory, InlineData(true)]
        public void CannotRethrowWithinFaultWithinCatch(bool useInterpreter)
        {
            LambdaExpression rethrowFinally = Expression.Lambda<Action>(
                Expression.TryCatch(
                    Expression.Empty(),
                    Expression.Catch(
                        typeof(Exception),
                        Expression.TryFault(
                            Expression.Empty(),
                            Expression.Rethrow()
                            )
                        )
                    )
                );
            Assert.Throws<InvalidOperationException>(() => rethrowFinally.Compile(useInterpreter));
        }

        [Fact, ActiveIssue(3838)]
        public void CannotRethrowWithinFaultWithinCatchCompiled()
        {
            CannotRethrowWithinFaultWithinCatch(false);
        }

        [Fact]
        public void CompilerCanCatchAndThrowNonExceptions()
        {
            TryExpression throwCatchString = Expression.TryCatch(
                Expression.Throw(Expression.Constant("Hello")),
                Expression.Catch(typeof(string), Expression.Empty())
                );
            Expression.Lambda<Action>(throwCatchString).Compile(false)();
        }

        [Fact]
        public void InterpreterCannotThrowNonExceptions()
        {
            UnaryExpression throwString = Expression.Throw(Expression.Constant("Hello"));
            var act = Expression.Lambda<Action>(throwString).Compile(true);
            Assert.Throws<InvalidOperationException>(act);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ThrowNullThrowsNRE(bool useInterpreter)
        {
            Action throwNull = Expression.Lambda<Action>(
                Expression.Throw(Expression.Constant(null, typeof(Expression)))
                ).Compile(useInterpreter);
            Assert.Throws<NullReferenceException>(throwNull);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ThrowNullThrowsCatchableNRE(bool useInterpreter)
        {
            Func<int> throwCatchNull = Expression.Lambda<Func<int>>(
                Expression.TryCatch(
                    Expression.Throw(Expression.Constant(null, typeof(ArgumentException)), typeof(int)),
                    Expression.Catch(typeof(ArgumentException), Expression.Constant(1)),
                    Expression.Catch(typeof(NullReferenceException), Expression.Constant(2)),
                    Expression.Catch(typeof(Expression), Expression.Constant(3))
                    )
                ).Compile(useInterpreter);
            Assert.Equal(2, throwCatchNull());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanCatchExceptionAsObject(bool useInterpreter)
        {
            Func<int> throwCatchAsObject = Expression.Lambda<Func<int>>(
                Expression.TryCatch(
                    Expression.Throw(Expression.Constant(new Exception()), typeof(int)),
                    Expression.Catch(typeof(ArgumentException), Expression.Constant(1)),
                    Expression.Catch(typeof(object), Expression.Constant(2))
                    )
                ).Compile(useInterpreter);
            Assert.Equal(2, throwCatchAsObject());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanCatchExceptionAsObjectObtainingException(bool useInterpreter)
        {
            Exception testException = new Exception();
            ParameterExpression param = Expression.Parameter(typeof(object));
            Func<object> throwCatchAsObject = Expression.Lambda<Func<object>>(
                Expression.TryCatch(
                    Expression.Throw(Expression.Constant(testException), typeof(object)),
                    Expression.Catch(typeof(ArgumentException), Expression.Constant("Will be skipped", typeof(object))),
                    Expression.Catch(param, param)
                    )
                ).Compile(useInterpreter);
            Assert.Same(testException, throwCatchAsObject());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanAccessExceptionCaught(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Variable(typeof(Exception));
            TryExpression throwCatch = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(string)),
                Expression.Catch(variable, Expression.Property(variable, "Message"))
                );
            Assert.Equal("This is a test exception", Expression.Lambda<Func<string>>(throwCatch).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FromMakeMethods(bool useInterpreter)
        {
            TryExpression tryExp = Expression.MakeTry(
                typeof(int),
                Expression.MakeUnary(ExpressionType.Throw, Expression.Constant(new TestException()), typeof(int)),
                null,
                null,
                new[] { Expression.MakeCatchBlock(typeof(TestException), null, Expression.Constant(3), null) }
                );
            Assert.Equal(3, Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter)());
        }

        [Fact]
        public void CannotReduceThrow()
        {
            UnaryExpression throwExp = Expression.Throw(Expression.Constant(new TestException()));
            Assert.False(throwExp.CanReduce);
            Assert.Same(throwExp, throwExp.Reduce());
            Assert.Throws<ArgumentException>(null, () => throwExp.ReduceAndCheck());
        }

        [Fact]
        public void CannotReduceTry()
        {
            TryExpression tryExp = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.False(tryExp.CanReduce);
            Assert.Same(tryExp, tryExp.Reduce());
            Assert.Throws<ArgumentException>(null, () => tryExp.ReduceAndCheck());
        }

        [Fact]
        public void CannotThrowValueType()
        {
            Assert.Throws<ArgumentException>("value", () => Expression.Throw(Expression.Constant(1)));
        }

        [Fact]
        public void CanCatchValueType()
        {
            // We can't test the actual catching with just C# and Expressions, but we can
            // test that creating such catch blocks doesn't throw.
            Expression.Catch(typeof(int), Expression.Empty());
            Expression.Catch(Expression.Variable(typeof(int)), Expression.Empty());
            Expression.Catch(Expression.Variable(typeof(int)), Expression.Empty(), Expression.Constant(true));
            Expression.Catch(typeof(int), Expression.Empty(), Expression.Constant(true));
        }

        [Fact]
        public void MustHaveCatchFinallyOrFault()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.MakeTry(typeof(int), Expression.Constant(1), null, null, null));
        }

        [Fact]
        public void FaultMustNotBeWithCatch()
        {
            Assert.Throws<ArgumentException>("fault", () => Expression.MakeTry(typeof(int), Expression.Constant(1), null, Expression.Constant(2), new[] { Expression.Catch(typeof(object), Expression.Constant(3)) }));
        }

        [Fact]
        public void FaultMustNotBeWithFinally()
        {
            Assert.Throws<ArgumentException>("fault", () => Expression.MakeTry(typeof(int), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3), null));
        }

        [Fact]
        public void TryMustNotHaveNullBody()
        {
            Assert.Throws<ArgumentNullException>("body", () => Expression.TryCatch(null, Expression.Catch(typeof(object), Expression.Constant(1))));
            Assert.Throws<ArgumentNullException>("body", () => Expression.TryCatchFinally(null, Expression.Constant(1), Expression.Catch(typeof(object), Expression.Constant(1))));
            Assert.Throws<ArgumentNullException>("body", () => Expression.TryFault(null, Expression.Constant(1)));
            Assert.Throws<ArgumentNullException>("body", () => Expression.TryFinally(null, Expression.Constant(1)));
        }

        [Fact]
        public void TryMustHaveReadableBody()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("body", () => Expression.TryCatch(value, Expression.Catch(typeof(object), Expression.Constant(1))));
            Assert.Throws<ArgumentException>("body", () => Expression.TryCatchFinally(value, Expression.Constant(1), Expression.Catch(typeof(object), Expression.Constant(1))));
            Assert.Throws<ArgumentException>("body", () => Expression.TryFault(value, Expression.Constant(1)));
            Assert.Throws<ArgumentException>("body", () => Expression.TryFinally(value, Expression.Constant(1)));
        }

        [Fact]
        public void FaultMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("fault", () => Expression.TryFault(Expression.Constant(1), value));
        }

        [Fact]
        public void FinallyMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("finally", () => Expression.TryFinally(Expression.Constant(1), value));
            Assert.Throws<ArgumentException>("finally", () => Expression.TryCatchFinally(Expression.Constant(1), value, Expression.Catch(typeof(object), Expression.Constant(1))));
        }

        [Theory]
        [InlineData(false)]
        public void NonExceptionDerivedExceptionWrapped(bool useInterpreter)
        {
            Action throwWrapped = Expression.Lambda<Action>(Expression.Throw(Expression.Constant("Hello"))).Compile(useInterpreter);
            var rwe = Assert.Throws<RuntimeWrappedException>(throwWrapped);
            Assert.Equal("Hello", rwe.WrappedException);
        }

        [Fact]
        [ActiveIssue(5898)]
        public void NonExceptionDerivedExceptionWrappedInterpreted()
        {
            NonExceptionDerivedExceptionWrapped(true);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FinallyDoesNotDetermineValue(bool useInterpreter)
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Constant(2));
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FinallyDoesNotNeedToMatchType(bool useInterpreter)
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Constant(""));
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FinallyCanBeVoid(bool useInterpreter)
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Empty());
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FinallyCanBeNonVoidWithVoidTry(bool useInterpreter)
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Empty(), Expression.Constant(0));
            Expression.Lambda<Action>(finally2).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FinallyDoesNotDetermineValueNothingCaught(bool useInterpreter)
        {
            TryExpression finally2 = Expression.TryCatchFinally(
                Expression.Constant(1),
                Expression.Constant(2),
                Expression.Catch(typeof(object), Expression.Constant(3))
                );
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FinallyDoesNotDetermineValueSomethingCaught(bool useInterpreter)
        {
            TryExpression finally2 = Expression.TryCatchFinally(
                Expression.Throw(Expression.Constant(new ArgumentException()), typeof(int)),
                Expression.Constant(2),
                Expression.Catch(typeof(ArgumentException), Expression.Constant(3))
                );
            Assert.Equal(3, Expression.Lambda<Func<int>>(finally2).Compile(useInterpreter)());
        }

        [Theory, InlineData(true)]
        public void FaultNotTriggeredOnNoThrow(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Parameter(typeof(int));
            LabelTarget target = Expression.Label(typeof(int));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(1)),
                Expression.TryFault(
                    Expression.Empty(),
                    Expression.Assign(variable, Expression.Constant(2))
                    ),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(int)))
                );
            Assert.Equal(1, Expression.Lambda<Func<int>>(block).Compile(useInterpreter)());
        }

        [Theory, InlineData(true)]
        public void FaultNotTriggeredOnNoThrowNonVoid(bool useInterpreter)
        {
            Func<int> func = Expression.Lambda<Func<int>>(
                Expression.TryFault(
                    Expression.Constant(42),
                    Expression.Throw(Expression.Constant(new TestException()))
                    )
                ).Compile(useInterpreter);
            Assert.Equal(42, func());
        }

        [Fact, ActiveIssue(3838)]
        public void FaultNotTriggeredOnNoThrowNonVoidCompiled()
        {
            FaultNotTriggeredOnNoThrowNonVoid(false);
        }

        [Theory, InlineData(true)]
        public void FaultTriggeredOnThrow(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Parameter(typeof(int));
            LabelTarget target = Expression.Label(typeof(int));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(1)),
                Expression.TryCatch(
                    Expression.TryFault(
                        Expression.Throw(Expression.Constant(new TestException())),
                        Expression.Assign(variable, Expression.Constant(2))
                        ),
                    Expression.Catch(typeof(TestException), Expression.Empty())
                    ),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(int)))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(useInterpreter)());
        }

        [Fact, ActiveIssue(3838)]
        public void FaultTriggeredOnThrowCompiled()
        {
            FaultTriggeredOnThrow(false);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FinallyTriggeredOnNoThrow(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Parameter(typeof(int));
            LabelTarget target = Expression.Label(typeof(int));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(1)),
                Expression.TryFinally(
                    Expression.Empty(),
                    Expression.Assign(variable, Expression.Constant(2))
                    ),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(int)))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(useInterpreter)());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ExceptionInFinallyReplacesException(bool useInterpreter)
        {
            Action act = Expression.Lambda<Action>(
                Expression.TryFinally(
                    Expression.Throw(Expression.Constant(new InvalidOperationException())),
                    Expression.Throw(Expression.Constant(new TestException()))
                    )
                ).Compile(useInterpreter);
            Assert.Throws<TestException>(act);
        }

        [Theory, InlineData(true)]
        public void ExceptionInFaultReplacesException(bool useInterpreter)
        {
            Action act = Expression.Lambda<Action>(
                Expression.TryFault(
                    Expression.Throw(Expression.Constant(new InvalidOperationException())),
                    Expression.Throw(Expression.Constant(new TestException()))
                    )
                ).Compile(useInterpreter);
            Assert.Throws<TestException>(act);
        }

        [Fact, ActiveIssue(3838)]
        public void ExceptionInFaultReplacesExceptionCompiled()
        {
            ExceptionInFaultReplacesException(false);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void FinallyTriggeredOnThrow(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Parameter(typeof(int));
            LabelTarget target = Expression.Label(typeof(int));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(1)),
                Expression.TryCatch(
                    Expression.TryFinally(
                        Expression.Throw(Expression.Constant(new TestException())),
                        Expression.Assign(variable, Expression.Constant(2))
                        ),
                    Expression.Catch(typeof(TestException), Expression.Empty())
                    ),
                Expression.Return(target, variable),
                Expression.Label(target, Expression.Default(typeof(int)))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CatchChaining(bool useInterpreter)
        {
            TryExpression chain = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new DerivedTestException()), typeof(int)),
                Expression.Catch(typeof(InvalidOperationException), Expression.Constant(1)),
                Expression.Catch(typeof(TestException), Expression.Constant(2)),
                Expression.Catch(typeof(DerivedTestException), Expression.Constant(3))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(chain).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void UserEmulatedFaultThroughCatch(bool useInterpreter)
        {
            ParameterExpression variable = Expression.Parameter(typeof(int));
            Func<int> func = Expression.Lambda<Func<int>>(
                Expression.Block(
                    new[] { variable },
                    Expression.TryCatch(
                        Expression.TryCatch(
                            Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                            Expression.Catch(typeof(Exception), Expression.Block(Expression.Assign(variable, Expression.Constant(10)), Expression.Rethrow(typeof(int))))
                            ),
                            Expression.Catch(typeof(TestException), Expression.Add(variable, Expression.Constant(2)))
                        )
                    )
                ).Compile(useInterpreter);
            Assert.Equal(12, func());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void UserEmulatedFaultThroughCatchVoid(bool useInterpreter)
        {
            StringBuilder output = new StringBuilder();
            ConstantExpression builder = Expression.Constant(output);
            var noTypes = new Type[0];
            Action act = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.TryCatch(
                        Expression.TryCatch(
                            Expression.Throw(Expression.Constant(new TestException())),
                            Expression.Catch(
                                typeof(Exception),
                                Expression.Block(
                                    Expression.Call(builder, "Append", noTypes, Expression.Constant('A')),
                                    Expression.Rethrow()
                                    )
                                )
                            ),
                            Expression.Catch(
                                typeof(TestException),
                                Expression.Block(
                                    Expression.Call(builder, "Append", noTypes, Expression.Constant('B')),
                                    Expression.Empty()
                                    )
                                )
                        )
                    )
                ).Compile(useInterpreter);
            act();
            Assert.Equal("AB", output.ToString());
        }

        [Fact]
        public void TypeInferred()
        {
            TryExpression noExplicitType = Expression.TryFault(Expression.Constant(1), Expression.Empty());
            Assert.Equal(typeof(int), noExplicitType.Type);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ExplicitType(bool useInterpreter)
        {
            TryExpression explicitType = Expression.MakeTry(typeof(object), Expression.Constant("hello"), Expression.Empty(), null, null);
            Assert.Equal(typeof(object), explicitType.Type);
            Assert.Equal("hello", Expression.Lambda<Func<object>>(explicitType).Compile(useInterpreter)());
        }

        [Fact]
        public void ByRefExceptionType()
        {
            ParameterExpression variable = Expression.Parameter(typeof(Exception).MakeByRefType());
            Assert.Throws<ArgumentException>("variable", () => Expression.Catch(variable, Expression.Empty()));
            Assert.Throws<ArgumentException>("variable", () => Expression.Catch(variable, Expression.Empty(), Expression.Constant(true)));
        }

        [Fact]
        public void NullTypeOnCatch()
        {
            Assert.Throws<ArgumentNullException>("type", () => Expression.Catch(default(Type), Expression.Empty()));
            Assert.Throws<ArgumentNullException>("type", () => Expression.Catch(default(Type), Expression.Empty(), Expression.Constant(true)));
        }

        [Fact]
        public void NullExceptionVariableOnCatch()
        {
            Assert.Throws<ArgumentNullException>("variable", () => Expression.Catch(default(ParameterExpression), Expression.Empty()));
            Assert.Throws<ArgumentNullException>("variable", () => Expression.Catch(default(ParameterExpression), Expression.Empty(), Expression.Constant(true)));
        }

        [Fact]
        public void CatchBodyMustBeNotBeNull()
        {
            Assert.Throws<ArgumentNullException>("body", () => Expression.Catch(typeof(Exception), null));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Catch(typeof(Exception), null, Expression.Constant(true)));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Catch(Expression.Parameter(typeof(Exception)), null));
            Assert.Throws<ArgumentNullException>("body", () => Expression.Catch(Expression.Parameter(typeof(Exception)), null, Expression.Constant(true)));
        }

        [Fact]
        public void CatchBodyMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("body", () => Expression.Catch(typeof(Exception), value));
            Assert.Throws<ArgumentException>("body", () => Expression.Catch(typeof(Exception), value, Expression.Constant(true)));
            Assert.Throws<ArgumentException>("body", () => Expression.Catch(Expression.Parameter(typeof(Exception)), value));
            Assert.Throws<ArgumentException>("body", () => Expression.Catch(Expression.Parameter(typeof(Exception)), value, Expression.Constant(true)));
        }

        [Fact]
        public void FilterMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("filter", () => Expression.Catch(typeof(Exception), Expression.Empty(), value));
            Assert.Throws<ArgumentException>("filter", () => Expression.Catch(Expression.Parameter(typeof(Exception)), Expression.Empty(), value));
        }

        [Fact]
        public void FilterMustBeBoolean()
        {
            Assert.Throws<ArgumentException>("filter", () => Expression.Catch(typeof(Exception), Expression.Empty(), Expression.Constant(42)));
            Assert.Throws<ArgumentException>("filter", () => Expression.Catch(Expression.Parameter(typeof(Exception)), Expression.Empty(), Expression.Constant(42)));
        }

        [Theory]
        [InlineData(true)]
        public void FilterOnCatch(bool useInterpreter)
        {
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(typeof(TestException), Expression.Constant(1), Expression.Constant(false)),
                Expression.Catch(typeof(TestException), Expression.Constant(2), Expression.Constant(true)),
                Expression.Catch(typeof(TestException), Expression.Constant(3))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FilterOnCatchCompiled(bool useInterpreter)
        {
            FilterOnCatch(false);
        }

        [Theory]
        [InlineData(true)]
        public void FilterCanAccessException(bool useInterpreter)
        {
            ParameterExpression exception = Expression.Variable(typeof(TestException));
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(exception, Expression.Constant(1), Expression.Equal(Expression.Constant("This is not a drill."), Expression.Property(exception, "Message"))),
                Expression.Catch(exception, Expression.Constant(2), Expression.Equal(Expression.Constant("This is a test exception"), Expression.Property(exception, "Message"))),
                Expression.Catch(exception, Expression.Constant(3))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FilterCanAccessExceptionCompiled(bool useInterpreter)
        {
            FilterCanAccessException(false);
        }

        [Theory]
        [InlineData(true)]
        public void FilterOverwiteExceptionVisibleToHandler(bool useInterpreter)
        {
            ParameterExpression exception = Expression.Variable(typeof(TestException));
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(bool)),
                Expression.Catch(
                    exception,
                    Expression.ReferenceEqual(Expression.Constant(null), exception),
                    Expression.Block(
                        Expression.Assign(exception, Expression.Constant(null, typeof(TestException))),
                        Expression.Constant(true)
                        )
                    )
                );
            Assert.True(Expression.Lambda<Func<bool>>(tryExp).Compile(useInterpreter)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FilterOverwiteExceptionVisibleToHandlerCompiler()
        {
            FilterOverwiteExceptionVisibleToHandler(false);
        }

        [Theory]
        [InlineData(true)]
        public void FilterOverwriteExceptionNotVisibleToNextFilterOrHandler(bool useInterpreter)
        {
            ParameterExpression exception = Expression.Variable(typeof(TestException));
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(
                    exception,
                    Expression.Constant(-1),
                    Expression.Block(
                        Expression.Assign(exception, Expression.Constant(null, typeof(TestException))),
                        Expression.Constant(false)
                        )
                    ),
                Expression.Catch(
                    exception,
                    Expression.Property(Expression.Property(exception, "Message"), "Length"),
                    Expression.ReferenceNotEqual(exception, Expression.Constant(null))
                )
            );
            Assert.Equal(24, Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FilterOverwriteExceptionNotVisibleToNextFilterOrHandlerCompiler()
        {
            FilterOverwriteExceptionNotVisibleToNextFilterOrHandler(false);
        }

        [Theory]
        [InlineData(true)]
        public void FilterBeforeInnerFinally(bool useInterpreter)
        {
            StringBuilder sb = new StringBuilder();

            /*
            Comparable to:

            try
            {
                try
                {
                    sb.Append("A");
                    throw new TestException();
                }
                finally
                {
                    sb.Append("B");
                }
            }
            catch (TestException) when (sb.Append("C") != null)
            {
                sb.Append("D");
            }

            The filter should execute on the first pass, so the result should be "ACBD".
            */

            ConstantExpression builder = Expression.Constant(sb);
            Type[] noTypes = Array.Empty<Type>();
            TryExpression tryExp = Expression.TryCatch(
                Expression.TryFinally(
                    Expression.Block(
                        Expression.Call(builder, "Append", noTypes, Expression.Constant('A')),
                        Expression.Throw(Expression.Constant(new TestException()), typeof(StringBuilder))
                        ),
                    Expression.Call(builder, "Append", noTypes, Expression.Constant('B'))
                ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Call(builder, "Append", noTypes, Expression.Constant('D')),
                    Expression.ReferenceNotEqual(Expression.Call(builder, "Append", noTypes, Expression.Constant('C')), Expression.Constant(null))
                    )
                );
            Func<StringBuilder> func = Expression.Lambda<Func<StringBuilder>>(tryExp).Compile(useInterpreter);
            Assert.Equal("ACBD", func().ToString());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FilterBeforeInnerFinallyCompiled()
        {
            FilterBeforeInnerFinally(false);
        }

        [Theory, InlineData(true)]
        public void FilterBeforeInnerFault(bool useInterpreter)
        {
            StringBuilder sb = new StringBuilder();
            ConstantExpression builder = Expression.Constant(sb);
            Type[] noTypes = Array.Empty<Type>();
            TryExpression tryExp = Expression.TryCatch(
                Expression.TryFault(
                    Expression.Block(
                        Expression.Call(builder, "Append", noTypes, Expression.Constant('A')),
                        Expression.Throw(Expression.Constant(new TestException()), typeof(StringBuilder))
                        ),
                    Expression.Call(builder, "Append", noTypes, Expression.Constant('B'))
                ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Call(builder, "Append", noTypes, Expression.Constant('D')),
                    Expression.ReferenceNotEqual(Expression.Call(builder, "Append", noTypes, Expression.Constant('C')), Expression.Constant(null))
                    )
                );
            Func<StringBuilder> func = Expression.Lambda<Func<StringBuilder>>(tryExp).Compile(useInterpreter);
            Assert.Equal("ACBD", func().ToString());
        }

        [Fact, ActiveIssue(3838)]
        public void FilterBeforeInnerFaultCompiled()
        {
            FilterBeforeInnerFault(false);
        }

        [Theory]
        [InlineData(true)]
        public void ExceptionThrownInFilter(bool useInterpreter)
        {
            // An exception in a filter should be eaten and the filter fail.

            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(2),
                    Expression.LessThan(
                        Expression.Constant(3),
                        Expression.Throw(Expression.Constant(new InvalidOperationException()), typeof(int))
                        )
                    ),
                Expression.Catch(typeof(TestException), Expression.Constant(9))
                );
            Func<int> func = Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter);
            Assert.Equal(9, func());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void ExceptionThrownInFilterCompiled()
        {
            ExceptionThrownInFilter(false);
        }

        private bool MethodWithManyArguments(
            int x, int y, int z,
            int α, int β, int γ, int δ,
            int klaatu, int barada, int nikto,
            int anáil, int nathrach, int ortha, int bháis, int @is, int beatha, int @do, int chéal, int déanaimh,
            bool returnBack)
        {
            return returnBack;
        }

        [Theory]
        [InlineData(true)]
        public void DeepExceptionFilter(bool useInterpreter)
        {
            // An expression where the deepest use of the stack is within filters.
            MethodCallExpression deepFalse = Expression.Call(
                Expression.Constant(this),
                "MethodWithManyArguments",
                new Type[0],
                Expression.Constant(1),
                Expression.Constant(2),
                Expression.Constant(3),
                Expression.Constant(4),
                Expression.Constant(5),
                Expression.Constant(6),
                Expression.Constant(7),
                Expression.Constant(8),
                Expression.Constant(9),
                Expression.Constant(10),
                Expression.Constant(11),
                Expression.Constant(12),
                Expression.Constant(13),
                Expression.Constant(14),
                Expression.Constant(15),
                Expression.Constant(16),
                Expression.Constant(17),
                Expression.Constant(18),
                Expression.Constant(19),
                Expression.Constant(false)
                );
            MethodCallExpression deepTrue = Expression.Call(
                Expression.Constant(this),
                "MethodWithManyArguments",
                new Type[0],
                Expression.Constant(1),
                Expression.Constant(2),
                Expression.Constant(3),
                Expression.Constant(4),
                Expression.Constant(5),
                Expression.Constant(6),
                Expression.Constant(7),
                Expression.Constant(8),
                Expression.Constant(9),
                Expression.Constant(10),
                Expression.Constant(11),
                Expression.Constant(12),
                Expression.Constant(13),
                Expression.Constant(14),
                Expression.Constant(15),
                Expression.Constant(16),
                Expression.Constant(17),
                Expression.Constant(18),
                Expression.Constant(19),
                Expression.Constant(true)
                );
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(typeof(TestException), Expression.Constant(1), deepFalse),
                Expression.Catch(typeof(TestException), Expression.Constant(2), deepTrue)
                );
            Func<int> func = Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter);
            Assert.Equal(2, func());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void DeepExceptionFilterCompiled()
        {
            DeepExceptionFilter(false);
        }

        [Theory]
        [InlineData(true)]
        public void JumpOutOfExceptionFilter(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            var tryExp = Expression.Lambda<Func<int>>(
                Expression.TryCatch(
                    Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                    Expression.Catch(
                        typeof(TestException),
                        Expression.Block(
                            Expression.Label(target),
                            Expression.Constant(0)
                            ),
                        Expression.Block(
                            Expression.Goto(target),
                            Expression.Constant(true)
                            )
                        )
                    )
                );
            Assert.Throws<InvalidOperationException>(() => tryExp.Compile(useInterpreter));
        }

        [Fact]
        [ActiveIssue(3838)]
        public void JumpOutOfExceptionFilterCompiled()
        {
            JumpOutOfExceptionFilter(false);
        }

        [Fact]
        public void NonAssignableTryAndCatchTypes()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.TryCatch(Expression.Constant(new Uri("http://example.net/")), Expression.Catch(typeof(Exception), Expression.Constant("hello"))));
        }

        [Fact]
        public void BodyTypeNotAssignableToTryType()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.MakeTry(typeof(int), Expression.Constant("hello"), Expression.Empty(), null, null));
        }

        [Fact]
        public void CatchTypeNotAssignableToTryType()
        {
            Assert.Throws<ArgumentException>(null, () => Expression.MakeTry(typeof(int), Expression.Constant(2), null, null, new[] { Expression.Catch(typeof(InvalidCastException), Expression.Constant("")) }));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanReturnAnythingFromExplicitVoid(bool useInterpreter)
        {
            TryExpression tryExp = Expression.MakeTry(
                typeof(void),
                Expression.Constant(1),
                null,
                null,
                new[]
                {
                    Expression.Catch(typeof(InvalidCastException), Expression.Constant("hello")),
                    Expression.Catch(typeof(Exception), Expression.Constant(2.2))
                }
                );
            Expression.Lambda<Action>(tryExp).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanReturnAnythingFromExplicitVoidVoidBody(bool useInterpreter)
        {
            TryExpression tryExp = Expression.MakeTry(
                typeof(void),
                Expression.Constant(1),
                null,
                null,
                new[]
                {
                    Expression.Catch(typeof(InvalidCastException), Expression.Constant("hello")),
                    Expression.Catch(typeof(Exception), Expression.Constant(2.2))
                }
                );
            Expression.Lambda<Action>(tryExp).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanReturnAnythingFromExplicitVoidVoidThrowingBody(bool useInterpreter)
        {
            TryExpression tryExp = Expression.MakeTry(
                typeof(void),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                null,
                null,
                new[]
                {
                    Expression.Catch(typeof(InvalidCastException), Expression.Constant("hello")),
                    Expression.Catch(typeof(Exception), Expression.Constant(2.2))
                }
                );
            Expression.Lambda<Action>(tryExp).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanReturnAnythingFromExplicitVoidTypedThrowingBody(bool useInterpreter)
        {
            TryExpression tryExp = Expression.MakeTry(
                typeof(void),
                Expression.Throw(Expression.Constant(new InvalidOperationException()), typeof(int)),
                null,
                null,
                new[]
                {
                    Expression.Catch(typeof(InvalidCastException), Expression.Constant("hello")),
                    Expression.Catch(typeof(Exception), Expression.Constant(2.2))
                }
                );
            Expression.Lambda<Action>(tryExp).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void CanReturnAnythingFromExplicitVoidCatch(bool useInterpreter)
        {
            TryExpression tryExp = Expression.MakeTry(
                typeof(void),
                Expression.Throw(Expression.Constant(new InvalidOperationException()), typeof(int)),
                null,
                null,
                new[]
                {
                    Expression.Catch(typeof(InvalidCastException), Expression.Empty()),
                    Expression.Catch(typeof(Exception), Expression.Constant(2.2))
                }
                );
            Expression.Lambda<Action>(tryExp).Compile(useInterpreter)();
        }

        [Fact]
        public void CatchesMustReturnVoidWithVoidBody()
        {
            Assert.Throws<ArgumentException>(
                () => Expression.TryCatch(
                    Expression.Empty(),
                    Expression.Catch(typeof(InvocationExpression), Expression.Constant("hello")),
                    Expression.Catch(typeof(Exception), Expression.Constant(2.2))
                    )
                 );
        }

        [Fact]
        public void UpdateCatchSameChildrenSameNode()
        {
            ParameterExpression var = Expression.Variable(typeof(Exception));
            Expression body = Expression.Empty();
            Expression filter = Expression.Default(typeof(bool));
            CatchBlock cb = Expression.Catch(var, body, filter);
            Assert.Same(cb, cb.Update(var, filter, body));
        }

        [Fact]
        public void UpdateCatchDifferentVariableDifferentNode()
        {
            Expression body = Expression.Empty();
            Expression filter = Expression.Default(typeof(bool));
            CatchBlock cb = Expression.Catch(Expression.Variable(typeof(Exception)), body, filter);
            Assert.NotSame(cb, cb.Update(Expression.Variable(typeof(Exception)), filter, body));
        }

        [Fact]
        public void UpdateCatchDifferentBodyDifferentNode()
        {
            ParameterExpression var = Expression.Variable(typeof(Exception));
            Expression filter = Expression.Default(typeof(bool));
            CatchBlock cb = Expression.Catch(var, Expression.Empty(), filter);
            Assert.NotSame(cb, cb.Update(var, filter, Expression.Empty()));
        }

        [Fact]
        public void UpdateCatchDifferentFilterDifferentNode()
        {
            ParameterExpression var = Expression.Variable(typeof(Exception));
            Expression body = Expression.Empty();
            CatchBlock cb = Expression.Catch(var, body, Expression.Default(typeof(bool)));
            Assert.NotSame(cb, cb.Update(var, Expression.Default(typeof(bool)), body));
        }

        [Fact]
        public void CatchToString()
        {
            CatchBlock cb = Expression.Catch(typeof(TestException), Expression.Empty());
            Assert.Equal("catch (" + cb.Test.Name + ") { ... }", cb.ToString());
            cb = Expression.Catch(Expression.Variable(typeof(TestException)), Expression.Empty());
            Assert.Equal("catch (" + cb.Test.Name + ") { ... }", cb.ToString());
        }

        [Fact]
        public void NamedExceptionCatchToString()
        {
            CatchBlock cb = Expression.Catch(Expression.Variable(typeof(TestException), "ex"), Expression.Empty());
            Assert.Equal("catch (" + cb.Test.Name + " ex) { ... }", cb.ToString());
        }

        [Fact]
        public void UpdateTrySameChildrenSameNode()
        {
            TryExpression tryExp = Expression.TryCatchFinally(Expression.Empty(), Expression.Empty(), Expression.Catch(typeof(Exception), Expression.Empty()));
            Assert.Same(tryExp, tryExp.Update(tryExp.Body, tryExp.Handlers, tryExp.Finally, tryExp.Fault));
            Assert.Same(tryExp, NoOpVisitor.Instance.Visit(tryExp));
            tryExp = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.Same(tryExp, tryExp.Update(tryExp.Body, tryExp.Handlers, tryExp.Finally, tryExp.Fault));
            Assert.Same(tryExp, NoOpVisitor.Instance.Visit(tryExp));
        }

        [Fact]
        [ActiveIssue(3958)]
        public void UpdateTrySameChildrenDifferentCollectionsSameNode()
        {
            TryExpression tryExp = Expression.TryCatchFinally(Expression.Empty(), Expression.Empty(), Expression.Catch(typeof(Exception), Expression.Empty()));
            Assert.Same(tryExp, tryExp.Update(tryExp.Body, tryExp.Handlers.ToArray(), tryExp.Finally, null));
            tryExp = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.Same(tryExp, tryExp.Update(tryExp.Body, null, null, tryExp.Fault));
        }

        [Fact]
        public void UpdateTryDiffBodyDiffNode()
        {
            TryExpression tryExp = Expression.TryCatchFinally(Expression.Empty(), Expression.Empty(), Expression.Catch(typeof(Exception), Expression.Empty()));
            Assert.NotSame(tryExp, tryExp.Update(Expression.Empty(), tryExp.Handlers, tryExp.Finally, null));
            tryExp = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.NotSame(tryExp, tryExp.Update(Expression.Empty(), null, null, tryExp.Fault));
        }

        [Fact]
        public void UpdateTryDiffHandlersDiffNode()
        {
            TryExpression tryExp = Expression.TryCatchFinally(Expression.Empty(), Expression.Empty(), Expression.Catch(typeof(Exception), Expression.Empty()));
            Assert.NotSame(tryExp, tryExp.Update(tryExp.Body, new[] { Expression.Catch(typeof(Exception), Expression.Empty()) }, tryExp.Finally, null));
        }

        [Fact]
        public void UpdateTryDiffFinallyDiffNode()
        {
            TryExpression tryExp = Expression.TryCatchFinally(Expression.Empty(), Expression.Empty(), Expression.Catch(typeof(Exception), Expression.Empty()));
            Assert.NotSame(tryExp, tryExp.Update(tryExp.Body, tryExp.Handlers, Expression.Empty(), null));
        }

        [Fact]
        public void UpdateTryDiffFaultDiffNode()
        {
            TryExpression tryExp = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.NotSame(tryExp, tryExp.Update(tryExp.Body, tryExp.Handlers, tryExp.Finally, Expression.Empty()));
        }

        [Fact]
        public void OpenGenericExceptionType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>), Expression.Constant(0), Expression.Constant(true)));
            Assert.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(List<>), null, Expression.Constant(0), null));
        }

        [Fact]
        public void ExceptionTypeContainingGenericParameters()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>.Enumerator), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>.Enumerator), Expression.Constant(0), Expression.Constant(true)));
            Assert.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(List<>.Enumerator), null, Expression.Constant(0), null));
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>).MakeGenericType(typeof(List<>)), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>).MakeGenericType(typeof(List<>)), Expression.Constant(0), Expression.Constant(true)));
            Assert.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(List<>).MakeGenericType(typeof(List<>)), null, Expression.Constant(0), null));
        }

        [Fact]
        public void PointerExceptionType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(int*), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(int*), Expression.Constant(0), Expression.Constant(true)));
            Assert.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(int*), null, Expression.Constant(0), null));
        }

        [Fact]
        public void TypedByRefExceptionType()
        {
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(int).MakeByRefType(), Expression.Constant(0)));
            Assert.Throws<ArgumentException>("type", () => Expression.Catch(typeof(int).MakeByRefType(), Expression.Constant(0), Expression.Constant(true)));
            Assert.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(int).MakeByRefType(), null, Expression.Constant(0), null));
        }

        [Fact]
        public void ToStringTest()
        {
            var e1 = Expression.Throw(Expression.Parameter(typeof(Exception), "ex"));
            Assert.Equal("throw(ex)", e1.ToString());

            var e2 = Expression.TryFinally(Expression.Empty(), Expression.Empty());
            Assert.Equal("try { ... }", e2.ToString());

            var e3 = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.Equal("try { ... }", e3.ToString());

            var e4 = Expression.TryCatch(Expression.Empty(), Expression.Catch(typeof(Exception), Expression.Empty()));
            Assert.Equal("try { ... }", e4.ToString());

            var e5 = Expression.Catch(typeof(Exception), Expression.Empty());
            Assert.Equal("catch (Exception) { ... }", e5.ToString());

            var e6 = Expression.Catch(Expression.Parameter(typeof(Exception), "ex"), Expression.Empty());
            Assert.Equal("catch (Exception ex) { ... }", e6.ToString());

            // NB: No ToString form for filters

            var e7 = Expression.Catch(Expression.Parameter(typeof(Exception), "ex"), Expression.Empty(), Expression.Constant(true));
            Assert.Equal("catch (Exception ex) { ... }", e7.ToString());
        }
    }
}
