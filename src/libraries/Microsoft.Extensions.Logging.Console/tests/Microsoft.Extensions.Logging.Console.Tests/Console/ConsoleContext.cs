// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Test.Console
{
    public class ConsoleContext
    {
        public ConsoleColor? BackgroundColor { get; set; }

        public ConsoleColor? ForegroundColor { get; set; }

        public string Message { get; set; }
    }
}
