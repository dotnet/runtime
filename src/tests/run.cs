// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#:property ImportDirectoryBuildProps=false
#:property ImportDirectoryBuildTargets=false
#:property TargetFramework=net11.0
#:property GenerateDependencyFile=true
#:property GenerateRuntimeConfigurationFiles=true
#:property SelfContained=false

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

return TestRunner.Run(args);

internal static class TestRunner
{
    private const int RLimitCore = 4;
    private static readonly Regex s_coreDumpRegex = new("^core$", RegexOptions.CultureInvariant);
    private static readonly Regex s_coreDumpWithPidRegex = new(@"^core\.[0-9]+", RegexOptions.CultureInvariant);
    private static readonly Regex s_issueUrlRegex = new(@"^https://github\.com/([^/]+/[^/]+)/issues/(\d+)", RegexOptions.CultureInvariant);
    private static readonly string[] s_validArchitectures = ["x64", "x86", "arm", "arm64", "loongarch64", "riscv64", "wasm"];
    private static readonly string[] s_validBuildTypes = ["Debug", "Checked", "Release"];
    private static readonly string[] s_validHostOperatingSystems = ["windows", "osx", "linux", "illumos", "solaris", "haiku", "freebsd", "browser", "android", "wasi"];
    private static readonly string[] s_validParallelValues = ["none", "collections", "assemblies", "all"];
    private static bool s_gcStress;
    private static string s_coreDumpPattern = "";

    public static int Run(string[] arguments)
    {
        try
        {
            int parseResult = Options.Parse(arguments, out Options? options);
            if (parseResult != 0 || options is null)
            {
                return parseResult;
            }

            RunnerArguments runnerArguments = SetupArguments(options);
            Dictionary<string, string> environment = GetEnvironment(options.TestEnvironment);
            int returnCode = 0;

            if (!runnerArguments.AnalyzeResultsOnly)
            {
                returnCode = runnerArguments.TestEnvironment is not null
                    ? RunTests(runnerArguments, runnerArguments.TestEnvironment)
                    : CreateAndUseTestEnvironment(runnerArguments, environment);
                Console.WriteLine("Test run finished.");
            }

            List<TestResult> tests = [];
            Dictionary<string, AssemblyInfo> assemblies = new(StringComparer.Ordinal);
            List<CrashedRunner> crashedRunners = ParseTestResults(runnerArguments, tests, assemblies);
            PrintSummary(tests, assemblies, crashedRunners);
            PrintActiveIssueSummary(tests, runnerArguments.ActiveIssueDetails);
            int reproCount = CreateRepro(runnerArguments, environment, tests);

            Console.WriteLine();
            Console.WriteLine($"Log files at: {runnerArguments.LogsDirectory}");
            if (reproCount > 0)
            {
                Console.WriteLine($"Repro files at: {runnerArguments.ReproLocation}");
            }

            return returnCode;
        }
        catch (RunnerExitException exception)
        {
            if (!string.IsNullOrEmpty(exception.Message))
            {
                Console.WriteLine(exception.Message);
            }

            return exception.ExitCode;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static RunnerArguments SetupArguments(Options options)
    {
        string runtimeRepositoryLocation = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(GetSourceFilePath())!, "..", ".."));
        string artifactsLocation = Path.Combine(runtimeRepositoryLocation, "artifacts");
        string hostOperatingSystem = options.HostOperatingSystem ?? GetDefaultHostOperatingSystem();
        ValidateValue(hostOperatingSystem, s_validHostOperatingSystems, "Unknown host_os {0}\nSupported OS: {1}");

        string architecture = options.Architecture ?? GetDefaultArchitecture();
        ValidateValue(architecture, s_validArchitectures, "Unknown arch {0}.\nSupported architectures: {1}");

        string buildType = NormalizeBuildType(options.BuildType);
        ValidateValue(buildType, s_validBuildTypes, "Unknown build_type {0}.\nSupported build types: {1}");

        string normalTestLocation = Path.Combine(artifactsLocation, "tests", "coreclr", $"{hostOperatingSystem}.{architecture}.{buildType}");
        string testLocation;
        if (Directory.Exists(normalTestLocation))
        {
            testLocation = normalTestLocation;
        }
        else if (options.TestLocation is not null && Directory.Exists(options.TestLocation))
        {
            testLocation = Path.GetFullPath(options.TestLocation);
        }
        else
        {
            throw new RunnerExitException("Error, incorrect test location.");
        }

        if (!testLocation.Contains(buildType, StringComparison.Ordinal))
        {
            string lastPart = testLocation.Split('.')[^1];
            string correctedBuildType = new(lastPart.Where(char.IsLetterOrDigit).ToArray());
            correctedBuildType = NormalizeBuildType(correctedBuildType);
            ValidateValue(correctedBuildType, s_validBuildTypes, $"Unsupported configuration: {correctedBuildType}.\nSupported configurations: {string.Join(", ", s_validBuildTypes)}");
            buildType = correctedBuildType;
        }

        if (!string.Equals(testLocation, normalTestLocation, StringComparison.Ordinal))
        {
            Console.WriteLine($"Error, msbuild currently expects tests in {normalTestLocation} (got test_location {testLocation})");
            throw new InvalidOperationException("Error, msbuild currently expects tests in artifacts/tests/...");
        }

        bool requiresCoreRoot = options.HostOperatingSystem is not ("browser" or "android");
        string? coreRoot = options.CoreRoot;
        if (coreRoot is not null)
        {
            coreRoot = Path.GetFullPath(coreRoot);
        }
        else
        {
            coreRoot = Path.Combine(testLocation, "Tests", "Core_Root");
        }

        if (requiresCoreRoot && !Directory.Exists(coreRoot))
        {
            throw new RunnerExitException("Error, Core_Root could not be determined, or points to a location that doesn't exist.");
        }

        if (options.Parallel is not null && !s_validParallelValues.Contains(options.Parallel, StringComparer.Ordinal))
        {
            throw new RunnerExitException($"Parallel argument '{options.Parallel}' unknown");
        }

        if (options.Sequential && options.Parallel is not null)
        {
            throw new RunnerExitException("Error: don't specify both --sequential and -parallel");
        }

        string logsDirectory = options.LogsDirectory is not null
            ? Path.GetFullPath(options.LogsDirectory)
            : Path.Combine(artifactsLocation, "log");

        if (options.AnalyzeResultsOnly)
        {
            if (!Directory.Exists(logsDirectory))
            {
                throw new RunnerExitException($"Error: logs_dir not found: {logsDirectory}");
            }
        }
        else
        {
            Directory.CreateDirectory(logsDirectory);
        }

        Console.WriteLine($"host_os                  : {hostOperatingSystem}");
        Console.WriteLine($"arch                     : {architecture}");
        Console.WriteLine($"build_type               : {buildType}");
        Console.WriteLine($"runtime_repo_location    : {runtimeRepositoryLocation}");
        Console.WriteLine($"core_root                : {coreRoot}");
        Console.WriteLine($"test_location            : {testLocation}");
        Console.WriteLine($"logs_dir                 : {logsDirectory}");

        string scriptExtension = OperatingSystem.IsWindows() ? ".cmd" : ".sh";
        string testsSourceDirectory = Path.Combine(runtimeRepositoryLocation, "src", "tests");
        return new RunnerArguments(options)
        {
            HostOperatingSystem = hostOperatingSystem,
            Architecture = architecture,
            BuildType = buildType,
            RuntimeRepositoryLocation = runtimeRepositoryLocation,
            ArtifactsLocation = artifactsLocation,
            CoreRoot = coreRoot,
            TestLocation = testLocation,
            LogsDirectory = logsDirectory,
            ReproLocation = Path.Combine(logsDirectory, "repro"),
            DotNetCliScriptPath = Path.Combine(runtimeRepositoryLocation, $"dotnet{scriptExtension}"),
            TestsSourceDirectory = testsSourceDirectory,
            RunInContextScriptPath = Path.Combine(testsSourceDirectory, "Common", "scripts", $"runincontext{scriptExtension}"),
            TieringTestScriptPath = Path.Combine(testsSourceDirectory, "Common", "scripts", $"tieringtest{scriptExtension}"),
            NativeAotTestScriptPath = Path.Combine(testsSourceDirectory, "Common", "scripts", $"nativeaottest{scriptExtension}"),
        };
    }

    private static Dictionary<string, string> GetEnvironment(string? testEnvironment)
    {
        Dictionary<string, string> dotnetVariables = CreateEnvironmentDictionary();
        List<(string Key, string Value)> variables = [];

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            string key = (string)entry.Key;
            if (key.Contains("dotnet", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("superpmi", StringComparison.OrdinalIgnoreCase))
            {
                variables.Add((key, (string?)entry.Value ?? ""));
            }
        }

        foreach ((string key, string value) in variables)
        {
            dotnetVariables[key] = value;
            Environment.SetEnvironmentVariable(key, "");
        }

        if (testEnvironment is not null)
        {
            foreach (string item in File.ReadLines(testEnvironment))
            {
                string[] parts = item.Split('=');
                if (parts.Length == 1)
                {
                    continue;
                }

                string key = parts[0].Split(' ')[^1];
                string value = parts[1].Trim().Split(' ')[0];
                dotnetVariables[key] = value;
                dotnetVariables[key.ToLowerInvariant()] = value;
            }

            if (dotnetVariables.ContainsKey("dotnet_gcstress"))
            {
                s_gcStress = true;
            }
        }

        return dotnetVariables;
    }

    private static int CreateAndUseTestEnvironment(RunnerArguments arguments, Dictionary<string, string> environment)
    {
        Dictionary<string, string> dotnetVariables = CreateEnvironmentDictionary();
        foreach ((string key, string value) in environment)
        {
            if (key.Contains("dotnet", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("superpmi", StringComparison.OrdinalIgnoreCase))
            {
                dotnetVariables[key] = value;
            }
        }

        if (dotnetVariables.Count == 0)
        {
            return RunTests(arguments, null);
        }

        Console.WriteLine("Found DOTNET variables in the current environment");
        Console.WriteLine();

        string suffix = OperatingSystem.IsWindows() ? ".bat" : "";
        string testEnvironmentPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + suffix);
        StringBuilder contents = new();
        contents.AppendLine(OperatingSystem.IsWindows() ? "@REM Temporary test env for test run.\r\n@echo on" : "# Temporary test env for test run.");

        foreach ((string key, string value) in dotnetVariables)
        {
            string command = OperatingSystem.IsWindows() ? "set" : "export";
            if (string.Equals(key, "dotnet_gcstress", StringComparison.OrdinalIgnoreCase))
            {
                s_gcStress = true;
            }

            Console.WriteLine($"Unset {key}");
            Environment.SetEnvironmentVariable(key, "");
            contents.AppendLine($"{command} {key}={value}");
        }

        if (OperatingSystem.IsWindows())
        {
            contents.AppendLine("@echo off");
        }

        File.WriteAllText(testEnvironmentPath, contents.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        try
        {
            Console.WriteLine();
            Console.WriteLine($"TestEnv: {testEnvironmentPath}");
            Console.WriteLine();
            Console.WriteLine("Contents:");
            Console.WriteLine();
            Console.Write(contents);
            Console.WriteLine();

            return RunTests(arguments, testEnvironmentPath);
        }
        finally
        {
            File.Delete(testEnvironmentPath);
        }
    }

    private static int RunTests(RunnerArguments arguments, string? testEnvironmentScriptPath)
    {
        int perTestTimeout = 30 * 60 * 1000;

        if (arguments.LongGc)
        {
            Console.WriteLine("Running Long GC Tests, extending timeout to 40 minutes.");
            perTestTimeout = 40 * 60 * 1000;
            Console.WriteLine("Setting RunningLongGCTests=1");
            Environment.SetEnvironmentVariable("RunningLongGCTests", "1");
        }

        if (arguments.GcSimulator)
        {
            Console.WriteLine("Running GCSimulator tests, extending timeout to two hours.");
            perTestTimeout = 120 * 60 * 1000;
            Console.WriteLine("Setting RunningGCSimulatorTests=1");
            Environment.SetEnvironmentVariable("RunningGCSimulatorTests", "1");
        }

        if (arguments.IlasmRoundTrip)
        {
            Console.WriteLine("Running ILasm round trip.");
            Console.WriteLine("Setting RunningIlasmRoundTrip=1");
            Environment.SetEnvironmentVariable("RunningIlasmRoundTrip", "1");
        }

        if (arguments.UseManagedIlasm)
        {
            if (!arguments.IlasmRoundTrip)
            {
                Console.WriteLine("--use_managed_ilasm implies --ilasmroundtrip; enabling ilasm round trip.");
                Console.WriteLine("Setting RunningIlasmRoundTrip=1");
                Environment.SetEnvironmentVariable("RunningIlasmRoundTrip", "1");
            }

            Console.WriteLine("Using managed ILasm for round trip.");
            Console.WriteLine("Setting IlasmRoundTripUseManagedIlasm=1");
            Environment.SetEnvironmentVariable("IlasmRoundTripUseManagedIlasm", "1");
        }

        if (arguments.RunCrossgen2Tests)
        {
            Console.WriteLine("Running tests R2R (Crossgen2)");
            Console.WriteLine("Setting RunCrossGen2=1");
            Environment.SetEnvironmentVariable("RunCrossGen2", "1");
        }

        if (arguments.LargeVersionBubble)
        {
            Console.WriteLine("Large Version Bubble enabled");
            Environment.SetEnvironmentVariable("LargeVersionBubble", "1");
        }

        if (arguments.SynthesizePgo)
        {
            Console.WriteLine("Synthesizing PGO");
            Environment.SetEnvironmentVariable("CrossGen2SynthesizePgo", "1");
        }

        if (arguments.LimitedCoreDumps)
        {
            SetupCoreDumpGeneration(arguments.HostOperatingSystem);
        }

        if (arguments.RunInContext)
        {
            Console.WriteLine("Running test in an unloadable AssemblyLoadContext");
            Environment.SetEnvironmentVariable("CLRCustomTestLauncher", arguments.RunInContextScriptPath);
            Environment.SetEnvironmentVariable("RunInUnloadableContext", "1");
            perTestTimeout = 40 * 60 * 1000;
        }

        if (arguments.TieringTest)
        {
            Console.WriteLine("Running test repeatedly to promote methods to tier1");
            Environment.SetEnvironmentVariable("CLRCustomTestLauncher", arguments.TieringTestScriptPath);
        }

        if (arguments.RunNativeAotTests)
        {
            Console.WriteLine("Running tests NativeAOT");
            Environment.SetEnvironmentVariable("CLRCustomTestLauncher", arguments.NativeAotTestScriptPath);
        }

        if (arguments.Interpreter)
        {
            Console.WriteLine("Running tests with the interpreter");
            Console.WriteLine("Setting RunInterpreter=1");
            Environment.SetEnvironmentVariable("RunInterpreter", "1");
        }

        if (arguments.Node)
        {
            Console.WriteLine("Running tests with the NodeJS");
            Console.WriteLine("Setting RunWithNodeJS=1");
            Environment.SetEnvironmentVariable("RunWithNodeJS", "1");
        }

        if (s_gcStress)
        {
            perTestTimeout *= 8;
            Console.WriteLine("Running GCStress, extending test timeout to cater for slower runtime.");
        }

        Console.WriteLine($"Setting __TestTimeout={perTestTimeout}");
        Environment.SetEnvironmentVariable("__TestTimeout", perTestTimeout.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine($"Setting CORE_ROOT={arguments.CoreRoot}");
        Environment.SetEnvironmentVariable("CORE_ROOT", arguments.CoreRoot);
        Console.WriteLine($"Setting __TestDotNetCmd={arguments.DotNetCliScriptPath}");
        Environment.SetEnvironmentVariable("__TestDotNetCmd", arguments.DotNetCliScriptPath);

        if (testEnvironmentScriptPath is not null)
        {
            Console.WriteLine($"Setting __TestEnv={testEnvironmentScriptPath}");
            Environment.SetEnvironmentVariable("__TestEnv", testEnvironmentScriptPath);
        }

        return CallMsBuild(arguments);
    }

    private static int CallMsBuild(RunnerArguments arguments)
    {
        List<string> commandArguments =
        [
            "msbuild",
            Path.Combine(arguments.TestsSourceDirectory, "build.proj"),
            "/t:RunTests",
        ];

        if (arguments.Parallel is not null)
        {
            commandArguments.Add($"/p:ParallelRun={arguments.Parallel}");
        }

        if (arguments.Sequential)
        {
            commandArguments.Add("/p:ParallelRun=none");
        }

        string msBuildDebugLogsDirectory = Path.Combine(arguments.LogsDirectory, "MsbuildDebugLogs");
        Directory.CreateDirectory(msBuildDebugLogsDirectory);
        Environment.SetEnvironmentVariable("MSBUILDDEBUGPATH", msBuildDebugLogsDirectory);

        string logPath = Path.Combine(arguments.LogsDirectory, $"TestRunResults_{arguments.HostOperatingSystem}_{arguments.Architecture}_{arguments.BuildType}");
        commandArguments.Add($"/fileLoggerParameters:\"Verbosity=normal;LogFile={logPath}.log\"");
        commandArguments.Add($"/fileLoggerParameters1:\"WarningsOnly;LogFile={logPath}.wrn\"");
        commandArguments.Add($"/fileLoggerParameters2:\"ErrorsOnly;LogFile={logPath}.err\"");
        commandArguments.Add($"/binaryLogger:{logPath}.binlog");
        commandArguments.Add("/consoleLoggerParameters:\"ShowCommandLine;Summary\"");

        if (arguments.Verbose)
        {
            commandArguments.Add("/verbosity:diag");
        }

        commandArguments.Add($"/p:TargetOS={arguments.HostOperatingSystem}");
        commandArguments.Add($"/p:TargetArchitecture={arguments.Architecture}");
        commandArguments.Add($"/p:Configuration={arguments.BuildType}");
        commandArguments.Add($"/p:__LogsDir={arguments.LogsDirectory}");

        if (arguments.LimitedCoreDumps)
        {
            commandArguments.Add("/p:LimitedCoreDumps=true");
        }

        if (arguments.RunnerFilter is not null)
        {
            commandArguments.Add($"/p:RunnerFilter={arguments.RunnerFilter}");
        }

        if (arguments.Tree is not null)
        {
            commandArguments.Add($"/p:TestSubtree={arguments.Tree}");
        }

        Console.WriteLine($"{arguments.DotNetCliScriptPath} {string.Join(' ', commandArguments)}");
        Console.Out.Flush();
        int exitCode = RunProcess(arguments.DotNetCliScriptPath, commandArguments);

        if (arguments.LimitedCoreDumps)
        {
            InspectAndDeleteCoreDumpFiles(arguments);
        }

        return exitCode;
    }

    private static void SetupCoreDumpGeneration(string hostOperatingSystem)
    {
        if (hostOperatingSystem is "osx" or "freebsd" or "openbsd")
        {
            ProcessResult result = RunProcessForOutput("sysctl", ["-n", "kern.corefile"]);
            if (result.ExitCode != 0)
            {
                return;
            }

            s_coreDumpPattern = result.StandardOutput.Trim();
        }
        else if (hostOperatingSystem == "linux")
        {
            s_coreDumpPattern = File.ReadAllText("/proc/sys/kernel/core_pattern").Trim();
        }
        else
        {
            Console.WriteLine($"CoreDump generation not enabled due to unsupported OS: {hostOperatingSystem}");
            return;
        }

        Console.WriteLine($"CoreDump Pattern: {s_coreDumpPattern}");
        if (s_coreDumpPattern is not ("core" or "core.%P"))
        {
            Console.WriteLine($"CoreDump generation not enabled due to unsupported coredump pattern: {s_coreDumpPattern}");
            return;
        }

        Console.WriteLine($"CoreDump pattern: {s_coreDumpPattern}");
        RLimit unlimited = new() { Current = ulong.MaxValue, Maximum = ulong.MaxValue };
        if (GetRLimit(RLimitCore, out RLimit limit) != 0 ||
            (limit.Current != ulong.MaxValue &&
             SetRLimit(RLimitCore, in unlimited) != 0))
        {
            Console.WriteLine($"Failed to enable CoreDump generation. rlimit_core: {limit.Current}");
            return;
        }

        Console.WriteLine("CoreDump generation enabled");
        if (hostOperatingSystem == "linux" && File.Exists("/proc/self/coredump_filter"))
        {
            File.WriteAllText("/proc/self/coredump_filter", "0x3F");
        }
    }

    private static void InspectAndDeleteCoreDumpFiles(RunnerArguments arguments)
    {
        bool coreDumpNameUsesPid = s_coreDumpPattern.Contains("%P", StringComparison.Ordinal);
        Console.WriteLine("Looking for coredumps...");

        if (!coreDumpNameUsesPid &&
            arguments.HostOperatingSystem == "linux" &&
            File.Exists("/proc/sys/kernel/core_uses_pid"))
        {
            coreDumpNameUsesPid = File.ReadAllText("/proc/sys/kernel/core_uses_pid").Trim() == "1";
        }

        Regex fileNamePattern = coreDumpNameUsesPid ? s_coreDumpWithPidRegex : s_coreDumpRegex;
        int matchedFileCount = 0;
        foreach (string file in Directory.EnumerateFiles(arguments.TestLocation, coreDumpNameUsesPid ? "core.*" : "core", SearchOption.AllDirectories))
        {
            if (!fileNamePattern.IsMatch(Path.GetFileName(file)))
            {
                continue;
            }

            Console.WriteLine($"Found coredump: {Path.GetFileName(file)} in {Path.GetDirectoryName(file)}");
            matchedFileCount++;
            InspectAndDeleteCoreDumpFile(arguments, file);
        }

        Console.WriteLine($"Found {matchedFileCount} coredumps.");
    }

    private static void InspectAndDeleteCoreDumpFile(RunnerArguments arguments, string coreDumpName)
    {
        string executableName = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT") ?? "", OperatingSystem.IsWindows() ? "corerun.exe" : "corerun");
        PrintInfoFromCoreDumpFile(arguments.HostOperatingSystem, coreDumpName, executableName);
        PreserveCoreDumpFile(coreDumpName, Path.Combine(Path.GetTempPath(), "coredumps_coreclr"));
        File.Delete(coreDumpName);
    }

    private static void PrintInfoFromCoreDumpFile(string hostOperatingSystem, string coreDumpName, string executableName)
    {
        if (!File.Exists(executableName))
        {
            Console.WriteLine($"Not printing coredump due to missing executable: {executableName}");
            return;
        }

        if (!File.Exists(coreDumpName))
        {
            Console.WriteLine($"Not printing coredump due to missing coredump: {coreDumpName}");
            return;
        }

        string command;
        List<string> arguments;
        if (hostOperatingSystem is "osx" or "freebsd" or "openbsd")
        {
            command = "lldb";
            arguments = ["-c", coreDumpName, "-b", "-o", "bt all", "-o", "disassemble -b -p"];
        }
        else if (hostOperatingSystem == "linux")
        {
            command = "gdb";
            arguments = ["--batch", "-ex", "thread apply all bt full", "-ex", "disassemble /r $pc", "-ex", "quit", executableName, coreDumpName];
        }
        else
        {
            Console.WriteLine($"Not printing coredump due to unsupported OS: {hostOperatingSystem}");
            return;
        }

        Console.WriteLine($"Printing info from coredump: {coreDumpName}");
        try
        {
            Console.Out.Flush();
            if (RunProcess(command, arguments) != 0)
            {
                Console.WriteLine($"Failed to print coredump: {coreDumpName}");
            }
        }
        catch
        {
            Console.WriteLine($"Failed to print coredump: {coreDumpName}");
        }
    }

    private static void PreserveCoreDumpFile(string coreDumpName, string rootStorageLocation)
    {
        Directory.CreateDirectory(rootStorageLocation);
        string storageLocation = Path.Combine(rootStorageLocation, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(storageLocation);
        if (File.Exists(coreDumpName) && !Directory.EnumerateFileSystemEntries(storageLocation).Any())
        {
            Console.WriteLine($"Copying coredump file {coreDumpName} to {storageLocation}");
            File.Copy(coreDumpName, Path.Combine(storageLocation, Path.GetFileName(coreDumpName)), overwrite: false);
        }
    }

    private static List<CrashedRunner> ParseTestResults(
        RunnerArguments arguments,
        List<TestResult> tests,
        Dictionary<string, AssemblyInfo> assemblies)
    {
        Console.WriteLine($"Parsing test results from ({arguments.LogsDirectory})");
        bool found = false;
        List<CrashedRunner> crashedRunners = [];

        foreach (string file in Directory.EnumerateFiles(arguments.LogsDirectory))
        {
            string item = Path.GetFileName(file);
            if (item.EndsWith("testrun.xml", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                string itemName = item.EndsWith(".testrun.xml", StringComparison.OrdinalIgnoreCase)
                    ? item[..^".testrun.xml".Length]
                    : "";
                ParseTestResultsXmlFile(arguments, item, itemName, tests, assemblies);
            }
            else if (string.Equals(item, "StandaloneRunnerTestResults.testrun.log", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                ParseStandaloneRunnerResultsFile(arguments, item, tests, assemblies);
            }
            else if (item.EndsWith(".testrun.xml.crashed", StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                CrashedRunner? crashedRunner = ParseCrashedRunnerFile(arguments, item);
                if (crashedRunner is not null)
                {
                    crashedRunners.Add(crashedRunner);
                }
            }
        }

        if (!found)
        {
            Console.WriteLine("Unable to find testRun.xml or StandaloneRunnerTestResults.testrun.log. This normally means the tests did not run.");
            Console.WriteLine("It could also mean there was a problem logging. Please run the tests again.");
        }

        return crashedRunners;
    }

    private static CrashedRunner? ParseCrashedRunnerFile(RunnerArguments arguments, string item)
    {
        string crashFile = Path.Combine(arguments.LogsDirectory, item);
        string name = item.EndsWith(".testrun.xml.crashed", StringComparison.OrdinalIgnoreCase)
            ? item[..^".testrun.xml.crashed".Length]
            : item;
        string? exitCode = null;
        string? script = null;

        try
        {
            foreach (string sourceLine in File.ReadLines(crashFile))
            {
                string line = sourceLine.Trim();
                if (line.StartsWith("ExitCode=", StringComparison.Ordinal))
                {
                    exitCode = line["ExitCode=".Length..];
                }
                else if (line.StartsWith("Script=", StringComparison.Ordinal))
                {
                    script = line["Script=".Length..];
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Warning: Failed to parse crash file {crashFile}: {exception.Message}");
            return null;
        }

        if (exitCode is null && script is null)
        {
            Console.WriteLine($"Warning: Crash file {crashFile} did not contain ExitCode or Script information.");
            return null;
        }

        return new CrashedRunner(name, exitCode, script);
    }

    private static void ParseStandaloneRunnerResultsFile(
        RunnerArguments arguments,
        string item,
        List<TestResult> tests,
        Dictionary<string, AssemblyInfo> assemblies)
    {
        string resultFile = Path.Combine(arguments.LogsDirectory, item);
        Console.WriteLine($"Analyzing {resultFile}");
        const string AssemblyName = "StandaloneRunnerTests";

        if (!assemblies.TryGetValue(AssemblyName, out AssemblyInfo? assembly))
        {
            assembly = new AssemblyInfo(AssemblyName, AssemblyName, isMergedTestsRun: false);
            assemblies.Add(AssemblyName, assembly);
        }

        foreach (string sourceLine in File.ReadLines(resultFile))
        {
            string line = sourceLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            int separator = line.LastIndexOf(": ", StringComparison.Ordinal);
            if (separator == -1)
            {
                continue;
            }

            string scriptPath = line[..separator];
            string result = line[(separator + 2)..];
            tests.Add(new TestResult
            {
                Name = Path.GetFileNameWithoutExtension(scriptPath),
                TestPath = scriptPath,
                Result = result,
                AssemblyDisplayName = AssemblyName,
                IsMerged = false,
            });

            if (result == "Pass")
            {
                assembly.Passed++;
            }
            else if (result == "Fail")
            {
                assembly.Failed++;
            }
            else
            {
                assembly.Skipped++;
            }
        }
    }

    private static void ParseTestResultsXmlFile(
        RunnerArguments arguments,
        string item,
        string itemName,
        List<TestResult> tests,
        Dictionary<string, AssemblyInfo> assemblies)
    {
        string resultFile = Path.Combine(arguments.LogsDirectory, item);
        Console.WriteLine($"Analyzing {resultFile}");
        XElement root;
        try
        {
            root = XDocument.Load(resultFile).Root!;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Warning: Failed to parse {resultFile} (possibly truncated from a crashed runner): {exception.Message}");
            return;
        }

        foreach (XElement assemblyElement in root.Elements())
        {
            XAttribute? nameAttribute = assemblyElement.Attribute("name");
            if (nameAttribute is null)
            {
                continue;
            }

            string assemblyName = nameAttribute.Value;
            string displayName = itemName.Length > 0 ? itemName : assemblyName;
            if (!assemblies.TryGetValue(assemblyName, out AssemblyInfo? assembly))
            {
                assembly = new AssemblyInfo(assemblyName, displayName, isMergedTestsRun: true);
                assemblies.Add(assemblyName, assembly);
            }

            assembly.Time += double.Parse(assemblyElement.Attribute("time")!.Value, CultureInfo.InvariantCulture);

            foreach (XElement collection in assemblyElement.Elements())
            {
                if (collection.Name.LocalName == "errors")
                {
                    if (!string.IsNullOrEmpty(collection.Value))
                    {
                        throw new RunnerExitException("Error running the tests, please run run.cs again.");
                    }

                    continue;
                }

                foreach (XElement test in collection.Elements())
                {
                    string type = test.Attribute("type")!.Value;
                    int groupSeparator = type.IndexOf("._", StringComparison.Ordinal);
                    if (groupSeparator >= 0)
                    {
                        type = type[..groupSeparator];
                    }

                    string testName = $"{type}::{test.Attribute("method")!.Value}";
                    string name = test.Attribute("name")!.Value;
                    if (name.Length > 0)
                    {
                        testName += $" ({name})";
                    }

                    string result = test.Attribute("result")!.Value;
                    string? reason = result == "Skip" ? test.Element("reason")?.Value : null;
                    tests.Add(new TestResult
                    {
                        Name = testName,
                        Result = result,
                        Time = double.Parse(collection.Attribute("time")!.Value, CultureInfo.InvariantCulture),
                        TestOutput = test.Element("output")?.Value,
                        SkipReason = reason,
                        AssemblyDisplayName = displayName,
                        IsMerged = true,
                    });

                    if (result == "Pass")
                    {
                        assembly.Passed++;
                    }
                    else if (result == "Fail")
                    {
                        assembly.Failed++;
                    }
                    else
                    {
                        assembly.Skipped++;
                        if (reason?.StartsWith("ActiveIssue:", StringComparison.Ordinal) == true)
                        {
                            assembly.ActiveIssue++;
                        }
                    }
                }
            }
        }
    }

    private static void PrintSummary(
        List<TestResult> tests,
        Dictionary<string, AssemblyInfo> assemblies,
        List<CrashedRunner> crashedRunners)
    {
        foreach (TestResult test in tests)
        {
            if (test.Result != "Fail")
            {
                continue;
            }

            Console.WriteLine($"Failed test: {test.Name} ({test.AssemblyDisplayName})");
            if (test.TestOutput is not null)
            {
                Console.WriteLine(test.TestOutput.Replace("\r\n", "\n", StringComparison.Ordinal));
            }
            else
            {
                Console.WriteLine("# Test output recorded in log file.");
            }

            Console.WriteLine();
        }

        Console.WriteLine("Time [secs] | Total | Passed | Failed | Skipped | ActiveIssue | Assembly Execution Summary");
        Console.WriteLine("===========================================================================================");

        double totalTime = 0;
        int totalTotal = 0;
        int totalPassed = 0;
        int totalFailed = 0;
        int totalSkipped = 0;
        int totalActiveIssue = 0;

        foreach (AssemblyInfo assembly in assemblies.Values)
        {
            int total = assembly.Passed + assembly.Failed + assembly.Skipped;
            Console.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0,11:F3} | {1,5} | {2,6} | {3,6} | {4,7} | {5,11} | {6}",
                assembly.Time,
                total,
                assembly.Passed,
                assembly.Failed,
                assembly.Skipped,
                assembly.ActiveIssue,
                assembly.DisplayName));
            totalTime += assembly.Time;
            totalTotal += total;
            totalPassed += assembly.Passed;
            totalFailed += assembly.Failed;
            totalSkipped += assembly.Skipped;
            totalActiveIssue += assembly.ActiveIssue;
        }

        Console.WriteLine("-------------------------------------------------------------------------------------------");
        Console.WriteLine(string.Format(
            CultureInfo.InvariantCulture,
            "{0,11:F3} | {1,5} | {2,6} | {3,6} | {4,7} | {5,11} | (total)",
            totalTime,
            totalTotal,
            totalPassed,
            totalFailed,
            totalSkipped,
            totalActiveIssue));
        Console.WriteLine();

        if (crashedRunners.Count > 0)
        {
            Console.WriteLine($"Crashed Runners ({crashedRunners.Count}):");
            Console.WriteLine("  Exit Code | Runner");
            Console.WriteLine("  ----------|-------");
            foreach (CrashedRunner runner in crashedRunners)
            {
                Console.WriteLine($"  {runner.ExitCode ?? "?",9} | {runner.Name}");
            }

            Console.WriteLine();
        }
    }

    private static Dictionary<string, List<(string TestName, string RunnerName)>> PrintActiveIssueSummary(
        List<TestResult> tests,
        bool showDetails)
    {
        Dictionary<string, List<(string TestName, string RunnerName)>> issues = new(StringComparer.Ordinal);
        foreach (TestResult test in tests)
        {
            if (test.Result != "Skip")
            {
                continue;
            }

            string? reason = test.SkipReason ?? test.TestOutput;
            if (reason?.StartsWith("ActiveIssue:", StringComparison.Ordinal) != true)
            {
                continue;
            }

            string issue = reason["ActiveIssue:".Length..].Trim();
            if (!issues.TryGetValue(issue, out List<(string TestName, string RunnerName)>? issueTests))
            {
                issueTests = [];
                issues.Add(issue, issueTests);
            }

            issueTests.Add((test.Name, test.AssemblyDisplayName ?? "unknown"));
        }

        if (issues.Count == 0)
        {
            Console.WriteLine("ActiveIssue Summary (0 issues, 0 tests):");
            Console.WriteLine();
            return issues;
        }

        Dictionary<string, string> issueTitles = showDetails ? FetchIssueTitles(issues) : new(StringComparer.Ordinal);
        int totalTests = issues.Sum(pair => pair.Value.Count);
        Console.WriteLine($"ActiveIssue Summary ({issues.Count} issues, {totalTests} tests):");
        Console.WriteLine($"  {"Tests",5} | Issue");
        Console.WriteLine($"  ------|{new string('-', 60)}");

        IEnumerable<KeyValuePair<string, List<(string TestName, string RunnerName)>>> sortedIssues =
            issues.OrderByDescending(pair => pair.Value.Count);
        foreach ((string reference, List<(string TestName, string RunnerName)> issueTests) in sortedIssues)
        {
            (_, _, string displayIssue) = ParseIssueReference(reference);
            string titleSuffix = issueTitles.TryGetValue(reference, out string? title) ? $" - {title}" : "";
            Console.WriteLine($"  {issueTests.Count,5} | {displayIssue}{titleSuffix}");
        }

        Console.WriteLine();
        if (showDetails)
        {
            Console.WriteLine("ActiveIssue Details:");
            Console.WriteLine();
            foreach ((string reference, List<(string TestName, string RunnerName)> issueTests) in sortedIssues)
            {
                (_, _, string displayIssue) = ParseIssueReference(reference);
                string titleSuffix = issueTitles.TryGetValue(reference, out string? title) ? $" - {title}" : "";
                Console.WriteLine($"  {displayIssue}{titleSuffix} ({issueTests.Count} tests)");

                foreach (IGrouping<string, (string TestName, string RunnerName)> runner in
                    issueTests.GroupBy(test => test.RunnerName, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
                {
                    Console.WriteLine($"    [{runner.Key}]");
                    foreach ((string testName, _) in runner.OrderBy(test => test.TestName, StringComparer.Ordinal))
                    {
                        Console.WriteLine($"      {testName}");
                    }
                }
            }

            Console.WriteLine();
        }

        return issues;
    }

    private static Dictionary<string, string> FetchIssueTitles(
        Dictionary<string, List<(string TestName, string RunnerName)>> issues)
    {
        Dictionary<string, string> titles = new(StringComparer.Ordinal);
        if (FindExecutable("gh") is null)
        {
            return titles;
        }

        Console.WriteLine($"Fetching issue titles from GitHub ({issues.Count} issues)...");
        foreach (string reference in issues.Keys)
        {
            (string? repository, string? number, _) = ParseIssueReference(reference);
            if (repository is null || number is null)
            {
                continue;
            }

            try
            {
                ProcessResult result = RunProcessForOutput(
                    "gh",
                    ["issue", "view", number, "--repo", repository, "--json", "title", "--jq", ".title"],
                    timeoutMilliseconds: 10_000);
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput))
                {
                    titles[reference] = result.StandardOutput.Trim();
                }
            }
            catch
            {
            }
        }

        return titles;
    }

    private static (string? Repository, string? Number, string Display) ParseIssueReference(string issueReference)
    {
        Match match = s_issueUrlRegex.Match(issueReference);
        if (match.Success)
        {
            return (match.Groups[1].Value, match.Groups[2].Value, $"#{match.Groups[2].Value}");
        }

        return issueReference.Length > 0 && issueReference.All(char.IsDigit)
            ? ("dotnet/runtime", issueReference, $"#{issueReference}")
            : (null, null, issueReference);
    }

    private static int CreateRepro(
        RunnerArguments arguments,
        Dictionary<string, string> environment,
        List<TestResult> tests)
    {
        List<TestResult> failedTests = tests.Where(test => test.Result == "Fail").ToList();
        if (failedTests.Count == 0)
        {
            return 0;
        }

        if (Directory.Exists(arguments.ReproLocation))
        {
            Directory.Delete(arguments.ReproLocation, recursive: true);
        }

        Console.WriteLine();
        Console.WriteLine($"Creating repro files at: {arguments.ReproLocation}");
        Directory.CreateDirectory(arguments.ReproLocation);

        foreach (TestResult test in failedTests)
        {
            if (test.IsMerged)
            {
                Console.WriteLine($"Skipping repro for merged test: {test.Name} ({test.AssemblyDisplayName})");
            }
            else if (test.TestPath is null)
            {
                Console.WriteLine($"Failed to create repro for test: {test.Name} ({test.AssemblyDisplayName})");
            }
            else
            {
                new DebugEnvironment(arguments, environment, test).WriteRepro();
            }
        }

        Console.WriteLine("Repro files written.");
        return failedTests.Count;
    }

    private static int RunProcess(string fileName, IEnumerable<string> arguments)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            UseShellExecute = false,
        };

        if (OperatingSystem.IsWindows() && fileName.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add("call");
            startInfo.ArgumentList.Add(fileName);
        }

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        process.WaitForExit();
        return process.ExitCode;
    }

    private static ProcessResult RunProcessForOutput(string fileName, IEnumerable<string> arguments, int timeoutMilliseconds = Timeout.Infinite)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {fileName}.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new TimeoutException($"{fileName} timed out.");
        }

        return new ProcessResult(
            process.ExitCode,
            standardOutput.GetAwaiter().GetResult(),
            standardError.GetAwaiter().GetResult());
    }

    private static string? FindExecutable(string name)
    {
        string[] extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';')
            : [""];

        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            foreach (string extension in extensions)
            {
                string candidate = Path.Combine(directory, name + extension);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static Dictionary<string, string> CreateEnvironmentDictionary() =>
        new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

    private static string NormalizeBuildType(string? buildType)
    {
        buildType ??= "Debug";
        return buildType.Length == 0
            ? buildType
            : char.ToUpperInvariant(buildType[0]) + buildType[1..].ToLowerInvariant();
    }

    private static void ValidateValue(string value, string[] validValues, string message)
    {
        if (!validValues.Contains(value, StringComparer.Ordinal))
        {
            throw new RunnerExitException(string.Format(CultureInfo.InvariantCulture, message, value, string.Join(", ", validValues)));
        }
    }

    private static string GetDefaultHostOperatingSystem()
    {
        string description = RuntimeInformation.OSDescription.ToLowerInvariant();
        return true switch
        {
            _ when OperatingSystem.IsLinux() => "linux",
            _ when OperatingSystem.IsMacOS() => "osx",
            _ when OperatingSystem.IsWindows() => "windows",
            _ when OperatingSystem.IsFreeBSD() => "freebsd",
            _ when description.Contains("illumos", StringComparison.Ordinal) => "illumos",
            _ when description.Contains("sunos", StringComparison.Ordinal) ||
                   description.Contains("solaris", StringComparison.Ordinal) => "solaris",
            _ when description.Contains("haiku", StringComparison.Ordinal) => "haiku",
            _ => throw new RunnerExitException($"Unknown OS: {RuntimeInformation.OSDescription}"),
        };
    }

    private static string GetDefaultArchitecture() =>
        RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm => "arm",
            Architecture.Arm64 => "arm64",
            _ => throw new RunnerExitException($"Unknown architecture: {RuntimeInformation.ProcessArchitecture}"),
        };

    private static string GetSourceFilePath([CallerFilePath] string path = "") => path;

    [DllImport("libc", EntryPoint = "getrlimit", SetLastError = true)]
    private static extern int GetRLimit(int resource, out RLimit limit);

    [DllImport("libc", EntryPoint = "setrlimit", SetLastError = true)]
    private static extern int SetRLimit(int resource, in RLimit limit);

    [StructLayout(LayoutKind.Sequential)]
    private struct RLimit
    {
        public ulong Current;
        public ulong Maximum;
    }

    private sealed class DebugEnvironment
    {
        private readonly RunnerArguments _arguments;
        private readonly Dictionary<string, string> _environment;
        private readonly TestResult _test;
        private readonly string _path;
        private readonly string _wrapper;

        public DebugEnvironment(RunnerArguments arguments, Dictionary<string, string> environment, TestResult test)
        {
            _arguments = arguments;
            _environment = environment;
            _test = test;
            _wrapper = OperatingSystem.IsWindows() ? CreateBatchWrapper() : CreateBashWrapper();

            string testScriptDirectory = Path.GetDirectoryName(test.TestPath!)!;
            string relativePath = Path.GetRelativePath(arguments.TestLocation, testScriptDirectory);
            string reproDirectory = Path.Combine(arguments.ReproLocation, relativePath);
            Directory.CreateDirectory(reproDirectory);
            _path = Path.Combine(reproDirectory, $"repro_{Path.GetFileName(test.TestPath)}");

            string executableLocation = Path.ChangeExtension(test.TestPath!, ".exe")!;
            if (File.Exists(executableLocation))
            {
                AddConfigurationToLaunchJson(executableLocation);
            }
        }

        public void WriteRepro()
        {
            Console.WriteLine($"Writing repro: {_path}");
            File.WriteAllText(_path, _wrapper, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private string CreateBatchWrapper()
        {
            StringBuilder wrapper = new();
            wrapper.Append(
$"""
@echo off
setlocal
REM ============================================================================
REM Repro environment for {_test.TestPath}
REM
REM This wrapper is automatically generated by run.cs. It includes the necessary environment to
REM reproduce a failure that occurred during running the tests.
REM
REM In order to change how this wrapper is generated, see run.cs. Please note that it is possible
REM to recreate this file by running `dotnet src/tests/run.cs --analyze_results_only`
REM passing the correct arch and build_type.
REM ============================================================================

REM Set Core_Root if it has not been already set.
if "%CORE_ROOT%"=="" set CORE_ROOT={_arguments.CoreRoot}

echo Core_Root is set to: "%CORE_ROOT%"

""");
            foreach ((string key, string value) in _environment)
            {
                wrapper.AppendLine($"echo set {key}={value}");
                wrapper.AppendLine($"set {key}={value}");
            }

            wrapper.AppendLine();
            wrapper.AppendLine($"echo call {_test.TestPath}");
            wrapper.AppendLine($"call {_test.TestPath}");
            return wrapper.ToString().ReplaceLineEndings(Environment.NewLine);
        }

        private string CreateBashWrapper()
        {
            StringBuilder wrapper = new();
            wrapper.Append(
$$"""
#============================================================================
# Repro environment for {{_test.TestPath}}
#
# This wrapper is automatically generated by run.cs. It includes the necessary environment to
# reproduce a failure that occurred during running the tests.
#
# In order to change how this wrapper is generated, see run.cs. Please note that it is possible
# to recreate this file by running `dotnet src/tests/run.cs --analyze_results_only`
# passing the correct arch and build_type.
# ============================================================================

# Set Core_Root if it has not been already set.
if [ "${CORE_ROOT}" = "" ] || [ ! -z "${CORE_ROOT}" ]; then
    export CORE_ROOT={{_arguments.CoreRoot}}
else
    echo "CORE_ROOT set to ${CORE_ROOT}"
fi

""");
            foreach ((string key, string value) in _environment)
            {
                wrapper.AppendLine($"echo export {key}={value}");
                wrapper.AppendLine($"export {key}={value}");
            }

            wrapper.AppendLine();
            wrapper.AppendLine($"echo bash {_test.TestPath}");
            wrapper.AppendLine($"bash {_test.TestPath}");
            return wrapper.ToString().ReplaceLineEndings(Environment.NewLine);
        }

        private void AddConfigurationToLaunchJson(string executableLocation)
        {
            string vscodeDirectory = Path.Combine(_arguments.ReproLocation, ".vscode");
            Directory.CreateDirectory(vscodeDirectory);
            string launchJsonLocation = Path.Combine(vscodeDirectory, "launch.json");
            JsonObject launchJson;
            if (File.Exists(launchJsonLocation))
            {
                launchJson = JsonNode.Parse(File.ReadAllText(launchJsonLocation))!.AsObject();
            }
            else
            {
                launchJson = new JsonObject
                {
                    ["version"] = "0.2.0",
                    ["configurations"] = new JsonArray(),
                };
            }

            _environment["DOTNET_AssertOnNYI"] = "1";
            _environment["DOTNET_ContinueOnAssert"] = "0";
            JsonArray environment = [];
            foreach ((string key, string value) in _environment)
            {
                environment.Add(new JsonObject { ["name"] = key, ["value"] = value });
            }

            string uniqueName = $"{_test.TestPath}_{_arguments.HostOperatingSystem}_{_arguments.Architecture}_{_arguments.BuildType}";
            JsonObject configuration = new()
            {
                ["name"] = uniqueName,
                ["type"] = OperatingSystem.IsWindows() ? "cppvsdbg" : "",
                ["request"] = "launch",
                ["program"] = Path.Combine(_arguments.CoreRoot, OperatingSystem.IsWindows() ? "corerun.exe" : "corerun"),
                ["args"] = new JsonArray(executableLocation),
                ["stopAtEntry"] = false,
                ["cwd"] = Path.Combine("${workspaceFolder}", "..", ".."),
                ["environment"] = environment,
                ["externalConsole"] = true,
            };

            if (!string.Equals(_arguments.BuildType, "Release", StringComparison.OrdinalIgnoreCase))
            {
                configuration["symbolSearchPath"] = Path.Combine(_arguments.CoreRoot, "PDB");
            }

            JsonArray configurations = launchJson["configurations"]!.AsArray();
            int existingIndex = -1;
            for (int i = 0; i < configurations.Count; i++)
            {
                if (configurations[i]?["name"]?.GetValue<string>() == uniqueName)
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                configurations[existingIndex] = configuration;
            }
            else
            {
                configurations.Add(configuration);
            }

            File.WriteAllText(
                launchJsonLocation,
                launchJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    private sealed class Options
    {
        public string? HostOperatingSystem { get; private set; }
        public string? Architecture { get; private set; } = "x64";
        public string? BuildType { get; private set; } = "Debug";
        public string? TestLocation { get; private set; }
        public string? CoreRoot { get; private set; }
        public string? TestEnvironment { get; private set; }
        public string? Parallel { get; private set; }
        public string? LogsDirectory { get; private set; }
        public bool IlLink { get; private set; }
        public bool LongGc { get; private set; }
        public bool GcSimulator { get; private set; }
        public bool IlasmRoundTrip { get; private set; }
        public bool UseManagedIlasm { get; private set; }
        public bool RunCrossgen2Tests { get; private set; }
        public bool LargeVersionBubble { get; private set; }
        public bool SynthesizePgo { get; private set; }
        public bool Sequential { get; private set; }
        public bool Interpreter { get; private set; }
        public bool Node { get; private set; }
        public string? RunnerFilter { get; private set; }
        public bool ActiveIssueDetails { get; private set; }
        public bool AnalyzeResultsOnly { get; private set; }
        public bool Verbose { get; private set; }
        public bool LimitedCoreDumps { get; private set; }
        public bool RunInContext { get; private set; }
        public bool TieringTest { get; private set; }
        public bool RunNativeAotTests { get; private set; }
        public string? Tree { get; private set; }

        public static int Parse(string[] arguments, out Options? options)
        {
            options = new Options();
            for (int i = 0; i < arguments.Length; i++)
            {
                string argument = arguments[i];
                string name = argument;
                string? inlineValue = null;
                int equalsIndex = argument.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    name = argument[..equalsIndex];
                    inlineValue = argument[(equalsIndex + 1)..];
                }

                switch (name)
                {
                    case "-h":
                    case "--help":
                        PrintHelp();
                        options = null;
                        return 0;
                    case "-os":
                        options.HostOperatingSystem = GetOptionalValue(arguments, ref i, inlineValue);
                        break;
                    case "-arch":
                        options.Architecture = GetOptionalValue(arguments, ref i, inlineValue);
                        break;
                    case "-build_type":
                        options.BuildType = GetOptionalValue(arguments, ref i, inlineValue);
                        break;
                    case "-test_location":
                        options.TestLocation = GetOptionalValue(arguments, ref i, inlineValue);
                        break;
                    case "-core_root":
                        options.CoreRoot = GetOptionalValue(arguments, ref i, inlineValue);
                        break;
                    case "-runtime_repo_location":
                        _ = GetRequiredValue(arguments, ref i, inlineValue, name);
                        break;
                    case "-test_env":
                        options.TestEnvironment = GetRequiredValue(arguments, ref i, inlineValue, name);
                        break;
                    case "-parallel":
                        options.Parallel = GetRequiredValue(arguments, ref i, inlineValue, name);
                        break;
                    case "-logs_dir":
                        options.LogsDirectory = GetRequiredValue(arguments, ref i, inlineValue, name);
                        break;
                    case "--il_link":
                        options.IlLink = true;
                        break;
                    case "--long_gc":
                        options.LongGc = true;
                        break;
                    case "--gcsimulator":
                        options.GcSimulator = true;
                        break;
                    case "--ilasmroundtrip":
                        options.IlasmRoundTrip = true;
                        break;
                    case "--use_managed_ilasm":
                        options.UseManagedIlasm = true;
                        break;
                    case "--run_crossgen2_tests":
                        options.RunCrossgen2Tests = true;
                        break;
                    case "--large_version_bubble":
                        options.LargeVersionBubble = true;
                        break;
                    case "--synthesize_pgo":
                        options.SynthesizePgo = true;
                        break;
                    case "--sequential":
                        options.Sequential = true;
                        break;
                    case "--interpreter":
                        options.Interpreter = true;
                        break;
                    case "--node":
                        options.Node = true;
                        break;
                    case "--runner_filter":
                        options.RunnerFilter = GetRequiredValue(arguments, ref i, inlineValue, name);
                        break;
                    case "--active_issue_details":
                        options.ActiveIssueDetails = true;
                        break;
                    case "--analyze_results_only":
                        options.AnalyzeResultsOnly = true;
                        break;
                    case "--verbose":
                        options.Verbose = true;
                        break;
                    case "--limited_core_dumps":
                        options.LimitedCoreDumps = true;
                        break;
                    case "--run_in_context":
                        options.RunInContext = true;
                        break;
                    case "--tiering_test":
                    case "--tieringtest":
                        options.TieringTest = true;
                        break;
                    case "--run_nativeaot_tests":
                        options.RunNativeAotTests = true;
                        break;
                    case "--tree":
                        options.Tree = GetRequiredValue(arguments, ref i, inlineValue, name);
                        break;
                    default:
                        Console.Error.WriteLine($"run.cs: error: unrecognized arguments: {argument}");
                        options = null;
                        return 2;
                }
            }

            return 0;
        }

        private static string? GetOptionalValue(string[] arguments, ref int index, string? inlineValue)
        {
            if (inlineValue is not null)
            {
                return inlineValue;
            }

            if (index + 1 < arguments.Length && !arguments[index + 1].StartsWith('-', StringComparison.Ordinal))
            {
                return arguments[++index];
            }

            return null;
        }

        private static string GetRequiredValue(string[] arguments, ref int index, string? inlineValue, string option)
        {
            return GetOptionalValue(arguments, ref index, inlineValue)
                ?? throw new RunnerExitException($"run.cs: error: argument {option}: expected one argument", exitCode: 2);
        }

        private static void PrintHelp()
        {
            Console.WriteLine(
"""
usage: run.cs [-h] [-os [HOST_OS]] [-arch [ARCH]] [-build_type [BUILD_TYPE]]
              [-test_location [TEST_LOCATION]] [-core_root [CORE_ROOT]]
              [-runtime_repo_location RUNTIME_REPO_LOCATION] [-test_env TEST_ENV]
              [-parallel PARALLEL] [-logs_dir LOGS_DIR] [--il_link] [--long_gc]
              [--gcsimulator] [--ilasmroundtrip] [--use_managed_ilasm]
              [--run_crossgen2_tests] [--large_version_bubble] [--synthesize_pgo]
              [--sequential] [--interpreter] [--node]
              [--runner_filter RUNNER_FILTER] [--active_issue_details]
              [--analyze_results_only] [--verbose] [--limited_core_dumps]
              [--run_in_context] [--tiering_test] [--run_nativeaot_tests]
              [--tree TREE]

Script to run the xunit console runner. The script relies on build.proj and the
bash and batch wrappers. All test excludes will also come from issues.targets.

options:
  -h, --help            show this help message and exit
  -os [HOST_OS]
  -arch [ARCH]
  -build_type [BUILD_TYPE]
  -test_location [TEST_LOCATION]
  -core_root [CORE_ROOT]
  -runtime_repo_location RUNTIME_REPO_LOCATION
  -test_env TEST_ENV
  -parallel PARALLEL    Specify the level of parallelism: none, collections,
                        assemblies, all. Default: collections.
  -logs_dir LOGS_DIR    Specify the directory where log files are written.
                        Default: artifacts/log.
  --il_link
  --long_gc
  --gcsimulator
  --ilasmroundtrip
  --use_managed_ilasm
  --run_crossgen2_tests
  --large_version_bubble
  --synthesize_pgo
  --sequential
  --interpreter
  --node
  --runner_filter RUNNER_FILTER
  --active_issue_details
  --analyze_results_only
  --verbose
  --limited_core_dumps
  --run_in_context
  --tiering_test
  --run_nativeaot_tests
  --tree TREE           Only run tests under the specified subtree (e.g.
                        JIT/Regression).
""");
        }
    }

    private sealed class RunnerArguments
    {
        public RunnerArguments(Options options)
        {
            TestEnvironment = options.TestEnvironment;
            Parallel = options.Parallel;
            IlLink = options.IlLink;
            LongGc = options.LongGc;
            GcSimulator = options.GcSimulator;
            IlasmRoundTrip = options.IlasmRoundTrip;
            UseManagedIlasm = options.UseManagedIlasm;
            RunCrossgen2Tests = options.RunCrossgen2Tests;
            LargeVersionBubble = options.LargeVersionBubble;
            SynthesizePgo = options.SynthesizePgo;
            Sequential = options.Sequential;
            Interpreter = options.Interpreter;
            Node = options.Node;
            RunnerFilter = options.RunnerFilter;
            ActiveIssueDetails = options.ActiveIssueDetails;
            AnalyzeResultsOnly = options.AnalyzeResultsOnly;
            Verbose = options.Verbose;
            LimitedCoreDumps = options.LimitedCoreDumps;
            RunInContext = options.RunInContext;
            TieringTest = options.TieringTest;
            RunNativeAotTests = options.RunNativeAotTests;
            Tree = options.Tree;
        }

        public required string HostOperatingSystem { get; init; }
        public required string Architecture { get; init; }
        public required string BuildType { get; init; }
        public required string RuntimeRepositoryLocation { get; init; }
        public required string ArtifactsLocation { get; init; }
        public required string CoreRoot { get; init; }
        public required string TestLocation { get; init; }
        public required string LogsDirectory { get; init; }
        public required string ReproLocation { get; init; }
        public required string DotNetCliScriptPath { get; init; }
        public required string TestsSourceDirectory { get; init; }
        public required string RunInContextScriptPath { get; init; }
        public required string TieringTestScriptPath { get; init; }
        public required string NativeAotTestScriptPath { get; init; }
        public string? TestEnvironment { get; }
        public string? Parallel { get; }
        public bool IlLink { get; }
        public bool LongGc { get; }
        public bool GcSimulator { get; }
        public bool IlasmRoundTrip { get; }
        public bool UseManagedIlasm { get; }
        public bool RunCrossgen2Tests { get; }
        public bool LargeVersionBubble { get; }
        public bool SynthesizePgo { get; }
        public bool Sequential { get; }
        public bool Interpreter { get; }
        public bool Node { get; }
        public string? RunnerFilter { get; }
        public bool ActiveIssueDetails { get; }
        public bool AnalyzeResultsOnly { get; }
        public bool Verbose { get; }
        public bool LimitedCoreDumps { get; }
        public bool RunInContext { get; }
        public bool TieringTest { get; }
        public bool RunNativeAotTests { get; }
        public string? Tree { get; }
    }

    private sealed class TestResult
    {
        public required string Name { get; init; }
        public string? TestPath { get; init; }
        public required string Result { get; init; }
        public double Time { get; init; }
        public string? TestOutput { get; init; }
        public string? SkipReason { get; init; }
        public string? AssemblyDisplayName { get; init; }
        public bool IsMerged { get; init; }
    }

    private sealed class AssemblyInfo(string name, string displayName, bool isMergedTestsRun)
    {
        public string Name { get; } = name;
        public string DisplayName { get; } = displayName;
        public bool IsMergedTestsRun { get; } = isMergedTestsRun;
        public double Time { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int ActiveIssue { get; set; }
    }

    private sealed record CrashedRunner(string Name, string? ExitCode, string? Script);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    private sealed class RunnerExitException(string message, int exitCode = 1) : Exception(message)
    {
        public int ExitCode { get; } = exitCode;
    }
}
