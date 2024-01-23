// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging.Test.Console;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class ConsoleTestsBase
    {
        // converting to record fails on net462
        internal sealed class SetupDisposeHelper : IDisposable
        {
            public ConsoleLogger Logger;
            public ConsoleSink Sink;
            public ConsoleSink ErrorSink;
            public Func<LogLevel, string> GetLevelPrefix;
            public int WritesPerMsg;
            public TestLoggerProcessor LoggerProcessor;

            public SetupDisposeHelper(
                ConsoleLogger logger,
                ConsoleSink sink,
                ConsoleSink errorSink,
                Func<LogLevel, string> getLevelPrefix,
                int writesPerMsg,
                TestLoggerProcessor loggerProcessor)
            {
                Logger = logger;
                Sink = sink;
                ErrorSink = errorSink;
                GetLevelPrefix = getLevelPrefix;
                WritesPerMsg = writesPerMsg;
                LoggerProcessor = loggerProcessor;
            }

            public void Dispose() => LoggerProcessor.Dispose();
        }
    }
}
