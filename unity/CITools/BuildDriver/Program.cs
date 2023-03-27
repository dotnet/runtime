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
                    if (Enum.TryParse(token.Value, out BuildTargets val))
                        retVal |= val;
                    else
                        result.AddError($"'{token.Value}' is not a valid build target");
                }
                return retVal;
            }
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
                    if (Enum.TryParse(token.Value, out TestTargets val))
                        retVal |= val;
                    else
                        result.AddError($"'{token.Value}' is not a valid test target");
                }
                return retVal;
            }
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

        var silentOption = new Option<bool>("--silent", "--s");
        var zipOption = new Option<bool>("--zip", "--z") { DefaultValueFactory = (_) => RunningOnYamato()};

        RootCommand rootCommand = new RootCommand("Unity CoreCLR Builder")
        {
            buildOption,
            testOption,
            architectureOption,
            configurationOption,
            silentOption,
            zipOption
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
            if (bTargets == BuildTargets.None && tTargets == TestTargets.None)
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
                Silent = context.ParseResult.GetValue(silentOption),
                DotNetVerbosity = "quiet"
            };

            // We always need to build the embedding host because on CI we have the build and tests split into separate jobs.  And the way we have artifacts setup,
            // we don't retain anything built under `unity`.  And therefore we need to rebuild it so that tests that depend on something in managed.sln can find what they need
            EmbeddingHost.Build(gConfig);

            if (bTargets != BuildTargets.None)
            {
                if (bTargets.HasFlag(BuildTargets.NullGC))
                    NullGC.Build(gConfig);
                if (bTargets.HasFlag(BuildTargets.CoreCLR))
                    CoreCLR.Build(gConfig);

                // TODO: Switch to using Embedding Host build to perform the copy instead of this once that lands.
                NPath artifacts = ConsolidateArtifacts(gConfig);

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
                if (tTargets.HasFlag(TestTargets.Embedding))
                    EmbeddingHost.Test(gConfig);

                if (tTargets.HasFlag(TestTargets.CoreClr))
                    CoreCLR.Test(gConfig);

                Console.WriteLine("******************************");
                Console.WriteLine("Unity: Tested CoreCLR successfully");
                Console.WriteLine("******************************");
            }
        }
    }

    static NPath ConsolidateArtifacts(GlobalConfig gConfig)
    {
        string osAbbrev = "win";
        string unityGCLib = "unitygc.dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            osAbbrev = "osx";
            unityGCLib = "libunitygc.dylib";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            osAbbrev = "linux";
            unityGCLib = "libunitygc.so";
        }

        NPath destDir = Paths.RepoRoot.Combine("artifacts", "bin",
            $"microsoft.netcore.app.runtime.{osAbbrev}-{gConfig.Architecture}", gConfig.Configuration, "runtimes",
            $"{osAbbrev}-{gConfig.Architecture}");

        Paths.UnityGC.Combine(gConfig.Configuration, unityGCLib).Copy(destDir.Combine("native"));

        NPath tfmDir = Paths.UnityEmbedHost.Combine("bin", gConfig.Configuration).Directories().Single();
        tfmDir.Files("unity-embed-host.*").Copy(destDir.Combine("lib", tfmDir.FileName));
        Paths.RepoRoot.Combine("LICENSE.md").Copy(destDir);

        return destDir;
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
