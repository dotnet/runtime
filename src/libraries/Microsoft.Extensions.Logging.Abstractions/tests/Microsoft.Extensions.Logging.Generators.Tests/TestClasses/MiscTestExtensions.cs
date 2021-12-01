// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

// Used to test use outside of a namespace
internal static partial class NoNamespace
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "Could not open socket to `{hostName}`")]
    public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
}

namespace Level1
{
    // used to test use inside a one-level namespace
    internal static partial class OneLevelNamespace
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "Could not open socket to `{hostName}`")]
        public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
    }
}

namespace Level1
{
    namespace Level2
    {
        // used to test use inside a two-level namespace
        internal static partial class TwoLevelNamespace
        {
            [LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "Could not open socket to `{hostName}`")]
            public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
        }
    }
}
