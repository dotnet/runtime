// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    public partial class TestInstances
    {
        private readonly ILogger _myLogger;

        public TestInstances(ILogger logger)
        {
            _myLogger = logger;
        }

        [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "M0")]
        public partial void M0();

        [LoggerMessage(EventId = 1, Level = LogLevel.Trace, Message = "M1 {p1}")]
        public partial void M1(string p1);

        // Test LoggerMessage Constructor's overloads

        [LoggerMessage(LogLevel.Information, "M2 {p1}")]
        public partial void M2(string p1);

        [LoggerMessage("M3 {p1}")]
        public partial void M3(LogLevel level, string p1);

        [LoggerMessage(LogLevel.Debug)]
        public partial void M4();

        // Test with named parameters
        [LoggerMessage(level: LogLevel.Warning, message: "custom message {v}", eventId: 12341)]
        public partial void M5(string v);

        // Test auto-generated EventId
        [LoggerMessage(EventName = "My Event Name", Level = LogLevel.Information, Message = "M6 - {p1}")]
        public partial void M6(string p1);

        [LoggerMessage(Level = LogLevel.Warning, Message = "M7 - {p1}")]
        public partial void M7(string p1);

        [LoggerMessage(EventId = 100, Level = LogLevel.Warning, Message = "M8 - {p1}")]
        public partial void M8(string p1);
    }
}
