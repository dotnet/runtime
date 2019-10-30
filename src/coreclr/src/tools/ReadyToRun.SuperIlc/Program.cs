// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace ReadyToRun.SuperIlc
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
