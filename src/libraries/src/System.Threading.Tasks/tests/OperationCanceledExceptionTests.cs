// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;

namespace System.Threading.Tasks.Tests
{
    public static class OperationCanceledExceptionTests
    {
        [Fact]
        public static void BasicConstructors()
        {
            CancellationToken ct1 = new CancellationTokenSource().Token;
            OperationCanceledException ex1 = new OperationCanceledException(ct1);
            Assert.Equal(ct1, ex1.CancellationToken);

            CancellationToken ct2 = new CancellationTokenSource().Token;
            OperationCanceledException ex2 = new OperationCanceledException("message", ct2);
            Assert.Equal(ct2, ex2.CancellationToken);

            CancellationToken ct3 = new CancellationTokenSource().Token;
            OperationCanceledException ex3 = new OperationCanceledException("message", new Exception("inner"), ct3);
            Assert.Equal(ct3, ex3.CancellationToken);
        }
    }
}
