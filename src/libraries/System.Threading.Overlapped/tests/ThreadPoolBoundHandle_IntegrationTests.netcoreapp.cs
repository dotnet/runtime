// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tests;
using Xunit;

public partial class ThreadPoolBoundHandleTests
{
    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)] // ThreadPoolBoundHandle.BindHandle is not supported on Unix
    public unsafe void MultipleOperationsOverSingleHandle_CompletedWorkItemCountTest()
    {
        long initialCompletedWorkItemCount = ThreadPool.CompletedWorkItemCount;
        MultipleOperationsOverMultipleHandles();
        ThreadTestHelpers.WaitForCondition(() => ThreadPool.CompletedWorkItemCount - initialCompletedWorkItemCount >= 2);
    }
}
