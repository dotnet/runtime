// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;

namespace ReadyToRun.SuperIlc
{
    internal static class CommandLineOptions
    {
        public static CommandLineBuilder Build()
        {
            var parser = new CommandLineBuilder()
                .AddCommand(CompileFolder())
                .AddCommand(CompileSubtree())
                .AddCommand(CompileFramework())
                .AddCommand(CompileNugetPackages())
                .AddCommand(CompileCrossgenRsp());

            return parser;

            Command CompileFolder() =>
                new Command("compile-directory", "Compile all assemblies in directory",
                    new Option[]
                    {
                        InputDirectory(),
                        OutputDirectory(),
                        CoreRootDirectory(),
                        Crossgen(),
                        CrossgenPath(),
                        NoJit(),
                        NoCrossgen2(),
                        Exe(),
                        NoExe(),
                        NoEtw(),
                        NoCleanup(),
                        DegreeOfParallelism(),
                        Sequential(),
                        Framework(),
                        UseFramework(),
                        Release(),
                        LargeBubble(),
                        ReferencePath(),
                        IssuesPath(),
                        CompilationTimeoutMinutes(),
                        ExecutionTimeoutMinutes(),
                        R2RDumpPath(),
                        MeasurePerf(),
                        InputFileSearchString(),
                    },
                    handler: CommandHandler.Create<BuildOptions>(CompileDirectoryCommand.CompileDirectory));

            Command CompileSubtree() =>
                new Command("compile-subtree", "Build each directory in a given subtree containing any managed assemblies as a separate app",
                    new Option[]
                    {
                        InputDirectory(),
                        OutputDirectory(),
                        CoreRootDirectory(),
                        Crossgen(),
                        CrossgenPath(),
                        NoJit(),
                        NoCrossgen2(),
                        Exe(),
                        NoExe(),
                        NoEtw(),
                        NoCleanup(),
                        DegreeOfParallelism(),
                        Sequential(),
                        Framework(),
                        UseFramework(),
                        Release(),
                        LargeBubble(),
                        ReferencePath(),
                        IssuesPath(),
                        CompilationTimeoutMinutes(),
                        ExecutionTimeoutMinutes(),
                        R2RDumpPath(),
                    },
                    handler: CommandHandler.Create<BuildOptions>(CompileSubtreeCommand.CompileSubtree));

            Command CompileFramework() =>
                new Command("compile-framework", "Compile managed framework assemblies in Core_Root",
                    new Option[]
                    {
                        CoreRootDirectory(),
                        Crossgen(),
                        CrossgenPath(),
                        NoCrossgen2(),
                        NoCleanup(),
                        DegreeOfParallelism(),
                        Sequential(),
                        Release(),
                        LargeBubble(),
                        ReferencePath(),
                        IssuesPath(),
                        CompilationTimeoutMinutes(),
                        R2RDumpPath(),
                        MeasurePerf(),
                        InputFileSearchString(),
                    },
                    handler: CommandHandler.Create<BuildOptions>(CompileFrameworkCommand.CompileFramework));

            Command CompileNugetPackages() =>
                new Command("compile-nuget", "Restore a list of Nuget packages into an empty console app, publish, and optimize with Crossgen / CPAOT",
                    new Option[]
                    {
                        R2RDumpPath(),
                        InputDirectory(),
                        OutputDirectory(),
                        PackageList(),
                        CoreRootDirectory(),
                        Crossgen(),
                        NoCleanup(),
                        DegreeOfParallelism(),
                        CompilationTimeoutMinutes(),
                        ExecutionTimeoutMinutes(),
                    },
                    handler: CommandHandler.Create<BuildOptions>(CompileNugetCommand.CompileNuget));

            Command CompileCrossgenRsp() =>
                new Command("compile-crossgen-rsp", "Use existing Crossgen .rsp file(s) to build assemblies, optionally rewriting base paths",
                    new Option[]
                    {
                        InputDirectory(),
                        CrossgenResponseFile(),
                        OutputDirectory(),
                        CoreRootDirectory(),
                        Crossgen(),
                        NoCleanup(),
                        DegreeOfParallelism(),
                        CompilationTimeoutMinutes(),
                        RewriteOldPath(),
                        RewriteNewPath(),
                    },
                    handler: CommandHandler.Create<BuildOptions>(CompileFromCrossgenRspCommand.CompileFromCrossgenRsp));

            // Todo: Input / Output directories should be required arguments to the command when they're made available to handlers
            // https://github.com/dotnet/command-line-api/issues/297
            Option InputDirectory() =>
                new Option(new[] { "--input-directory", "-in" }, "Folder containing assemblies to optimize", new Argument<DirectoryInfo>().ExistingOnly());

            Option OutputDirectory() =>
                new Option(new[] { "--output-directory", "-out" }, "Folder to emit compiled assemblies", new Argument<DirectoryInfo>().LegalFilePathsOnly());

            Option CoreRootDirectory() =>
                new Option(new[] { "--core-root-directory", "-cr" }, "Location of the CoreCLR CORE_ROOT folder", new Argument<DirectoryInfo>().ExistingOnly());

            Option ReferencePath() =>
                new Option(new[] { "--reference-path", "-r" }, "Folder containing assemblies to reference during compilation", new Argument<DirectoryInfo[]>() { Arity = ArgumentArity.ZeroOrMore }.ExistingOnly());

            Option Crossgen() =>
                new Option(new[] { "--crossgen" }, "Compile the apps using Crossgen in the CORE_ROOT folder", new Argument<bool>());

            Option CrossgenPath() =>
                new Option(new[] { "--crossgen-path", "-cp" }, "Explicit Crossgen path (useful for cross-targeting)", new Argument<FileInfo>().ExistingOnly());

            Option NoJit() =>
                new Option(new[] { "--nojit" }, "Don't run tests in JITted mode", new Argument<bool>());

            Option NoCrossgen2() =>
                new Option(new[] { "--nocrossgen2" }, "Don't run tests in Crossgen2 mode", new Argument<bool>());

            Option Exe() =>
                new Option(new[] { "--exe" }, "Don't compile tests, just execute them", new Argument<bool>());

            Option NoExe() =>
                new Option(new[] { "--noexe" }, "Compilation-only mode (don't execute the built apps)", new Argument<bool>());

            Option NoEtw() =>
                new Option(new[] { "--noetw" }, "Don't capture jitted methods using ETW", new Argument<bool>());

            Option NoCleanup() =>
                new Option(new[] { "--nocleanup" }, "Don't clean up compilation artifacts after test runs", new Argument<bool>());

            Option DegreeOfParallelism() =>
                new Option(new[] { "--degree-of-parallelism", "-dop" }, "Override default compilation / execution DOP (default = logical processor count)", new Argument<int>());

            Option Sequential() =>
                new Option(new[] { "--sequential" }, "Run tests sequentially", new Argument<bool>());

            Option Framework() =>
                new Option(new[] { "--framework" }, "Precompile and use native framework", new Argument<bool>());

            Option UseFramework() =>
                new Option(new[] { "--use-framework" }, "Use native framework (don't precompile, assume previously compiled)", new Argument<bool>());

            Option Release() =>
                new Option(new[] { "--release" }, "Build the tests in release mode", new Argument<bool>());

            Option LargeBubble() =>
                new Option(new[] { "--large-bubble" }, "Assume all input files as part of one version bubble", new Argument<bool>());

            Option IssuesPath() =>
                new Option(new[] { "--issues-path", "-ip" }, "Path to issues.targets", new Argument<FileInfo[]>() { Arity = ArgumentArity.ZeroOrMore });

            Option CompilationTimeoutMinutes() =>
                new Option(new[] { "--compilation-timeout-minutes", "-ct" }, "Compilation timeout (minutes)", new Argument<int>());

            Option ExecutionTimeoutMinutes() =>
                new Option(new[] { "--execution-timeout-minutes", "-et" }, "Execution timeout (minutes)", new Argument<int>());

            Option R2RDumpPath() =>
                new Option(new[] { "--r2r-dump-path", "-r2r" }, "Path to R2RDump.exe/dll", new Argument<FileInfo>().ExistingOnly());

            Option CrossgenResponseFile() =>
                new Option(new [] { "--crossgen-response-file", "-rsp" }, "Response file to transpose", new Argument<FileInfo>().ExistingOnly());

            Option RewriteOldPath() =>
                new Option(new [] { "--rewrite-old-path" }, "Path substring to replace", new Argument<DirectoryInfo[]>(){ Arity = ArgumentArity.ZeroOrMore });

            Option RewriteNewPath() =>
                new Option(new [] { "--rewrite-new-path" }, "Path substring to use instead", new Argument<DirectoryInfo[]>(){ Arity = ArgumentArity.ZeroOrMore });

            Option MeasurePerf() =>
                new Option(new[] { "--measure-perf" }, "Print out compilation time", new Argument<bool>());

            Option InputFileSearchString() =>
                new Option(new[] { "--input-file-search-string", "-input-file" }, "Search string for input files in the input directory", new Argument<string>());

            //
            // compile-nuget specific options
            //
            Option PackageList() =>
                new Option(new[] { "--package-list", "-pl" }, "Text file containing a package name on each line", new Argument<FileInfo>().ExistingOnly());
        }
    }
}
