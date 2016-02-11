// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        [Fact]
        public void ThrowNullSameAsRethrow()
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
            Action doRethrowTwice = Expression.Lambda<Action>(rethrowTwice).Compile(false);
            Assert.Throws<TestException>(doRethrowTwice);
        }

        [Fact]
        public void ThrowNullSameAsRethrowInterpreted()
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
            Action doRethrowTwice = Expression.Lambda<Action>(rethrowTwice).Compile(true);
            Assert.Throws<TestException>(doRethrowTwice);
        }

        [Fact]
        public void TypedThrowNullSameAsRethrow()
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
            Action doRethrowTwice = Expression.Lambda<Action>(rethrowTwice).Compile(false);
            Assert.Throws<TestException>(() => doRethrowTwice());
        }

        [Fact]
        public void TypedThrowNullSameAsRethrowInterpreted()
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
            Action doRethrowTwice = Expression.Lambda<Action>(rethrowTwice).Compile(true);
            Assert.Throws<TestException>(() => doRethrowTwice());
        }

        [Fact]
        public void CannotRethrowOutsideCatch()
        {
            LambdaExpression rethrowNothing = Expression.Lambda<Action>(Expression.Rethrow());
            Assert.Throws<InvalidOperationException>(() => rethrowNothing.Compile(false));
        }

        [Fact]
        public void CannotRethrowOutsideCatchInterpreted()
        {
            LambdaExpression rethrowNothing = Expression.Lambda<Action>(Expression.Rethrow());
            Assert.Throws<InvalidOperationException>(() => rethrowNothing.Compile(true));
        }

        [Fact]
        public void CanCatchAndThrowNonExceptions()
        {
            TryExpression throwCatchString = Expression.TryCatch(
                Expression.Throw(Expression.Constant("Hello")),
                Expression.Catch(typeof(string), Expression.Empty())
                );
            Expression.Lambda<Action>(throwCatchString).Compile(false)();
        }

        [Fact]
        [ActiveIssue(5898)]
        public void CanCatchAndThrowNonExceptionsInterpreted()
        {
            TryExpression throwCatchString = Expression.TryCatch(
                Expression.Throw(Expression.Constant("Hello")),
                Expression.Catch(typeof(string), Expression.Empty())
                );
            Expression.Lambda<Action>(throwCatchString).Compile(true)();
        }

        [Fact]
        public void CanAccessExceptionCaught()
        {
            ParameterExpression variable = Expression.Variable(typeof(Exception));
            TryExpression throwCatch = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(string)),
                Expression.Catch(variable, Expression.Property(variable, "Message"))
                );
            Assert.Equal("This is a test exception", Expression.Lambda<Func<string>>(throwCatch).Compile(false)());
        }

        [Fact]
        public void CanAccessExceptionCaughtInterpreted()
        {
            ParameterExpression variable = Expression.Variable(typeof(Exception));
            TryExpression throwCatch = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(string)),
                Expression.Catch(variable, Expression.Property(variable, "Message"))
                );
            Assert.Equal("This is a test exception", Expression.Lambda<Func<string>>(throwCatch).Compile(true)());
        }

        [Fact]
        public void FromMakeMethods()
        {
            TryExpression tryExp = Expression.MakeTry(
                typeof(int),
                Expression.MakeUnary(ExpressionType.Throw, Expression.Constant(new TestException()), typeof(int)),
                null,
                null,
                new[] { Expression.MakeCatchBlock(typeof(TestException), null, Expression.Constant(3), null) }
                );
            Assert.Equal(3, Expression.Lambda<Func<int>>(tryExp).Compile(false)());
        }

        [Fact]
        public void FromMakeMethodsInterpreted()
        {
            TryExpression tryExp = Expression.MakeTry(
                typeof(int),
                Expression.MakeUnary(ExpressionType.Throw, Expression.Constant(new TestException()), typeof(int)),
                null,
                null,
                new[] { Expression.MakeCatchBlock(typeof(TestException), null, Expression.Constant(3), null) }
                );
            Assert.Equal(3, Expression.Lambda<Func<int>>(tryExp).Compile(true)());
        }

        [Fact]
        public void CannotReduceThrow()
        {
            UnaryExpression throwExp = Expression.Throw(Expression.Constant(new TestException()));
            Assert.False(throwExp.CanReduce);
            Assert.Same(throwExp, throwExp.Reduce());
            Assert.Throws<ArgumentException>(() => throwExp.ReduceAndCheck());
        }

        [Fact]
        public void CannotReduceTry()
        {
            TryExpression tryExp = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.False(tryExp.CanReduce);
            Assert.Same(tryExp, tryExp.Reduce());
            Assert.Throws<ArgumentException>(() => tryExp.ReduceAndCheck());
        }

        [Fact]
        public void CannotThrowValueType()
        {
            Assert.Throws<ArgumentException>(() => Expression.Throw(Expression.Constant(1)));
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
            Assert.Throws<ArgumentException>(() => Expression.MakeTry(typeof(int), Expression.Constant(1), null, null, null));
        }

        [Fact]
        public void FaultMustNotBeWithCatch()
        {
            Assert.Throws<ArgumentException>(() => Expression.MakeTry(typeof(int), Expression.Constant(1), null, Expression.Constant(2), new[] { Expression.Catch(typeof(object), Expression.Constant(3)) }));
        }

        [Fact]
        public void FaultMustNotBeWithFinally()
        {
            Assert.Throws<ArgumentException>(() => Expression.MakeTry(typeof(int), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3), null));
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

        [Fact]
        public void NonExceptionDerivedExceptionWrapped()
        {
            Action throwWrapped = Expression.Lambda<Action>(Expression.Throw(Expression.Constant(""))).Compile(false);
            Exception ex = Assert.ThrowsAny<Exception>(throwWrapped);
            Assert.Equal("System.Runtime.CompilerServices.RuntimeWrappedException", ex.GetType().FullName);
        }

        [Fact]
        [ActiveIssue(5898)]
        public void NonExceptionDerivedExceptionWrappedInterpreted()
        {
            Action throwWrapped = Expression.Lambda<Action>(Expression.Throw(Expression.Constant(""))).Compile(true);
            Exception ex = Assert.ThrowsAny<Exception>(throwWrapped);
            Assert.Equal("System.Runtime.CompilerServices.RuntimeWrappedException", ex.GetType().FullName);
        }

        [Fact]
        public void FinallyDoesNotDetermineValue()
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Constant(2));
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(false)());
        }

        [Fact]
        public void FinallyDoesNotDetermineValueInterpreted()
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Constant(2));
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(true)());
        }

        [Fact]
        public void FinallyDoesNotNeedToMatchType()
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Constant(""));
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(false)());
        }

        [Fact]
        public void FinallyDoesNotNeedToMatchTypeInterpreted()
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Constant(""));
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(true)());
        }

        [Fact]
        public void FinallyCanBeVoid()
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Empty());
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(false)());
        }

        [Fact]
        public void FinallyCanBeVoidInterpreted()
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Constant(1), Expression.Empty());
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(true)());
        }

        [Fact]
        public void FinallyCanBeNonVoidWithVoidTry()
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Empty(), Expression.Constant(0));
            Expression.Lambda<Action>(finally2).Compile(false)();
        }

        [Fact]
        public void FinallyCanBeNonVoidWithVoidTryInterpreted()
        {
            TryExpression finally2 = Expression.TryFinally(Expression.Empty(), Expression.Constant(0));
            Expression.Lambda<Action>(finally2).Compile(true)();
        }

        [Fact]
        public void FinallyDoesNotDetermineValueNothingCaught()
        {
            TryExpression finally2 = Expression.TryCatchFinally(
                Expression.Constant(1),
                Expression.Constant(2),
                Expression.Catch(typeof(object), Expression.Constant(3))
                );
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(false)());
        }

        [Fact]
        public void FinallyDoesNotDetermineValueNothingCaughtInterpreted()
        {
            TryExpression finally2 = Expression.TryCatchFinally(
                Expression.Constant(1),
                Expression.Constant(2),
                Expression.Catch(typeof(object), Expression.Constant(3))
                );
            Assert.Equal(1, Expression.Lambda<Func<int>>(finally2).Compile(true)());
        }

        [Fact]
        public void FinallyDoesNotDetermineValueSomethingCaught()
        {
            TryExpression finally2 = Expression.TryCatchFinally(
                Expression.Throw(Expression.Constant(new ArgumentException()), typeof(int)),
                Expression.Constant(2),
                Expression.Catch(typeof(ArgumentException), Expression.Constant(3))
                );
            Assert.Equal(3, Expression.Lambda<Func<int>>(finally2).Compile(false)());
        }

        [Fact]
        public void FinallyDoesNotDetermineValueSomethingCaughtInterpreted()
        {
            TryExpression finally2 = Expression.TryCatchFinally(
                Expression.Throw(Expression.Constant(new ArgumentException()), typeof(int)),
                Expression.Constant(2),
                Expression.Catch(typeof(ArgumentException), Expression.Constant(3))
                );
            Assert.Equal(3, Expression.Lambda<Func<int>>(finally2).Compile(true)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FaultNotTriggeredOnNoThrow()
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
            Assert.Equal(1, Expression.Lambda<Func<int>>(block).Compile(false)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FaultNotTriggeredOnNoThrowInterpreted()
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
            Assert.Equal(1, Expression.Lambda<Func<int>>(block).Compile(true)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FaultTriggeredOnThrow()
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
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(false)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FaultTriggeredOnThrowInterpreted()
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
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(true)());
        }

        [Fact]
        public void FinallyTriggeredOnNoThrow()
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
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(false)());
        }

        [Fact]
        public void FinallyTriggeredOnNoThrowInterpreted()
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
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(true)());
        }

        [Fact]
        public void FinallyTriggeredOnThrow()
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
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(false)());
        }

        [Fact]
        public void FinallyTriggeredOnThrowInterpreted()
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
            Assert.Equal(2, Expression.Lambda<Func<int>>(block).Compile(true)());
        }

        [Fact]
        public void CatchChaining()
        {
            TryExpression chain = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new DerivedTestException()), typeof(int)),
                Expression.Catch(typeof(InvalidOperationException), Expression.Constant(1)),
                Expression.Catch(typeof(TestException), Expression.Constant(2)),
                Expression.Catch(typeof(DerivedTestException), Expression.Constant(3))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(chain).Compile(false)());
        }

        [Fact]
        public void CatchChainingInterpreted()
        {
            TryExpression chain = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new DerivedTestException()), typeof(int)),
                Expression.Catch(typeof(InvalidOperationException), Expression.Constant(1)),
                Expression.Catch(typeof(TestException), Expression.Constant(2)),
                Expression.Catch(typeof(DerivedTestException), Expression.Constant(3))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(chain).Compile(true)());
        }

        [Fact]
        public void TypeInferred()
        {
            TryExpression noExplicitType = Expression.TryFault(Expression.Constant(1), Expression.Empty());
            Assert.Equal(typeof(int), noExplicitType.Type);
        }

        [Fact]
        public void ExplicitType()
        {
            TryExpression explicitType = Expression.MakeTry(typeof(object), Expression.Constant("hello"), Expression.Empty(), null, null);
            Assert.Equal(typeof(object), explicitType.Type);
            Assert.Equal("hello", Expression.Lambda<Func<object>>(explicitType).Compile(false)());
        }

        [Fact]
        public void ExplicitTypeInterpreted()
        {
            TryExpression explicitType = Expression.MakeTry(typeof(object), Expression.Constant("hello"), Expression.Empty(), null, null);
            Assert.Equal(typeof(object), explicitType.Type);
            Assert.Equal("hello", Expression.Lambda<Func<object>>(explicitType).Compile(true)());
        }

        [Fact]
        public void ByRefExceptionType()
        {
            ParameterExpression variable = Expression.Parameter(typeof(Exception).MakeByRefType());
            Assert.Throws<ArgumentException>(() => Expression.Catch(variable, Expression.Empty()));
            Assert.Throws<ArgumentException>(() => Expression.Catch(variable, Expression.Empty(), Expression.Constant(true)));
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
            Assert.Throws<ArgumentException>(() => Expression.Catch(typeof(Exception), Expression.Empty(), Expression.Constant(42)));
            Assert.Throws<ArgumentException>(() => Expression.Catch(Expression.Parameter(typeof(Exception)), Expression.Empty(), Expression.Constant(42)));
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FilterOnCatch()
        {
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(typeof(TestException), Expression.Constant(1), Expression.Constant(false)),
                Expression.Catch(typeof(TestException), Expression.Constant(2), Expression.Constant(true)),
                Expression.Catch(typeof(TestException), Expression.Constant(3))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(tryExp).Compile(false)());
        }

        [Fact]
        [ActiveIssue(3838)]
        public void FilterOnCatchInterpreted()
        {
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(typeof(TestException), Expression.Constant(1), Expression.Constant(false)),
                Expression.Catch(typeof(TestException), Expression.Constant(2), Expression.Constant(true)),
                Expression.Catch(typeof(TestException), Expression.Constant(3))
                );
            Assert.Equal(2, Expression.Lambda<Func<int>>(tryExp).Compile(true)());
        }

        [Fact]
        public void NonAssignableTryAndCatchTypes()
        {
            Assert.Throws<ArgumentException>(() => Expression.TryCatch(Expression.Constant(new Uri("http://example.net/")), Expression.Catch(typeof(Exception), Expression.Constant("hello"))));
        }

        [Fact]
        public void BodyTypeNotAssignableToTryType()
        {
            Assert.Throws<ArgumentException>(() => Expression.MakeTry(typeof(int), Expression.Constant("hello"), Expression.Empty(), null, null));
        }

        [Fact]
        public void CatchTypeNotAssignableToTryType()
        {
            Assert.Throws<ArgumentException>(() => Expression.MakeTry(typeof(int), Expression.Constant(2), null, null, new[] { Expression.Catch(typeof(InvalidCastException), Expression.Constant("")) }));
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoid()
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
            Expression.Lambda<Action>(tryExp).Compile(false)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidInterpreted()
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
            Expression.Lambda<Action>(tryExp).Compile(true)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidVoidBody()
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
            Expression.Lambda<Action>(tryExp).Compile(false)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidVoidBodyInterpreted()
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
            Expression.Lambda<Action>(tryExp).Compile(true)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidVoidThrowingBody()
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
            Expression.Lambda<Action>(tryExp).Compile(false)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidVoidThrowingBodyInterpreted()
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
            Expression.Lambda<Action>(tryExp).Compile(true)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidTypedThrowingBody()
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
            Expression.Lambda<Action>(tryExp).Compile(false)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidTypedThrowingBodyInterpreted()
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
            Expression.Lambda<Action>(tryExp).Compile(true)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidCatch()
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
            Expression.Lambda<Action>(tryExp).Compile(false)();
        }

        [Fact]
        public void CanReturnAnythingFromExplicitVoidCatchInterpreted()
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
            Expression.Lambda<Action>(tryExp).Compile(true)();
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
            tryExp = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.Same(tryExp, tryExp.Update(tryExp.Body, tryExp.Handlers, tryExp.Finally, tryExp.Fault));
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
    }
}
