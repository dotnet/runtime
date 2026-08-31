// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace ArbitraryDiagnosticEventPatternTestSite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using var listener = new DiagnosticListener("Microsoft.Extensions.Hosting");
            listener.Write("CustomEvent", 42);

            new HostBuilder().Build().Dispose();
        }
    }
}
