// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console.Test
{
    internal class TestFormatterOptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : ConsoleFormatterOptions
    {
        private TOptions _options;
        private event Action<TOptions, string> _onChange;
        public TestFormatterOptionsMonitor(TOptions options)
        {
            _options = options;
        }

        public TOptions Get(string name) => _options;

        public IDisposable OnChange(Action<TOptions, string> listener)
        {
                _onChange += listener;
                return null;
        }

        public TOptions CurrentValue => _options;

        public void Set(TOptions options)
        {
            _options = options;
            _onChange?.Invoke(options, "");
        }
    }
}
