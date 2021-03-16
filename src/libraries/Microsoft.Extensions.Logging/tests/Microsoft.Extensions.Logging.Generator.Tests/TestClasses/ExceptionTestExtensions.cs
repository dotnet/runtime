// © Microsoft Corporation. All rights reserved.

using System;

#pragma warning disable CA1801 // Review unused parameters

namespace Microsoft.Extensions.Logging.Generators.Test.TestClasses
{
    internal static partial class ExceptionTestExtensions
    {
        [LoggerMessage(0, LogLevel.Trace, "M0 {ex2}")]
        public static partial void M0(ILogger logger, Exception ex1, Exception ex2);

        [LoggerMessage(1, LogLevel.Debug, "M1 {ex2}")]
        public static partial void M1(Exception ex1, ILogger logger, Exception ex2);
    }
}
