// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class TaskCanceledExceptionTests
    {
        [Fact]
        public void TaskCanceledException_Ctor_StringExceptionToken()
        {
            string message = "my exception message";
            var ioe = new InvalidOperationException();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var tce = new TaskCanceledException(message, ioe, cts.Token);
            Assert.Equal(message, tce.Message);
            Assert.Null(tce.Task);
            Assert.Same(ioe, tce.InnerException);
            Assert.Equal(cts.Token, tce.CancellationToken);
        }
    }
}
