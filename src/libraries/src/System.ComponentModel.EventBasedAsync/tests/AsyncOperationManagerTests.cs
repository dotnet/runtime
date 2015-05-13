// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
