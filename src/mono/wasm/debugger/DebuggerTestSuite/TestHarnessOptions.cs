// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.WebAssembly.Diagnostics
{
    public class TestHarnessOptions : ProxyOptions
    {
        public string ChromePath { get; set; }
        public string AppPath { get; set; }
        public string PagePath { get; set; }
        public string NodeApp { get; set; }
    }
}