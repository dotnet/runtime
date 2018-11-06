// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging
{
    internal struct LoggerInformation
    {
        public ILogger Logger { get; set; }

        public string Category { get; set; }

        public Type ProviderType { get; set; }

        public LogLevel? MinLevel { get; set; }

        public Func<string, string, LogLevel, bool> Filter { get; set; }

        public bool ExternalScope { get; set; }

        public bool CreateScopes => !ExternalScope && IsEnabled(LogLevel.Critical);

        public bool IsEnabled(LogLevel level)
        {
            if (MinLevel != null && level < MinLevel)
            {
                return false;
            }

            if (Filter != null)
            {
                return Filter(ProviderType.FullName, Category, level);
            }

            return true;
        }
    }
}