// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class OverloadTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Trace, Message = "M0{p0}")]
        public static partial void M0(ILogger logger, int p0);

        [LoggerMessage(EventId = 1, Level = LogLevel.Trace, Message = "M0{p0}")]
        public static partial void M0(ILogger logger, string p0);
    }
}
