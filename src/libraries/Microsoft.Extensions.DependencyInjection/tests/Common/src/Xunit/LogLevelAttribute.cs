﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Testing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LogLevelAttribute : Attribute
    {
        public LogLevelAttribute(LogLevel logLevel)
        {
            LogLevel = logLevel;
        }

        public LogLevel LogLevel { get; }
    }
}
