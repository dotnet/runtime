// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class DynamicExpressionTests
    {
        public static IEnumerable<object[]> SizesAndSuffixes =>
            Enumerable.Range(0, 6).Select(i => new object[] { i, i > 0 & i < 5 ? i.ToString() : "N" });

        private static Type[] Types = { typeof(int), typeof(object), typeof(DateTime), typeof(DynamicExpressionTests) };

        public static IEnumerable<object[]> SizesAndTypes
            => Enumerable.Range(1, 6).SelectMany(i => Types, (i, t) => new object[] { i, t });

        [Theory, MemberData(nameof(SizesAndSuffixes))]
        public void AritySpecialisedUsedWhenPossible(int size, string nameSuffix)
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Type delType = Expression.GetFuncType(
                Enumerable.Repeat(typeof(object), size + 1).Prepend(typeof(CallSite)).ToArray());
            DynamicExpression exp = DynamicExpression.MakeDynamic(
                delType, binder, Enumerable.Range(0, size).Select(_ => Expression.Constant(null)));
            Assert.Equal("DynamicExpression" + nameSuffix, exp.GetType().Name);
            exp = Expression.MakeDynamic(
                delType, binder, Enumerable.Range(0, size).Select(_ => Expression.Constant(null)));
            Assert.Equal("DynamicExpression" + nameSuffix, exp.GetType().Name);
            if (size != 0)
            {
                exp = Expression.Dynamic(
                    binder, typeof(object), Enumerable.Range(0, size).Select(_ => Expression.Constant(null)));
                Assert.Equal("DynamicExpression" + nameSuffix, exp.GetType().Name);
            }
        }

        [Theory, MemberData(nameof(SizesAndSuffixes))]
        public void TypedAritySpecialisedUsedWhenPossible(int size, string nameSuffix)
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Type delType = Expression.GetFuncType(
                Enumerable.Repeat(typeof(object), size).Append(typeof(string)).Prepend(typeof(CallSite)).ToArray());
            DynamicExpression exp = DynamicExpression.MakeDynamic(
                delType, binder, Enumerable.Range(0, size).Select(_ => Expression.Constant(null)));
            Assert.Equal("TypedDynamicExpression" + nameSuffix, exp.GetType().Name);
            exp = Expression.MakeDynamic(
                delType, binder, Enumerable.Range(0, size).Select(_ => Expression.Constant(null)));
            Assert.Equal("TypedDynamicExpression" + nameSuffix, exp.GetType().Name);
            if (size != 0)
            {
                exp = Expression.Dynamic(
                    binder, typeof(string), Enumerable.Range(0, size).Select(_ => Expression.Constant(null)));
                Assert.Equal("TypedDynamicExpression" + nameSuffix, exp.GetType().Name);
            }
        }

        [Theory, MemberData(nameof(SizesAndTypes))]
        public void TypeProperty(int size, Type type)
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            DynamicExpression exp = Expression.Dynamic(
                binder, type, Enumerable.Range(0, size).Select(_ => Expression.Constant(0)));
            Assert.Equal(type, exp.Type);
            Assert.Equal(ExpressionType.Dynamic, exp.NodeType);
        }

        [Theory, MemberData(nameof(SizesAndTypes))]
        public void Reduce(int size, Type type)
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            DynamicExpression exp = Expression.Dynamic(
                binder, type, Enumerable.Range(0, size).Select(_ => Expression.Constant(0)));
            Assert.True(exp.CanReduce);
            InvocationExpression reduced = (InvocationExpression)exp.ReduceAndCheck();
            Assert.Equal(exp.Arguments, reduced.Arguments.Skip(1));
        }

        [Theory, MemberData(nameof(SizesAndTypes))]
        public void GetArguments(int size, Type type)
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            ConstantExpression[] arguments = Enumerable.Range(0, size).Select(_ => Expression.Constant(0)).ToArray();
            DynamicExpression exp = Expression.Dynamic(binder, type, arguments);
            Assert.Equal(arguments, exp.Arguments);
        }

        [Theory, MemberData(nameof(SizesAndTypes))]
        public void ArgumentProvider(int size, Type type)
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            ConstantExpression[] arguments = Enumerable.Range(0, size).Select(_ => Expression.Constant(0)).ToArray();
            IArgumentProvider ap = Expression.Dynamic(binder, type, arguments);
            Assert.Equal(size, ap.ArgumentCount);
            for (int i = 0; i != size; ++i)
            {
                Assert.Same(arguments[i], ap.GetArgument(i));
            }

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ap.GetArgument(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => ap.GetArgument(size));
        }

        [Fact]
        public void UpdateToSameReturnsSame0()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            DynamicExpression exp = Expression.MakeDynamic(typeof(Func<CallSite, object>), binder);
            Assert.Same(exp, exp.Update(null));
            Assert.Same(exp, exp.Update(Array.Empty<Expression>()));
            Assert.Same(exp, exp.Update(Enumerable.Repeat<Expression>(null, 0)));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSame1()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            Expression arg = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(typeof(Func<CallSite, object, object>), binder, arg);
            Assert.Same(exp, exp.Update(new[] {arg}));
            Assert.Same(exp, exp.Update(Enumerable.Repeat(arg, 1)));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSame2()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object>), binder, arg0, arg1);
            Assert.Same(exp, exp.Update(new[] {arg0, arg1}));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSame3()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, object>), binder, arg0, arg1, arg2);
            Assert.Same(exp, exp.Update(new[] {arg0, arg1, arg2}));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSame4()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            Expression arg3 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, object, object>), binder, arg0, arg1, arg2, arg3);
            Assert.Same(exp, exp.Update(new[] {arg0, arg1, arg2, arg3}));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSame5()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] {CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)});
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            Expression arg3 = Expression.Constant(null);
            Expression arg4 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, object, object, object>), binder, arg0, arg1, arg2, arg3,
                arg4);
            Assert.Same(exp, exp.Update(new[] {arg0, arg1, arg2, arg3, arg4}));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSameTyped0()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            DynamicExpression exp = Expression.MakeDynamic(typeof(Func<CallSite, string>), binder);
            Assert.Same(exp, exp.Update(null));
            Assert.Same(exp, exp.Update(Array.Empty<Expression>()));
            Assert.Same(exp, exp.Update(Enumerable.Repeat<Expression>(null, 0)));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSameTyped1()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(typeof(Func<CallSite, object, string>), binder, arg);
            Assert.Same(exp, exp.Update(new[] { arg }));
            Assert.Same(exp, exp.Update(Enumerable.Repeat(arg, 1)));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSameTyped2()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, string>), binder, arg0, arg1);
            Assert.Same(exp, exp.Update(new[] { arg0, arg1 }));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSameTyped3()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, string>), binder, arg0, arg1, arg2);
            Assert.Same(exp, exp.Update(new[] { arg0, arg1, arg2 }));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSameTyped4()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            Expression arg3 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, object, string>), binder, arg0, arg1, arg2, arg3);
            Assert.Same(exp, exp.Update(new[] { arg0, arg1, arg2, arg3 }));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToSameReturnsSameTyped5()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            Expression arg3 = Expression.Constant(null);
            Expression arg4 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, object, object, string>), binder, arg0, arg1, arg2, arg3,
                arg4);
            Assert.Same(exp, exp.Update(new[] { arg0, arg1, arg2, arg3, arg4 }));
            Assert.Same(exp, exp.Update(exp.Arguments));
        }

        [Fact]
        public void UpdateToDifferentReturnsDifferent0()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            DynamicExpression exp = Expression.MakeDynamic(typeof(Func<CallSite, object>), binder);
            // Wrong number of arguments continues to attempt to create new expression, which fails.
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(new [] {Expression.Constant(null)}));
        }

        [Fact]
        public void UpdateToDifferentReturnsDifferent1()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(typeof(Func<CallSite, object, object>), binder, arg);
            Assert.NotSame(exp, exp.Update(new[] { Expression.Constant(null) }));
            // Wrong number of arguments continues to attempt to create new expression, which fails.
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(null));
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(new[] { arg, arg }));
        }

        [Fact]
        public void UpdateToDifferentReturnsDifferent2()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object>), binder, arg0, arg1);
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg0 }));
            Assert.NotSame(exp, exp.Update(new[] { arg1, arg0 }));
            // Wrong number of arguments continues to attempt to create new expression, which fails.
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(null));
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(new Expression[0]));
        }

        [Fact]
        public void UpdateToDifferentReturnsDifferent3()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, object>), binder, arg0, arg1, arg2);
            Assert.NotSame(exp, exp.Update(new[] { arg1, arg1, arg2 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg0, arg2 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg1, arg0 }));
            // Wrong number of arguments continues to attempt to create new expression, which fails.
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(null));
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(new Expression[0]));
        }

        [Fact]
        public void UpdateToDifferentReturnsDifferent4()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            Expression arg3 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, object, object>), binder, arg0, arg1, arg2, arg3);
            Assert.NotSame(exp, exp.Update(new[] { arg1, arg1, arg2, arg3 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg0, arg2, arg3 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg1, arg0, arg3 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg1, arg2, arg0 }));
            // Wrong number of arguments continues to attempt to create new expression, which fails.
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(null));
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(new Expression[0]));
        }

        [Fact]
        public void UpdateToDifferentReturnsDifferent5()
        {
            CallSiteBinder binder = Binder.GetMember(
                CSharpBinderFlags.None, "Member", GetType(),
                new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) });
            Expression arg0 = Expression.Constant(null);
            Expression arg1 = Expression.Constant(null);
            Expression arg2 = Expression.Constant(null);
            Expression arg3 = Expression.Constant(null);
            Expression arg4 = Expression.Constant(null);
            DynamicExpression exp = Expression.MakeDynamic(
                typeof(Func<CallSite, object, object, object, object, object, object>), binder, arg0, arg1, arg2, arg3,
                arg4);
            Assert.NotSame(exp, exp.Update(new[] { arg1, arg1, arg2, arg3, arg4 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg0, arg2, arg3, arg4 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg1, arg0, arg3, arg4 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg1, arg2, arg0, arg4 }));
            Assert.NotSame(exp, exp.Update(new[] { arg0, arg1, arg2, arg3, arg0 }));
            // Wrong number of arguments continues to attempt to create new expression, which fails.
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(null));
            AssertExtensions.Throws<ArgumentException>("method", () => exp.Update(new Expression[0]));
        }
    }
}
