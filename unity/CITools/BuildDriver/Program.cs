using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;

public class Program
{
    public static bool RunningOnYamato() => Environment.GetEnvironmentVariable("YAMATO_PROJECT_ID") != null;
    public static int Main(string[] args)
    {
        var buildOption = new Option<BuildTargets>("--build", "--b")
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore,
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                    return BuildTargets.All; //For when --build is provided without any arguments.
                BuildTargets retVal = BuildTargets.None;
                foreach (Token token in result.Tokens)
                {
                    if (Enum.TryParse(token.Value, ignoreCase: true, out BuildTargets val))
                        retVal |= val;
                    else
                        result.AddError($"'{token.Value}' is not a valid build target");
                }
                return retVal;
            },
            Description = "Select how much of CoreCLR you wish to build."
        };
        var testOption = new Option<TestTargets>("--test")
        {
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore,
            CustomParser = result =>
            {
                if (result.Tokens.Count == 0)
                    return TestTargets.All; // For when --test is provided without any arguments
                TestTargets retVal = TestTargets.None;
                foreach (Token token in result.Tokens)
                {
                    if (Enum.TryParse(token.Value, ignoreCase: true, out TestTargets val))
                        retVal |= val;
                    else
                        result.AddError($"'{token.Value}' is not a valid test target");
                }
                return retVal;
            },
            Description = "Select how much of CoreCLR you wish to test."
        };
        var architectureOption = new Option<string>("--architecture", "--a", "--arch")
        {
            Description = "Set the target architecture",
            DefaultValueFactory = (_) => GetArchitectures()[0]
        };
        architectureOption.Validators.Add(result =>
        {
            string? val = result.GetValue(architectureOption);
            string[] validArgs = GetArchitectures(true);
            if (val != null && !validArgs.Contains(val))
                result.AddError($"The value of {architectureOption.Name} must be one of the following: {validArgs.AggregateWithSpace()}");
        });
        var configurationOption = new Option<string>("--configuration", "--c", "--config")
        {
            Description = "Set the build type",
            DefaultValueFactory = (_) => "Release"
        };
        configurationOption.Validators.Add(result =>
        {
            string? val = result.GetValue(configurationOption);
            if (val == null || !(val.Equals("Release") || val.Equals("Debug")))
                result.AddError($"The value of {configurationOption.Name} must be either Release or Debug");
        });

        var verbosityOption = new Option<Verbosity>("--verbosity", "--v")
        {
            Description = "Set the verbosity level for the build.",
            DefaultValueFactory = (_) => Verbosity.Normal
        };
        var zipOption = new Option<bool>("--zip", "--z")
        {
            DefaultValueFactory = (_) => RunningOnYamato(),
            Description = "Produce zip artifacts. This is only used when building."
        };

        var deployToPlayer= new Option<string>("--deploy-to-player")
        {
            DefaultValueFactory = (_) => string.Empty,
            Description = "Copies the artifacts into a player build",
        };
        deployToPlayer.Validators.Add(result =>
        {
            string? val = result.GetValue(deployToPlayer);

            // Default value, nothing to validate
            if (val == string.Empty)
                return;

            if (val == null)
                result.AddError($"Must specify a value for {deployToPlayer.Name}");
            else
            {
                var valAsPath = val.ToNPath();
                if (!valAsPath.DirectoryExists())
                    result.AddError($"Directory does not exist {val}");
                else if (!valAsPath.Combine("CoreCLR").DirectoryExists())
                    result.AddError($"The directory does not appear to be a built player directory because {valAsPath.Combine("CoreCLR")} does not exist");
            }
        });

        RootCommand rootCommand = new RootCommand("Unity CoreCLR Builder")
        {
            buildOption,
            testOption,
            architectureOption,
            configurationOption,
            verbosityOption,
            zipOption,
            deployToPlayer
        };
        rootCommand.SetAction(Run);

        return new CommandLineBuilder(rootCommand).UseParseErrorReporting().UseHelp().Build().Invoke(args);

        void Run(InvocationContext context)
        {
            Task<NPath>? zipTask = null;
            BuildTargets bTargets = context.ParseResult.GetValue(buildOption);
            TestTargets tTargets = context.ParseResult.GetValue(testOption);
            string? architecture = context.ParseResult.GetValue(architectureOption);
            string? configuration = context.ParseResult.GetValue(configurationOption);
            string? deployToProjectPath = context.ParseResult.GetValue(deployToPlayer);

            if (bTargets == BuildTargets.None && tTargets == TestTargets.None && string.IsNullOrEmpty(deployToProjectPath))
                bTargets = BuildTargets.All;

            if (RunningOnYamato() && bTargets != BuildTargets.None)
            {
                zipTask = SevenZip.DownloadAndUnzip7Zip();
            }

            Console.WriteLine("*****************************");
            Console.WriteLine("Unity: Starting CoreCLR build");
            Console.WriteLine($"\tPlatform:\t\t{RuntimeInformation.OSDescription}");
            Console.WriteLine($"\tArchitecture:\t\t{architecture}");
            Console.WriteLine($"\tConfiguration:\t\t{configuration}");
            Console.WriteLine("*****************************");

            GlobalConfig gConfig = new()
            {
                Architecture = architecture ?? GetArchitectures()[0],
                Configuration = configuration ?? "Release",
                VerbosityLevel = context.ParseResult.GetValue(verbosityOption)
            };

            if (bTargets != BuildTargets.None)
            {
                if (bTargets.HasFlag(BuildTargets.Runtime) || bTargets.HasFlag(BuildTargets.ClassLibs))
                    CoreCLR.Build(gConfig, bTargets);

                // TODO: Switch to using Embedding Host build to perform the copy instead of this once that lands.
                NPath artifacts = Artifacts.ConsolidateArtifacts(gConfig);

                NPath zipExe = new("7z");
                if (zipTask != null)
                {
                    zipTask.Wait();
                    zipExe = zipTask.Result;
                    if (zipTask.Exception != null)
                        throw zipTask.Exception;
                }

                if (context.ParseResult.GetValue(zipOption))
                    SevenZip.Zip(zipExe, artifacts, gConfig);
            }

            if (tTargets != TestTargets.None)
            {
                if (tTargets.HasFlag(TestTargets.Classlibs))
                    CoreCLR.TestClassLibraries(gConfig);

                if (tTargets.HasFlag(TestTargets.Runtime))
                    CoreCLR.TestUnityRuntime(gConfig);

                if (tTargets.HasFlag(TestTargets.Pal))
                    CoreCLR.TestUnityPal(gConfig);

                Console.WriteLine("******************************");
                Console.WriteLine("Unity: Tested CoreCLR successfully");
                Console.WriteLine("******************************");
            }

            if (!string.IsNullOrEmpty(deployToProjectPath))
                Deploy.ToPlayer(gConfig, deployToProjectPath.ToNPath());
        }
    }

    private static string[] GetArchitectures(bool allArchitectures = false)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (allArchitectures)
            {
                return new[] { "x64", "x86", "arm64" };
            }

            if (RuntimeInformation.OSArchitecture == Architecture.X86)
                return new[] { "x86" };
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                return new[] { "arm64" };
            return new[] { "x64" };
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new[] { "x64" };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (allArchitectures)
                return new[] { "x64", "arm64" };

            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                return new[] { "arm64" };
            return new[] { "x64" };
        }

        throw new ArgumentException($"Unsupported build platform {RuntimeInformation.OSDescription}");
    }
}
