// © Microsoft Corporation. All rights reserved.

#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable CA1822

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal partial class TestInstances
    {
        private readonly ILogger _logger;

        public TestInstances(ILogger logger)
        {
            _logger = logger;
        }

        [LoggerMessage(0, LogLevel.Error, "M0")]
        public partial void M0();

        [LoggerMessage(1, LogLevel.Trace, "M1 {p1}")]
        public partial void M1(string p1);
    }
}
