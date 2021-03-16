// © Microsoft Corporation. All rights reserved.

#pragma warning disable CA1801 // Review unused parameters

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal static partial class EventNameTestExtensions
    {
        [LoggerMessage(0, LogLevel.Trace, "M0", EventName = "CustomEventName")]
        public static partial void M0(ILogger logger);
    }
}
