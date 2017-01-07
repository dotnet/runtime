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
            Assert.Throws<ArgumentException>("method", () => exp.Update(new [] {Expression.Constant(null)}));
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
            Assert.Throws<ArgumentException>("method", () => exp.Update(null));
            Assert.Throws<ArgumentException>("method", () => exp.Update(new[] { arg, arg }));
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
            Assert.Throws<ArgumentException>("method", () => exp.Update(null));
            Assert.Throws<ArgumentException>("method", () => exp.Update(new Expression[0]));
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
            Assert.Throws<ArgumentException>("method", () => exp.Update(null));
            Assert.Throws<ArgumentException>("method", () => exp.Update(new Expression[0]));
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
            Assert.Throws<ArgumentException>("method", () => exp.Update(null));
            Assert.Throws<ArgumentException>("method", () => exp.Update(new Expression[0]));
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
            Assert.Throws<ArgumentException>("method", () => exp.Update(null));
            Assert.Throws<ArgumentException>("method", () => exp.Update(new Expression[0]));
        }
    }
}