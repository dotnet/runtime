// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;

namespace R2RTest
{
    public class R2RTestRootCommand : RootCommand
    {
        void CreateCommand(string name, string description, Option[] options, Func<BuildOptions, int> action)
        {
            Command command = new Command(name, description);
            foreach (var option in GetCommonOptions())
                command.AddOption(option);
            foreach (var option in options)
                command.AddOption(option);
            command.SetHandler<InvocationContext>((InvocationContext context) =>
                context.ExitCode = action(new BuildOptions(this, context.ParseResult)));
            AddCommand(command);
        }

        Option[] GetCommonOptions() => new Option[] { CoreRootDirectory, DotNetCli };

        R2RTestRootCommand()
        {
            CreateCommand("compile-directory", "Compile all assemblies in directory",
                new Option[]
                {
                    InputDirectory,
                    OutputDirectory,
                    Crossgen2Path,
                    TargetArch,
                    VerifyTypeAndFieldLayout,
                    NoJit,
                    NoCrossgen2,
                    Exe,
                    NoExe,
                    NoEtw,
                    NoCleanup,
                    Map,
                    Pdb,
                    Perfmap,
                    PerfmapFormatVersion,
                    DegreeOfParallelism,
                    Sequential,
                    Iterations,
                    Framework,
                    UseFramework,
                    Release,
                    LargeBubble,
                    Composite,
                    Crossgen2Parallelism,
                    Crossgen2JitPath,
                    ReferencePath,
                    IssuesPath,
                    CompilationTimeoutMinutes,
                    ExecutionTimeoutMinutes,
                    R2RDumpPath,
                    MeasurePerf,
                    InputFileSearchString,
                    MibcPath,
                },
                CompileDirectoryCommand.CompileDirectory);

            CreateCommand("compile-subtree", "Build each directory in a given subtree containing any managed assemblies as a separate app",
                new Option[]
                {
                    InputDirectory,
                    OutputDirectory,
                    Crossgen2Path,
                    TargetArch,
                    VerifyTypeAndFieldLayout,
                    NoJit,
                    NoCrossgen2,
                    Exe,
                    NoExe,
                    NoEtw,
                    NoCleanup,
                    Map,
                    Pdb,
                    Perfmap,
                    PerfmapFormatVersion,
                    DegreeOfParallelism,
                    Sequential,
                    Iterations,
                    Framework,
                    UseFramework,
                    Release,
                    LargeBubble,
                    Composite,
                    Crossgen2Parallelism,
                    Crossgen2JitPath,
                    ReferencePath,
                    IssuesPath,
                    CompilationTimeoutMinutes,
                    ExecutionTimeoutMinutes,
                    R2RDumpPath,
                    GCStress,
                    MibcPath,
                },
                CompileSubtreeCommand.CompileSubtree);

            CreateCommand("compile-framework", "Compile managed framework assemblies in Core_Root",
                new Option[]
                {
                    Crossgen2Path,
                    TargetArch,
                    VerifyTypeAndFieldLayout,
                    NoCrossgen2,
                    NoCleanup,
                    Map,
                    Pdb,
                    Perfmap,
                    PerfmapFormatVersion,
                    Crossgen2Parallelism,
                    Crossgen2JitPath,
                    DegreeOfParallelism,
                    Sequential,
                    Iterations,
                    Release,
                    LargeBubble,
                    Composite,
                    ReferencePath,
                    IssuesPath,
                    CompilationTimeoutMinutes,
                    R2RDumpPath,
                    MeasurePerf,
                    InputFileSearchString,
                    OutputDirectory,
                    MibcPath,
                },
                CompileFrameworkCommand.CompileFramework);

            CreateCommand("compile-nuget", "Restore a list of Nuget packages into an empty console app, publish, and optimize with Crossgen / CPAOT",
                new Option[]
                {
                    R2RDumpPath,
                    InputDirectory,
                    OutputDirectory,
                    PackageList,
                    NoCleanup,
                    Map,
                    Pdb,
                    Perfmap,
                    PerfmapFormatVersion,
                    DegreeOfParallelism,
                    CompilationTimeoutMinutes,
                    ExecutionTimeoutMinutes,
                    MibcPath,
                },
                CompileNugetCommand.CompileNuget);

            CreateCommand("compile-serp", "Compile existing application",
                new Option[]
                {
                    InputDirectory,
                    DegreeOfParallelism,
                    AspNetPath,
                    Composite,
                    Map,
                    Pdb,
                    Perfmap,
                    PerfmapFormatVersion,
                    CompilationTimeoutMinutes,
                    Crossgen2Path,
                    MibcPath,
                },
                options =>
                {
                    var compileSerp = new CompileSerpCommand(options);
                    return compileSerp.CompileSerpAssemblies();
                });
        }

        // Todo: Input / Output directories should be required arguments to the command when they're made available to handlers
        // https://github.com/dotnet/command-line-api/issues/297
        public Option<DirectoryInfo> InputDirectory { get; } =
            new Option<DirectoryInfo>(new[] { "--input-directory", "-in" }, "Folder containing assemblies to optimize").ExistingOnly();

        public Option<DirectoryInfo> OutputDirectory { get; } =
            new Option<DirectoryInfo>(new[] { "--output-directory", "-out" }, "Folder to emit compiled assemblies").LegalFilePathsOnly();

        public Option<DirectoryInfo> CoreRootDirectory { get; } =
            new Option<DirectoryInfo>(new[] { "--core-root-directory", "-cr" }, "Location of the CoreCLR CORE_ROOT folder")
            { Arity = ArgumentArity.ExactlyOne }.ExistingOnly();

        public Option<DirectoryInfo[]> ReferencePath { get; } =
            new Option<DirectoryInfo[]>(new[] { "--reference-path", "-r" }, "Folder containing assemblies to reference during compilation")
            { Arity = ArgumentArity.ZeroOrMore }.ExistingOnly();

        public Option<FileInfo[]> MibcPath { get; } =
            new Option<FileInfo[]>(new[] { "--mibc-path", "-m" }, "Mibc files to use in compilation")
            { Arity = ArgumentArity.ZeroOrMore }.ExistingOnly();

        public Option<FileInfo> Crossgen2Path { get; } =
            new Option<FileInfo>(new[] { "--crossgen2-path", "-c2p" }, "Explicit Crossgen2 path (useful for cross-targeting)").ExistingOnly();

        public Option<bool> VerifyTypeAndFieldLayout { get; } =
            new(new[] { "--verify-type-and-field-layout" }, "Verify that struct type layout and field offsets match between compile time and runtime. Use only for diagnostic purposes.");

        public Option<bool> NoJit { get; } =
            new(new[] { "--nojit" }, "Don't run tests in JITted mode");

        public Option<bool> NoCrossgen2 { get; } =
            new(new[] { "--nocrossgen2" }, "Don't run tests in Crossgen2 mode");

        public Option<bool> Exe { get; } =
            new(new[] { "--exe" }, "Don't compile tests, just execute them");

        public Option<bool> NoExe { get; } =
            new(new[] { "--noexe" }, "Compilation-only mode (don't execute the built apps)");

        public Option<bool> NoEtw { get; } =
            new(new[] { "--noetw" }, "Don't capture jitted methods using ETW");

        public Option<bool> NoCleanup { get; } =
            new(new[] { "--nocleanup" }, "Don't clean up compilation artifacts after test runs");

        public Option<bool> Map { get; } =
            new(new[] { "--map" }, "Generate a map file (Crossgen2)");

        public Option<bool> Pdb { get; } =
            new(new[] { "--pdb" }, "Generate PDB symbol information (Crossgen2 / Windows only)");

        public Option<bool> Perfmap { get; } =
            new(new[] { "--perfmap" }, "Generate perfmap symbol information");

        public Option<int> PerfmapFormatVersion { get; } =
            new(new[] { "--perfmap-format-version" }, () => 1, "Perfmap format version to generate");

        public Option<int> DegreeOfParallelism { get; } =
            new(new[] { "--degree-of-parallelism", "-dop" }, "Override default compilation / execution DOP (default = logical processor count)");

        public Option<bool> Sequential { get; } =
            new(new[] { "--sequential" }, "Run tests sequentially");

        public Option<int> Iterations { get; } =
            new(new[] { "--iterations" }, () => 1, "Number of iterations for each test execution");

        public Option<bool> Framework { get; } =
            new(new[] { "--framework" }, "Precompile and use native framework");

        public Option<bool> UseFramework { get; } =
            new(new[] { "--use-framework" }, "Use native framework (don't precompile, assume previously compiled)");

        public Option<bool> Release { get; } =
            new(new[] { "--release" }, "Build the tests in release mode");

        public Option<bool> LargeBubble { get; } =
            new(new[] { "--large-bubble" }, "Assume all input files as part of one version bubble");

        public Option<bool> Composite { get; } =
            new(new[] { "--composite" }, "Compile tests in composite R2R mode");

        public Option<int> Crossgen2Parallelism { get; } =
            new(new[] { "--crossgen2-parallelism" }, "Max number of threads to use in Crossgen2 (default = logical processor count)");

        public Option<FileInfo> Crossgen2JitPath { get; } =
            new(new[] { "--crossgen2-jitpath" }, "Jit path to use for crossgen2");

        public Option<FileInfo[]> IssuesPath { get; } =
            new Option<FileInfo[]>(new[] { "--issues-path", "-ip" }, "Path to issues.targets")
                { Arity = ArgumentArity.ZeroOrMore };

        public Option<int> CompilationTimeoutMinutes { get; } =
            new(new[] { "--compilation-timeout-minutes", "-ct" }, "Compilation timeout (minutes)");

        public Option<int> ExecutionTimeoutMinutes { get; } =
            new(new[] { "--execution-timeout-minutes", "-et" }, "Execution timeout (minutes)");

        public Option<FileInfo> R2RDumpPath { get; } =
            new Option<FileInfo>(new[] { "--r2r-dump-path" }, "Path to R2RDump.exe/dll").ExistingOnly();

        public Option<bool> MeasurePerf { get; } =
            new(new[] { "--measure-perf" }, "Print out compilation time");

        public Option<string> InputFileSearchString { get; } =
            new(new[] { "--input-file-search-string", "-input-file" }, "Search string for input files in the input directory");

        public Option<string> GCStress { get; } =
            new(new[] { "--gcstress" }, "Run tests with the specified GC stress level enabled (the argument value is in hex)");

        public Option<string> DotNetCli { get; } =
            new(new [] { "--dotnet-cli", "-cli" }, "For dev box testing, point at .NET 5 dotnet.exe or <repo>/dotnet.cmd.");

        public Option<string> TargetArch { get; } =
            new(new[] { "--target-arch" }, "Target architecture for crossgen2");

        //
        // compile-nuget specific options
        //
        public Option<FileInfo> PackageList { get; } =
            new Option<FileInfo>(new[] { "--package-list", "-pl" }, "Text file containing a package name on each line").ExistingOnly();

        //
        // compile-serp specific options
        //
        public Option<DirectoryInfo> AspNetPath { get; } =
            new Option<DirectoryInfo>(new[] { "--asp-net-path", "-asp" }, "Path to SERP's ASP.NET Core folder").ExistingOnly();

        static int Main(string[] args)
        {
            return new CommandLineBuilder(new R2RTestRootCommand())
                .UseHelp()
                .UseParseErrorReporting()
                .Build()
                .Invoke(args);
        }
    }

    public partial class BuildOptions
    {
        public BuildOptions(R2RTestRootCommand cmd, ParseResult res)
        {
            InputDirectory = res.GetValueForOption(cmd.InputDirectory);
            OutputDirectory = res.GetValueForOption(cmd.OutputDirectory);
            CoreRootDirectory = res.GetValueForOption(cmd.CoreRootDirectory);
            Crossgen2Path = res.GetValueForOption(cmd.Crossgen2Path);
            VerifyTypeAndFieldLayout = res.GetValueForOption(cmd.VerifyTypeAndFieldLayout);
            TargetArch = res.GetValueForOption(cmd.TargetArch);
            Exe = res.GetValueForOption(cmd.Exe);
            NoJit = res.GetValueForOption(cmd.NoJit);
            NoCrossgen2 = res.GetValueForOption(cmd.NoCrossgen2);
            NoExe = res.GetValueForOption(cmd.NoExe);
            NoEtw = res.GetValueForOption(cmd.NoEtw);
            NoCleanup = res.GetValueForOption(cmd.NoCleanup);
            Map = res.GetValueForOption(cmd.Map);
            Pdb = res.GetValueForOption(cmd.Pdb);

            Perfmap = res.GetValueForOption(cmd.Perfmap);
            PerfmapFormatVersion = res.GetValueForOption(cmd.PerfmapFormatVersion);
            PackageList = res.GetValueForOption(cmd.PackageList);
            DegreeOfParallelism = res.GetValueForOption(cmd.DegreeOfParallelism);
            Sequential = res.GetValueForOption(cmd.Sequential);
            Iterations = res.GetValueForOption(cmd.Iterations);
            Framework = res.GetValueForOption(cmd.Framework);
            UseFramework = res.GetValueForOption(cmd.UseFramework);
            Release = res.GetValueForOption(cmd.Release);
            LargeBubble = res.GetValueForOption(cmd.LargeBubble);
            Composite = res.GetValueForOption(cmd.Composite);
            Crossgen2Parallelism = res.GetValueForOption(cmd.Crossgen2Parallelism);
            Crossgen2JitPath = res.GetValueForOption(cmd.Crossgen2JitPath);
            CompilationTimeoutMinutes = res.GetValueForOption(cmd.CompilationTimeoutMinutes);
            ExecutionTimeoutMinutes = res.GetValueForOption(cmd.ExecutionTimeoutMinutes);
            ReferencePath = res.GetValueForOption(cmd.ReferencePath);
            IssuesPath = res.GetValueForOption(cmd.IssuesPath);
            R2RDumpPath = res.GetValueForOption(cmd.R2RDumpPath);
            AspNetPath = res.GetValueForOption(cmd.AspNetPath);
            MeasurePerf = res.GetValueForOption(cmd.MeasurePerf);
            InputFileSearchString = res.GetValueForOption(cmd.InputFileSearchString);
            GCStress = res.GetValueForOption(cmd.GCStress);
            MibcPath = res.GetValueForOption(cmd.MibcPath);
        }

        public DirectoryInfo InputDirectory { get; set; }
        public DirectoryInfo OutputDirectory { get; set; }
        public DirectoryInfo CoreRootDirectory { get; }
        public FileInfo Crossgen2Path { get; }
        public bool VerifyTypeAndFieldLayout { get; }
        public string TargetArch { get; }
        public bool Exe { get; }
        public bool NoJit { get; set; }
        public bool NoCrossgen2 { get; }
        public bool NoExe { get; set; }
        public bool NoEtw { get; set; }
        public bool NoCleanup { get; }
        public bool Map { get; }
        public bool Pdb { get; }

        public bool Perfmap { get; }
        public int PerfmapFormatVersion { get; }
        public FileInfo PackageList { get; }
        public int DegreeOfParallelism { get; set; }
        public bool Sequential { get; }
        public int Iterations { get; }
        public bool Framework { get; set; }
        public bool UseFramework { get; }
        public bool Release { get; set; }
        public bool LargeBubble { get; }
        public bool Composite { get; }
        public int Crossgen2Parallelism { get; }
        public FileInfo Crossgen2JitPath { get; }
        public int CompilationTimeoutMinutes { get; }
        public int ExecutionTimeoutMinutes { get; }
        public DirectoryInfo[] ReferencePath { get; }
        public FileInfo[] IssuesPath { get; }
        public FileInfo R2RDumpPath { get; }
        public DirectoryInfo AspNetPath { get; }
        public bool MeasurePerf { get; }
        public string InputFileSearchString { get; }
        public string GCStress { get; }
        public FileInfo[] MibcPath { get; }
    }
}
