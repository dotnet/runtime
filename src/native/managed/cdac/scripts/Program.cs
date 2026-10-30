// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Microsoft.DotNet.Diagnostics.CdacDumpInspect;

internal sealed class Program
{
    public static async Task<int> Main(string[] args)
    {
        RootCommand rootCommand = new("Inspect .NET crash dumps using the cDAC (contract-based Data Access) reader.");
        rootCommand.Add(new DescriptorCommand());
        rootCommand.Add(new ThreadsCommand());
        rootCommand.Add(new StacksCommand());

        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(true);
    }
}
