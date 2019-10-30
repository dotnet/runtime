// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.EventBasedAsync.Tests
{
    // We want to test protected method AsyncCompletedEventArgs.RaiseExceptionIfNecessary
    // so we should make derived class for access it.
    public class AsyncCompletedEventArgsTests : AsyncCompletedEventArgs
    {
        public AsyncCompletedEventArgsTests()
            : base(null, false, null)
        {
        }

        private AsyncCompletedEventArgsTests(Exception error, bool cancelled, object userState)
            : base(error, cancelled, userState)
        {
        }

        public static IEnumerable<object[]> TestInput
        {
            get
            {
                return new[]
                {
                    new object[] { null, false, (Type)null },
                    new object[] { null, true, typeof(InvalidOperationException) },
                    // dummy exceptions
                    new object[] { new FormatException(), false, typeof(FormatException) },
                    new object[] { new DllNotFoundException(), true, typeof(DllNotFoundException) }
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestInput))]
        public static void CtorTest(Exception expectedException, bool expectedCancelled, object expectedState)
        {
            var target = new AsyncCompletedEventArgsTests(expectedException, expectedCancelled, expectedState);
            Assert.Equal(expectedException, target.Error);
            Assert.Equal(expectedCancelled, target.Cancelled);
            Assert.Equal(expectedState, target.UserState);
        }

        [Theory]
        [MemberData(nameof(TestInput))]
        public static void RaiseExceptionIfNecessaryTest(Exception expectedError, bool cancelled, Type expectedExceptionType)
        {
            var target = new AsyncCompletedEventArgsTests(expectedError, cancelled, null);

            if (expectedExceptionType == null) // if null should NOT throw
            {
                target.RaiseExceptionIfNecessary();
            }
            else
            {
                if (expectedError != null)
                {
                    TargetInvocationException error = Assert.Throws<TargetInvocationException>(() => target.RaiseExceptionIfNecessary());
                    Assert.Equal(expectedExceptionType, error.InnerException.GetType());
                    Assert.Same(expectedError, error.InnerException);
                }
                else if (cancelled)
                {
                    Assert.Throws(expectedExceptionType, () => target.RaiseExceptionIfNecessary());
                }
            }
        }
    }
}
