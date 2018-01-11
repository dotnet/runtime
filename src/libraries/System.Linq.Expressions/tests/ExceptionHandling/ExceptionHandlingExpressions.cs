// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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

        private static RuntimeWrappedException CreateRuntimeWrappedException(object inner)
        {
            return
                (RuntimeWrappedException)
                    typeof(RuntimeWrappedException).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.ExactBinding,
                                                                   null,
                                                                   new Type[] { typeof(object) },
                                                                   null).Invoke(new[] {inner});
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

        [Fact]
        public void DefaultThrowTypeIsVoid()
        {
            Assert.Equal(typeof(void), Expression.Throw(null).Type);
            Assert.Equal(typeof(void), Expression.Throw(Expression.Constant(new TestException())).Type);
            Assert.Equal(typeof(void), Expression.Rethrow().Type);
        }

        [Fact]
        public void ExceptionMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<Exception>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("value", () => Expression.Throw(value));
        }

        [Fact]
        public void GenericThrowType()
        {
            Type listType = typeof(List<>);
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Throw(Expression.Constant(new TestException()), listType));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Rethrow(listType));
        }

        [Fact]
        public void ThrowTypeWithGenericParamters()
        {
            Type listType = typeof(List<>);
            Type listListListType = listType.MakeGenericType(listType.MakeGenericType(listType));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Throw(Expression.Constant(new TestException()), listListListType));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Rethrow(listListListType));
        }

        [Fact]
        public void PointerThrowType()
        {
            Type pointer = typeof(int).MakeByRefType();
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Throw(Expression.Constant(new TestException()), pointer));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Rethrow(pointer));
        }

        [Fact]
        public void ByRefThrowType()
        {
            Type byRefType = typeof(int).MakeByRefType();
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Throw(Expression.Constant(new TestException()), byRefType));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Rethrow(byRefType));
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CanCatchAndThrowNonExceptions(bool useInterpreter)
        {
            TryExpression throwCatchString = Expression.TryCatch(
                Expression.Throw(Expression.Constant("Hello")),
                Expression.Catch(typeof(string), Expression.Empty())
                );
            Expression.Lambda<Action>(throwCatchString).Compile(useInterpreter)();
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CanCatchAndUseNonExceptions(bool useInterpreter)
        {
            ParameterExpression ex = Expression.Variable(typeof(string));
            TryExpression throwCatchString = Expression.TryCatch(
                Expression.Throw(Expression.Constant("Hello"), typeof(int)),
                Expression.Catch(ex, Expression.Property(ex, "Length"))
                );
            Func<int> func = Expression.Lambda<Func<int>>(throwCatchString).Compile(useInterpreter);
            Assert.Equal(5, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void DontCatchInternallWrappedExceptions(bool useInterpreter)
        {
            TryExpression throwCatch = Expression.TryCatch(
                Expression.Throw(Expression.Constant("Boo!")),
                Expression.Catch(typeof(RuntimeWrappedException), Expression.Empty()));
            Action throwRWE = Expression.Lambda<Action>(throwCatch).Compile(useInterpreter);
            RuntimeWrappedException rwe = Assert.Throws<RuntimeWrappedException>(throwRWE);
            Assert.Equal("Boo!", rwe.WrappedException);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ThrownNonExceptionPassesThroughNonMatchingHandlers(bool useInterpreter)
        {
            ParameterExpression ex = Expression.Variable(typeof(object));
            TryExpression nested = Expression.TryCatch(
                Expression.TryCatch(
                    Expression.Throw(Expression.Constant("1234567890"), typeof(int)),
                    Expression.Catch(typeof(RuntimeWrappedException), Expression.Constant(1)),
                    Expression.Catch(typeof(Exception), Expression.Constant(2))
                    ),
                Expression.Catch(ex, Expression.Property(Expression.Convert(ex, typeof(string)), "Length"))
                );
            Func<int> func = Expression.Lambda<Func<int>>(nested).Compile(useInterpreter);
            Assert.Equal(10, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void ExpressionsUnwrapeExternallyThrownRuntimeWrappedException(bool useInterpreter)
        {
            ParameterExpression exRWE = Expression.Variable(typeof(RuntimeWrappedException));
            ParameterExpression exEx = Expression.Variable(typeof(Exception));
            ParameterExpression exStr = Expression.Variable(typeof(string));
            RuntimeWrappedException rwe = CreateRuntimeWrappedException("1234");
            TryExpression tryCatch = Expression.TryCatch(
                Expression.Throw(Expression.Constant(rwe), typeof(int)),
                Expression.Catch(exRWE, Expression.Constant(0)),
                Expression.Catch(exEx, Expression.Constant(1)),
                Expression.Catch(exStr, Expression.Property(exStr, "Length")),
                Expression.Catch(typeof(object), Expression.Constant(3)));
            Func<int> func = Expression.Lambda<Func<int>>(tryCatch).Compile(useInterpreter);
            Assert.Equal(4, func());
        }

#if FEATURE_COMPILE

        [Theory, ClassData(typeof(CompilationTypes))]
        public void CatchFromExternallyThrownString(bool useInterpreter)
        {
            foreach(bool assemblyWraps in new []{false, true})
            {
                CustomAttributeBuilder custAtt =
                    new CustomAttributeBuilder(
                        typeof(RuntimeCompatibilityAttribute).GetConstructors()[0], Array.Empty<object>(),
                        new[]{ typeof(RuntimeCompatibilityAttribute).GetProperty(nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows)) },
                        new object[] { assemblyWraps });
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
                assembly.SetCustomAttribute(custAtt);
                ModuleBuilder module = assembly.DefineDynamicModule("Name");
                TypeBuilder type = module.DefineType("Type");
                MethodBuilder throwingMethod = type.DefineMethod(
                    "WillThrow", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Array.Empty<Type>());
                ILGenerator ilGen = throwingMethod.GetILGenerator();
                ilGen.Emit(OpCodes.Ldstr, "An Exceptional Exception!");
                ilGen.Emit(OpCodes.Throw);
                ilGen.Emit(OpCodes.Ret);
                Type createdType = type.CreateTypeInfo();
                ParameterExpression ex = Expression.Variable(typeof(string));
                TryExpression tryCatch =
                    Expression.TryCatch(
                        Expression.Block(
                            Expression.Call(createdType.GetMethod("WillThrow")), Expression.Constant("Nothing Thrown")),
                        Expression.Catch(ex, ex));
                Func<string> func = Expression.Lambda<Func<string>>(tryCatch).Compile(useInterpreter);
                Assert.Equal("An Exceptional Exception!", func());
            }
        }
#endif

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
            AssertExtensions.Throws<ArgumentException>(null, () => throwExp.ReduceAndCheck());
        }

        [Fact]
        public void CannotReduceTry()
        {
            TryExpression tryExp = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.False(tryExp.CanReduce);
            Assert.Same(tryExp, tryExp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => tryExp.ReduceAndCheck());
        }

        [Fact]
        public void CannotThrowValueType()
        {
            AssertExtensions.Throws<ArgumentException>("value", () => Expression.Throw(Expression.Constant(1)));
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
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.MakeTry(typeof(int), Expression.Constant(1), null, null, null));
        }

        [Fact]
        public void FaultMustNotBeWithCatch()
        {
            AssertExtensions.Throws<ArgumentException>("fault", () => Expression.MakeTry(typeof(int), Expression.Constant(1), null, Expression.Constant(2), new[] { Expression.Catch(typeof(object), Expression.Constant(3)) }));
        }

        [Fact]
        public void FaultMustNotBeWithFinally()
        {
            AssertExtensions.Throws<ArgumentException>("fault", () => Expression.MakeTry(typeof(int), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3), null));
        }

        [Fact]
        public void TryMustNotHaveNullBody()
        {
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.TryCatch(null, Expression.Catch(typeof(object), Expression.Constant(1))));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.TryCatchFinally(null, Expression.Constant(1), Expression.Catch(typeof(object), Expression.Constant(1))));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.TryFault(null, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.TryFinally(null, Expression.Constant(1)));
        }

        [Fact]
        public void TryMustHaveReadableBody()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("body", () => Expression.TryCatch(value, Expression.Catch(typeof(object), Expression.Constant(1))));
            AssertExtensions.Throws<ArgumentException>("body", () => Expression.TryCatchFinally(value, Expression.Constant(1), Expression.Catch(typeof(object), Expression.Constant(1))));
            AssertExtensions.Throws<ArgumentException>("body", () => Expression.TryFault(value, Expression.Constant(1)));
            AssertExtensions.Throws<ArgumentException>("body", () => Expression.TryFinally(value, Expression.Constant(1)));
        }

        [Fact]
        public void FaultMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("fault", () => Expression.TryFault(Expression.Constant(1), value));
        }

        [Fact]
        public void FinallyMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("finally", () => Expression.TryFinally(Expression.Constant(1), value));
            AssertExtensions.Throws<ArgumentException>("finally", () => Expression.TryCatchFinally(Expression.Constant(1), value, Expression.Catch(typeof(object), Expression.Constant(1))));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void NonExceptionDerivedExceptionWrapped(bool useInterpreter)
        {
            Action throwWrapped = Expression.Lambda<Action>(Expression.Throw(Expression.Constant("Hello"))).Compile(useInterpreter);
            RuntimeWrappedException rwe = Assert.Throws<RuntimeWrappedException>(throwWrapped);
            Assert.Equal("Hello", rwe.WrappedException);
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
        public void FinallyAndFaultAfterManyLabels(bool useInterpreter)
        {
            // There is a caching optimisation used below a certain number of faults or
            // finally, so go past that to catch any regressions in the non-optimised
            // path.
            ParameterExpression variable = Expression.Parameter(typeof(int));
            LabelTarget target = Expression.Label(typeof(int));
            BlockExpression block = Expression.Block(
                new[] { variable },
                Expression.Assign(variable, Expression.Constant(1)),
                Expression.TryCatch(
                    Expression.Block(
                        Enumerable.Repeat(Expression.TryFinally(Expression.Empty(), Expression.Empty()), 40).Append(
                            Expression.TryFault(
                                Expression.Throw(Expression.Constant(new TestException())),
                                Expression.Assign(variable, Expression.Constant(2))
                                )
                            )
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

        [Theory, ClassData(typeof(CompilationTypes))]
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
            AssertExtensions.Throws<ArgumentException>("variable", () => Expression.Catch(variable, Expression.Empty()));
            AssertExtensions.Throws<ArgumentException>("variable", () => Expression.Catch(variable, Expression.Empty(), Expression.Constant(true)));
        }

        [Fact]
        public void NullTypeOnCatch()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Catch(default(Type), Expression.Empty()));
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Catch(default(Type), Expression.Empty(), Expression.Constant(true)));
        }

        [Fact]
        public void NullExceptionVariableOnCatch()
        {
            AssertExtensions.Throws<ArgumentNullException>("variable", () => Expression.Catch(default(ParameterExpression), Expression.Empty()));
            AssertExtensions.Throws<ArgumentNullException>("variable", () => Expression.Catch(default(ParameterExpression), Expression.Empty(), Expression.Constant(true)));
        }

        [Fact]
        public void CatchBodyMustBeNotBeNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Catch(typeof(Exception), null));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Catch(typeof(Exception), null, Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Catch(Expression.Parameter(typeof(Exception)), null));
            AssertExtensions.Throws<ArgumentNullException>("body", () => Expression.Catch(Expression.Parameter(typeof(Exception)), null, Expression.Constant(true)));
        }

        [Fact]
        public void CatchBodyMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("body", () => Expression.Catch(typeof(Exception), value));
            AssertExtensions.Throws<ArgumentException>("body", () => Expression.Catch(typeof(Exception), value, Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentException>("body", () => Expression.Catch(Expression.Parameter(typeof(Exception)), value));
            AssertExtensions.Throws<ArgumentException>("body", () => Expression.Catch(Expression.Parameter(typeof(Exception)), value, Expression.Constant(true)));
        }

        [Fact]
        public void FilterMustBeReadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("filter", () => Expression.Catch(typeof(Exception), Expression.Empty(), value));
            AssertExtensions.Throws<ArgumentException>("filter", () => Expression.Catch(Expression.Parameter(typeof(Exception)), Expression.Empty(), value));
        }

        [Fact]
        public void FilterMustBeBoolean()
        {
            AssertExtensions.Throws<ArgumentException>("filter", () => Expression.Catch(typeof(Exception), Expression.Empty(), Expression.Constant(42)));
            AssertExtensions.Throws<ArgumentException>("filter", () => Expression.Catch(Expression.Parameter(typeof(Exception)), Expression.Empty(), Expression.Constant(42)));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, InlineData(true)]
        public void TryFinallyWithinFilter(bool useInterpreter)
        {
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(1),
                    Expression.TryFinally(Expression.Constant(false), Expression.Empty())
                    ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(2),
                    Expression.TryFinally(Expression.Constant(true), Expression.Empty())
                    ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(3)
                    )
                );
            Func<int> func = Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter);
            Assert.Equal(2, func());
        }

        [Fact, ActiveIssue(15719)]
        public void TryFinallyWithinFilterCompiled()
        {
            TryFinallyWithinFilter(false);
        }

        [Fact]
        public void TryFinallyWithinFilterCompiledProhibited()
        {
            // Ideally we can change this behaviour (see issue 15719 above),
            // but for now, check correct exception thrown.

            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(1),
                    Expression.TryFinally(Expression.Constant(false), Expression.Empty())
                ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(2),
                    Expression.TryFinally(Expression.Constant(true), Expression.Empty())
                ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(3)
                )
            );
            Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(tryExp);
#if FEATURE_COMPILE
            Assert.Throws<InvalidOperationException>(() => lambda.Compile(false));
#else
            lambda.Compile(true);
#endif
        }

        [Theory, InlineData(true)]
        public void TryCatchWithinFilter(bool useInterpreter)
        {
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(1),
                    Expression.TryCatch(Expression.Constant(false), Expression.Catch(typeof(Expression), Expression.Constant(false)))
                    ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(2),
                    Expression.TryCatch(Expression.Constant(true), Expression.Catch(typeof(Expression), Expression.Constant(true)))
                    ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(3)
                    )
                );
            Func<int> func = Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter);
            Assert.Equal(2, func());
        }

        [Fact, ActiveIssue(15719)]
        public void TryCatchWithinFilterCompiled()
        {
            TryCatchWithinFilter(false);
        }

        [Theory, InlineData(true)]
        public void TryCatchThrowingWithinFilter(bool useInterpreter)
        {
            TryExpression tryExp = Expression.TryCatch(
                Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(1),
                    Expression.TryCatch(Expression.Throw(Expression.Constant(new TestException()), typeof(bool)), Expression.Catch(typeof(Exception), Expression.Constant(false)))
                    ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(2),
                    Expression.TryCatch(Expression.Throw(Expression.Constant(new TestException()), typeof(bool)), Expression.Catch(typeof(Exception), Expression.Constant(true)))
                    ),
                Expression.Catch(
                    typeof(TestException),
                    Expression.Constant(3)
                    )
                );
            Func<int> func = Expression.Lambda<Func<int>>(tryExp).Compile(useInterpreter);
            Assert.Equal(2, func());
        }

        [Fact, ActiveIssue(15719)]
        public void TryCatchThrowingWithinFilterCompiled()
        {
            TryCatchThrowingWithinFilter(false);
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

        [Theory, ClassData(typeof(CompilationTypes))]
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

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpOutOfExceptionFilter(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Func<int>> tryExp = Expression.Lambda<Func<int>>(
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

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpOutOfFinally(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Func<int>> tryExp = Expression.Lambda<Func<int>>(
                Expression.Block(
                    Expression.Label(target),
                    Expression.TryFinally(
                        Expression.Throw(Expression.Constant(new TestException()), typeof(int)),
                            Expression.Block(
                                Expression.Goto(target),
                                Expression.Constant(true)
                                )
                            )
                        )
                    );
            Assert.Throws<InvalidOperationException>(() => tryExp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpIntoTry(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Action> tryExp = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.Goto(target),
                    Expression.TryFinally(
                        Expression.Label(target),
                        Expression.Empty())));
            Assert.Throws<InvalidOperationException>(() => tryExp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpOutOfTry(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Action> tryExp = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.TryFinally(
                        Expression.Block(
                            Expression.Goto(target),
                            Expression.Throw(Expression.Constant(new TestException()))),
                        Expression.Empty()),
                    Expression.Label(target)));
            Action act = tryExp.Compile(useInterpreter);
            act();
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpOutOfTryToPreviousLabel(bool useInterpreter)
        {
            LabelTarget skipStart = Expression.Label();
            LabelTarget skipToEnd = Expression.Label(typeof(int));
            LabelTarget backToStart = Expression.Label();
            Expression<Func<int>> tryExp = Expression.Lambda<Func<int>>(
                Expression.Block(
                    Expression.Goto(skipStart), Expression.Label(backToStart),
                    Expression.Return(skipToEnd, Expression.Constant(1)), Expression.Label(skipStart),
                    Expression.TryCatch(
                        Expression.Goto(backToStart), Expression.Catch(typeof(Exception), Expression.Empty())),
                    Expression.Return(skipToEnd, Expression.Constant(2)),
                    Expression.Label(skipToEnd, Expression.Constant(0))));
            Func<int> func = tryExp.Compile(useInterpreter);
            Assert.Equal(1, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpOutOfTryToPreviousLabelInOtherBlock(bool useInterpreter)
        {
            LabelTarget skipStart = Expression.Label();
            LabelTarget skipToEnd = Expression.Label(typeof(int));
            LabelTarget backToStart = Expression.Label();
            Expression<Func<int>> tryExp = Expression.Lambda<Func<int>>(
                Expression.Block(
                    Expression.Goto(skipStart),
                    Expression.Block(
                        Expression.Label(backToStart), Expression.Return(skipToEnd, Expression.Constant(1))),
                    Expression.Block(
                        Expression.Label(skipStart),
                        Expression.TryCatch(
                            Expression.Goto(backToStart), Expression.Catch(typeof(Exception), Expression.Empty())),
                        Expression.Return(skipToEnd, Expression.Constant(2))),
                    Expression.Label(skipToEnd, Expression.Constant(0))));
            Func<int> func = tryExp.Compile(useInterpreter);
            Assert.Equal(1, func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpOutOfCatch(bool useIntepreter)
        {
            LabelTarget target = Expression.Label(typeof(int));
            Expression<Func<int>> tryExp = Expression.Lambda<Func<int>>(
                Expression.Block(
                    Expression.TryCatch(
                        Expression.Throw(Expression.Constant(new Exception())),
                        Expression.Catch(
                            typeof(Exception),
                            Expression.Block(
                                Expression.Goto(target, Expression.Constant(1)),
                                Expression.Throw(Expression.Constant(new Exception()))))),
                    Expression.Return(target, Expression.Constant(2)),
                    Expression.Label(target, Expression.Constant(0))));
            Assert.Equal(1, tryExp.Compile(useIntepreter)());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpOutOfCatchToPreviousLabel(bool useIntepreter)
        {
            LabelTarget skipStart = Expression.Label();
            LabelTarget skipToEnd = Expression.Label(typeof(int));
            LabelTarget backToStart = Expression.Label();
            Expression<Func<int>> tryExp = Expression.Lambda<Func<int>>(
                Expression.Block(
                    Expression.Goto(skipStart), Expression.Label(backToStart),
                    Expression.Return(skipToEnd, Expression.Constant(1)), Expression.Label(skipStart),
                    Expression.TryCatch(
                        Expression.Throw(Expression.Constant(new Exception())),
                        Expression.Catch(typeof(Exception), Expression.Goto(backToStart))),
                    Expression.Return(skipToEnd, Expression.Constant(2)),
                    Expression.Label(skipToEnd, Expression.Constant(0))));
            Assert.Equal(1, tryExp.Compile(useIntepreter)());
        }

        [Fact]
        public void NonAssignableTryAndCatchTypes()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.TryCatch(Expression.Constant(new Uri("http://example.net/")), Expression.Catch(typeof(Exception), Expression.Constant("hello"))));
        }

        [Fact]
        public void BodyTypeNotAssignableToTryType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.MakeTry(typeof(int), Expression.Constant("hello"), Expression.Empty(), null, null));
        }

        [Fact]
        public void CatchTypeNotAssignableToTryType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.MakeTry(typeof(int), Expression.Constant(2), null, null, new[] { Expression.Catch(typeof(InvalidCastException), Expression.Constant("")) }));
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
            Assert.Throws<ArgumentException>(null, () => 
                Expression.TryCatch(
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
        public void UpdateDoesntRepeatEnumeration()
        {
            TryExpression tryExp = Expression.TryCatchFinally(Expression.Empty(), Expression.Empty(), Expression.Catch(typeof(Exception), Expression.Empty()));
            IEnumerable<CatchBlock> newHandlers =
                new RunOnceEnumerable<CatchBlock>(new[] {Expression.Catch(typeof(Exception), Expression.Empty())});
            Assert.NotSame(tryExp, tryExp.Update(tryExp.Body, newHandlers, tryExp.Finally, null));
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
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>), Expression.Constant(0), Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(List<>), null, Expression.Constant(0), null));
        }

        [Fact]
        public void ExceptionTypeContainingGenericParameters()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>.Enumerator), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>.Enumerator), Expression.Constant(0), Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(List<>.Enumerator), null, Expression.Constant(0), null));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>).MakeGenericType(typeof(List<>)), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(List<>).MakeGenericType(typeof(List<>)), Expression.Constant(0), Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(List<>).MakeGenericType(typeof(List<>)), null, Expression.Constant(0), null));
        }

        [Fact]
        public void PointerExceptionType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(int*), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(int*), Expression.Constant(0), Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(int*), null, Expression.Constant(0), null));
        }

        [Fact]
        public void TypedByRefExceptionType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(int).MakeByRefType(), Expression.Constant(0)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Catch(typeof(int).MakeByRefType(), Expression.Constant(0), Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.MakeCatchBlock(typeof(int).MakeByRefType(), null, Expression.Constant(0), null));
        }

        [Fact]
        public void ToStringTest()
        {
            UnaryExpression e1 = Expression.Throw(Expression.Parameter(typeof(Exception), "ex"));
            Assert.Equal("throw(ex)", e1.ToString());

            TryExpression e2 = Expression.TryFinally(Expression.Empty(), Expression.Empty());
            Assert.Equal("try { ... }", e2.ToString());

            TryExpression e3 = Expression.TryFault(Expression.Empty(), Expression.Empty());
            Assert.Equal("try { ... }", e3.ToString());

            TryExpression e4 = Expression.TryCatch(Expression.Empty(), Expression.Catch(typeof(Exception), Expression.Empty()));
            Assert.Equal("try { ... }", e4.ToString());

            CatchBlock e5 = Expression.Catch(typeof(Exception), Expression.Empty());
            Assert.Equal("catch (Exception) { ... }", e5.ToString());

            CatchBlock e6 = Expression.Catch(Expression.Parameter(typeof(Exception), "ex"), Expression.Empty());
            Assert.Equal("catch (Exception ex) { ... }", e6.ToString());

            // NB: No ToString form for filters

            CatchBlock e7 = Expression.Catch(Expression.Parameter(typeof(Exception), "ex"), Expression.Empty(), Expression.Constant(true));
            Assert.Equal("catch (Exception ex) { ... }", e7.ToString());
        }
    }
}
