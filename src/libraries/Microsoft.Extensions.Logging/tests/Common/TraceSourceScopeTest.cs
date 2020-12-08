// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class TraceSourceScopeTest
    {
#if NETFRAMEWORK
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
