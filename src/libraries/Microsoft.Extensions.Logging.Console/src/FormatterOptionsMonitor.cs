// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal class FormatterOptionsMonitor<TOptions> : IOptionsMonitor<TOptions> where TOptions : ConsoleFormatterOptions
    {
        private TOptions _options;
        private event Action<TOptions, string> _onChange;

        public FormatterOptionsMonitor(TOptions options)
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