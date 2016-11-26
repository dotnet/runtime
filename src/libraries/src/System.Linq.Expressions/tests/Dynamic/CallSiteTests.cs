// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq.Expressions;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class CallSiteTests
    {
        [Fact]
        public void CannotCreateForNonDelegate()
        {
            string msg = Assert.Throws<ArgumentException>(() => CallSite<Expression>.Create(null)).Message;
            CallSiteBinder binder = new CallSiteBinderDefaultBehaviourTests.NopCallSiteBinder();
            Assert.Equal(msg, Assert.Throws<ArgumentException>(() => CallSite<Expression>.Create(binder)).Message);
            Assert.Equal(msg, Assert.Throws<ArgumentException>(() => CallSite.Create(typeof(Expression), binder)).Message);
        }

        [Fact]
        public void NonGenericCreateNullType()
        {
            CallSiteBinder binder = new CallSiteBinderDefaultBehaviourTests.NopCallSiteBinder();
            Assert.Throws<ArgumentNullException>("delegateType", () => CallSite.Create(null, binder));
        }

        [Fact]
        public void NonGenericCreateNullBinder()
        {
            Assert.Throws<ArgumentNullException>("binder", () => CallSite.Create(typeof(Func<string>), null));
        }
    }
}
