// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class DebuggerDisplayFormatting
    {
        internal static string DebuggerToString(string name, ILogger logger)
        {
            ReadOnlySpan<LogLevel> logLevels = stackalloc LogLevel[]
            {
                LogLevel.Critical,
                LogLevel.Error,
                LogLevel.Warning,
                LogLevel.Information,
                LogLevel.Debug,
                LogLevel.Trace,
            };

            LogLevel minimumLevel = LogLevel.None;

            // Check log level from highest to lowest. Report the lowest log level.
            foreach (LogLevel logLevel in logLevels)
            {
                if (!logger.IsEnabled(logLevel))
                {
                    break;
                }

                minimumLevel = logLevel;
            }

            var debugText = $@"Name = ""{name}""";
            if (minimumLevel != LogLevel.None)
            {
                debugText += $", MinLevel = {minimumLevel}";
            }
            else
            {
                // Display "Enabled = false". This makes it clear that the entire ILogger
                // is disabled and nothing is written.
                //
                // If "MinLevel = None" was displayed then someone could think that the
                // min level is disabled and everything is written.
                debugText += $", Enabled = false";
            }

            return debugText;
        }
    }
}
