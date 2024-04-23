// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        CliRootCommand rootCommand = new ();
        var verboseOption = new CliOption<bool>("-v", "--verbose") {Recursive = true, Description = "Verbose"};
        rootCommand.Add(verboseOption);
        rootCommand.Add(new DiagramDirective());
        rootCommand.Add(new ComposeCommand(verboseOption));
        return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(true);
    }
}
