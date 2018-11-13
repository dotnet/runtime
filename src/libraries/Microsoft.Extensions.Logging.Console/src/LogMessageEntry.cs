// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Console
{
    internal readonly struct LogMessageEntry
    {
        public LogMessageEntry(string timeStamp, string levelString, ConsoleColor? levelBackground, ConsoleColor? levelForeground, ConsoleColor? messageColor, string message, bool logAsError)
        {
            TimeStamp = timeStamp;
            LevelString = levelString;
            LevelBackground = levelBackground;
            LevelForeground = levelForeground;
            MessageColor = messageColor;
            Message = message;
            LogAsError = logAsError;
        }

        public readonly string TimeStamp;
        public readonly string LevelString;
        public readonly ConsoleColor? LevelBackground;
        public readonly ConsoleColor? LevelForeground;
        public readonly ConsoleColor? MessageColor;
        public readonly string Message;
        public readonly bool LogAsError;
    }
}
