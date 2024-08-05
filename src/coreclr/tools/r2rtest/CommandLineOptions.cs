// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;

namespace R2RTest
{
    public class R2RTestRootCommand : CliRootCommand
    {
        void CreateCommand(string name, string description, CliOption[] options, Func<BuildOptions, int> action)
        {
            CliCommand command = new(name, description);
            foreach (var option in GetCommonOptions())
                command.Options.Add(option);
            foreach (var option in options)
                command.Options.Add(option);
            command.SetAction(result => action(new BuildOptions(this, result)));
            Subcommands.Add(command);
        }

        CliOption[] GetCommonOptions() => new CliOption[] { CoreRootDirectory, DotNetCli };

        R2RTestRootCommand()
        {
            OutputDirectory.AcceptLegalFilePathsOnly();

            CreateCommand("compile-directory", "Compile all assemblies in directory",
                new CliOption[]
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
                new CliOption[]
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
                new CliOption[]
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
                new CliOption[]
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
                new CliOption[]
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
        public CliOption<DirectoryInfo> InputDirectory { get; } =
            new CliOption<DirectoryInfo>("--input-directory", "-in") { Description = "Folder containing assemblies to optimize" }.AcceptExistingOnly();

        public CliOption<DirectoryInfo> OutputDirectory { get; } =
            new CliOption<DirectoryInfo>("--output-directory", "-out") { Description = "Folder to emit compiled assemblies" };

        public CliOption<DirectoryInfo> CoreRootDirectory { get; } =
            new CliOption<DirectoryInfo>("--core-root-directory", "-cr") { Description = "Location of the CoreCLR CORE_ROOT folder", Arity = ArgumentArity.ExactlyOne }.AcceptExistingOnly();

        public CliOption<DirectoryInfo[]> ReferencePath { get; } =
            new CliOption<DirectoryInfo[]>("--reference-path", "-r") { Description = "Folder containing assemblies to reference during compilation", Arity = ArgumentArity.ZeroOrMore }.AcceptExistingOnly();

        public CliOption<FileInfo[]> MibcPath { get; } =
            new CliOption<FileInfo[]>("--mibc-path", "-m") { Description = "Mibc files to use in compilation", Arity = ArgumentArity.ZeroOrMore }.AcceptExistingOnly();

        public CliOption<FileInfo> Crossgen2Path { get; } =
            new CliOption<FileInfo>("--crossgen2-path", "-c2p") { Description = "Explicit Crossgen2 path (useful for cross-targeting)" }.AcceptExistingOnly();

        public CliOption<bool> VerifyTypeAndFieldLayout { get; } =
            new("--verify-type-and-field-layout") { Description = "Verify that struct type layout and field offsets match between compile time and runtime. Use only for diagnostic purposes." };

        public CliOption<bool> NoJit { get; } =
            new("--nojit") { Description = "Don't run tests in JITted mode" };

        public CliOption<bool> NoCrossgen2 { get; } =
            new("--nocrossgen2") { Description = "Don't run tests in Crossgen2 mode" };

        public CliOption<bool> Exe { get; } =
            new("--exe") { Description = "Don't compile tests, just execute them" };

        public CliOption<bool> NoExe { get; } =
            new("--noexe") { Description = "Compilation-only mode (don't execute the built apps)" };

        public CliOption<bool> NoEtw { get; } =
            new("--noetw") { Description = "Don't capture jitted methods using ETW" };

        public CliOption<bool> NoCleanup { get; } =
            new("--nocleanup") { Description = "Don't clean up compilation artifacts after test runs" };

        public CliOption<bool> Map { get; } =
            new("--map") { Description = "Generate a map file (Crossgen2)" };

        public CliOption<bool> Pdb { get; } =
            new("--pdb") { Description = "Generate PDB symbol information (Crossgen2 / Windows only)" };

        public CliOption<bool> Perfmap { get; } =
            new("--perfmap") { Description = "Generate perfmap symbol information" };

        public CliOption<int> PerfmapFormatVersion { get; } =
            new("--perfmap-format-version") { DefaultValueFactory = _ => 1, Description = "Perfmap format version to generate" };

        public CliOption<int> DegreeOfParallelism { get; } =
            new("--degree-of-parallelism", "-dop") { Description = "Override default compilation / execution DOP (default = logical processor count)" };

        public CliOption<bool> Sequential { get; } =
            new("--sequential") { Description = "Run tests sequentially" };

        public CliOption<int> Iterations { get; } =
            new("--iterations") { DefaultValueFactory = _ => 1, Description = "Number of iterations for each test execution" };

        public CliOption<bool> Framework { get; } =
            new("--framework") { Description = "Precompile and use native framework" };

        public CliOption<bool> UseFramework { get; } =
            new("--use-framework") { Description = "Use native framework (don't precompile, assume previously compiled)" };

        public CliOption<bool> Release { get; } =
            new("--release") { Description = "Build the tests in release mode" };

        public CliOption<bool> LargeBubble { get; } =
            new("--large-bubble") { Description = "Assume all input files as part of one version bubble" };

        public CliOption<bool> Composite { get; } =
            new("--composite") { Description = "Compile tests in composite R2R mode" };

        public CliOption<int> Crossgen2Parallelism { get; } =
            new("--crossgen2-parallelism") { Description = "Max number of threads to use in Crossgen2 (default = logical processor count)" };

        public CliOption<FileInfo> Crossgen2JitPath { get; } =
            new("--crossgen2-jitpath") { Description = "Jit path to use for crossgen2" };

        public CliOption<FileInfo[]> IssuesPath { get; } =
            new("--issues-path", "-ip") { Description = "Path to issues.targets", Arity = ArgumentArity.ZeroOrMore };

        public CliOption<int> CompilationTimeoutMinutes { get; } =
            new("--compilation-timeout-minutes", "-ct") { Description = "Compilation timeout (minutes)" };

        public CliOption<int> ExecutionTimeoutMinutes { get; } =
            new("--execution-timeout-minutes", "-et") { Description = "Execution timeout (minutes)" };

        public CliOption<FileInfo> R2RDumpPath { get; } =
            new CliOption<FileInfo>("--r2r-dump-path") { Description = "Path to R2RDump.exe/dll" }.AcceptExistingOnly();

        public CliOption<bool> MeasurePerf { get; } =
            new("--measure-perf") { Description = "Print out compilation time" };

        public CliOption<string> InputFileSearchString { get; } =
            new("--input-file-search-string", "-input-file") { Description = "Search string for input files in the input directory" };

        public CliOption<string> GCStress { get; } =
            new("--gcstress") { Description = "Run tests with the specified GC stress level enabled (the argument value is in hex)" };

        public CliOption<string> DotNetCli { get; } =
            new("--dotnet-cli", "-cli") { Description = "For dev box testing, point at .NET 5 dotnet.exe or <repo>/dotnet.cmd." };

        public CliOption<string> TargetArch { get; } =
            new("--target-arch") { Description = "Target architecture for crossgen2" };

        //
        // compile-nuget specific options
        //
        public CliOption<FileInfo> PackageList { get; } =
            new CliOption<FileInfo>("--package-list", "-pl") { Description = "Text file containing a package name on each line" }.AcceptExistingOnly();

        //
        // compile-serp specific options
        //
        public CliOption<DirectoryInfo> AspNetPath { get; } =
            new CliOption<DirectoryInfo>("--asp-net-path", "-asp") { Description = "Path to SERP's ASP.NET Core folder" }.AcceptExistingOnly();

        private static int Main(string[] args) =>
            new CliConfiguration(new R2RTestRootCommand().UseVersion()).Invoke(args);
    }

    public partial class BuildOptions
    {
        public BuildOptions(R2RTestRootCommand cmd, ParseResult res)
        {
            InputDirectory = res.GetValue(cmd.InputDirectory);
            OutputDirectory = res.GetValue(cmd.OutputDirectory);
            CoreRootDirectory = res.GetValue(cmd.CoreRootDirectory);
            Crossgen2Path = res.GetValue(cmd.Crossgen2Path);
            VerifyTypeAndFieldLayout = res.GetValue(cmd.VerifyTypeAndFieldLayout);
            TargetArch = res.GetValue(cmd.TargetArch);
            Exe = res.GetValue(cmd.Exe);
            NoJit = res.GetValue(cmd.NoJit);
            NoCrossgen2 = res.GetValue(cmd.NoCrossgen2);
            NoExe = res.GetValue(cmd.NoExe);
            NoEtw = res.GetValue(cmd.NoEtw);
            NoCleanup = res.GetValue(cmd.NoCleanup);
            Map = res.GetValue(cmd.Map);
            Pdb = res.GetValue(cmd.Pdb);

            Perfmap = res.GetValue(cmd.Perfmap);
            PerfmapFormatVersion = res.GetValue(cmd.PerfmapFormatVersion);
            PackageList = res.GetValue(cmd.PackageList);
            DegreeOfParallelism = res.GetValue(cmd.DegreeOfParallelism);
            Sequential = res.GetValue(cmd.Sequential);
            Iterations = res.GetValue(cmd.Iterations);
            Framework = res.GetValue(cmd.Framework);
            UseFramework = res.GetValue(cmd.UseFramework);
            Release = res.GetValue(cmd.Release);
            LargeBubble = res.GetValue(cmd.LargeBubble);
            Composite = res.GetValue(cmd.Composite);
            Crossgen2Parallelism = res.GetValue(cmd.Crossgen2Parallelism);
            Crossgen2JitPath = res.GetValue(cmd.Crossgen2JitPath);
            CompilationTimeoutMinutes = res.GetValue(cmd.CompilationTimeoutMinutes);
            ExecutionTimeoutMinutes = res.GetValue(cmd.ExecutionTimeoutMinutes);
            ReferencePath = res.GetValue(cmd.ReferencePath);
            IssuesPath = res.GetValue(cmd.IssuesPath);
            R2RDumpPath = res.GetValue(cmd.R2RDumpPath);
            AspNetPath = res.GetValue(cmd.AspNetPath);
            MeasurePerf = res.GetValue(cmd.MeasurePerf);
            InputFileSearchString = res.GetValue(cmd.InputFileSearchString);
            GCStress = res.GetValue(cmd.GCStress);
            MibcPath = res.GetValue(cmd.MibcPath);
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
