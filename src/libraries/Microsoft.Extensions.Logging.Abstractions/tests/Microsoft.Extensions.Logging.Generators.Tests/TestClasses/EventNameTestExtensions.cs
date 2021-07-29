// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class EventNameTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Trace, Message = "M0", EventName = "CustomEventName")]
        public static partial void M0(ILogger logger);

        [LoggerMessage(EventId = 2, Level = LogLevel.Trace, Message = "CustomEventName")] // EventName inferred from method name
        public static partial void CustomEventName(ILogger logger);
    }
}
