// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Dynamic.Tests
{
    public class InvokeBinderTests
    {
        private class MinimumOverrideInvokeBinder : InvokeBinder
        {
            public MinimumOverrideInvokeBinder(CallInfo callInfo)
                : base(callInfo)
            {
            }

            public override DynamicMetaObject FallbackInvoke(
                DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        [Fact]
        public void DynamicIsAction()
        {
            bool changed = false;
            Action change = () => changed = true;
            dynamic d = change;
            d();
            Assert.True(changed);
        }

        [Fact]
        public void DynamicIsActionArgument()
        {
            bool isEven = false;
            Action<int> reportParity = x => isEven = x % 2 == 0;
            dynamic even = 22;
            reportParity(even);
            Assert.True(isEven);
        }

        [Fact]
        public void DynamicIsFunc()
        {
            Func<int, int> doubleIt = x => x * 2;
            dynamic d = doubleIt;
            Assert.Equal(98, d(49));
        }

        [Fact]
        public void DynamicIsFuncArgument()
        {
            Func<int, int> doubleIt = x => x * 2;
            dynamic d = 49;
            Assert.Equal(98, doubleIt(d));
        }

        [Fact]
        public void DynamicIsArgumentToDynamicFunc()
        {
            Func<int, int> doubleIt = x => x * 2;
            dynamic dFunc = doubleIt;
            dynamic dArg = 49;
            Assert.Equal(98, dFunc(dArg));
        }

        [Fact]
        public void NullCallInfo()
        {
            AssertExtensions.Throws<ArgumentNullException>("callInfo", () => new MinimumOverrideInvokeBinder(null));
        }

        [Fact]
        public void CallInfoStored()
        {
            CallInfo info = new CallInfo(0);
            Assert.Same(info, new MinimumOverrideInvokeBinder(info).CallInfo);
        }

        [Fact]
        public void ReturnTypeObject()
        {
            Assert.Equal(typeof(object), new MinimumOverrideInvokeBinder(new CallInfo(0)).ReturnType);
        }

        [Fact]
        public void NullTarget()
        {
            var binder = new MinimumOverrideInvokeBinder(new CallInfo(0));
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("target", () => binder.Bind(null, new[] { arg }));
        }

        [Fact]
        public void NullArgs()
        {
            var binder = new MinimumOverrideInvokeBinder(new CallInfo(0));
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("args", () => binder.Bind(target, null));
        }

        [Fact]
        public void NullArg()
        {
            var binder = new MinimumOverrideInvokeBinder(new CallInfo(0));
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("args[1]", () => binder.Bind(target, new[] { arg, null, arg }));
        }
    }
}
