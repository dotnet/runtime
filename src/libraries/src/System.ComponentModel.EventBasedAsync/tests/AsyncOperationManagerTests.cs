// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.ComponentModel.EventBasedAsync.Tests
{
    public class AsyncOperationManagerTests
    {
        [Fact]
        public static void SetNullSynchronizationContext()
        {
            var originalContext = SynchronizationContext.Current;

            try
            {
                var oldContext = AsyncOperationManager.SynchronizationContext;
                Assert.NotNull(oldContext);

                AsyncOperationManager.SynchronizationContext = null;
                var newContext = AsyncOperationManager.SynchronizationContext;
                Assert.NotNull(newContext);
                Assert.NotSame(oldContext, newContext);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        [Fact]
        public static void SetNonNullSynchronizationContext()
        {
            var originalContext = SynchronizationContext.Current;

            try
            {
                Assert.NotNull(AsyncOperationManager.SynchronizationContext);

                var testContext = new SynchronizationContext();
                AsyncOperationManager.SynchronizationContext = testContext;
                Assert.Same(testContext, AsyncOperationManager.SynchronizationContext);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }
    }
}
