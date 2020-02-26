// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    internal class StaticFilterOptionsMonitor : IOptionsMonitor<LoggerFilterOptions>
    {
        public StaticFilterOptionsMonitor(LoggerFilterOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public IDisposable OnChange(Action<LoggerFilterOptions, string> listener) => null;

        public LoggerFilterOptions Get(string name) => CurrentValue;

        public LoggerFilterOptions CurrentValue { get; }
    }
}