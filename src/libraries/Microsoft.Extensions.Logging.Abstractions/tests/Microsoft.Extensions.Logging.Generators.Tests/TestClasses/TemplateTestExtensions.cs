// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class TemplateTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Error, Message = "M0 {A1}")]
        public static partial void M0(ILogger logger, int a1);

        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "M1 {A1} {A1}")]
        public static partial void M1(ILogger logger, int a1);

        [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "M2 {A1} {a2} {A3} {a4} {A5} {a6} {A7}")]
        public static partial void M2(ILogger logger, int a1, int a2, int a3, int a4, int a5, int a6, int a7);

        [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "M3 {a2} {A1}")]
        public static partial void M3(ILogger logger, int a1, int a2);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "M4 {A1}")]
        public static partial void M4(ILogger logger, double a1);

        [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "M5 {A1} {a2} {A3} {a4} {A5} {a6} {A7}")]
        public static partial void M5(ILogger logger, double a1, double a2, double a3, double a4, double a5, double a6, double a7);
    }
}
