// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WebAssembly.Diagnostics;

namespace DebuggerTests
{
    public class TestHarnessOptions : ProxyOptions
    {
        public string AppPath { get; set; }
        public string PagePath { get; set; }
        public string NodeApp { get; set; }
        public string BrowserParms { get; set; }
        public bool WebServerUseCors { get; set; }
        public bool WebServerUseCrossOriginPolicy { get; set; }
        public Func<string, ILogger<TestHarnessProxy>, Task<string>> ExtractConnUrl { get; set; }
        public string Locale { get; set; }
    }
}
