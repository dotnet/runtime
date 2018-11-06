// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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