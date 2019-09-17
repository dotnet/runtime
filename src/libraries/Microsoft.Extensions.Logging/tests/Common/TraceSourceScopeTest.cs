// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class TraceSourceScopeTest
    {
#if NET472
        [Fact]
        public static void DiagnosticsScope_PushesAndPops_LogicalOperationStack()
        {
            // Arrange
            var baseState = "base";
            Trace.CorrelationManager.StartLogicalOperation(baseState);
            var state = "1337state7331";

            var factory = TestLoggerBuilder.Create(builder =>
                builder.AddTraceSource(new SourceSwitch("TestSwitch"), new ConsoleTraceListener()));

            var logger = factory.CreateLogger("Test");

            // Act
            var a = Trace.CorrelationManager.LogicalOperationStack.Peek();
            var scope = logger.BeginScope(state);
            var b = Trace.CorrelationManager.LogicalOperationStack.Peek();
            scope.Dispose();
            var c = Trace.CorrelationManager.LogicalOperationStack.Peek();

            // Assert
            Assert.Same(a, c);
            Assert.Same(state, b);
        }
#elif NETCOREAPP
#else
#error Target framework needs to be updated
#endif
    }
}
