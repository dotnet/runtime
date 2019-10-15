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
            string msg = AssertExtensions.Throws<ArgumentException>(null, () => CallSite<Expression>.Create(null)).Message;
            CallSiteBinder binder = new CallSiteBinderDefaultBehaviourTests.NopCallSiteBinder();
            Assert.Equal(msg, AssertExtensions.Throws<ArgumentException>(null, () => CallSite<Expression>.Create(binder)).Message);
            Assert.Equal(msg, AssertExtensions.Throws<ArgumentException>(null, () => CallSite.Create(typeof(Expression), binder)).Message);
        }

        [Fact]
        public void NonGenericCreateNullType()
        {
            CallSiteBinder binder = new CallSiteBinderDefaultBehaviourTests.NopCallSiteBinder();
            AssertExtensions.Throws<ArgumentNullException>("delegateType", () => CallSite.Create(null, binder));
        }

        [Fact]
        public void NonGenericCreateNullBinder()
        {
            AssertExtensions.Throws<ArgumentNullException>("binder", () => CallSite.Create(typeof(Func<string>), null));
        }

        [Fact]
        public void NullBinder()
        {
            AssertExtensions.Throws<ArgumentNullException>("binder", () => CallSite<Func<CallSite, object, object>>.Create(null));
        }
    }
}
