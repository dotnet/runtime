// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class AtSymbolTestExtensions
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "M0 {event}")]
        internal static partial void M0(ILogger logger, string @event);

        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "M1 {@myevent}")]
        internal static partial void M1(ILogger logger, string @myevent);

        [LoggerMessage(Message = "Force use of Struct, {@myevent} {otherevent}", EventId = 2)]
        public static partial void UseAtSymbol3(this ILogger logger,  LogLevel level, string @myevent, int otherevent);

        [LoggerMessage(Message = "Force use of Struct with error, {@myevent} {otherevent}", EventId = 3)]
        public static partial void UseAtSymbol4(this ILogger logger,  LogLevel level, string @myevent, int otherevent, System.Exception ex);
    }
}