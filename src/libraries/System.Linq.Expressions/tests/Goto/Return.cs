// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class Return : GotoExpressionTests
    {
        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void JustReturnValue(object value, bool useInterpreter)
        {
            Type type = value.GetType();
            LabelTarget target = Expression.Label(type);
            Expression block = Expression.Block(
                Expression.Return(target, Expression.Constant(value)),
                Expression.Label(target, Expression.Default(type))
                );
            Expression equals = Expression.Equal(Expression.Constant(value), block);
            Assert.True(Expression.Lambda<Func<bool>>(equals).Compile(useInterpreter)());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ReturnToMiddle(bool useInterpreter)
        {
            // The behaviour is that return jumps to a label, but does not necessarily leave a block.
            LabelTarget target = Expression.Label(typeof(int));
            Expression block = Expression.Block(
                Expression.Return(target, Expression.Constant(1)),
                Expression.Label(target, Expression.Constant(2)),
                Expression.Constant(3)
                );
            Assert.Equal(3, Expression.Lambda<Func<int>>(block).Compile(useInterpreter)());
        }

        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void ReturnJumps(object value, bool useInterpreter)
        {
            Type type = value.GetType();
            LabelTarget target = Expression.Label(type);
            Expression block = Expression.Block(
                Expression.Return(target, Expression.Constant(value)),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target, Expression.Default(type))
                );
            Assert.True(Expression.Lambda<Func<bool>>(Expression.Equal(Expression.Constant(value), block)).Compile(useInterpreter)());
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetReturnHasNoValue(Type type)
        {
            LabelTarget target = Expression.Label(type);
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Return(target));
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NonVoidTargetReturnHasNoValueTypeExplicit(Type type)
        {
            LabelTarget target = Expression.Label(type);
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Return(target, type));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ReturnVoidNoValue(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Return(target),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile(useInterpreter)();
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public void ReturnExplicitVoidNoValue(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression block = Expression.Block(
                Expression.Return(target, typeof(void)),
                Expression.Throw(Expression.Constant(new InvalidOperationException())),
                Expression.Label(target)
                );
            Expression.Lambda<Action>(block).Compile(useInterpreter)();
        }

        [Theory]
        [MemberData(nameof(TypesData))]
        public void NullValueOnNonVoidReturn(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Return(Expression.Label(type)));
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Return(Expression.Label(type), default(Expression)));
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Return(Expression.Label(type), null, type));
        }

        [Theory]
        [MemberData(nameof(ConstantValueData))]
        public void ExplicitNullTypeWithValue(object value)
        {
            AssertExtensions.Throws<ArgumentException>("target", () => Expression.Return(Expression.Label(value.GetType()), default(Type)));
        }

        [Fact]
        public void UnreadableLabel()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            LabelTarget target = Expression.Label(typeof(string));
            AssertExtensions.Throws<ArgumentException>("value", () => Expression.Return(target, value));
            AssertExtensions.Throws<ArgumentException>("value", () => Expression.Return(target, value, typeof(string)));
        }

        [Theory]
        [PerCompilationType(nameof(ConstantValueData))]
        public void CanAssignAnythingToVoid(object value, bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            BlockExpression block = Expression.Block(
                Expression.Return(target, Expression.Constant(value)),
                Expression.Label(target)
                );
            Assert.Equal(typeof(void), block.Type);
            Expression.Lambda<Action>(block).Compile(useInterpreter)();
        }

        [Theory]
        [MemberData(nameof(NonObjectAssignableConstantValueData))]
        public void CannotAssignValueTypesToObject(object value)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Return(Expression.Label(typeof(object)), Expression.Constant(value)));
        }

        [Theory]
        [PerCompilationType(nameof(ObjectAssignableConstantValueData))]
        public void ExplicitTypeAssigned(object value, bool useInterpreter)
        {
            LabelTarget target = Expression.Label(typeof(object));
            BlockExpression block = Expression.Block(
                Expression.Return(target, Expression.Constant(value), typeof(object)),
                Expression.Label(target, Expression.Default(typeof(object)))
                );
            Assert.Equal(typeof(object), block.Type);
            Assert.Equal(value, Expression.Lambda<Func<object>>(block).Compile(useInterpreter)());
        }

        [Fact]
        public void ReturnQuotesIfNecessary()
        {
            LabelTarget target = Expression.Label(typeof(Expression<Func<int>>));
            BlockExpression block = Expression.Block(
                Expression.Return(target, Expression.Lambda<Func<int>>(Expression.Constant(0))),
                Expression.Label(target, Expression.Default(typeof(Expression<Func<int>>)))
                );
            Assert.Equal(typeof(Expression<Func<int>>), block.Type);
        }

        [Fact]
        public void UpdateSameIsSame()
        {
            LabelTarget target = Expression.Label(typeof(int));
            Expression value = Expression.Constant(0);
            GotoExpression ret = Expression.Return(target, value);
            Assert.Same(ret, ret.Update(target, value));
            Assert.Same(ret, NoOpVisitor.Instance.Visit(ret));
        }

        [Fact]
        public void UpdateDifferentValueIsDifferent()
        {
            LabelTarget target = Expression.Label(typeof(int));
            GotoExpression ret = Expression.Return(target, Expression.Constant(0));
            Assert.NotSame(ret, ret.Update(target, Expression.Constant(0)));
        }

        [Fact]
        public void UpdateDifferentTargetIsDifferent()
        {
            Expression value = Expression.Constant(0);
            GotoExpression ret = Expression.Return(Expression.Label(typeof(int)), value);
            Assert.NotSame(ret, ret.Update(Expression.Label(typeof(int)), value));
        }

        [Fact]
        public void OpenGenericType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Return(Expression.Label(typeof(void)), typeof(List<>)));
        }

        [Fact]
        public static void TypeContainsGenericParameters()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Return(Expression.Label(typeof(void)), typeof(List<>.Enumerator)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Return(Expression.Label(typeof(void)), typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Fact]
        public void PointerType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Return(Expression.Label(typeof(void)), typeof(int).MakePointerType()));
        }

        [Fact]
        public void ByRefType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Return(Expression.Label(typeof(void)), typeof(int).MakeByRefType()));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void UndefinedLabel(bool useInterpreter)
        {
            Expression<Action> returnNowhere = Expression.Lambda<Action>(Expression.Return(Expression.Label()));
            Assert.Throws<InvalidOperationException>(() => returnNowhere.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void AmbiguousJump(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Action> exp = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.Return(target),
                    Expression.Block(Expression.Label(target)),
                    Expression.Block(Expression.Label(target))
                    )
                );
            Assert.Throws<InvalidOperationException>(() => exp.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void MultipleDefinitionsInSeparateBlocks(bool useInterpreter)
        {
            LabelTarget target = Expression.Label(typeof(int));
            Func<int> add = Expression.Lambda<Func<int>>(
                Expression.Add(
                    Expression.Add(
                        Expression.Block(
                            Expression.Return(target, Expression.Constant(6)),
                            Expression.Throw(Expression.Constant(new Exception())),
                            Expression.Label(target, Expression.Constant(0))
                        ),
                        Expression.Block(
                            Expression.Return(target, Expression.Constant(4)),
                            Expression.Throw(Expression.Constant(new Exception())),
                            Expression.Label(target, Expression.Constant(0))
                        )
                    ),
                    Expression.Block(
                        Expression.Return(target, Expression.Constant(5)),
                        Expression.Throw(Expression.Constant(new Exception())),
                        Expression.Label(target, Expression.Constant(0))
                    )
                )
            ).Compile(useInterpreter);
            Assert.Equal(15, add());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpIntoExpression(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Func<bool>> isInt = Expression.Lambda<Func<bool>>(
                Expression.Block(
                    Expression.Return(target),
                    Expression.TypeIs(Expression.Label(target), typeof(int))
                )
            );
            Assert.Throws<InvalidOperationException>(() => isInt.Compile(useInterpreter));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public void JumpInWithValue(bool useInterpreter)
        {
            LabelTarget target = Expression.Label(typeof(int));
            Expression<Func<int>> exp = Expression.Lambda<Func<int>>(
                Expression.Block(
                Expression.Return(target, Expression.Constant(2)),
                Expression.Block(
                    Expression.Label(target, Expression.Constant(3)))
                )
            );
            Assert.Throws<InvalidOperationException>(() => exp.Compile(useInterpreter));
        }

#pragma warning disable xUnit1013 // needs to be public for reflection-based test
        public static void DoNothing()
#pragma warning restore xUnit1013
        {
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        [ActiveIssue("https://github.com/mono/mono/issues/14919", TestRuntimes.Mono)]
        public void TailCallThenReturn(bool useInterpreter)
        {
            LabelTarget target = Expression.Label();
            Expression<Action> lambda = Expression.Lambda<Action>(
                Expression.Block(
                    Expression.Call(GetType().GetMethod(nameof(DoNothing))),
                    Expression.Return(target),
                    Expression.Throw(Expression.Constant(new Exception())),
                    Expression.Label(target)),
                true);
            Action act = lambda.Compile(useInterpreter);
            act();
            lambda.Verify(@"
.method void ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure)
{
  .maxstack 2

  IL_0000: tail.
  IL_0002: call       void class [System.Linq.Expressions.Tests]System.Linq.Expressions.Tests.Return::DoNothing()
  IL_0007: ret
  IL_0008: ldarg.0
  IL_0009: ldfld      class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure::Constants
  IL_000e: ldc.i4.0
  IL_000f: ldelem.ref
  IL_0010: castclass  class [System.Private.CoreLib]System.Exception
  IL_0015: throw
  IL_0016: ret
}", @"
object lambda_method(object[])
{
  .locals 0
  .maxstack 1
  .maxcontinuation 0

  IP_0000: Call(Void DoNothing())
  IP_0001: Goto[0] -> 4
  IP_0002: LoadCached(0: System.Exception: Exception of type 'System.Exception' was thrown.)
  IP_0003: Throw()
}");
        }
    }
}
