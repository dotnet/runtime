// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging.Test.Console;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class ConsoleTestsBase
    {
        internal sealed record SetupDisposeHelper(
            ConsoleLogger Logger,
            ConsoleSink Sink,
            ConsoleSink ErrorSink,
            Func<LogLevel, string> GetLevelPrefix,
            int WritesPerMsg,
            TestLoggerProcessor LoggerProcessor) : IDisposable
        {
            public void Dispose() => LoggerProcessor.Dispose();
        }
    }
}