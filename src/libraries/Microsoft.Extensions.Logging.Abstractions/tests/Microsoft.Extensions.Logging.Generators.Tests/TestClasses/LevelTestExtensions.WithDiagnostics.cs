// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class LevelTestExtensions
    {
        [LoggerMessage(EventId = 12, Message = "M12 {level}")]
        internal static partial void M12(ILogger logger, LogLevel level);

        [LoggerMessage(EventId = 13, Message = "M13 {logger}")]
        internal static partial void M13(ILogger logger, LogLevel level);
    }
}
