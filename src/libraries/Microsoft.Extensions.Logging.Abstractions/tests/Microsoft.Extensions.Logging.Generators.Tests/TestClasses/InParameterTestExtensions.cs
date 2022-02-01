// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class InParameterTestExtensions
    {
        internal struct S
        {
            public override string ToString() => "Hello from S";
        }

        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "M0 {s}")]
        internal static partial void M0(ILogger logger, in S s);
    }
}
