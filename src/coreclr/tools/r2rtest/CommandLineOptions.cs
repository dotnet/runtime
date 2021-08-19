// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;

namespace R2RTest
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
                .AddCommand(CompileSerp());

            return parser;

            Command CreateCommand(string name, string description, Option[] options, Func<BuildOptions, int> action)
            {
                Command command = new Command(name, description);
                foreach (var option in GetCommonOptions())
                    command.AddOption(option);
                foreach (var option in options)
                    command.AddOption(option);
                command.Handler = CommandHandler.Create<BuildOptions>(action);
                return command;
            }

            Option[] GetCommonOptions() => new[] { CoreRootDirectory(), DotNetCli() };

            Command CompileFolder() =>
                CreateCommand("compile-directory", "Compile all assemblies in directory",
                    new Option[]
                    {
                        InputDirectory(),
                        OutputDirectory(),
                        Crossgen2Path(),
                        TargetArch(),
                        VerifyTypeAndFieldLayout(),
                        NoJit(),
                        NoCrossgen2(),
                        Exe(),
                        NoExe(),
                        NoEtw(),
                        NoCleanup(),
                        Map(),
                        Pdb(),
                        DegreeOfParallelism(),
                        Sequential(),
                        Framework(),
                        UseFramework(),
                        Release(),
                        LargeBubble(),
                        Composite(),
                        Crossgen2Parallelism(),
                        Crossgen2JitPath(),
                        ReferencePath(),
                        IssuesPath(),
                        CompilationTimeoutMinutes(),
                        ExecutionTimeoutMinutes(),
                        R2RDumpPath(),
                        MeasurePerf(),
                        InputFileSearchString(),
                        MibcPath(),
                    },
                    CompileDirectoryCommand.CompileDirectory);

            Command CompileSubtree() =>
                CreateCommand("compile-subtree", "Build each directory in a given subtree containing any managed assemblies as a separate app",
                    new Option[]
                    {
                        InputDirectory(),
                        OutputDirectory(),
                        Crossgen2Path(),
                        TargetArch(),
                        VerifyTypeAndFieldLayout(),
                        NoJit(),
                        NoCrossgen2(),
                        Exe(),
                        NoExe(),
                        NoEtw(),
                        NoCleanup(),
                        Map(),
                        Pdb(),
                        DegreeOfParallelism(),
                        Sequential(),
                        Framework(),
                        UseFramework(),
                        Release(),
                        LargeBubble(),
                        Composite(),
                        Crossgen2Parallelism(),
                        Crossgen2JitPath(),
                        ReferencePath(),
                        IssuesPath(),
                        CompilationTimeoutMinutes(),
                        ExecutionTimeoutMinutes(),
                        R2RDumpPath(),
                        GCStress(),
                        MibcPath(),
                    },
                    CompileSubtreeCommand.CompileSubtree);

            Command CompileFramework() =>
                CreateCommand("compile-framework", "Compile managed framework assemblies in Core_Root",
                    new Option[]
                    {
                        Crossgen2Path(),
                        TargetArch(),
                        VerifyTypeAndFieldLayout(),
                        NoCrossgen2(),
                        NoCleanup(),
                        Map(),
                        Pdb(),
                        Crossgen2Parallelism(),
                        Crossgen2JitPath(),
                        DegreeOfParallelism(),
                        Sequential(),
                        Release(),
                        LargeBubble(),
                        Composite(),
                        ReferencePath(),
                        IssuesPath(),
                        CompilationTimeoutMinutes(),
                        R2RDumpPath(),
                        MeasurePerf(),
                        InputFileSearchString(),
                        OutputDirectory(),
                        MibcPath(),
                    },
                    CompileFrameworkCommand.CompileFramework);

            Command CompileNugetPackages() =>
                CreateCommand("compile-nuget", "Restore a list of Nuget packages into an empty console app, publish, and optimize with Crossgen / CPAOT",
                    new Option[]
                    {
                        R2RDumpPath(),
                        InputDirectory(),
                        OutputDirectory(),
                        PackageList(),
                        NoCleanup(),
                        Map(),
                        Pdb(),
                        DegreeOfParallelism(),
                        CompilationTimeoutMinutes(),
                        ExecutionTimeoutMinutes(),
                        MibcPath(),
                    },
                    CompileNugetCommand.CompileNuget);

            Command CompileSerp() =>
                CreateCommand("compile-serp", "Compile existing application",
                    new Option[]
                    {
                        InputDirectory(),
                        DegreeOfParallelism(),
                        AspNetPath(),
                        Composite(),
                        Map(),
                        Pdb(),
                        CompilationTimeoutMinutes(),
                        Crossgen2Path(),
                        MibcPath(),
                    },
                    options =>
                    {
                        var compileSerp = new CompileSerpCommand(options);
                        return compileSerp.CompileSerpAssemblies();
                    });

            // Todo: Input / Output directories should be required arguments to the command when they're made available to handlers
            // https://github.com/dotnet/command-line-api/issues/297
            Option InputDirectory() =>
                new Option<DirectoryInfo>(new[] { "--input-directory", "-in" }, "Folder containing assemblies to optimize").ExistingOnly();

            Option OutputDirectory() =>
                new Option<DirectoryInfo>(new[] { "--output-directory", "-out" }, "Folder to emit compiled assemblies").LegalFilePathsOnly();

            Option CoreRootDirectory() =>
                new Option<DirectoryInfo>(new[] { "--core-root-directory", "-cr" }, "Location of the CoreCLR CORE_ROOT folder")
                {
                    Required = true
                }.ExistingOnly();

            Option ReferencePath() =>
                new Option<DirectoryInfo[]>(new[] { "--reference-path", "-r" }, "Folder containing assemblies to reference during compilation")
                { Argument = new Argument<DirectoryInfo[]>() { Arity = ArgumentArity.ZeroOrMore }.ExistingOnly() };

            Option MibcPath() =>
                new Option<FileInfo[]>(new[] { "--mibc-path", "-m" }, "Mibc files to use in compilation")
                { Argument = new Argument<FileInfo[]>() { Arity = ArgumentArity.ZeroOrMore }.ExistingOnly() };

            Option Crossgen2Path() =>
                new Option<FileInfo>(new[] { "--crossgen2-path", "-c2p" }, "Explicit Crossgen2 path (useful for cross-targeting)").ExistingOnly();

            Option VerifyTypeAndFieldLayout() =>
                new Option<bool>(new[] { "--verify-type-and-field-layout" }, "Verify that struct type layout and field offsets match between compile time and runtime. Use only for diagnostic purposes.");

            Option NoJit() =>
                new Option<bool>(new[] { "--nojit" }, "Don't run tests in JITted mode");

            Option NoCrossgen2() =>
                new Option<bool>(new[] { "--nocrossgen2" }, "Don't run tests in Crossgen2 mode");

            Option Exe() =>
                new Option<bool>(new[] { "--exe" }, "Don't compile tests, just execute them");

            Option NoExe() =>
                new Option<bool>(new[] { "--noexe" }, "Compilation-only mode (don't execute the built apps)");

            Option NoEtw() =>
                new Option<bool>(new[] { "--noetw" }, "Don't capture jitted methods using ETW");

            Option NoCleanup() =>
                new Option<bool>(new[] { "--nocleanup" }, "Don't clean up compilation artifacts after test runs");

            Option Map() =>
                new Option<bool>(new[] { "--map" }, "Generate a map file (Crossgen2)");

            Option Pdb() =>
                new Option<bool>(new[] { "--pdb" }, "Generate PDB symbol information (Crossgen2 / Windows only)");

            Option DegreeOfParallelism() =>
                new Option<int>(new[] { "--degree-of-parallelism", "-dop" }, "Override default compilation / execution DOP (default = logical processor count)");

            Option Sequential() =>
                new Option<bool>(new[] { "--sequential" }, "Run tests sequentially");

            Option Framework() =>
                new Option<bool>(new[] { "--framework" }, "Precompile and use native framework");

            Option UseFramework() =>
                new Option<bool>(new[] { "--use-framework" }, "Use native framework (don't precompile, assume previously compiled)");

            Option Release() =>
                new Option<bool>(new[] { "--release" }, "Build the tests in release mode");

            Option LargeBubble() =>
                new Option<bool>(new[] { "--large-bubble" }, "Assume all input files as part of one version bubble");

            Option Composite() =>
                new Option<bool>(new[] { "--composite" }, "Compile tests in composite R2R mode");

            Option Crossgen2Parallelism() =>
                new Option<int>(new[] { "--crossgen2-parallelism" }, "Max number of threads to use in Crossgen2 (default = logical processor count)");
            
            Option Crossgen2JitPath() =>
                new Option<FileInfo>(new[] { "--crossgen2-jitpath" }, "Jit path to use for crossgen2");

            Option IssuesPath() =>
                new Option<FileInfo[]>(new[] { "--issues-path", "-ip" }, "Path to issues.targets")
                    { Argument = new Argument<FileInfo[]>() { Arity = ArgumentArity.ZeroOrMore } };

            Option CompilationTimeoutMinutes() =>
                new Option<int>(new[] { "--compilation-timeout-minutes", "-ct" }, "Compilation timeout (minutes)");

            Option ExecutionTimeoutMinutes() =>
                new Option<int>(new[] { "--execution-timeout-minutes", "-et" }, "Execution timeout (minutes)");

            Option R2RDumpPath() =>
                new Option<FileInfo>(new[] { "--r2r-dump-path", "-r2r" }, "Path to R2RDump.exe/dll").ExistingOnly();

            Option MeasurePerf() =>
                new Option<bool>(new[] { "--measure-perf" }, "Print out compilation time");

            Option InputFileSearchString() =>
                new Option<string>(new[] { "--input-file-search-string", "-input-file" }, "Search string for input files in the input directory");

            Option GCStress() =>
                new Option<string>(new[] { "--gcstress" }, "Run tests with the specified GC stress level enabled (the argument value is in hex)");

            Option DotNetCli() =>
                new Option<string>(new [] { "--dotnet-cli", "-cli" }, "For dev box testing, point at .NET 5 dotnet.exe or <repo>/dotnet.cmd.");

            Option TargetArch() =>
                new Option<string>(new[] { "--target-arch" }, "Target architecture for crossgen2");

            //
            // compile-nuget specific options
            //
            Option PackageList() =>
                new Option<FileInfo>(new[] { "--package-list", "-pl" }, "Text file containing a package name on each line").ExistingOnly();

            //
            // compile-serp specific options
            //
            Option AspNetPath() =>
                new Option<DirectoryInfo>(new[] { "--asp-net-path", "-asp" }, "Path to SERP's ASP.NET Core folder").ExistingOnly();
        }
    }
}
