// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    internal sealed class DefaultLoggerLevelConfigureOptions : ConfigureOptions<LoggerFilterOptions>
    {
        public DefaultLoggerLevelConfigureOptions(LogLevel level) : base(options => options.MinLevel = level)
        {
        }
    }
}
