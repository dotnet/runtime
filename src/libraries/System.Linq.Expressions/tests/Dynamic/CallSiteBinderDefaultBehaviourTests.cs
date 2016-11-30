// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class CallSiteBinderDefaultBehaviourTests
    {
        public class NopCallSiteBinder : CallSiteBinder
        {
            // Adds no behavior.

            public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
            {
                throw new NotImplementedException();
            }
        }

        public class ThrowOnBindDelegate : NopCallSiteBinder
        {
            public static readonly Exception ExceptionToThrow = new Exception();

            public override T BindDelegate<T>(CallSite<T> site, object[] args)
            {
                throw ExceptionToThrow;
            }
        }

        [Fact]
        public void UpdateLabelImmutableInstance()
        {
            Assert.Same(CallSiteBinder.UpdateLabel, CallSiteBinder.UpdateLabel);
        }

        [Fact]
        public void UpdateLabelProperties()
        {
            Assert.Equal("CallSiteBinder.UpdateLabel", CallSiteBinder.UpdateLabel.Name);
            Assert.Equal(typeof(void), CallSiteBinder.UpdateLabel.Type);
        }

        [Fact]
        public void BindDelegateNoValiationNoChangeNullResult()
        {
            CallSiteBinder binder = new NopCallSiteBinder();
            // Not even a delegate type, and both arguments null.
            Assert.Null(binder.BindDelegate<object>(null, null));
            // Likewise, with empty arguments;
            Assert.Null(binder.BindDelegate<object>(null, Array.Empty<object>()));
            // And elements not mutated in any way.
            var boxedInts = Enumerable.Range(0, 10).Select(i => (object)i).ToArray();
            var args = boxedInts.ToArray(); // copy.
            Assert.Null(binder.BindDelegate<object>(null, args));
            for (int i = 0; i != 10; ++i)
            {
                Assert.Same(boxedInts[i], args[i]);
            }
        }
    }
}
