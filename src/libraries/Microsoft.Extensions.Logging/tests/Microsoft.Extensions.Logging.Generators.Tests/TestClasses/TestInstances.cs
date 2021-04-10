// © Microsoft Corporation. All rights reserved.

#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable CA1822

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
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
    }
}
