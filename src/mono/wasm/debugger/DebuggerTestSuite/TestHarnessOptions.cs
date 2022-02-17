// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.WebAssembly.Diagnostics
{
    public class TestHarnessOptions : ProxyOptions
    {
        public string BrowserPath { get; set; }
        public string AppPath { get; set; }
        public string PagePath { get; set; }
        public string NodeApp { get; set; }
        public string BrowserParms { get; set; }
        public Func<string, ILogger<TestHarnessProxy>, Task<string>> ExtractConnUrl { get; set; }
    }
}