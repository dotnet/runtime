// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading;
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
        return await rootCommand.Parse(args).InvokeAsync();
    }
}
