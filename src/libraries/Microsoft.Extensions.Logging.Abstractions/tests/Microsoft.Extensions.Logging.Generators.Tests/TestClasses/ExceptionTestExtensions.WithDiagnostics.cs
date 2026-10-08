// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging.Generators.Tests.TestClasses
{
    internal static partial class ExceptionTestExtensions
    {
        [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "M2 {arg1}: {ex}")]
        internal static partial void M2(ILogger logger, string arg1, Exception ex);
    }
}
