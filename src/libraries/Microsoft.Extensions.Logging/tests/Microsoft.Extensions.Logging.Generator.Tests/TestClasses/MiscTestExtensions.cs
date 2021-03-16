// © Microsoft Corporation. All rights reserved.

using Microsoft.Extensions.Logging;

#pragma warning disable CA1801 // Review unused parameters
#pragma warning disable S1118 // Utility classes should not have public constructors
#pragma warning disable S3903 // Types should be defined in named namespaces
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1207 // Protected should come before internal
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1403 // File may only contain a single namespace

// Used to test use outside of a namespace
internal static partial class NoNamespace
{
    [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
    public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
}

namespace Level1
{
    // used to test use inside a one-level namespace
    internal static partial class OneLevelNamespace
    {
        [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
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
            [LoggerMessage(0, LogLevel.Critical, "Could not open socket to `{hostName}`")]
            public static partial void CouldNotOpenSocket(ILogger logger, string hostName);
        }
    }
}
