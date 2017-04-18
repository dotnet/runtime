// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class SetIndexBinderTests
    {
        private class MinimumOverrideSetIndexBinder : SetIndexBinder
        {
            public MinimumOverrideSetIndexBinder(CallInfo callInfo) : base(callInfo)
            {
            }

            public override DynamicMetaObject FallbackSetIndex(
                DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value,
                DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        [Fact]
        public void ArrayIndexing()
        {
            int[] array = {-1, -1, -1, -1};
            dynamic d = array;
            for (int i = 0; i != 4; ++i)
            {
                d[i] = i;
            }

            Assert.Equal(new[] {0, 1, 2, 3}, array);
        }

        [Fact]
        public void ListIndexing()
        {
            List<int> list = new List<int> {0, 1, 2, 3};
            dynamic d = list;
            for (int i = 0; i != 4; ++i)
            {
                d[i] = i;
            }

            Assert.Equal(new[] {0, 1, 2, 3}, list);
        }

        [Fact]
        public void MultiDimensionalIndexing()
        {
            int[,] array = new[,] {{-1, -1, -1, -1}, {-1, -1, -1, -1}, {-1, -1, -1, -1}, {-1, -1, -1, -1}};
            dynamic d = array;
            for (int i = 0; i != 4; ++i)
            {
                for (int j = 0; j != 4; ++j)
                {
                    d[i, j] = i + j;
                }
            }

            Assert.Equal(new[,] {{0, 1, 2, 3}, {1, 2, 3, 4}, {2, 3, 4, 5}, {3, 4, 5, 6}}, array);
        }

        [Fact]
        public void NotIndexable()
        {
            dynamic d = 23;
            Assert.Throws<RuntimeBinderException>(() => d[2] = 2);
        }

        [Fact]
        public void TooFewIndices()
        {
            dynamic d = new[,] { { 0, 1, 2, 3 }, { 1, 2, 3, 4 }, { 2, 3, 4, 5 }, { 3, 4, 5, 6 } };
            Assert.Throws<RuntimeBinderException>(() => d[2] = 2);
        }

        [Fact]
        public void TooManyIndices()
        {
            dynamic d = new[] { 0, 1, 2, 3 };
            Assert.Throws<RuntimeBinderException>(() => d[1, 3] = 2);
        }

        [Fact]
        public void IndexErrorIsNotBindingError()
        {
            dynamic d = new[] { 0, 1, 2, 3 };
            Assert.Throws<IndexOutOfRangeException>(() => d[9] = 8);
            d = new List<int> { 0, 1, 2, 3 };
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => d[9] = 8);
        }

        [Fact]
        public void NullCallInfo()
        {
            AssertExtensions.Throws<ArgumentNullException>("callInfo", () => new MinimumOverrideSetIndexBinder(null));
        }

        [Fact]
        public void CallInfoStored()
        {
            CallInfo info = new CallInfo(0);
            Assert.Same(info, new MinimumOverrideSetIndexBinder(info).CallInfo);
        }

        [Fact]
        public void ReturnTypeObject()
        {
            Assert.Equal(typeof(object), new MinimumOverrideSetIndexBinder(new CallInfo(0)).ReturnType);
        }

        [Fact]
        public void NullTarget()
        {
            var binder = new MinimumOverrideSetIndexBinder(new CallInfo(0));
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("target", () => binder.Bind(null, new[] { arg }));
        }

        [Fact]
        public void NullArgs()
        {
            var binder = new MinimumOverrideSetIndexBinder(new CallInfo(0));
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("args", () => binder.Bind(target, null));
        }

        [Fact]
        public void NullArg()
        {
            var binder = new MinimumOverrideSetIndexBinder(new CallInfo(0));
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("args[1]", () => binder.Bind(target, new[] { arg, null, arg }));
        }
    }
}
