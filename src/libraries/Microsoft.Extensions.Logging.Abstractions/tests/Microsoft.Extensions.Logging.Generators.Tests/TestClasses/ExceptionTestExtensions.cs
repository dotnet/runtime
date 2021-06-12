// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class ExceptionTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Trace, Message = "M0 {ex2}")]
        public static partial void M0(ILogger logger, Exception ex1, Exception ex2);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "M1 {ex2}")]
        public static partial void M1(Exception ex1, ILogger logger, Exception ex2);
    }
}
