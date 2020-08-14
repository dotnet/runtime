// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace R2RTest
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var parser = CommandLineOptions.Build().UseDefaults().Build();

            return await parser.InvokeAsync(args);
        }
    }
}
