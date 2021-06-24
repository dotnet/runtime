
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class SkipEnabledCheckExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "Message: When using SkipEnabledCheck, the generated code skips logger.IsEnabled(logLevel) check before calling log. To be used when consumer has already guarded logger method in an IsEnabled check.", SkipEnabledCheck = true)]
        internal static partial void LoggerMethodWithTrueSkipEnabledCheck(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "M1", SkipEnabledCheck = false)]
        internal static partial void LoggerMethodWithFalseSkipEnabledCheck(ILogger logger);
    }
}