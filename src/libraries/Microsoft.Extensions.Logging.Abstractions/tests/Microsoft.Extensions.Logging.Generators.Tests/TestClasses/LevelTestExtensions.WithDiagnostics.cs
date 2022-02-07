// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class LevelTestExtensions
    {
#pragma warning disable SYSLIB1002
        [LoggerMessage(EventId = 12, Message = "M12 {level}")]
        public static partial void M12(ILogger logger, LogLevel level);
#pragma warning restore SYSLIB1002

#pragma warning disable SYSLIB1018
        [LoggerMessage(EventId = 13, Message = "M13 {logger}")]
        public static partial void M13(ILogger logger, LogLevel level);
#pragma warning restore SYSLIB1018
    }
}
