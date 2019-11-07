// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Tests
{
    public class GetIndexBinderTests
    {
        private class MinimumOverrideGetIndexBinder : GetIndexBinder
        {
            public MinimumOverrideGetIndexBinder(CallInfo callInfo)
                : base(callInfo)
            {
            }

            public override DynamicMetaObject FallbackGetIndex(
                DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        [Fact]
        public void ArrayIndexing()
        {
            dynamic d = new [] {0, 1, 2, 3};
            for (int i = 0; i != 4; ++i)
            {
                Assert.Equal(i, d[i]);
            }
        }

        [Fact]
        public void ListIndexing()
        {
            dynamic d = new List<int> {0, 1, 2, 3};
            for (int i = 0; i != 4; ++i)
            {
                Assert.Equal(i, d[i]);
            }
        }

        [Fact]
        public void MultiDimensionalIndexing()
        {
            dynamic d = new[,] {{0, 1, 2, 3}, {1, 2, 3, 4}, {2, 3, 4, 5}, {3, 4, 5, 6}};
            for (int i = 0; i != 4; ++i)
            {
                for (int j = 0; j != 4; ++j)
                {
                    Assert.Equal(i + j, d[i,j]);
                }
            }
        }

        [Fact]
        public void NotIndexable()
        {
            dynamic d = 23;
            Assert.Throws<RuntimeBinderException>(() => d[2]);
        }

        [Fact]
        public void TooFewIndices()
        {
            dynamic d = new[,] { { 0, 1, 2, 3 }, { 1, 2, 3, 4 }, { 2, 3, 4, 5 }, { 3, 4, 5, 6 } };
            Assert.Throws<RuntimeBinderException>(() => d[2]);
        }

        [Fact]
        public void TooManyIndices()
        {
            dynamic d = new[] {0, 1, 2, 3};
            Assert.Throws<RuntimeBinderException>(() => d[1, 3]);
        }

        [Fact]
        public void IndexErrorIsNotBindingError()
        {
            dynamic d = new[] {0, 1, 2, 3};
            Assert.Throws<IndexOutOfRangeException>(() => d[9]);
            d = new List<int> {0, 1, 2, 3};
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => d[9]);
        }

        [Fact]
        public void NullCallInfo()
        {
            AssertExtensions.Throws<ArgumentNullException>("callInfo", () => new MinimumOverrideGetIndexBinder(null));
        }

        [Fact]
        public void CallInfoStored()
        {
            CallInfo info = new CallInfo(0);
            Assert.Same(info, new MinimumOverrideGetIndexBinder(info).CallInfo);
        }

        [Fact]
        public void ReturnTypeObject()
        {
            Assert.Equal(typeof(object), new MinimumOverrideGetIndexBinder(new CallInfo(0)).ReturnType);
        }

        [Fact]
        public void NullTarget()
        {
            var binder = new MinimumOverrideGetIndexBinder(new CallInfo(0));
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("target", () => binder.Bind(null, new[] {arg}));
        }

        [Fact]
        public void NullArgs()
        {
            var binder = new MinimumOverrideGetIndexBinder(new CallInfo(0));
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("args", () => binder.Bind(target, null));
        }

        [Fact]
        public void NullArg()
        {
            var binder = new MinimumOverrideGetIndexBinder(new CallInfo(0));
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            AssertExtensions.Throws<ArgumentNullException>("args[1]", () => binder.Bind(target, new [] {arg, null, arg}));
        }
    }
}
