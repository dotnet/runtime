// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    // test particular method signature variations are generated correctly
    internal static partial class SignatureTestExtensions
    {
        // extension method
        [LoggerMessage(EventId = 10, Level = LogLevel.Critical, Message = "Message11")]
        internal static partial void M11(this ILogger logger);

        public static void Combo(ILogger logger)
        {
            logger.M11();
        }
    }

    // test particular method signature variations are generated correctly
    internal partial class SignatureTestExtensions<T>
        where T : class
    {
        public static void Combo(ILogger logger, ILogger<int> logger2)
        {
            M1(logger);
            M2(logger);
            M3(logger);
            M4(logger2);
            M5(logger, new[] { "A" });
            M6(logger);
            M8(logger);
            M9(logger);
            M10(logger, null);
            M11(logger, "A", LogLevel.Debug, "B");
        }

        // normal public method
        [LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "Message1")]
        public static partial void M1(ILogger logger);

        // internal method
        [LoggerMessage(EventId = 1, Level = LogLevel.Critical, Message = "Message2")]
        internal static partial void M2(ILogger logger);

        // private method
        [LoggerMessage(EventId = 2, Level = LogLevel.Critical, Message = "Message3")]
        private static partial void M3(ILogger logger);

        // generic ILogger
        [LoggerMessage(EventId = 3, Level = LogLevel.Critical, Message = "Message4")]
        private static partial void M4(ILogger<int> logger);

        // random type method parameter
        [LoggerMessage(EventId = 4, Level = LogLevel.Critical, Message = "Message5 {items}")]
        private static partial void M5(ILogger logger, System.Collections.IEnumerable items);

        // line feeds and quotes in the message string
        [LoggerMessage(EventId = 5, Level = LogLevel.Critical, Message = "Message6\n\"\r")]
        private static partial void M6(ILogger logger);

        // generic parameter
        [LoggerMessage(EventId = 6, Level = LogLevel.Critical, Message = "Message7 {p1}\n\"\r")]
        private static partial void M7(ILogger logger, T p1);

        // normal public method
        [LoggerMessage(EventId = 7, Level = LogLevel.Critical, Message = "Message8")]
        private protected static partial void M8(ILogger logger);

        // internal method
        [LoggerMessage(EventId = 8, Level = LogLevel.Critical, Message = "Message9")]
        protected internal static partial void M9(ILogger logger);

        // nullable parameter
        [LoggerMessage(EventId = 9, Level = LogLevel.Critical, Message = "Message10 {optional}")]
        internal static partial void M10(ILogger logger, string? optional);

        // dynamic log level
        [LoggerMessage(EventId = 10, Message = "Message11 {p1} {p2}")]
        internal static partial void M11(ILogger logger, string p1, LogLevel level, string p2);
    }
}
